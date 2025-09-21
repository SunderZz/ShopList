<script setup lang="ts">
import { onMounted, ref, nextTick } from 'vue'
import { useIngredientsStore } from '@/stores/ingredients'
import BaseInput from '@/components/ui/BaseInput.vue'
import BaseButton from '@/components/ui/BaseButton.vue'
import DataTable from '@/components/ui/DataTable.vue'
import BaseModal from '@/components/ui/BaseModal.vue'
import type { Ingredient } from '@/api/types'
import { useFlash } from '@/composables/useFlash'

const store = useIngredientsStore()

const form = ref({ name: '', aisle: '' })

const editingId = ref<string | null>(null)
const editForm = ref<{ name: string; aisle: string }>({ name: '', aisle: '' })

const { flashSet, flashRow } = useFlash()

onMounted(() => {
  store.fetchAll()
})

async function createItem() {
  if (!form.value.name) return
  const prevLen = store.items.length
  await store.createOne(form.value)
  await nextTick()
  const created = store.items.length > prevLen ? store.items[store.items.length - 1] : null
  flashRow(created?.id)
  form.value = { name: '', aisle: '' }
}

function startEdit(i: { id: string; name: string; aisle: string | null }) {
  editingId.value = i.id
  editForm.value = { name: i.name ?? '', aisle: i.aisle ?? '' }
}

function cancelEdit() {
  editingId.value = null
}

async function confirmEdit(id: string) {
  await store.updateOne(id, {
    name: editForm.value.name,
    aisle: editForm.value.aisle,
  })
  editingId.value = null
  await nextTick()
  flashRow(id)
}

const confirmOpen = ref(false)
const toDelete = ref<Ingredient | null>(null)

function askDelete(i: Ingredient) {
  toDelete.value = i
  confirmOpen.value = true
}

async function confirmDelete() {
  if (!toDelete.value) return
  await store.deleteOne(toDelete.value.id)
  confirmOpen.value = false
  toDelete.value = null
}

function cancelDelete() {
  confirmOpen.value = false
  toDelete.value = null
}
</script>

