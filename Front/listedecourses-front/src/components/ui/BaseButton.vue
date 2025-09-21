<script setup lang="ts">
import { withDefaults, defineProps } from 'vue'

const props = withDefaults(defineProps<{
  type?: 'button' | 'submit' | 'reset'
  disabled?: boolean
}>(), {
  type: 'button',
  disabled: false,
})
</script>

<template>
  <button
    :type="props.type"
    :disabled="props.disabled"
    class="inline-flex w-full sm:w-auto items-center justify-center h-11 rounded-lg px-4 text-base font-medium bg-gray-900 text-white hover:bg-black/90 active:opacity-90 disabled:opacity-50 disabled:cursor-not-allowed focus-ring tap-transparent"
  >
    <slot />
  </button>
</template>
