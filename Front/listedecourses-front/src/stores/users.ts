import { defineStore } from 'pinia'
import { ref } from 'vue'
import { api } from '@/api/axios'
import { endpoints } from '@/api/endpoints'
import type { User, UserCreateRequest } from '@/api/types'

export const useUsersStore = defineStore('users', () => {
  const items = ref<User[]>([])
  const loading = ref(false)

  async function fetchAll() {
    loading.value = true
    try {
      const { data } = await api.get<User[]>(endpoints.users)
      items.value = data
    } finally {
      loading.value = false
    }
  }

  async function createOne(user: UserCreateRequest) {
    const { data } = await api.post<User>(endpoints.users, user)
    items.value.push(data)
  }

  async function updateOne(id: string, patch: Partial<User>) {
    const { data } = await api.put<User>(`${endpoints.users}/${id}`, patch)
    const idx = items.value.findIndex((u) => u.id === id)
    if (idx !== -1) items.value[idx] = data
  }

  async function deleteOne(id: string) {
    await api.delete(`${endpoints.users}/${id}`)
    items.value = items.value.filter((u) => u.id !== id)
  }

  return { items, loading, fetchAll, createOne, updateOne, deleteOne }
})
