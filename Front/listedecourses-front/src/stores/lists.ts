import { defineStore } from 'pinia'
import { ref } from 'vue'
import { api } from '@/api/axios'
import { endpoints } from '@/api/endpoints'
import type { ShoppingList, ListCreateRequest, ListUpdateRequest } from '@/api/types'
import { upsertById, removeById } from './_helpers'

export const useListsStore = defineStore('lists', () => {
  const items = ref<ShoppingList[]>([])
  const current = ref<ShoppingList | null>(null)
  const loading = ref(false)

  async function fetchAll() {
    loading.value = true
    try {
      const { data } = await api.get<ShoppingList[]>(endpoints.lists)
      items.value = data
    } finally {
      loading.value = false
    }
  }

  async function fetchById(id: string) {
    const { data } = await api.get<ShoppingList>(`${endpoints.lists}/${id}`)
    current.value = data
    items.value = upsertById(items.value, data)
  }

  async function createOne(payload: ListCreateRequest) {
    const { data } = await api.post<ShoppingList>(endpoints.lists, payload)
    items.value = [data, ...items.value]
    return data
  }

  async function updateOne(id: string, payload: ListUpdateRequest) {
    const { data } = await api.put<ShoppingList>(`${endpoints.lists}/${id}`, payload)
    items.value = upsertById(items.value, data)
    if (current.value?.id === id) current.value = data
    return data
  }

  async function deleteOne(id: string) {
    await api.delete(`${endpoints.lists}/${id}`)
    items.value = removeById(items.value, id)
    if (current.value?.id === id) current.value = null
  }

  return { items, current, loading, fetchAll, fetchById, createOne, updateOne, deleteOne }
})
