<script setup lang="ts">
import { onMounted, ref, computed, nextTick } from 'vue'
import { useDishesStore } from '@/stores/dishes'
import { useIngredientsStore } from '@/stores/ingredients'
import BaseInput from '@/components/ui/BaseInput.vue'
import BaseButton from '@/components/ui/BaseButton.vue'
import DataTable from '@/components/ui/DataTable.vue'
import QtyUnitInput from '@/components/ui/QtyUnitInput.vue'
import BaseModal from '@/components/ui/BaseModal.vue'
import { useFlash } from '@/composables/useFlash'
import { useMapById } from '@/composables/useMapById'
import type { Dish, Ingredient } from '@/api/types'

const dishes = useDishesStore()
const ingredients = useIngredientsStore()

const UNIT_OPTIONS = ['g', 'kg', 'paquet','unité']

const createName = ref<string>('')
type QtyUnit = { quantity: number | null; unit: string | null }

type CreateSelMeta = { quantity: number | null; unit: string | null }
const showCreatePicker = ref(false)
const ingSearchCreate = ref('')
const selectedCreateIds = ref<string[]>([])
const createMetaById = ref<Record<string, CreateSelMeta>>({})

const createPickerErrors = ref<Record<string, string>>({})
const globalCreatePickerError = ref<string | null>(null)

const ingredientOptions = computed<Ingredient[]>(() => ingredients.items)
const { byId: ingById } = useMapById(() => ingredientOptions.value)

const filteredIngredientsCreate = computed(() => {
  const q = ingSearchCreate.value.trim().toLowerCase()
  if (!q) return ingredientOptions.value
  return ingredientOptions.value.filter(
    (i) => i.name.toLowerCase().includes(q) || i.aisle?.toLowerCase().includes(q)
  )
})

function openCreatePicker() {
  showCreatePicker.value = true
  createPickerErrors.value = {}
  globalCreatePickerError.value = null
}
function cancelCreatePicker() {
  showCreatePicker.value = false
}
function toggleCreateSelect(id: string) {
  const set = new Set(selectedCreateIds.value)
  set.has(id) ? set.delete(id) : set.add(id)
  selectedCreateIds.value = [...set]
  if (!createMetaById.value[id]) createMetaById.value[id] = { quantity: null, unit: null }
  if (!set.has(id)) delete createPickerErrors.value[id]
}
function isMetaValid(m?: QtyUnit | null) {
  if (!m) return false
  const qOk = typeof m.quantity === 'number' && isFinite(m.quantity) && m.quantity > 0
  const uOk = !!m.unit && m.unit.trim().length > 0
  return qOk && uOk
}
function validateCreateSelection(): boolean {
  createPickerErrors.value = {}
  globalCreatePickerError.value = null
  for (const id of selectedCreateIds.value) {
    const meta = createMetaById.value[id]
    if (!isMetaValid(meta)) {
      createPickerErrors.value[id] = 'Quantité (> 0) et unité requises.'
    }
  }
  if (Object.keys(createPickerErrors.value).length) {
    globalCreatePickerError.value = 'Complétez quantité + unité pour les ingrédients sélectionnés.'
    return false
  }
  return true
}
function confirmCreatePicker() {
  if (!validateCreateSelection()) return
  showCreatePicker.value = false
}

const { flashSet, flashRow } = useFlash()

async function createDish() {
  if (selectedCreateIds.value.length && !validateCreateSelection()) return
  if (!createName.value.trim()) return

  const ingredientsPayload = selectedCreateIds.value.map((id) => {
    const meta = createMetaById.value[id] ?? { quantity: null, unit: null }
    return { ingredientId: id, quantity: meta.quantity, unit: meta.unit }
  })

  const prevLen = dishes.items.length
  const created = await dishes.createOne({
    name: createName.value.trim(),
    ingredients: ingredientsPayload,
  })

  let createdId: string | null = (created as any)?.id ?? null
  if (!createdId && dishes.items.length > prevLen) {
    createdId = dishes.items[dishes.items.length - 1]?.id ?? null
  }
  await nextTick()
  flashRow(createdId)

  createName.value = ''
  selectedCreateIds.value = []
  createMetaById.value = {}
  createPickerErrors.value = {}
  globalCreatePickerError.value = null
}

type EditRow = {
  ingredientId: string
  name: string
  aisle: string | null
  quantity: number | null
  unit: string | null
  included: boolean
}
const editingDishId = ref<string | null>(null)
const editRows = ref<EditRow[]>([])
const showAddInline = ref(false)
const ingSearchEdit = ref('')
const editSectionRef = ref<HTMLElement | null>(null)

