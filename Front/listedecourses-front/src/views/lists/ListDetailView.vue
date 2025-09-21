<script setup lang="ts">
import { onMounted, ref, computed, onBeforeUnmount, watch } from 'vue'
import { useRoute, useRouter, onBeforeRouteLeave } from 'vue-router'
import { useListsStore } from '@/stores/lists'
import { useDishesStore } from '@/stores/dishes'
import { useIngredientsStore } from '@/stores/ingredients'
import BaseInput from '@/components/ui/BaseInput.vue'
import BaseButton from '@/components/ui/BaseButton.vue'
import QtyUnitInput from '@/components/ui/QtyUnitInput.vue'
import type { ShoppingList, ShoppingListItem, Ingredient, Dish } from '@/api/types'
import { useMapById } from '@/composables/useMapById'

const route = useRoute()
const router = useRouter()
const lists = useListsStore()
const dishes = useDishesStore()
const ingredients = useIngredientsStore()
type QtyUnit = { quantity: number | null; unit: string | null }

const id = String(route.params.id)
const current = computed<ShoppingList | null>(
  () => lists.items.find((l: { id: string }) => l.id === id) ?? lists.current ?? null
)

const itemsView = ref<ShoppingListItem[]>([])
function syncFromCurrent() {
  const l = current.value
  itemsView.value = l ? l.items.map((it) => ({ ...it, checked: !!it.checked })) : []
}

const locked = ref(false)
function toggleLock() {
  locked.value = !locked.value
  if (locked.value) {
    showPicker.value = false
  }
}

onBeforeRouteLeave(() => {
  if (locked.value) return false
})
function handleBeforeUnload(e: BeforeUnloadEvent) {
  if (!locked.value) return
  e.preventDefault()
  e.returnValue = ''
}
onBeforeUnmount(() => window.removeEventListener('beforeunload', handleBeforeUnload))

function onGlobalClickCapture(ev: Event) {
  if (!locked.value) return
  const target = ev.target as HTMLElement | null
  if (!target) return
  const logoutBtn =
    target.closest('[aria-label="Se déconnecter"]') ||
    [...document.querySelectorAll('button, a')].find((el) => {
      const t = (el.textContent || '').trim().toLowerCase()
      return t === 'déconnexion' || t.includes('se déconnecter')
    })
  if (logoutBtn && (target === logoutBtn || logoutBtn.contains(target))) {
    ev.preventDefault()
    ev.stopPropagation()
  }
}

onMounted(async () => {
  if (!current.value) await lists.fetchById(id)
  if (!dishes.items.length) await dishes.fetchAll()
  if (!ingredients.items.length) await ingredients.fetchAll()
  window.addEventListener('beforeunload', handleBeforeUnload)
  document.addEventListener('click', onGlobalClickCapture, true)
  syncFromCurrent()
})
onBeforeUnmount(() => {
  document.removeEventListener('click', onGlobalClickCapture, true)
})

const groups = computed<[string, ShoppingListItem[]][]>(() => {
  const map = new Map<string, ShoppingListItem[]>()
  for (const it of itemsView.value) {
    const k = it.aisle ?? 'Sans rayon'
    const arr = map.get(k) ?? []
    arr.push(it)
    map.set(k, arr)
  }
  for (const [, arr] of map) {
    arr.sort((a, b) => a.ingredientName.localeCompare(b.ingredientName, 'fr'))
  }
  return Array.from(map.entries()).sort((a, b) => a[0].localeCompare(b[0], 'fr'))
})

const dishOptions = computed<Dish[]>(() => dishes.items)
const ingredientOptions = computed<Ingredient[]>(() => ingredients.items)
const { byId: ingById } = useMapById(() => ingredientOptions.value)

