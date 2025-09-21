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

    async function fetchAll() {
      loading.value = true
      try {
        const { data } = await api.get<T[]>(baseUrl)
        items.value = data
      } finally {
        loading.value = false
      }
    }

    async function createOne(payload: CreatePayload<T>) {
      const { data } = await api.post<T>(baseUrl, payload)
      items.value = [...items.value, data]
      return data
    }

    async function updateOne(id: string, patch: UpdatePayload<T>) {
      const { data } = await api.put<T>(`${baseUrl}/${id}`, patch)
      items.value = upsertById(items.value, data)    
      return data
    }

    async function deleteOne(id: string) {
      await api.delete(`${baseUrl}/${id}`)
      items.value = removeById(items.value, id)
    }

    return { items, loading, fetchAll, createOne, updateOne, deleteOne }
  })
}
