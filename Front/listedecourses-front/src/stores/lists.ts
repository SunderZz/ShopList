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
  const hasLoaded = ref(false)
  let loadPromise: Promise<void> | null = null
  let loadVersion = 0

  async function fetchAll() {
    if (loadPromise) return loadPromise

    loading.value = true
    const currentVersion = ++loadVersion
    loadPromise = (async () => {
      try {
        const { data } = await api.get<ShoppingList[]>(endpoints.lists)
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

  async function fetchById(id: string) {
    const { data } = await api.get<ShoppingList>(`${endpoints.lists}/${id}`)
    current.value = data
    items.value = upsertById(items.value, data)
    hasLoaded.value = true
  }

  async function createOne(payload: ListCreateRequest) {
    const { data } = await api.post<ShoppingList>(endpoints.lists, payload)
    items.value = [data, ...items.value]
    hasLoaded.value = true
    return data
  }

  async function updateOne(id: string, payload: ListUpdateRequest) {
    const { data } = await api.put<ShoppingList>(`${endpoints.lists}/${id}`, payload)
    items.value = upsertById(items.value, data)
    if (current.value?.id === id) current.value = data
    hasLoaded.value = true
    return data
  }

  async function patchChecked(listId: string, ingredientId: string, checked: boolean) {
    const { data } = await api.patch<ShoppingList>(
      `${endpoints.lists}/${listId}/items/${ingredientId}/checked`,
      { checked }
    )
    items.value = upsertById(items.value, data)
    if (current.value?.id === listId) current.value = data
    hasLoaded.value = true
    return data
  }

  async function deleteOne(id: string) {
    await api.delete(`${endpoints.lists}/${id}`)
    items.value = removeById(items.value, id)
    if (current.value?.id === id) current.value = null
    hasLoaded.value = true
  }

  function clear() {
    loadVersion++
    items.value = []
    current.value = null
    loading.value = false
    hasLoaded.value = false
    loadPromise = null
  }

  return {
    items,
    current,
    loading,
    fetchAll,
    ensureLoaded,
    fetchById,
    createOne,
    updateOne,
    patchChecked,
    deleteOne,
    clear,
  }
})
