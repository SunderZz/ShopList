<script setup lang="ts">
import { onMounted, ref, computed } from 'vue'
import { useRouter } from 'vue-router'
import { useListsStore } from '@/stores/lists'
import { useDishesStore } from '@/stores/dishes'
import { useIngredientsStore } from '@/stores/ingredients'
import BaseInput from '@/components/ui/BaseInput.vue'
import BaseButton from '@/components/ui/BaseButton.vue'
import DataTable from '@/components/ui/DataTable.vue'
import QtyUnitInput from '@/components/ui/QtyUnitInput.vue'
import type { ShoppingListItem, Ingredient, Dish } from '@/api/types'

const router = useRouter()
const lists = useListsStore()
const dishes = useDishesStore()
const ingredients = useIngredientsStore()

const createName = ref<string>('')
const createDate = ref<string>(new Date().toISOString().slice(0, 10))
const createDishIds = ref<string[]>([])
const createManualItems = ref<ShoppingListItem[]>([])

const showPicker = ref(false)
const pickerTab = ref<'dishes' | 'ingredients'>('dishes')
const selectedDishIds = ref<string[]>([])
const selectedIngredientIds = ref<string[]>([])
type IngMeta = { quantity: number | null; unit: string | null }
const selectedIngredientMetaById = ref<Record<string, IngMeta>>({})
const dishSearch = ref('')
const ingSearch = ref('')
type QtyUnit = { quantity: number | null; unit: string | null }

const UNIT_OPTIONS = ['kg', 'g', 'paquet']

const pickerErrors = ref<Record<string, string>>({})
const globalPickerError = ref<string | null>(null)

onMounted(() => {
  lists.fetchAll()
  dishes.fetchAll()
  ingredients.fetchAll()
})

const dishOptions = computed<Dish[]>(() => dishes.items)
const ingredientOptions = computed<Ingredient[]>(() => ingredients.items)
const ingById = computed<Record<string, Ingredient>>(() => {
  const m: Record<string, Ingredient> = {}
  for (const i of ingredientOptions.value) m[i.id] = i
  return m
})

