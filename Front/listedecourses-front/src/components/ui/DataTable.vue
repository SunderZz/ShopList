<script setup lang="ts">
import { withDefaults, defineProps } from 'vue'

const props = withDefaults(defineProps<{
  loading?: boolean
  emptyText?: string
}>(), {
  loading: false,
  emptyText: 'Aucune donnée',
})
</script>

<template>
  <div class="overflow-x-auto rounded-lg border bg-white">
    <table
      class="min-w-full text-base md:text-sm"
      :aria-busy="props.loading ? 'true' : 'false'"
    >
      <thead class="bg-gray-50 text-left text-gray-700">
        <tr>
          <slot name="head" />
        </tr>
      </thead>

      <tbody>
        <tr v-if="props.loading">
          <td class="p-4 text-center text-gray-500 h-touch" colspan="100">Chargement…</td>
        </tr>

        <template v-else>
          <slot />
        </template>
      </tbody>
    </table>

    <div v-if="!props.loading && !$slots.default" class="p-4 text-center text-gray-500">
      <slot name="empty">
        {{ props.emptyText }}
      </slot>
    </div>
  </div>
</template>

<style>
@keyframes rowFlash3s {
  0%   { background-color: rgba(16,185,129,0.45); }
  20%  { background-color: rgba(16,185,129,0.35); }
  60%  { background-color: rgba(16,185,129,0.18); }
  100% { background-color: transparent; }
}
tr.row-flash > td {
  animation: rowFlash3s 3s ease-out both;
  will-change: background-color;
}
@media (prefers-reduced-motion: reduce) {
  tr.row-flash > td {
    animation: none;
    background-color: rgba(16,185,129,0.2);
  }
}
</style>
