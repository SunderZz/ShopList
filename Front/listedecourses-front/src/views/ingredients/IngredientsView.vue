<script setup lang="ts">
import { computed, nextTick, onMounted, ref } from 'vue'
import { useIngredientsStore } from '@/stores/ingredients'
import BaseInput from '@/components/ui/BaseInput.vue'
import BaseButton from '@/components/ui/BaseButton.vue'
import DataTable from '@/components/ui/DataTable.vue'
import BaseModal from '@/components/ui/BaseModal.vue'
import IconGlyph from '@/components/ui/IconGlyph.vue'
import type { Ingredient } from '@/api/types'
import { useFlash } from '@/composables/useFlash'
import { getApiErrorMessage } from '@/api/errors'

const store = useIngredientsStore()

const form = ref({ name: '', aisle: '' })
const formError = ref<string | null>(null)
const showCreateModal = ref(false)
const search = ref('')

const editingId = ref<string | null>(null)
const editForm = ref<{ name: string; aisle: string }>({ name: '', aisle: '' })
const editError = ref<string | null>(null)

const { flashSet, flashRow } = useFlash()

function normalizeSearch(value: string | null | undefined) {
  return (value ?? '')
    .normalize('NFD')
    .replace(/[\u0300-\u036f]/g, '')
    .trim()
    .replace(/\s+/g, ' ')
    .toLowerCase()
}

const filteredItems = computed(() => {
  const q = normalizeSearch(search.value)
  if (!q) return store.items
  return store.items.filter((i) => {
    return normalizeSearch(i.name).includes(q) || normalizeSearch(i.aisle).includes(q)
  })
})

const createNameAlreadyExists = computed(() => {
  const name = normalizeSearch(form.value.name)
  if (!name) return false
  return store.items.some((i) => normalizeSearch(i.name) === name)
})

onMounted(() => {
  void store.ensureLoaded()
})

function resetCreateForm() {
  form.value = { name: '', aisle: '' }
  formError.value = null
}

function openCreateModal() {
  resetCreateForm()
  showCreateModal.value = true
}

function cancelCreateModal() {
  showCreateModal.value = false
  resetCreateForm()
}

async function createItem() {
  if (!form.value.name.trim()) return
  if (createNameAlreadyExists.value) return
  formError.value = null

  try {
    const prevLen = store.items.length
    await store.createOne({
      name: form.value.name.trim(),
      aisle: form.value.aisle.trim(),
    })
    await nextTick()
    const created = store.items.length > prevLen ? store.items[store.items.length - 1] : null
    flashRow(created?.id)
    showCreateModal.value = false
    resetCreateForm()
  } catch (error: unknown) {
    formError.value = getApiErrorMessage(error, 'Impossible de creer cet ingredient.')
  }
}

function startEdit(i: Ingredient) {
  editingId.value = i.id
  editForm.value = { name: i.name ?? '', aisle: i.aisle ?? '' }
  editError.value = null
}

function cancelEdit() {
  editingId.value = null
  editError.value = null
}

async function confirmEdit(id: string) {
  editError.value = null
  if (!editForm.value.name.trim()) return

  try {
    await store.updateOne(id, {
      name: editForm.value.name.trim(),
      aisle: editForm.value.aisle.trim(),
    })
    editingId.value = null
    await nextTick()
    flashRow(id)
  } catch (error: unknown) {
    editError.value = getApiErrorMessage(error, 'Impossible de modifier cet ingredient.')
  }
}

const confirmOpen = ref(false)
const toDelete = ref<Ingredient | null>(null)
const deleteError = ref<string | null>(null)

function askDelete(i: Ingredient) {
  toDelete.value = i
  deleteError.value = null
  confirmOpen.value = true
}

async function confirmDelete() {
  if (!toDelete.value) return
  deleteError.value = null

  try {
    await store.deleteOne(toDelete.value.id)
    confirmOpen.value = false
    toDelete.value = null
  } catch (error: unknown) {
    deleteError.value = getApiErrorMessage(error, 'Impossible de supprimer cet ingredient.')
  }
}