const editErrors = ref<Record<string, string>>({})
const globalEditError = ref<string | null>(null)

const filteredIngredientsEdit = computed(() => {
  const q = ingSearchEdit.value.trim().toLowerCase()
  if (!q) return ingredientOptions.value
  return ingredientOptions.value.filter(
    (i) => i.name.toLowerCase().includes(q) || i.aisle?.toLowerCase().includes(q)
  )
})

function cancelEdit() {
  editingDishId.value = null
  editRows.value = []
  showAddInline.value = false
  ingSearchEdit.value = ''
  editErrors.value = {}
  globalEditError.value = null
}

function addIngredientToEdit(ing: Ingredient) {
  const idx = editRows.value.findIndex((r) => r.ingredientId === ing.id)
  if (idx === -1) {
    editRows.value.push({
      ingredientId: ing.id,
      name: ing.name,
      aisle: ing.aisle ?? null,
      quantity: null,
      unit: null,
      included: true,
    })
  } else {
    editRows.value[idx].included = true
  }
}

function removeRow(idx: number) {
  editRows.value[idx].included = false
  delete editErrors.value[editRows.value[idx].ingredientId]
}

function validateEdit(): boolean {
  editErrors.value = {}
  globalEditError.value = null
  const rows = editRows.value.filter((r) => r.included)
  for (const r of rows) {
    if (!isMetaValid({ quantity: r.quantity, unit: r.unit })) {
      editErrors.value[r.ingredientId] = 'Quantité (> 0) et unité requises.'
    }
  }
  if (Object.keys(editErrors.value).length) {
    globalEditError.value = 'Complétez quantité + unité pour les ingrédients inclus.'
    return false
  }
  return true
}

async function saveEdit() {
  if (!editingDishId.value) return
  if (!validateEdit()) return

  const payload = editRows.value
    .filter((r) => r.included)
    .map((r) => ({
      ingredientId: r.ingredientId,
      quantity: r.quantity,
      unit: r.unit,
    }))
  await dishes.updateOne(editingDishId.value, { ingredients: payload })
  const editedId = editingDishId.value
  editingDishId.value = null
  editRows.value = []
  showAddInline.value = false
  await nextTick()
  flashRow(editedId)
}

function scrollToEdit() {
  const el = editSectionRef.value
  if (!el) return
  const y = el.getBoundingClientRect().top + window.scrollY - 16
  window.scrollTo({ top: y, behavior: 'smooth' })
}

async function startEdit(d: Dish) {
  editingDishId.value = d.id
  editRows.value = (d.ingredients ?? [])
    .map((it) => {
      const iid = String(it.ingredientId)
      const src = ingById.value[iid]
      if (!src) return null
      return {
        ingredientId: iid,
        name: src.name,
        aisle: src.aisle ?? null,
        quantity: typeof it.quantity === 'number' ? it.quantity : null,
        unit: it.unit ?? null,
        included: true,
      } as EditRow
    })
    .filter(Boolean) as EditRow[]

  showAddInline.value = false
  ingSearchEdit.value = ''
  editErrors.value = {}
  globalEditError.value = null

  await nextTick()
  await nextTick()
  scrollToEdit()
}

const confirmDeleteOpen = ref(false)
const deleteTarget = ref<{ id: string; name: string } | null>(null)

function askDelete(d: Dish) {
  deleteTarget.value = { id: d.id, name: d.name }
  confirmDeleteOpen.value = true
}
function closeDeleteModal() {
  confirmDeleteOpen.value = false
  deleteTarget.value = null
}
async function confirmDelete() {
  if (!deleteTarget.value) return
  await dishes.deleteOne(deleteTarget.value.id)
  closeDeleteModal()
}

function dishDisplayedCount(d: Dish) {
  return (d.ingredients ?? []).filter((it) => !!ingById.value[String(it.ingredientId)]).length
}

onMounted(() => {
  dishes.fetchAll()
  ingredients.fetchAll()
})
</script>

