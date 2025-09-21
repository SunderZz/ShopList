export type WithId = { id: string }

export function upsertById<T extends WithId>(arr: readonly T[], entity: T): T[] {
  const idx = arr.findIndex(x => x.id === entity.id)
  if (idx === -1) return [...arr, entity]
  const copy = arr.slice() as T[]
  copy[idx] = entity
  return copy
}

export function removeById<T extends WithId>(arr: readonly T[], id: string): T[] {
  return (arr as T[]).filter(x => x.id !== id)
}