type Agg = Record<string, { qty: number | null; unit: string | null }>
function aggregateFromDishIds(dishIds: string[]): Agg {
  const acc: Agg = {}
  const ids = Array.from(new Set(dishIds ?? []))
  for (const dId of ids) {
    const d = dishes.items.find((x: { id: string }) => x.id === dId)
    if (!d) continue
    for (const di of d.ingredients) {
      const key = di.ingredientId
      const u = di.unit?.trim() || null
      const q = di.quantity ?? null
      const cur = acc[key]
      if (!cur) {
        acc[key] = { qty: q, unit: u }
      } else {
        const curUnit = cur.unit?.toLowerCase() ?? null
        const newUnit = u?.toLowerCase() ?? null
        if (curUnit && newUnit && curUnit === newUnit) {
          acc[key] = { qty: (cur.qty ?? 0) + (q ?? 0), unit: cur.unit }
        } else {
          acc[key] = { qty: null, unit: null }
        }
      }
    }
  }
  return acc
}

const showPicker = ref(false)
const pickerTab = ref<'dishes' | 'ingredients'>('dishes')
const dishSearch = ref('')
const ingSearch = ref('')

const selectedDishIds = ref<string[]>([])
const selectedIngredientIds = ref<string[]>([])
type IngMeta = { quantity: number | null; unit: string | null }
const selectedIngredientMetaById = ref<Record<string, IngMeta>>({})
const UNIT_OPTIONS = ['kg', 'g', 'paquet']

const pickerErrors = ref<Record<string, string>>({})
const globalPickerError = ref<string | null>(null)

function isMetaValid(m?: IngMeta | null) {
  if (!m) return false
  const qOk = typeof m.quantity === 'number' && isFinite(m.quantity) && m.quantity > 0
  const uOk = !!m.unit && m.unit.trim().length > 0
  return qOk && uOk
}

function validateManualIngredients(): boolean {
  pickerErrors.value = {}
  globalPickerError.value = null
  for (const iid of selectedIngredientIds.value) {
    const meta = selectedIngredientMetaById.value[iid]
    if (!isMetaValid(meta)) {
      pickerErrors.value[iid] = 'Renseigne une quantité (> 0) et une unité.'
    }
  }
  const hasErrors = Object.keys(pickerErrors.value).length > 0
  if (hasErrors) {
    globalPickerError.value =
      'Complète la quantité et l’unité pour les ingrédients hors plats sélectionnés.'
  }
  return !hasErrors
}

watch(selectedIngredientIds, (ids) => {
  const set = new Set(ids)
  for (const k of Object.keys(pickerErrors.value)) {
    if (!set.has(k)) delete pickerErrors.value[k]
  }
})

function dishIngredientIdSet(dishIds: string[]): Set<string> {
  const set = new Set<string>()
  for (const dId of dishIds) {
    const d = dishes.items.find((x: { id: string }) => x.id === dId)
    if (!d) continue
    for (const di of d.ingredients) set.add(di.ingredientId)
  }
  return set
}

function openPicker() {
  if (locked.value) return
  const l = current.value
  if (!l) return
  pickerTab.value = 'dishes'
  dishSearch.value = ''
  ingSearch.value = ''

  selectedDishIds.value = [...l.dishIds]

  const agg = aggregateFromDishIds(l.dishIds)
  const manualNow = l.items.filter((it) => {
    const a = agg[it.ingredientId]
    if (!a) return true
    const unitEqual = (a.unit?.toLowerCase() ?? null) === (it.unit?.toLowerCase() ?? null)
    const qtyEqual = (a.qty ?? null) === (it.quantity ?? null)
    return !(unitEqual && qtyEqual)
  })

  selectedIngredientIds.value = manualNow.map((it) => it.ingredientId)
  selectedIngredientMetaById.value = {}
  for (const it of manualNow) {
    selectedIngredientMetaById.value[it.ingredientId] = {
      quantity: it.quantity ?? null,
      unit: it.unit ?? null,
    }
  }

  pickerErrors.value = {}
  globalPickerError.value = null
  showPicker.value = true
}
function cancelPicker() {
  showPicker.value = false
}
async function confirmPicker() {
  if (!current.value || locked.value) return
  if (!validateManualIngredients()) return

  await lists.updateOne(current.value.id, {
    dishIds: [...selectedDishIds.value],
    items: selectedIngredientIds.value.map((iid: string) => {
      const meta = selectedIngredientMetaById.value[iid] ?? { quantity: null, unit: null }
      const ing = ingById.value[iid]
      return {
        ingredientId: iid,
        ingredientName: ing?.name ?? '',
        aisle: ing?.aisle ?? null,
        quantity: meta.quantity,
        unit: meta.unit,
        checked: false as boolean,
      } satisfies ShoppingListItem
    }),
  })
  await lists.fetchById(id)
  syncFromCurrent()
  pickerErrors.value = {}
  globalPickerError.value = null
  showPicker.value = false
}

