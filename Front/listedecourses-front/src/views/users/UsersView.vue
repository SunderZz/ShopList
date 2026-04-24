<script setup lang="ts">
import { nextTick, onMounted, ref } from 'vue'
import BaseInput from '@/components/ui/BaseInput.vue'
import BaseButton from '@/components/ui/BaseButton.vue'
import BaseModal from '@/components/ui/BaseModal.vue'
import DataTable from '@/components/ui/DataTable.vue'
import IconGlyph from '@/components/ui/IconGlyph.vue'
import { useUsersStore } from '@/stores/users'
import type { User } from '@/api/types'
import { getApiErrorMessage } from '@/api/errors'
import { useFlash } from '@/composables/useFlash'

const users = useUsersStore()

const createForm = ref<{ email: string; pseudo: string; password: string; isSuperUser: boolean }>({
  email: '',
  pseudo: '',
  password: '',
  isSuperUser: false,
})
const createError = ref<string | null>(null)

const editingId = ref<string | null>(null)
const editForm = ref<{ email: string; pseudo: string; isSuperUser: boolean }>({
  email: '',
  pseudo: '',
  isSuperUser: false,
})
const editError = ref<string | null>(null)

const confirmDeleteOpen = ref(false)
const deleteTarget = ref<User | null>(null)
const deleteError = ref<string | null>(null)

const { flashSet, flashRow } = useFlash()

onMounted(() => {
  void users.ensureLoaded()
})

async function createUser() {
  if (!createForm.value.email || !createForm.value.pseudo || !createForm.value.password) return
  createError.value = null

  try {
    const prevLen = users.items.length
    await users.createOne({
      email: createForm.value.email,
      pseudo: createForm.value.pseudo,
      password: createForm.value.password,
      isSuperUser: createForm.value.isSuperUser,
    })
    await nextTick()
    const created = users.items.length > prevLen ? users.items[users.items.length - 1] : null
    flashRow(created?.id)
    createForm.value = { email: '', pseudo: '', password: '', isSuperUser: false }
  } catch (error: unknown) {
    createError.value = getApiErrorMessage(error, 'Impossible de creer cet utilisateur.')
  }
}

function startEdit(u: User) {
  editingId.value = u.id
  editForm.value = {
    email: u.email ?? '',
    pseudo: u.pseudo ?? '',
    isSuperUser: !!u.isSuperUser,
  }
  editError.value = null
}

function cancelEdit() {
  editingId.value = null
  editError.value = null
}

async function confirmEdit(id: string) {
  editError.value = null

  try {
    await users.updateOne(id, {
      email: editForm.value.email,
      pseudo: editForm.value.pseudo,
      isSuperUser: editForm.value.isSuperUser,
    })
    editingId.value = null
    await nextTick()
    flashRow(id)
  } catch (error: unknown) {
    editError.value = getApiErrorMessage(error, 'Impossible de modifier cet utilisateur.')
  }
}

function askDelete(u: User) {
  deleteTarget.value = u
  deleteError.value = null
  confirmDeleteOpen.value = true
}

function closeDeleteModal() {
  confirmDeleteOpen.value = false
  deleteTarget.value = null
  deleteError.value = null
}

async function confirmDelete() {
  if (!deleteTarget.value) return
  deleteError.value = null

  try {
    await users.deleteOne(deleteTarget.value.id)
    closeDeleteModal()
  } catch (error: unknown) {
    deleteError.value = getApiErrorMessage(error, 'Impossible de supprimer cet utilisateur.')
  }
}
</script>

