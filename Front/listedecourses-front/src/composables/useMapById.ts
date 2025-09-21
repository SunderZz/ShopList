import { computed } from 'vue'

export function useMapById<T extends { id: string }>(listRef: () => readonly T[]) {
  const byId = computed<Record<string, T>>(
    () => Object.fromEntries(listRef().map(i => [i.id, i]))
  )
  return { byId }
}