<template>
  <section class="container mx-auto px-4 safe-pt safe-pb py-8 space-y-8">
    <div class="bg-white border border-gray-200 rounded-xl p-4 shadow-sm space-y-4">
      <h2 class="text-xl md:text-lg font-semibold">Créer un plat</h2>

      <div class="grid gap-3 items-end md:grid-cols-3">
        <BaseInput label="Nom du plat" v-model="createName" placeholder="ex: Pâtes bolognaise" />
        <div class="flex justify-end md:justify-start">
          <BaseButton
            type="button"
            class="!h-11 !rounded-xl !px-5 !shadow-sm hover:!shadow-md"
            @click="openCreatePicker"
          >
            Choisir les ingrédients
          </BaseButton>
        </div>
        <div class="text-right md:col-span-1">
          <BaseButton
            :disabled="!createName.trim()"
            class="!h-11 !rounded-xl !px-5 !shadow-sm hover:!shadow-md"
            @click="createDish"
          >
            Créer
          </BaseButton>
        </div>
      </div>

      <div
        v-if="globalCreatePickerError"
        class="rounded-md bg-red-50 border border-red-200 px-3 py-2 text-sm text-red-700"
      >
        {{ globalCreatePickerError }}
      </div>

      <div class="border border-gray-200 rounded-xl p-3" v-if="selectedCreateIds.length">
        <h3 class="text-sm font-medium mb-2">Ingrédients sélectionnés</h3>

        <div class="space-y-3">
          <div v-for="id in selectedCreateIds" :key="id" class="space-y-2">
            <div class="flex items-start gap-2">
              <div class="flex-1 min-w-0">
                <div class="font-medium text-sm truncate">{{ ingById[id]?.name ?? id }}</div>
                <div class="text-xs text-gray-500 truncate">{{ ingById[id]?.aisle }}</div>
              </div>
            </div>

            <div>
              <QtyUnitInput
                :model-value="createMetaById[id] ?? { quantity: null, unit: null }"
                :units="UNIT_OPTIONS"
                @update:modelValue="(v: QtyUnit) => (createMetaById[id] = v)"
                :input-class="'w-full md:w-24 h-10 rounded-lg border px-2 text-sm'"
                :select-class="'w-full md:w-28 h-10 rounded-lg border px-2 text-sm max-w-none md:max-w-[8rem]'"
              />
              <p v-if="createPickerErrors[id]" class="mt-1 text-xs text-red-600">
                {{ createPickerErrors[id] }}
              </p>
            </div>
          </div>
        </div>
      </div>
    </div>

    <div class="md:hidden space-y-3">
      <div
        v-for="d in dishes.items"
        :key="d.id"
        class="bg-white border border-gray-200 rounded-xl p-4 shadow-sm"
        :class="flashSet[d.id] ? 'row-flash' : ''"
      >
        <div class="flex items-center justify-between gap-3">
          <div class="min-w-0">
            <div class="font-medium truncate">{{ d.name }}</div>
            <div class="text-xs text-gray-500"># Ingrédients : {{ dishDisplayedCount(d) }}</div>
          </div>
          <div class="flex gap-2">
            <BaseButton class="!px-3 !h-9 !rounded-lg" @click="startEdit(d)">Modifier</BaseButton>
            <BaseButton class="!bg-red-600 !px-3 !h-9 !rounded-lg" @click="askDelete(d)"
              >Supprimer</BaseButton
            >
          </div>
        </div>
      </div>

      <div
        v-if="editingDishId"
        class="bg-gray-50 border border-gray-200 rounded-xl p-3"
        ref="editSectionRef"
      >
        <div class="flex items-center justify-between mb-3">
          <div class="font-medium">Éditer les ingrédients du plat</div>
          <div class="flex gap-2">
            <BaseButton
              class="!bg-gray-200 !text-gray-800 !h-9 !px-3 !rounded-lg"
              @click="cancelEdit"
              >Annuler</BaseButton
            >
            <BaseButton class="!h-9 !px-3 !rounded-lg" @click="saveEdit">Enregistrer</BaseButton>
          </div>
        </div>

        <div
          v-if="globalEditError"
          class="mb-2 rounded-md bg-red-50 border border-red-200 px-3 py-2 text-sm text-red-700"
        >
          {{ globalEditError }}
        </div>

        <div class="space-y-2">
          <div
            v-for="(row, idx) in editRows"
            :key="row.ingredientId"
            class="space-y-2 bg-white border border-gray-200 rounded-xl px-3 py-2"
          >
            <div class="flex items-start gap-2">
              <input
                type="checkbox"
                v-model="row.included"
                class="h-5 w-5 mt-0.5 rounded border-gray-300 text-emerald-600 focus:ring-emerald-500"
              />
              <div class="flex-1 min-w-0">
                <div class="font-medium text-sm truncate">{{ row.name }}</div>
                <div class="text-xs text-gray-500 truncate">{{ row.aisle }}</div>
              </div>
              <BaseButton
                class="!bg-red-600 !px-2 !h-9 !w-9 !p-0 !rounded-lg"
                @click="removeRow(idx)"
                title="Retirer"
                aria-label="Retirer"
                >×</BaseButton
              >
            </div>

            <div class="pl-7">
              <QtyUnitInput
                :model-value="{ quantity: row.quantity, unit: row.unit }"
                :units="UNIT_OPTIONS"
                @update:modelValue="
                  (v: QtyUnit) => {
                    row.quantity = v.quantity
                    row.unit = v.unit
                    delete editErrors[row.ingredientId]
                  }
                "
                :input-class="
                  'w-full md:w-24 h-10 rounded-lg border px-2 text-sm' +
                  (editErrors[row.ingredientId] ? ' border-red-500 ring-1 ring-red-300' : '')
                "
                :select-class="
                  'w-full md:w-28 h-10 rounded-lg border px-2 text-sm max-w-none md:max-w-[8rem]' +
                  (editErrors[row.ingredientId] ? ' border-red-500 ring-1 ring-red-300' : '')
                "
              />
              <p v-if="editErrors[row.ingredientId]" class="mt-1 text-xs text-red-600">
                {{ editErrors[row.ingredientId] }}
              </p>
            </div>
          </div>
        </div>

        <div class="mt-3">
          <BaseButton
            class="!px-2 !h-9 !w-9 !p-0 !rounded-lg"
            @click="showAddInline = !showAddInline"
            title="Ajouter"
            aria-label="Ajouter"
          >
            <span class="md:hidden text-base leading-none">+</span>
            <span class="hidden md:inline">+ Ajouter un ingrédient</span>
          </BaseButton>

          <div v-if="showAddInline" class="mt-2 border border-gray-200 rounded-xl p-3 bg-white">
            <div class="grid gap-3 items-end md:grid-cols-2">
              <BaseInput label="Rechercher un ingrédient" v-model="ingSearchEdit" />
            </div>
            <div class="max-h-64 overflow-y-auto border border-gray-200 rounded-xl mt-2">
              <div
                v-for="ing in filteredIngredientsEdit"
                :key="ing.id"
                class="flex items-center justify-between px-3 py-2 border-b last:border-b-0"
              >
                <div class="min-w-0">
                  <div class="text-sm font-medium truncate">{{ ing.name }}</div>
                  <div class="text-xs text-gray-500 truncate">{{ ing.aisle }}</div>
                </div>
                <BaseButton
                  class="!px-2 !h-9 !w-9 !p-0 !rounded-lg"
                  @click="addIngredientToEdit(ing)"
                  title="Ajouter"
                  aria-label="Ajouter"
                >
                  <span class="md:hidden text-base leading-none">+</span>
                  <span class="hidden md:inline">Ajouter</span>
                </BaseButton>
              </div>
              <div v-if="!filteredIngredientsEdit.length" class="px-3 py-4 text-sm text-gray-500">
                Aucun ingrédient
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>

    <div
      class="hidden md:block bg-white border border-gray-200 rounded-xl shadow-sm overflow-hidden"
    >
      <DataTable :loading="dishes.loading">
        <template #head>
          <th class="p-3 bg-gray-50 text-left text-gray-700 font-medium">Nom</th>
          <th class="p-3 bg-gray-50 text-left text-gray-700 font-medium"># Ingrédients</th>
          <th class="p-3 bg-gray-50 text-right text-gray-700 font-medium">Actions</th>
        </template>

        <tr
          v-for="d in dishes.items"
          :key="d.id"
          class="border-t hover:bg-gray-50/60 transition"
          :class="flashSet[d.id] ? 'row-flash' : ''"
        >
          <td class="p-3 font-medium align-middle">{{ d.name }}</td>
          <td class="p-3 align-middle">{{ dishDisplayedCount(d) }}</td>
          <td class="p-3 text-right space-x-2 whitespace-nowrap align-middle">
            <BaseButton class="!h-9 !px-3 !rounded-lg" @click="startEdit(d)">Modifier</BaseButton>
            <BaseButton class="!bg-red-600 !h-9 !px-3 !rounded-lg" @click="askDelete(d)"
              >Supprimer</BaseButton
            >
          </td>
        </tr>

        <tr v-if="editingDishId" class="border-t">
          <td colspan="3" class="p-0">
            <div class="bg-gray-50 p-3" ref="editSectionRef">
              <div class="flex items-center justify-between mb-3">
                <div class="font-medium">Éditer les ingrédients du plat</div>
                <div class="flex gap-2">
                  <BaseButton
                    class="!bg-gray-200 !text-gray-800 !h-9 !px-3 !rounded-lg"
                    @click="cancelEdit"
                    >Annuler</BaseButton
                  >
                  <BaseButton class="!h-9 !px-3 !rounded-lg" @click="saveEdit"
                    >Enregistrer</BaseButton
                  >
                </div>
              </div>

              <div
                v-if="globalEditError"
                class="mb-2 rounded-md bg-red-50 border border-red-200 px-3 py-2 text-sm text-red-700"
              >
                {{ globalEditError }}
              </div>

              <div class="space-y-2">
                <div
                  v-for="(row, idx) in editRows"
                  :key="row.ingredientId"
                  class="grid grid-cols-12 gap-2 items-center bg-white border border-gray-200 rounded-lg px-3 py-2"
                >
                  <div class="col-span-1 flex justify-center">
                    <input
                      type="checkbox"
                      v-model="row.included"
                      class="h-4 w-4 rounded border-gray-300 text-emerald-600 focus:ring-emerald-500"
                    />
                  </div>
                  <div class="col-span-5 min-w-0">
                    <div class="font-medium text-sm truncate">{{ row.name }}</div>
                    <div class="text-xs text-gray-500 truncate">{{ row.aisle }}</div>
                  </div>
                  <div class="col-span-5">
                    <QtyUnitInput
                      :model-value="{ quantity: row.quantity, unit: row.unit }"
                      :units="UNIT_OPTIONS"
                      @update:modelValue="
                        (v:QtyUnit) => {
                          row.quantity = v.quantity
                          row.unit = v.unit
                          delete editErrors[row.ingredientId]
                        }
                      "
                      :input-class="
                        'w-24 rounded-md border px-2 py-2 text-sm' +
                        (editErrors[row.ingredientId] ? ' border-red-500 ring-1 ring-red-300' : '')
                      "
                      :select-class="
                        'w-28 rounded-md border px-2 py-2 text-sm max-w-[8rem]' +
                        (editErrors[row.ingredientId] ? ' border-red-500 ring-1 ring-red-300' : '')
                      "
                    />
                    <p v-if="editErrors[row.ingredientId]" class="mt-1 text-xs text-red-600">
                      {{ editErrors[row.ingredientId] }}
                    </p>
                  </div>
                  <div class="col-span-1 text-right">
                    <BaseButton class="!bg-red-600 !px-2 !h-9 !rounded-lg" @click="removeRow(idx)">
                      <span class="hidden md:inline">Supprimer</span>
                      <span class="md:hidden">×</span>
                    </BaseButton>
                  </div>
                </div>
              </div>

              <div class="mt-3">
                <BaseButton class="!px-2 !h-9 !rounded-lg" @click="showAddInline = !showAddInline">
                  <span class="hidden md:inline">+ Ajouter un ingrédient</span>
                  <span class="md:hidden text-base leading-none">+</span>
                </BaseButton>
                <div
                  v-if="showAddInline"
                  class="mt-2 border border-gray-200 rounded-lg p-3 bg-white"
                >
                  <div class="grid md:grid-cols-2 gap-3 items-end">
                    <BaseInput label="Rechercher un ingrédient" v-model="ingSearchEdit" />
                  </div>
                  <div class="max-h-64 overflow-y-auto border border-gray-200 rounded-lg mt-2">
                    <div
                      v-for="ing in filteredIngredientsEdit"
                      :key="ing.id"
                      class="flex items-center justify-between px-3 py-2 border-b last:border-b-0"
                    >
                      <div class="min-w-0">
                        <div class="text-sm font-medium truncate">{{ ing.name }}</div>
                        <div class="text-xs text-gray-500 truncate">{{ ing.aisle }}</div>
                      </div>
                      <BaseButton class="!px-2 !h-9 !rounded-lg" @click="addIngredientToEdit(ing)">
                        <span class="hidden md:inline">Ajouter</span>
                        <span class="md:hidden text-base leading-none">+</span>
                      </BaseButton>
                    </div>
                    <div
                      v-if="!filteredIngredientsEdit.length"
                      class="px-3 py-4 text-sm text-gray-500"
                    >
                      Aucun ingrédient
                    </div>
                  </div>
                </div>
              </div>
            </div>
          </td>
        </tr>
      </DataTable>
    </div>

    <div
      v-if="showCreatePicker"
      class="fixed inset-0 z-50 flex flex-col"
      aria-modal="true"
      role="dialog"
    >
      <div class="flex-1 bg-black/40" @click="cancelCreatePicker"></div>
      <div
        class="mt-auto w-full bg-white shadow-2xl flex flex-col rounded-t-2xl max-h-[85vh] safe-pb md:max-h-none md:h-full md:rounded-none md:w/[520px] md:ml-auto"
      >
        <div class="border-b px-4 py-3 flex items-center justify-between sticky top-0 bg-white">
          <div class="font-medium">Choisir les ingrédients du plat</div>
          <div class="flex gap-2">
            <BaseButton
              class="!bg-gray-200 !text-gray-800 !h-9 !px-3 !rounded-lg"
              @click="cancelCreatePicker"
            >
              Fermer
            </BaseButton>
            <BaseButton class="!h-9 !px-3 !rounded-lg" @click="confirmCreatePicker"
              >Valider</BaseButton
            >
          </div>
        </div>

        <div
          v-if="globalCreatePickerError"
          class="mx-4 mt-3 rounded-md bg-red-50 border border-red-200 px-3 py-2 text-sm text-red-700"
        >
          {{ globalCreatePickerError }}
        </div>

        <div class="p-4 flex flex-col gap-3 overflow-y-auto">
          <BaseInput label="Rechercher un ingrédient" v-model="ingSearchCreate" />
          <div class="border border-gray-200 rounded-xl overflow-hidden">
            <div class="max-h-[60vh] overflow-y-auto">
              <div
                v-for="ing in filteredIngredientsCreate"
                :key="ing.id"
                class="flex flex-col gap-1 px-3 py-2 border-b last:border-b-0"
              >
                <label class="flex items-center gap-3">
                  <input
                    type="checkbox"
                    class="h-5 w-5 rounded border-gray-300 text-emerald-600 focus:ring-emerald-500"
                    :checked="selectedCreateIds.includes(ing.id)"
                    @change="() => toggleCreateSelect(ing.id)"
                  />
                  <span class="text-sm flex-1 min-w-0 truncate">{{ ing.name }}</span>
                  <span class="text-xs text-gray-500 truncate">{{ ing.aisle }}</span>
                </label>

                <div v-if="selectedCreateIds.includes(ing.id)" class="pl-8">
                  <QtyUnitInput
                    :model-value="createMetaById[ing.id] ?? { quantity: null, unit: null }"
                    :units="UNIT_OPTIONS"
                    @update:modelValue="(v: QtyUnit) => { createMetaById[ing.id] = v; delete createPickerErrors[ing.id] }"
                    :input-class="'w-full md:w-24 h-10 rounded-lg border px-2 text-sm'"
                    :select-class="'w-full md:w-28 h-10 rounded-lg border px-2 text-sm max-w-none md:max-w-[8rem]'"
                  />
                  <p v-if="createPickerErrors[ing.id]" class="mt-1 text-xs text-red-600">
                    {{ createPickerErrors[ing.id] }}
                  </p>
                </div>
              </div>

              <div v-if="!filteredIngredientsCreate.length" class="px-3 py-4 text-sm text-gray-500">
                Aucun ingrédient
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>

    <BaseModal :show="confirmDeleteOpen" @close="closeDeleteModal">
      <h3 class="text-lg font-semibold mb-2">Supprimer le plat ?</h3>
      <p class="text-sm text-gray-700 mb-4">
        Confirmer la suppression de
        <span class="font-medium">« {{ deleteTarget?.name }} »</span> ?
      </p>
      <div class="flex justify-end gap-2">
        <BaseButton
          class="!bg-gray-200 !text-gray-800 !h-9 !px-3 !rounded-lg"
          @click="closeDeleteModal"
        >
          Annuler
        </BaseButton>
        <BaseButton class="!bg-red-600 !h-9 !px-3 !rounded-lg" @click="confirmDelete"
          >Supprimer</BaseButton
        >
      </div>
    </BaseModal>
  </section>
</template>

<style scoped>
@keyframes flashRow {
  0% {
    background-color: #ecfdf5;
  }
  100% {
    background-color: transparent;
  }
}
.row-flash {
  animation: flashRow 1.2s ease-out 1;
}
</style>
