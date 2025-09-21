import { ref } from 'vue'

export function useFlash() {
  const flashSet = ref<Record<string, boolean>>({})

  function flashRow(id?: string | null, ms = 3000) {
    if (!id) return
    flashSet.value = { ...flashSet.value, [id]: true }
    setTimeout(() => {
      const copy = { ...flashSet.value }
      delete copy[id]
      flashSet.value = copy
    }, ms)
  }

  return { flashSet, flashRow }
}
