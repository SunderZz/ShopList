<script setup lang="ts">
import { withDefaults, defineProps, defineModel } from 'vue'

const props = withDefaults(defineProps<{
  id?: string
  label?: string
  type?: string
  placeholder?: string
  error?: string
  autocomplete?: string
}>(), {
  type: 'text',
  placeholder: '',
  autocomplete: 'off',
})

const model = defineModel<string | number | undefined>({ default: '' })
</script>

<template>
  <label class="block space-y-1">
    <span v-if="props.label" class="text-sm text-gray-700">{{ props.label }}</span>

    <input
      :id="props.id"
      :type="props.type"
      :aria-invalid="props.error ? 'true' : undefined"
      :aria-describedby="props.error ? `${props.id ?? 'input'}-error` : undefined"
      :placeholder="props.placeholder"
      :autocomplete="props.autocomplete"
      v-model="model"
      class="w-full h-11 md:h-10 rounded-lg border border-slate-300 px-3 py-2 text-base md:text-sm outline-none focus:ring-2 focus:ring-emerald-400 focus:border-emerald-400 placeholder:text-slate-400"
    />

    <span
      v-if="props.error"
      :id="`${props.id ?? 'input'}-error`"
      class="text-xs text-red-600"
    >
      {{ props.error }}
    </span>
  </label>
</template>
