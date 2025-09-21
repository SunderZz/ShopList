<script setup lang="ts">
import { withDefaults, defineProps, defineEmits, computed } from 'vue'

type Model = { quantity: number | null; unit: string | null }

const props = withDefaults(defineProps<{
  modelValue?: Model
  units?: string[]
  inputClass?: string
  selectClass?: string
  disabled?: boolean
  placeholderQty?: string
}>(), {
  modelValue: () => ({ quantity: null, unit: null }),
  units: () => ['g', 'kg', 'paquet'],
  inputClass: 'w-full h-10 rounded-lg border px-2 text-sm',
  selectClass: 'w-full h-10 rounded-lg border px-2 text-sm',
  disabled: false,
  placeholderQty: 'Qté',
})

const emit = defineEmits<{ (e:'update:modelValue', v: Model): void }>()
const val = computed<Model>({
  get: () => props.modelValue,
  set: (v) => emit('update:modelValue', v),
})

function onQty(e: Event) {
  const raw = (e.target as HTMLInputElement).value
  const q = raw === '' ? null : Number(raw)
  val.value = { ...val.value, quantity: q }
}
function onUnit(e: Event) {
  const u = (e.target as HTMLSelectElement).value || null
  val.value = { ...val.value, unit: u }
}
</script>

<template>
  <div class="grid grid-cols-12 gap-2 items-center">
    <div class="col-span-6">
      <input
        type="number"
        min="0"
        step="any"
        :class="inputClass"
        :value="val.quantity ?? ''"
        @input="onQty"
        :placeholder="placeholderQty"
        :disabled="disabled"
      />
    </div>
    <div class="col-span-6">
      <select
        :class="selectClass"
        :value="val.unit ?? ''"
        @change="onUnit"
        :disabled="disabled"
      >
        <option value=""></option>
        <option v-for="u in units" :key="u" :value="u">{{ u }}</option>
      </select>
    </div>
  </div>
</template>
