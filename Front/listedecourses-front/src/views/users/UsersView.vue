<script setup lang="ts">
import { onMounted, ref, nextTick } from 'vue'
import BaseInput from '@/components/ui/BaseInput.vue'
import BaseButton from '@/components/ui/BaseButton.vue'
import DataTable from '@/components/ui/DataTable.vue'
import { useUsersStore } from '@/stores/users'
import type { User } from '@/api/types'
import { useFlash } from '@/composables/useFlash'

const users = useUsersStore()

const createForm = ref<{ email: string; pseudo: string; password: string; isSuperUser: boolean }>({
  email: '',
  pseudo: '',
  password: '',
  isSuperUser: false,
})

const editingId = ref<string | null>(null)
const editForm = ref<{ email: string; pseudo: string; isSuperUser: boolean }>({
  email: '',
  pseudo: '',
  isSuperUser: false,
})

const { flashSet, flashRow } = useFlash()

onMounted(() => {
  users.fetchAll()
})

async function createUser() {
  if (!createForm.value.email || !createForm.value.pseudo || !createForm.value.password) return
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
}

function startEdit(u: User) {
  editingId.value = u.id
  editForm.value = {
    email: u.email ?? '',
    pseudo: u.pseudo ?? '',
    isSuperUser: !!u.isSuperUser,
  }
}

function cancelEdit() {
  editingId.value = null
}

async function confirmEdit(id: string) {
  await users.updateOne(id, {
    email: editForm.value.email,
    pseudo: editForm.value.pseudo,
    isSuperUser: editForm.value.isSuperUser,
  })
  editingId.value = null
  await nextTick()
  flashRow(id)
}
</script>

<template>
  <section class="container mx-auto px-4 safe-pt safe-pb py-8 space-y-6">
    <div class="flex items-center justify-between gap-3">
      <h1 class="text-3xl md:text-2xl font-semibold tracking-tight">Utilisateurs</h1>
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
          Créer
        </BaseButton>
      </div>
    </form>

    <div class="md:hidden space-y-3">
      <div
        v-for="u in users.items"
        :key="u.id"
        class="bg-white border border-gray-200 rounded-xl p-4 shadow-sm transition ring-0"
        :class="flashSet[u.id] ? 'row-flash' : ''"
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
              class="!bg-white !text-gray-700 border hover:bg-gray-50 !h-9 !px-3 !rounded-lg"
              @click="startEdit(u)"
            >
              Modifier
            </BaseButton>
            <BaseButton class="!bg-red-600 hover:!bg-red-700 !h-9 !px-3 !rounded-lg" @click="users.deleteOne(u.id)">
              Supprimer
            </BaseButton>
          </div>
        </div>

        <div v-else class="space-y-3">
          <BaseInput label="Email" v-model="editForm.email" type="email" />
          <BaseInput label="Pseudo" v-model="editForm.pseudo" />
          <div class="flex items-center gap-2">
            <input
              id="isSuperEdit_m"
              type="checkbox"
              class="h-5 w-5 rounded border-gray-300 text-emerald-600 focus:ring-emerald-500"
              v-model="editForm.isSuperUser"
            />
            <label for="isSuperEdit_m" class="text-sm">SuperUser</label>
          </div>
          <div class="flex justify-end gap-2">
            <BaseButton
              class="!bg-emerald-600 hover:!bg-emerald-700 !h-10 !rounded-lg"
              :disabled="!editForm.email || !editForm.pseudo"
              @click="confirmEdit(u.id)"
            >
              Confirmer
            </BaseButton>
            <BaseButton class="!bg-gray-500 hover:!bg-gray-600 !h-10 !rounded-lg" @click="cancelEdit">
              Annuler
            </BaseButton>
          </div>
        </div>
      </div>
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
            </template>
            <template v-else>
              {{ u.pseudo }}
            </template>
          </td>

          <td class="p-3 align-middle">
            <template v-if="editingId === u.id">
              <div class="flex items-center gap-2">
                <input
                  id="isSuperEdit"
                  type="checkbox"
                  class="h-4 w-4 rounded border-gray-300 text-emerald-600 focus:ring-emerald-500"
                  v-model="editForm.isSuperUser"
                />
                <label for="isSuperEdit" class="text-sm">SuperUser</label>
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
                class="!bg-white !text-gray-700 border hover:bg-gray-50 !h-9 !px-3 !rounded-lg"
                @click="startEdit(u)"
              >
                <span class="inline-flex items-center gap-2">
                  <svg xmlns="http://www.w3.org/2000/svg" class="h-4 w-4" viewBox="0 0 20 20" fill="currentColor" aria-hidden="true">
                    <path
                      d="M13.586 3.586a2 2 0 012.828 2.828l-9.193 9.193a1 1 0 01-.39.242l-3 1a1 1 0 01-1.266-1.266l1-3a1 1 0 01.242-.39l9.193-9.193z"
                    />
                  </svg>
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

              <BaseButton class="!bg-red-600 hover:!bg-red-700 !h-9 !px-3 !rounded-lg" @click="users.deleteOne(u.id)">
                Supprimer
              </BaseButton>
            </div>
          </td>
        </tr>
      </DataTable>
    </div>
  </section>
</template>