async function onCheckChange(r: ShoppingListItem, e: Event) {
  const isChecked = (e.target as HTMLInputElement).checked
  r.checked = !!isChecked
  const l = current.value
  if (!l) return
  const payloadItems: ShoppingListItem[] = itemsView.value.map((it) => ({
    ingredientId: it.ingredientId,
    ingredientName: it.ingredientName,
    quantity: it.quantity ?? null,
    unit: it.unit ?? null,
    aisle: it.aisle ?? null,
    checked: !!it.checked,
  }))
  await lists.updateOne(l.id, { items: payloadItems, dishIds: l.dishIds })
}

const showConfirmDelete = ref(false)

const filteredDishes = computed(() => {
  const q = dishSearch.value.trim().toLowerCase()
  if (!q) return dishOptions.value
  return dishOptions.value.filter((d) => d.name.toLowerCase().includes(q))
})
const filteredIngredients = computed(() => {
  const q = ingSearch.value.trim().toLowerCase()
  if (!q) return ingredientOptions.value
  return ingredientOptions.value.filter(
    (i) => i.name.toLowerCase().includes(q) || i.aisle?.toLowerCase().includes(q)
  )
})

async function removeList() {
  if (!current.value || locked.value) return
  await lists.deleteOne(current.value.id)
  router.push('/lists')
}
</script>