function cancelDelete() {
  confirmOpen.value = false
  toDelete.value = null
  deleteError.value = null
}
</script>

<template>
  <section class="container mx-auto px-4 safe-pt safe-pb py-8 space-y-6">
    <div class="flex items-center justify-between gap-3">
      <div>
        <h1 class="text-3xl md:text-2xl font-semibold tracking-tight">Ingredients</h1>
        <p class="mt-1 text-sm text-gray-500 md:hidden">{{ store.items.length }} ingredient(s)</p>
      </div>

      <div class="flex items-center gap-2">
        <span
          class="hidden md:inline-flex items-center gap-2 text-xs text-gray-600 bg-gray-50 border border-gray-200 rounded-full px-3 py-1"
        >
          {{ store.items.length }} ingredient(s)
        </span>
        <BaseButton
          class="!w-auto !h-10 !rounded-xl !px-4 !shadow-sm hover:!shadow-md"
          @click="openCreateModal"
        >
          <span class="inline-flex items-center gap-2">
            <IconGlyph name="plus" />
            Creer
          </span>
        </BaseButton>
      </div>
    </div>

    <div class="bg-white border border-gray-200 rounded-xl p-4 shadow-sm">
      <div class="grid gap-3 md:grid-cols-[1fr_auto] md:items-end">
        <BaseInput label="Rechercher un ingredient" v-model="search" placeholder="Nom ou rayon" />
        <BaseButton
          v-if="search"
          type="button"
          class="!bg-gray-200 !text-gray-800 !h-11 !rounded-xl !px-5"
          @click="search = ''"
        >
          Effacer
        </BaseButton>
      </div>
      <p class="mt-2 text-xs text-gray-500">
        {{ filteredItems.length }} resultat(s) sur {{ store.items.length }} ingredient(s)
      </p>
    </div>

    <div class="md:hidden space-y-3">
      <div v-if="store.loading" class="bg-white border border-gray-200 rounded-xl p-4 text-sm text-gray-500">
        Chargement...
      </div>

      <template v-else>
        <div
          v-for="i in filteredItems"
          :key="i.id"
          class="bg-white border border-gray-200 rounded-xl p-4 shadow-sm"
          :class="flashSet[i.id] ? 'row-flash-card' : ''"
        >
          <div v-if="editingId !== i.id" class="flex items-start justify-between gap-3">
            <div class="min-w-0">
              <div class="font-medium truncate">{{ i.name }}</div>
              <div class="text-sm text-gray-600 truncate">{{ i.aisle || '-' }}</div>
            </div>
            <div class="flex flex-col gap-2 shrink-0">
              <BaseButton
                title="Modifier"
                aria-label="Modifier"
                class="!bg-white !text-gray-700 border hover:bg-gray-50 !h-9 !w-9 !p-0 !rounded-lg"
                @click="startEdit(i)"
              >
                <IconGlyph name="edit" />
              </BaseButton>
              <BaseButton
                title="Supprimer"
                aria-label="Supprimer"
                class="!bg-red-600 hover:!bg-red-700 !h-9 !w-9 !p-0 !rounded-lg"
                @click="askDelete(i)"
              >
                <IconGlyph name="trash" />
              </BaseButton>
            </div>
          </div>

          <div v-else class="space-y-3">
            <BaseInput label="Nom" v-model="editForm.name" />
            <BaseInput label="Rayon" v-model="editForm.aisle" />
            <p v-if="editError" class="rounded-md bg-red-50 border border-red-200 px-3 py-2 text-sm text-red-700">
              {{ editError }}
            </p>
            <div class="flex justify-end gap-2">
              <BaseButton
                class="!bg-gray-500 hover:!bg-gray-600 !h-10 !rounded-lg"
                @click="cancelEdit"
              >
                Annuler
              </BaseButton>
              <BaseButton
                class="!bg-emerald-600 hover:!bg-emerald-700 !h-10 !rounded-lg"
                :disabled="!editForm.name.trim()"
                @click="confirmEdit(i.id)"
              >
                Confirmer
              </BaseButton>
            </div>
          </div>
        </div>

        <div
          v-if="!filteredItems.length"
          class="bg-white border border-gray-200 rounded-xl p-4 text-sm text-gray-500"
        >
          Aucun ingredient trouve
        </div>
      </template>
    </div>

    <div class="hidden md:block bg-white border border-gray-200 rounded-xl shadow-sm overflow-hidden">
      <DataTable :loading="store.loading">
        <template #head>
          <th class="p-3 text-left bg-gray-50 text-gray-700 font-medium">Nom</th>
          <th class="p-3 text-left bg-gray-50 text-gray-700 font-medium">Rayon</th>
          <th class="p-3 text-right bg-gray-50 text-gray-700 font-medium">Actions</th>
        </template>

        <tr
          v-for="i in filteredItems"
          :key="i.id"
          class="border-t hover:bg-gray-50/60 transition"
          :class="flashSet[i.id] ? 'row-flash' : ''"
        >
          <td class="p-3 align-middle">
            <template v-if="editingId === i.id">
              <BaseInput v-model="editForm.name" />
              <p v-if="editError" class="mt-1 text-xs text-red-600">{{ editError }}</p>
            </template>
            <template v-else>
              {{ i.name }}
            </template>
          </td>

          <td class="p-3 align-middle">
            <template v-if="editingId === i.id">
              <BaseInput v-model="editForm.aisle" />
            </template>
            <template v-else>
              {{ i.aisle || '-' }}
            </template>
          </td>

          <td class="p-3 align-middle">
            <div class="flex justify-end gap-2">
              <BaseButton
                v-if="editingId !== i.id"
                title="Modifier"
                aria-label="Modifier"
                class="!bg-white !text-gray-700 border hover:bg-gray-50 !h-9 !w-9 !p-0 !rounded-lg md:!px-3 md:!w-auto"
                @click="startEdit(i)"
              >
                <span class="inline-flex items-center gap-2 justify-center w-full">
                  <IconGlyph name="edit" />
                  <span class="hidden md:inline">Modifier</span>
                </span>
              </BaseButton>

              <template v-if="editingId === i.id">
                <BaseButton
                  class="!bg-emerald-600 hover:!bg-emerald-700 !h-9 !w-9 !p-0 !rounded-lg md:!px-3 md:!w-auto"
                  :disabled="!editForm.name.trim()"
                  aria-label="Confirmer"
                  title="Confirmer"
                  @click="confirmEdit(i.id)"
                >
                  <span class="inline-flex items-center gap-2 justify-center w-full">
                    <IconGlyph name="check" />
                    <span class="hidden md:inline">Confirmer</span>
                  </span>
                </BaseButton>
                <BaseButton
                  class="!bg-gray-500 hover:!bg-gray-600 !h-9 !w-9 !p-0 !rounded-lg md:!px-3 md:!w-auto"
                  aria-label="Annuler"
                  title="Annuler"
                  @click="cancelEdit"
                >
                  <span class="inline-flex items-center gap-2 justify-center w-full">
                    <IconGlyph name="x" />
                    <span class="hidden md:inline">Annuler</span>
                  </span>
                </BaseButton>
              </template>

              <BaseButton
                class="!bg-red-600 hover:!bg-red-700 !h-9 !w-9 !p-0 !rounded-lg md:!px-3 md:!w-auto"
                aria-label="Supprimer"
                title="Supprimer"
                @click="askDelete(i)"
              >
                <span class="inline-flex items-center gap-2 justify-center w-full">
                  <IconGlyph name="trash" />
                  <span class="hidden md:inline">Supprimer</span>
                </span>
              </BaseButton>
            </div>
          </td>
        </tr>

        <tr v-if="!filteredItems.length && !store.loading">
          <td colspan="3" class="p-4 text-sm text-gray-500">Aucun ingredient trouve</td>
        </tr>
      </DataTable>
    </div>

    <BaseModal
      :show="showCreateModal"
      panel-class="relative w-full md:w-[520px] bg-white rounded-t-2xl md:rounded-xl shadow-xl p-4 max-h-[calc(100vh-2rem)] overflow-y-auto"
      @close="cancelCreateModal"
    >
      <form class="space-y-4" @submit.prevent="createItem">
        <div class="flex items-center justify-between gap-3">
          <h2 class="text-lg font-semibold">Creer un ingredient</h2>
          <BaseButton
            type="button"
            class="!bg-gray-200 !text-gray-800 !h-9 !w-9 !p-0 !rounded-lg"
            title="Annuler"
            aria-label="Annuler"
            @click="cancelCreateModal"
          >
            <IconGlyph name="x" />
          </BaseButton>
        </div>

        <div class="grid gap-3">
          <BaseInput label="Nom" v-model="form.name" placeholder="ex: Tomates" />
          <BaseInput label="Rayon" v-model="form.aisle" placeholder="ex: Fruits & Legumes" />
        </div>

        <p
          v-if="createNameAlreadyExists"
          class="rounded-md bg-amber-50 border border-amber-200 px-3 py-2 text-sm text-amber-800"
        >
          Cet ingredient existe deja. Utilisez la recherche pour le retrouver avant d'en creer un nouveau.
        </p>

        <p v-if="formError" class="rounded-md bg-red-50 border border-red-200 px-3 py-2 text-sm text-red-700">
          {{ formError }}
        </p>

        <div class="flex justify-end gap-2">
          <BaseButton
            type="button"
            class="!bg-gray-200 !text-gray-800 !h-10 !rounded-lg"
            @click="cancelCreateModal"
          >
            Annuler
          </BaseButton>
          <BaseButton
            type="submit"
            class="!h-10 !rounded-lg"
            :disabled="!form.name.trim() || createNameAlreadyExists"
          >
            Ajouter
          </BaseButton>
        </div>
      </form>
    </BaseModal>

    <BaseModal :show="confirmOpen" @close="cancelDelete">
      <div class="p-1">
        <h2 class="text-lg font-semibold mb-2">Confirmer la suppression</h2>
        <p class="text-sm text-gray-700 mb-4">
          Supprimer "<span class="font-medium">{{ toDelete?.name }}</span>" ?
        </p>
        <p v-if="deleteError" class="mb-4 rounded-md bg-red-50 border border-red-200 px-3 py-2 text-sm text-red-700">
          {{ deleteError }}
        </p>
        <div class="flex justify-end gap-2">
          <BaseButton
            class="!bg-gray-500 hover:!bg-gray-600 !h-9 !w-9 !p-0 !rounded-lg md:!px-3 md:!w-auto"
            aria-label="Annuler"
            title="Annuler"
            @click="cancelDelete"
          >
            <span class="inline-flex items-center gap-2 justify-center w-full">
              <IconGlyph name="x" />
              <span class="hidden md:inline">Annuler</span>
            </span>
          </BaseButton>

          <BaseButton
            class="!bg-red-600 hover:!bg-red-700 !h-9 !w-9 !p-0 !rounded-lg md:!px-3 md:!w-auto"
            aria-label="Supprimer"
            title="Supprimer"
            @click="confirmDelete"
          >
            <span class="inline-flex items-center gap-2 justify-center w-full">
              <IconGlyph name="trash" />
              <span class="hidden md:inline">Supprimer</span>
            </span>
          </BaseButton>
        </div>
      </div>
    </BaseModal>
  </section>
</template>

<style scoped>
@keyframes rowFlashCard {
  0% {
    background-color: #ecfdf5;
  }
  100% {
    background-color: white;
  }
}

.row-flash-card {
  animation: rowFlashCard 1.2s ease-out 1;
}
</style>
