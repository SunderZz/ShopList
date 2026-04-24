import { shallowRef } from 'vue'
import { defineStore } from 'pinia'
import { api } from '@/api/axios'
import type { WithId } from './_helpers'
import { upsertById, removeById } from './_helpers'

type CreatePayload<T> = Omit<T, 'id'>
type UpdatePayload<T> = Partial<T>

/**
 * Store
 * @param name   
 * @param baseUrl 
 */
export function createCrudStore<T extends WithId>(name: string, baseUrl: string) {
  return defineStore(name, () => {
    const items = shallowRef<T[]>([])       
    const loading = shallowRef(false)
    const hasLoaded = shallowRef(false)
    let loadPromise: Promise<void> | null = null
    let loadVersion = 0

    async function fetchAll() {
      if (loadPromise) return loadPromise

      loading.value = true
      const currentVersion = ++loadVersion
      loadPromise = (async () => {
        try {
          const { data } = await api.get<T[]>(baseUrl)
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

    async function createOne(payload: CreatePayload<T>) {
      const { data } = await api.post<T>(baseUrl, payload)
      items.value = [...items.value, data]
      hasLoaded.value = true
      return data
    }

    async function updateOne(id: string, patch: UpdatePayload<T>) {
      const { data } = await api.put<T>(`${baseUrl}/${id}`, patch)
      items.value = upsertById(items.value, data)    
      hasLoaded.value = true
      return data
    }

    async function deleteOne(id: string) {
      await api.delete(`${baseUrl}/${id}`)
      items.value = removeById(items.value, id)
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
}