function openPicker() {
  pickerTab.value = 'dishes'
  dishSearch.value = ''
  ingSearch.value = ''
  selectedDishIds.value = [...createDishIds.value]
  selectedIngredientIds.value = createManualItems.value.map((i) => i.ingredientId)
  selectedIngredientMetaById.value = {}
  for (const it of createManualItems.value) {
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
function isMetaValid(m?: IngMeta | null) {
  if (!m) return false
  const qOk = typeof m.quantity === 'number' && isFinite(m.quantity) && m.quantity > 0
  const uOk = !!m.unit && m.unit.trim().length > 0
  return qOk && uOk
}
function validateSelectedIngredients(): boolean {
  pickerErrors.value = {}
  globalPickerError.value = null
  for (const id of selectedIngredientIds.value) {
    const meta = selectedIngredientMetaById.value[id]
    if (!isMetaValid(meta)) pickerErrors.value[id] = 'Quantité (> 0) et unité requises.'
  }
  if (Object.keys(pickerErrors.value).length) {
    globalPickerError.value = 'Complétez quantité + unité pour les ingrédients sélectionnés.'
    return false
  }
  return true
}
function confirmPicker() {
  if (pickerTab.value === 'ingredients' && !validateSelectedIngredients()) return
  createDishIds.value = [...selectedDishIds.value]
  const rebuilt: ShoppingListItem[] = []
  for (const id of selectedIngredientIds.value) {
    const ing = ingById.value[id]
    if (!ing) continue
    const meta = selectedIngredientMetaById.value[id] ?? { quantity: null, unit: null }
    rebuilt.push({
      ingredientId: id,
      ingredientName: ing.name,
      aisle: ing.aisle ?? null,
      quantity: meta.quantity ?? null,
      unit: meta.unit ?? null,
      checked: false,
    })
  }
  createManualItems.value = rebuilt
  showPicker.value = false
}

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

async function createList() {
  if (!createName.value.trim()) return
  const { id } = await lists.createOne({
    name: createName.value.trim(),
    date: createDate.value,
    dishIds: createDishIds.value,
    items: createManualItems.value,
  })
  createName.value = ''
  createDate.value = new Date().toISOString().slice(0, 10)
  createDishIds.value = []
  createManualItems.value = []
  router.push(`/lists/${id}`)
}

function goDetail(lId: string) {
  router.push(`/lists/${lId}`)
}
</script>

<template>
  <section class="container mx-auto px-4 safe-pt safe-pb py-8 space-y-8">
    <div class="bg-white border border-gray-200 rounded-xl p-4 shadow-sm space-y-4">
      <div class="flex items-center justify-between">
        <h2 class="text-xl md:text-lg font-semibold">Créer une liste</h2>
      </div>

      <div class="grid md:grid-cols-3 gap-3 items-end">
        <BaseInput
          label="Nom de la liste"
          v-model="createName"
          placeholder="ex: Courses du samedi"
        />
        <BaseInput label="Date" type="date" v-model="createDate" />
        <div class="flex justify-end md:justify-start">
          <BaseButton
            type="button"
            class="!h-11 !rounded-xl !px-5 !shadow-sm hover:!shadow-md"
            @click="openPicker"
          >
            Ajouter des plats/ingrédients
          </BaseButton>
        </div>
      </div>
      <div class="text-right">
        <BaseButton
          :disabled="!createName.trim()"
          class="!h-11 !rounded-xl !px-5 !shadow-sm hover:!shadow-md"
          @click="createList"
        >
          Créer
        </BaseButton>
      </div>
    </div>

    <div class="bg-white border border-gray-200 rounded-xl shadow-sm overflow-hidden">
      <div class="hidden md:block">
        <DataTable :loading="lists.loading">
          <template #head>
            <th class="p-3 bg-gray-50 text-left text-gray-700 font-medium">Nom</th>
            <th class="p-3 bg-gray-50 text-left text-gray-700 font-medium">Date</th>
            <th class="p-3 bg-gray-50 text-left text-gray-700 font-medium">Ingrédients</th>
            <th class="p-3 bg-gray-50 text-left text-gray-700 font-medium">Plats</th>
          </template>
          <tr
            v-for="l in lists.items"
            :key="l.id"
            class="border-t hover:bg-gray-50/60 transition cursor-pointer"
            @click="goDetail(l.id)"
          >
            <td class="p-3 font-medium">{{ l.name }}</td>
            <td class="p-3">{{ new Date(l.date).toLocaleDateString() }}</td>
            <td class="p-3">{{ l.items.length }}</td>
            <td class="p-3">{{ l.dishIds.length }}</td>
          </tr>
        </DataTable>
      </div>

      <div class="md:hidden divide-y">
        <button
          v-for="l in lists.items"
          :key="l.id"
          class="w-full text-left px-4 py-3 active:bg-gray-50"
          @click="goDetail(l.id)"
        >
          <div class="flex items-start justify-between gap-3">
            <div class="min-w-0">
              <div class="font-medium truncate">{{ l.name }}</div>
              <div class="text-xs text-gray-500 mt-0.5">
                {{ new Date(l.date).toLocaleDateString() }}
              </div>
            </div>
            <div class="flex items-center gap-2 shrink-0">
              <span
                class="inline-flex items-center gap-1 text-[11px] px-2 py-1 rounded-full bg-gray-100 text-gray-700"
              >
                <svg class="h-3.5 w-3.5" viewBox="0 0 24 24" fill="currentColor" aria-hidden="true">
                  <path
                    d="M7 5h14v2H7zM7 11h14v2H7zM7 17h14v2H7zM3 5h2v2H3zM3 11h2v2H3zM3 17h2v2H3z"
                  />
                </svg>
                {{ l.items.length }}
              </span>
              <span
                class="inline-flex items-center gap-1 text-[11px] px-2 py-1 rounded-full bg-gray-100 text-gray-700"
              >
                <svg class="h-3.5 w-3.5" viewBox="0 0 24 24" fill="currentColor" aria-hidden="true">
                  <path d="M12 3a9 9 0 100 18 9 9 0 000-18zm1 9h6a7 7 0 01-6 6v-6z" />
                </svg>
                {{ l.dishIds.length }}
              </span>
            </div>
          </div>
        </button>
        <div v-if="!lists.items.length" class="px-4 py-6 text-sm text-gray-500">Aucune liste</div>
      </div>
    </div>

    <div
      v-if="showPicker"
      class="fixed inset-0 z-50 flex"
      aria-modal="true"
      role="dialog"
      tabindex="-1"
      @keydown.esc="cancelPicker"
    >
      <div class="absolute inset-0 bg-black/50 backdrop-blur-sm" @click="cancelPicker"></div>

      <div
        class="relative ml-auto w-full md:w-[640px] h-full bg-white shadow-2xl flex flex-col md:rounded-none"
      >
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
            >
              Ingrédients
            </button>
          </div>
          <div class="flex gap-2">
            <BaseButton
              class="!bg-gray-200 !text-gray-800 !h-9 !px-3 !rounded-lg"
              @click="cancelPicker"
            >
              Annuler
            </BaseButton>
            <BaseButton class="!h-9 !px-3 !rounded-lg" @click="confirmPicker">Valider</BaseButton>
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

          <div class="grid md:grid-cols-2 gap-4">
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
                      />
                      <span class="text-sm">{{ ing.name }}</span>
                      <span class="text-xs text-gray-500">{{ ing.aisle }}</span>
                    </label>
                  </div>
                  <div v-if="!filteredIngredients.length" class="px-3 py-4 text-sm text-gray-500">
                    Aucun ingrédient
                  </div>
                </template>
              </div>
            </div>

            <div v-if="pickerTab === 'ingredients'" class="border border-gray-200 rounded-xl p-3">
              <h3 class="text-sm font-medium mb-2">Ingrédients sélectionnées</h3>
              <div class="space-y-2 max-h-[60vh] overflow-y-auto">
                <div
                  v-for="ingId in selectedIngredientIds"
                  :key="ingId"
                  class="grid grid-cols-12 gap-2 items-center bg-gray-50 border border-gray-200 rounded-xl px-2 py-2"
                >
                  <div class="col-span-6">
                    <div class="font-medium text-sm">{{ ingById[ingId]?.name ?? ingId }}</div>
                    <div class="text-xs text-gray-500">{{ ingById[ingId]?.aisle }}</div>
                  </div>

                  <div class="col-span-6">
                    <QtyUnitInput
                      :model-value="
                        selectedIngredientMetaById[ingId] ?? { quantity: null, unit: null }
                      "
                      :units="UNIT_OPTIONS"
                      @update:modelValue="(v:QtyUnit) => (selectedIngredientMetaById[ingId] = v)"
                      :input-class="
                        'w-full rounded-md border px-2 py-2 text-sm' +
                        (pickerErrors[ingId] ? ' border-red-500 ring-1 ring-red-300' : '')
                      "
                      :select-class="
                        'w-full rounded-md border px-2 py-2 text-sm' +
                        (pickerErrors[ingId] ? ' border-red-500 ring-1 ring-red-300' : '')
                      "
                    />
                    <p v-if="pickerErrors[ingId]" class="mt-1 text-xs text-red-600">
                      {{ pickerErrors[ingId] }}
                    </p>
                  </div>
                </div>

                <div v-if="!selectedIngredientIds.length" class="text-sm text-gray-500">
                  Aucun élément
                </div>
              </div>
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
</template>