<template>
  <section class="container mx-auto px-4 safe-pt safe-pb py-8 space-y-6">
    <div class="flex items-center justify-between gap-3">
      <h1 class="text-3xl md:text-2xl font-semibold tracking-tight">Ingrédients</h1>
      <span
        class="hidden md:inline-flex items-center gap-2 text-xs text-gray-600 bg-gray-50 border border-gray-200 rounded-full px-3 py-1"
      >
        {{ store.items.length }} ingrédient(s)
      </span>
    </div>

    <form
      class="grid gap-3 items-end bg-white border border-gray-200 rounded-xl p-4 shadow-sm md:grid-cols-4"
      @submit.prevent="createItem"
    >
      <BaseInput label="Nom" v-model="form.name" placeholder="ex: Tomates" />
      <BaseInput label="Rayon" v-model="form.aisle" placeholder="ex: Fruits & Légumes" />
      <div class="md:col-span-1">
        <BaseButton
          type="submit"
          class="!h-11 !rounded-xl !px-5 !shadow-sm hover:!shadow-md w-full md:w-auto"
          :disabled="!form.name"
        >
          Ajouter
        </BaseButton>
      </div>
    </form>

    <div class="bg-white border border-gray-200 rounded-xl shadow-sm overflow-hidden">
      <DataTable :loading="store.loading">
        <template #head>
          <th class="p-3 text-left bg-gray-50 text-gray-700 font-medium">Nom</th>
          <th class="p-3 text-left bg-gray-50 text-gray-700 font-medium">Rayon</th>
          <th class="p-3 text-right bg-gray-50 text-gray-700 font-medium">Actions</th>
        </template>

        <tr
          v-for="i in store.items"
          :key="i.id"
          class="border-t hover:bg-gray-50/60 transition"
          :class="flashSet[i.id] ? 'row-flash' : ''"
        >
          <td class="p-3 align-middle">
            <template v-if="editingId === i.id">
              <BaseInput v-model="editForm.name" />
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
              {{ i.aisle || '—' }}
            </template>
          </td>

          <td class="p-3 align-middle">
            <div class="flex justify-end gap-1 md:gap-2">
              <BaseButton
                v-if="editingId !== i.id"
                title="Modifier"
                aria-label="Modifier"
                class="!bg-white !text-gray-700 border hover:bg-gray-50 !h-9 !w-9 !p-0 !rounded-lg md:!px-3 md:!w-auto"
                @click="startEdit(i as any)"
              >
                <span class="inline-flex items-center gap-2 justify-center w-full">
                  <svg
                    xmlns="http://www.w3.org/2000/svg"
                    class="h-4 w-4"
                    viewBox="0 0 20 20"
                    fill="currentColor"
                    aria-hidden="true"
                  >
                    <path
                      d="M13.586 3.586a2 2 0 012.828 2.828l-9.193 9.193a1 1 0 01-.39.242l-3 1a1 1 0 01-1.266-1.266l1-3a1 1 0 01.242-.39l9.193-9.193z"
                    />
                  </svg>
                  <span class="hidden md:inline">Modifier</span>
                </span>
              </BaseButton>

              <template v-if="editingId === i.id">
                <BaseButton
                  class="!bg-emerald-600 hover:!bg-emerald-700 !h-9 !w-9 !p-0 !rounded-lg md:!px-3 md:!w-auto"
                  :disabled="!editForm.name"
                  aria-label="Confirmer"
                  title="Confirmer"
                  @click="confirmEdit(i.id)"
                >
                  <span class="inline-flex items-center gap-2 justify-center w-full">
                    <svg
                      xmlns="http://www.w3.org/2000/svg"
                      class="h-4 w-4"
                      viewBox="0 0 20 20"
                      fill="currentColor"
                      aria-hidden="true"
                    >
                      <path
                        fill-rule="evenodd"
                        d="M16.707 5.293a1 1 0 010 1.414L8.5 14.914l-3.207-3.207a1 1 0 011.414-1.414L8.5 12.086l6.793-6.793a1 1 0 011.414 0z"
                        clip-rule="evenodd"
                      />
                    </svg>
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
                    <svg
                      xmlns="http://www.w3.org/2000/svg"
                      class="h-4 w-4"
                      viewBox="0 0 20 20"
                      fill="currentColor"
                      aria-hidden="true"
                    >
                      <path
                        fill-rule="evenodd"
                        d="M4.293 4.293a1 1 0 011.414 0L10 8.586l4.293-4.293a1 1 0 111.414 1.414L11.414 10l4.293 4.293a1 1 0 01-1.414 1.414L10 11.414l-4.293 4.293a1 1 0 01-1.414-1.414L8.586 10 4.293 5.707a1 1 0 010-1.414z"
                        clip-rule="evenodd"
                      />
                    </svg>
                    <span class="hidden md:inline">Annuler</span>
                  </span>
                </BaseButton>
              </template>

              <BaseButton
                class="!bg-red-600 hover:!bg-red-700 !h-9 !w-9 !p-0 !rounded-lg md:!px-3 md:!w-auto"
                aria-label="Supprimer"
                title="Supprimer"
                @click="askDelete(i as any)"
              >
                <span class="inline-flex items-center gap-2 justify-center w-full">
                  <svg
                    xmlns="http://www.w3.org/2000/svg"
                    class="h-4 w-4"
                    viewBox="0 0 20 20"
                    fill="currentColor"
                    aria-hidden="true"
                  >
                    <path
                      fill-rule="evenodd"
                      d="M6 8a1 1 0 012 0v7a1 1 0 11-2 0V8zm5-1a1 1 0 00-1 1v7a1 1 0 102 0V8a1 1 0 00-1-1zM4 5h12v2H4V5zm3-2h6a1 1 0 011 1v1H6V4a1 1 0 011-1z"
                      clip-rule="evenodd"
                    />
                  </svg>
                  <span class="hidden md:inline">Supprimer</span>
                </span>
              </BaseButton>
            </div>
          </td>
        </tr>
      </DataTable>
    </div>

    <BaseModal :show="confirmOpen" @close="cancelDelete">
      <div class="p-1">
        <h2 class="text-lg font-semibold mb-2">Confirmer la suppression</h2>
        <p class="text-sm text-gray-700 mb-4">
          Supprimer "<span class="font-medium">{{ toDelete?.name }}</span
          >" ?
        </p>
        <div class="flex justify-end gap-1 md:gap-2">
          <BaseButton
            class="!bg-gray-500 hover:!bg-gray-600 !h-9 !w-9 !p-0 !rounded-lg md:!px-3 md:!w-auto"
            aria-label="Annuler"
            title="Annuler"
            @click="cancelDelete"
          >
            <span class="inline-flex items-center gap-2 justify-center w-full">
              <svg
                xmlns="http://www.w3.org/2000/svg"
                class="h-4 w-4"
                viewBox="0 0 20 20"
                fill="currentColor"
                aria-hidden="true"
              >
                <path
                  fill-rule="evenodd"
                  d="M4.293 4.293a1 1 0 011.414 0L10 8.586l4.293-4.293a1 1 0 111.414 1.414L11.414 10l4.293 4.293a1 1 0 01-1.414 1.414L10 11.414l-4.293 4.293a1 1 0 01-1.414-1.414L8.586 10 4.293 5.707a1 1 0 010-1.414z"
                  clip-rule="evenodd"
                />
              </svg>
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
              <svg
                xmlns="http://www.w3.org/2000/svg"
                class="h-4 w-4"
                viewBox="0 0 20 20"
                fill="currentColor"
                aria-hidden="true"
              >
                <path
                  fill-rule="evenodd"
                  d="M6 8a1 1 0 012 0v7a1 1 0 11-2 0V8zm5-1a1 1 0 00-1 1v7a1 1 0 102 0V8a1 1 0 00-1-1zM4 5h12v2H4V5zm3-2h6a1 1 0 011 1v1H6V4a1 1 0 011-1z"
                  clip-rule="evenodd"
                />
              </svg>
              <span class="hidden md:inline">Supprimer</span>
            </span>
          </BaseButton>
        </div>
      </div>
    </BaseModal>
  </section>
</template>