<template>
  <section class="container mx-auto px-4 safe-pt safe-pb py-8 space-y-6">
    <div class="flex items-center justify-between gap-3">
      <div>
        <h1 class="text-3xl md:text-2xl font-semibold tracking-tight">Utilisateurs</h1>
        <p class="mt-1 text-sm text-gray-500 md:hidden">{{ users.items.length }} utilisateur(s)</p>
      </div>
      <span class="hidden md:inline-flex items-center gap-2 text-xs text-gray-600 bg-gray-50 border border-gray-200 rounded-full px-3 py-1">
        {{ users.items.length }} utilisateur(s)
      </span>
    </div>

    <form
      class="grid gap-3 items-end bg-white border border-gray-200 rounded-xl p-4 shadow-sm md:grid-cols-5"
      @submit.prevent="createUser"
    >
      <BaseInput label="Email" type="email" v-model="createForm.email" required />
      <BaseInput label="Pseudo" v-model="createForm.pseudo" required />
      <BaseInput label="Mot de passe" type="password" v-model="createForm.password" required />
      <div class="flex items-center gap-2 h-11">
        <input
          id="isSuperCreate"
          type="checkbox"
          class="h-5 w-5 rounded border-gray-300 text-emerald-600 focus:ring-emerald-500"
          v-model="createForm.isSuperUser"
        />
        <label for="isSuperCreate" class="text-sm">SuperUser</label>
      </div>
      <div class="md:col-span-1">
        <BaseButton
          type="submit"
          :disabled="!createForm.email || !createForm.pseudo || !createForm.password"
          class="!h-11 !rounded-xl !px-5 !shadow-sm hover:!shadow-md w-full md:w-auto"
        >
          Creer
        </BaseButton>
      </div>
      <p v-if="createError" class="md:col-span-5 rounded-md bg-red-50 border border-red-200 px-3 py-2 text-sm text-red-700">
        {{ createError }}
      </p>
    </form>

    <div class="md:hidden space-y-3">
      <div v-if="users.loading" class="bg-white border border-gray-200 rounded-xl p-4 text-sm text-gray-500">
        Chargement...
      </div>

      <template v-else>
        <div
          v-for="u in users.items"
          :key="u.id"
          class="bg-white border border-gray-200 rounded-xl p-4 shadow-sm transition ring-0"
          :class="flashSet[u.id] ? 'row-flash-card' : ''"
        >
          <div v-if="editingId !== u.id" class="flex items-start justify-between gap-3">
            <div class="min-w-0">
              <div class="font-medium truncate">{{ u.email }}</div>
              <div class="text-sm text-gray-700 truncate">{{ u.pseudo }}</div>
              <span
                class="inline-flex items-center rounded-full px-2 py-0.5 text-xs font-medium mt-2"
                :class="u.isSuperUser ? 'bg-emerald-100 text-emerald-700' : 'bg-gray-100 text-gray-600'"
              >
                {{ u.isSuperUser ? 'SuperUser' : 'Standard' }}
              </span>
            </div>
            <div class="flex flex-col gap-2 shrink-0">
              <BaseButton
                title="Modifier"
                aria-label="Modifier"
                class="!bg-white !text-gray-700 border hover:bg-gray-50 !h-9 !w-9 !p-0 !rounded-lg"
                @click="startEdit(u)"
              >
                <IconGlyph name="edit" />
              </BaseButton>
              <BaseButton
                title="Supprimer"
                aria-label="Supprimer"
                class="!bg-red-600 hover:!bg-red-700 !h-9 !w-9 !p-0 !rounded-lg"
                @click="askDelete(u)"
              >
                <IconGlyph name="trash" />
              </BaseButton>
            </div>
          </div>

          <div v-else class="space-y-3">
            <BaseInput label="Email" v-model="editForm.email" type="email" />
            <BaseInput label="Pseudo" v-model="editForm.pseudo" />
            <div class="flex items-center gap-2">
              <input
                :id="`isSuperEdit_m_${u.id}`"
                type="checkbox"
                class="h-5 w-5 rounded border-gray-300 text-emerald-600 focus:ring-emerald-500"
                v-model="editForm.isSuperUser"
              />
              <label :for="`isSuperEdit_m_${u.id}`" class="text-sm">SuperUser</label>
            </div>
            <p v-if="editError" class="rounded-md bg-red-50 border border-red-200 px-3 py-2 text-sm text-red-700">
              {{ editError }}
            </p>
            <div class="flex justify-end gap-2">
              <BaseButton class="!bg-gray-500 hover:!bg-gray-600 !h-10 !rounded-lg" @click="cancelEdit">
                Annuler
              </BaseButton>
              <BaseButton
                class="!bg-emerald-600 hover:!bg-emerald-700 !h-10 !rounded-lg"
                :disabled="!editForm.email || !editForm.pseudo"
                @click="confirmEdit(u.id)"
              >
                Confirmer
              </BaseButton>
            </div>
          </div>
        </div>

        <div
          v-if="!users.items.length"
          class="bg-white border border-gray-200 rounded-xl p-4 text-sm text-gray-500"
        >
          Aucun utilisateur
        </div>
      </template>
    </div>

    <div class="hidden md:block bg-white border border-gray-200 rounded-xl shadow-sm overflow-hidden">
      <DataTable :loading="users.loading">
        <template #head>
          <th class="p-3 text-left bg-gray-50 text-gray-700 font-medium">Email</th>
          <th class="p-3 text-left bg-gray-50 text-gray-700 font-medium">Pseudo</th>
          <th class="p-3 text-left bg-gray-50 text-gray-700 font-medium">SuperUser</th>
          <th class="p-3 text-right bg-gray-50 text-gray-700 font-medium">Actions</th>
        </template>

        <tr
          v-for="u in users.items"
          :key="u.id"
          class="border-t hover:bg-gray-50/60 transition"
          :class="flashSet[u.id] ? 'row-flash' : ''"
        >
          <td class="p-3 align-middle">
            <template v-if="editingId === u.id">
              <BaseInput v-model="editForm.email" type="email" />
            </template>
            <template v-else>
              {{ u.email }}
            </template>
          </td>

          <td class="p-3 align-middle">
            <template v-if="editingId === u.id">
              <BaseInput v-model="editForm.pseudo" />
              <p v-if="editError" class="mt-1 text-xs text-red-600">{{ editError }}</p>
            </template>
            <template v-else>
              {{ u.pseudo }}
            </template>
          </td>

          <td class="p-3 align-middle">
            <template v-if="editingId === u.id">
              <div class="flex items-center gap-2">
                <input
                  :id="`isSuperEdit_${u.id}`"
                  type="checkbox"
                  class="h-4 w-4 rounded border-gray-300 text-emerald-600 focus:ring-emerald-500"
                  v-model="editForm.isSuperUser"
                />
                <label :for="`isSuperEdit_${u.id}`" class="text-sm">SuperUser</label>
              </div>
            </template>
            <template v-else>
              <span
                class="inline-flex items-center rounded-full px-2 py-0.5 text-xs font-medium"
                :class="u.isSuperUser ? 'bg-emerald-100 text-emerald-700' : 'bg-gray-100 text-gray-600'"
              >
                {{ u.isSuperUser ? 'Oui' : 'Non' }}
              </span>
            </template>
          </td>

          <td class="p-3 align-middle">
            <div class="flex justify-end gap-2">
              <BaseButton
                v-if="editingId !== u.id"
                title="Modifier"
                aria-label="Modifier"
                class="!bg-white !text-gray-700 border hover:bg-gray-50 !h-9 !px-3 !rounded-lg"
                @click="startEdit(u)"
              >
                <span class="inline-flex items-center gap-2">
                  <IconGlyph name="edit" />
                  Modifier
                </span>
              </BaseButton>

              <template v-if="editingId === u.id">
                <BaseButton
                  class="!bg-emerald-600 hover:!bg-emerald-700 !h-9 !px-3 !rounded-lg"
                  :disabled="!editForm.email || !editForm.pseudo"
                  @click="confirmEdit(u.id)"
                >
                  Confirmer
                </BaseButton>
                <BaseButton class="!bg-gray-500 hover:!bg-gray-600 !h-9 !px-3 !rounded-lg" @click="cancelEdit">
                  Annuler
                </BaseButton>
              </template>

              <BaseButton class="!bg-red-600 hover:!bg-red-700 !h-9 !px-3 !rounded-lg" @click="askDelete(u)">
                Supprimer
              </BaseButton>
            </div>
          </td>
        </tr>

        <tr v-if="!users.items.length && !users.loading">
          <td colspan="4" class="p-4 text-sm text-gray-500">Aucun utilisateur</td>
        </tr>
      </DataTable>
    </div>

    <BaseModal :show="confirmDeleteOpen" @close="closeDeleteModal">
      <h3 class="text-lg font-semibold mb-2">Supprimer l'utilisateur ?</h3>
      <p class="text-sm text-gray-700 mb-4">
        Confirmer la suppression de
        <span class="font-medium">{{ deleteTarget?.pseudo || deleteTarget?.email }}</span> ?
      </p>
      <p v-if="deleteError" class="mb-4 rounded-md bg-red-50 border border-red-200 px-3 py-2 text-sm text-red-700">
        {{ deleteError }}
      </p>
      <div class="flex justify-end gap-2">
        <BaseButton
          class="!bg-gray-200 !text-gray-800 !h-9 !px-3 !rounded-lg"
          @click="closeDeleteModal"
        >
          Annuler
        </BaseButton>
        <BaseButton class="!bg-red-600 !h-9 !px-3 !rounded-lg" @click="confirmDelete">
          Supprimer
        </BaseButton>
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