<template>
  <section v-if="current" class="container mx-auto px-4 safe-pt safe-pb py-6 space-y-6">
    <div class="flex flex-col md:flex-row md:items-center md:justify-between gap-3">
      <div>
        <h1 class="text-3xl md:text-2xl font-semibold tracking-tight">{{ current.name }}</h1>
        <p class="text-sm text-gray-500">{{ new Date(current.date).toLocaleDateString() }}</p>
      </div>
      <div class="flex flex-wrap gap-2">
        <BaseButton
          class="!bg-amber-500 hover:!bg-amber-600 !h-10 !px-4 !rounded-lg"
          :class="locked ? '' : '!bg-amber-400 hover:!bg-amber-500'"
          @click="toggleLock"
        >
          {{ locked ? 'Déverrouiller' : 'Verrouiller' }}
        </BaseButton>
        <BaseButton :disabled="locked" class="!h-10 !px-4 !rounded-lg" @click="openPicker"
          >Modifier</BaseButton
        >
        <BaseButton
          :disabled="locked"
          class="!bg-red-600 disabled:opacity-50 !h-10 !px-4 !rounded-lg"
          @click="showConfirmDelete = true"
        >
          Supprimer
        </BaseButton>
      </div>
    </div>

    <div class="space-y-4">
      <div
        v-for="[aisle, rows] in groups"
        :key="aisle"
        class="bg-white border border-gray-200 rounded-xl overflow-hidden shadow-sm"
      >
        <div
          class="border-b px-4 py-2 font-medium flex items-center justify-between sticky top-0 bg-white z-10"
        >
          <span>{{ aisle }}</span>
          <span class="text-xs text-gray-500" v-if="locked">Mode verrouillé</span>
        </div>
        <ul class="divide-y">
          <li
            v-for="r in rows"
            :key="r.ingredientId"
            class="flex items-center justify-between px-4 py-3"
          >
            <label class="flex items-center gap-3">
              <input
                type="checkbox"
                class="h-5 w-5 rounded border-gray-300 text-emerald-600 focus:ring-emerald-500"
                :checked="!!r.checked"
                @change="onCheckChange(r, $event)"
              />
              <span class="font-medium">{{ r.ingredientName }}</span>
            </label>
            <div class="text-sm text-gray-600">
              <template v-if="r.quantity !== null">
                {{ r.quantity }} <span v-if="r.unit">&nbsp;{{ r.unit }}</span>
              </template>
              <template v-else>—</template>
            </div>
          </li>
        </ul>
      </div>
    </div>

    <Transition name="modal" appear>
      <div
        v-if="showConfirmDelete"
        class="fixed inset-0 z-50 flex items-end sm:items-center justify-center"
        role="dialog"
        aria-modal="true"
      >
        <div class="absolute inset-0 bg-black/50" @click="showConfirmDelete = false"></div>
        <div
          class="relative w-full sm:w-[420px] bg-white rounded-t-2xl sm:rounded-2xl shadow-2xl p-5 space-y-4"
        >
          <h3 class="text-lg font-semibold">Supprimer la liste ?</h3>
          <p class="text-sm text-gray-600">
            Cette action est définitive. Êtes-vous sûr de vouloir supprimer « {{ current.name }} » ?
          </p>
          <div class="flex justify-end gap-2">
            <BaseButton class="!bg-gray-200 !text-gray-800" @click="showConfirmDelete = false">
              Annuler
            </BaseButton>
            <BaseButton class="!bg-red-600" @click=";(showConfirmDelete = false), removeList()">
              Confirmer
            </BaseButton>
          </div>
        </div>
      </div>
    </Transition>

    <div
      v-if="showPicker"
      class="fixed inset-0 z-50 flex flex-col"
      aria-modal="true"
      role="dialog"
      tabindex="-1"
      @keydown.esc="cancelPicker"
    >
      <div class="flex-1 bg-black/50 backdrop-blur-sm" @click="cancelPicker"></div>
      <div
        class="mt-auto w-full bg-white shadow-2xl flex flex-col rounded-t-2xl max-h-[85vh] safe-pb md:max-h-none md:h-full md:rounded-none md:w-[560px] md:ml-auto"
      >
        <div class="mx-auto mt-2 h-1.5 w-10 rounded-full bg-gray-300 md:hidden"></div>

        <div class="border-b px-4 py-3 flex items-center justify-between sticky top-0 bg-white">
          <div class="flex gap-1">
            <button
              type="button"
              class="px-3 py-1 rounded-md border"
              :class="
                pickerTab === 'dishes'
                  ? 'bg-gray-900 text-white border-gray-900'
                  : 'bg-white text-gray-700'
              "
              @click="pickerTab = 'dishes'"
              :disabled="locked"
            >
              Plats
            </button>
            <button
              type="button"
              class="px-3 py-1 rounded-md border"
              :class="
                pickerTab === 'ingredients'
                  ? 'bg-gray-900 text-white border-gray-900'
                  : 'bg-white text-gray-700'
              "
              @click="pickerTab = 'ingredients'"
              :disabled="locked"
            >
              Ingrédients (hors plats)
            </button>
          </div>
          <div class="flex gap-2">
            <BaseButton
              class="!bg-gray-200 !text-gray-800 !h-9 !px-3 !rounded-lg"
              @click="cancelPicker"
            >
              Annuler
            </BaseButton>
            <BaseButton :disabled="locked" class="!h-9 !px-3 !rounded-lg" @click="confirmPicker"
              >Valider</BaseButton
            >
          </div>
        </div>

        <div
          v-if="globalPickerError"
          class="mx-4 mt-3 rounded-md bg-red-50 border border-red-200 px-3 py-2 text-sm text-red-700"
        >
          {{ globalPickerError }}
        </div>

        <div class="p-4 flex flex-col gap-3 overflow-y-auto">
          <div v-if="pickerTab === 'dishes'">
            <BaseInput label="Rechercher un plat" v-model="dishSearch" />
          </div>
          <div v-else>
            <BaseInput label="Rechercher un ingrédient" v-model="ingSearch" />
          </div>

          <div class="border border-gray-200 rounded-xl overflow-hidden">
            <div class="max-h-[60vh] overflow-y-auto">
              <template v-if="pickerTab === 'dishes'">
                <div
                  v-for="d in filteredDishes"
                  :key="d.id"
                  class="flex items-center gap-3 px-3 py-2 border-b last:border-b-0"
                >
                  <input
                    type="checkbox"
                    class="h-5 w-5 rounded border-gray-300 text-emerald-600 focus:ring-emerald-500"
                    :value="d.id"
                    v-model="selectedDishIds"
                    :disabled="locked"
                  />
                  <span class="text-sm">{{ d.name }}</span>
                </div>
                <div v-if="!filteredDishes.length" class="px-3 py-4 text-sm text-gray-500">
                  Aucun plat
                </div>
              </template>

              <template v-else>
                <div
                  v-for="ing in filteredIngredients"
                  :key="ing.id"
                  class="flex items-center justify-between gap-3 px-3 py-2 border-b last:border-b-0"
                >
                  <label class="flex items-center gap-3 flex-1">
                    <input
                      type="checkbox"
                      class="h-5 w-5 rounded border-gray-300 text-emerald-600 focus:ring-emerald-500"
                      :value="ing.id"
                      v-model="selectedIngredientIds"
                      :disabled="locked"
                    />
                    <span class="text-sm">{{ ing.name }}</span>
                    <span class="text-xs text-gray-500">{{ ing.aisle }}</span>
                  </label>

                  <div v-if="selectedIngredientIds.includes(ing.id)" class="flex-1">
                    <QtyUnitInput
                      :model-value="
                        selectedIngredientMetaById[ing.id] ?? { quantity: null, unit: null }
                      "
                      :units="UNIT_OPTIONS"
                      @update:modelValue="(v: QtyUnit) => (selectedIngredientMetaById[ing.id] = v)"
                      :disabled="locked"
                      :input-class="
                        'w-20 h-10 rounded-lg border px-2 text-sm' +
                        (pickerErrors[ing.id] ? ' border-red-500 ring-1 ring-red-300' : '')
                      "
                      :select-class="
                        'w-24 h-10 rounded-lg border px-2 text-sm' +
                        (pickerErrors[ing.id] ? ' border-red-500 ring-1 ring-red-300' : '')
                      "
                    />
                    <p v-if="pickerErrors[ing.id]" class="mt-1 text-xs text-red-600">
                      {{ pickerErrors[ing.id] }}
                    </p>
                  </div>
                </div>
                <div v-if="!filteredIngredients.length" class="px-3 py-4 text-sm text-gray-500">
                  Aucun ingrédient
                </div>
              </template>
            </div>
          </div>

          <div class="text-xs text-gray-600">
            <template v-if="pickerTab === 'dishes'">
              {{ selectedDishIds.length }} plat(s) sélectionné(s)
            </template>
            <template v-else>
              {{ selectedIngredientIds.length }} ingrédient(s) sélectionné(s)
            </template>
          </div>
        </div>
      </div>
    </div>
  </section>

  <section v-else class="text-center text-gray-500 py-10">Chargement…</section>
</template>

<style scoped>
.modal-enter-from .relative {
  transform: scale(0.98);
  opacity: 0;
}
.modal-enter-active .relative,
.modal-leave-active .relative {
  transition: all 0.2s ease;
}
.modal-leave-to .relative {
  transform: scale(0.98);
  opacity: 0;
}
</style>
