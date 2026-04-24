import { defineStore } from 'pinia'
import { ref } from 'vue'
import { api } from '@/api/axios'
import { endpoints } from '@/api/endpoints'
import type { User, UserCreateRequest } from '@/api/types'

export const useUsersStore = defineStore('users', () => {
  const items = ref<User[]>([])
  const loading = ref(false)
  const hasLoaded = ref(false)
  let loadPromise: Promise<void> | null = null
  let loadVersion = 0

  async function fetchAll() {
    if (loadPromise) return loadPromise

    loading.value = true
    const currentVersion = ++loadVersion
    loadPromise = (async () => {
      try {
        const { data } = await api.get<User[]>(endpoints.users)
        if (currentVersion !== loadVersion) return
        items.value = data
        hasLoaded.value = true
      } finally {
        if (currentVersion === loadVersion) {
          loading.value = false
          loadPromise = null
        }
      }
    })()

    return loadPromise
  }

  async function ensureLoaded() {
    if (hasLoaded.value) return
    await fetchAll()
  }

  async function createOne(user: UserCreateRequest) {
    const { data } = await api.post<User>(endpoints.users, user)
    items.value.push(data)
    hasLoaded.value = true
  }

  async function updateOne(id: string, patch: Partial<User>) {
    const { data } = await api.put<User>(`${endpoints.users}/${id}`, patch)
    const idx = items.value.findIndex((u) => u.id === id)
    if (idx !== -1) items.value[idx] = data
    hasLoaded.value = true
  }

  async function deleteOne(id: string) {
    await api.delete(`${endpoints.users}/${id}`)
    items.value = items.value.filter((u) => u.id !== id)
    hasLoaded.value = true
  }

  function clear() {
    loadVersion++
    items.value = []
    loading.value = false
    hasLoaded.value = false
    loadPromise = null
  }

  return { items, loading, fetchAll, ensureLoaded, createOne, updateOne, deleteOne, clear }
})
