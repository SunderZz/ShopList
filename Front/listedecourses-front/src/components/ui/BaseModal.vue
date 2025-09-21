<script setup lang="ts">
import { withDefaults, defineProps, defineEmits } from 'vue'

const props = withDefaults(defineProps<{
  show: boolean
  panelClass?: string
  closeOnBackdrop?: boolean
  role?: string
}>(), {
  panelClass: 'relative w-full md:w-[440px] bg-white rounded-t-2xl md:rounded-xl shadow-xl p-4',
  closeOnBackdrop: true,
  role: 'dialog',
})

const emit = defineEmits<{ (e:'close'): void }>()
</script>

<template>
  <div
    v-if="show"
    class="fixed inset-0 z-50 flex items-end md:items-center justify-center"
    aria-modal="true"
    :role="role"
  >
    <div class="absolute inset-0 bg-black/40" @click="props.closeOnBackdrop && emit('close')"></div>
    <div :class="panelClass">
      <slot />
    </div>
  </div>
</template>
