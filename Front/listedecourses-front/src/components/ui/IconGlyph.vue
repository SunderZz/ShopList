<script setup lang="ts">
type IconName =
  | 'check'
  | 'dish'
  | 'edit'
  | 'ingredient'
  | 'list'
  | 'logout'
  | 'plus'
  | 'trash'
  | 'users'
  | 'x'

const props = defineProps<{
  name: IconName
}>()
</script>

<template>
  <svg
    class="h-4 w-4 shrink-0"
    viewBox="0 0 20 20"
    fill="currentColor"
    aria-hidden="true"
  >
    <path
      v-if="props.name === 'check'"
      fill-rule="evenodd"
      d="M16.707 5.293a1 1 0 010 1.414L8.5 14.914l-3.207-3.207a1 1 0 011.414-1.414L8.5 12.086l6.793-6.793a1 1 0 011.414 0z"
      clip-rule="evenodd"
    />
    <path
      v-else-if="props.name === 'dish'"
      d="M10 2a8 8 0 100 16 8 8 0 000-16zm1 8h5a6 6 0 01-5 5v-5z"
    />
    <path
      v-else-if="props.name === 'edit'"
      d="M13.586 3.586a2 2 0 012.828 2.828l-9.193 9.193a1 1 0 01-.39.242l-3 1a1 1 0 01-1.266-1.266l1-3a1 1 0 01.242-.39l9.193-9.193z"
    />
    <path
      v-else-if="props.name === 'list'"
      d="M6 4h11v2H6V4zM6 9h11v2H6V9zM6 14h11v2H6v-2zM3 4h2v2H3V4zM3 9h2v2H3V9zM3 14h2v2H3v-2z"
    />
    <path
      v-else-if="props.name === 'ingredient'"
      d="M10 2l2.4 5.6 6.1.1-4.7 3.9 1.5 6L10 14.3l-5.3 3.3 1.5-6-4.7-3.9 6.1-.1L10 2z"
    />
    <path
      v-else-if="props.name === 'logout'"
      d="M14 11V9H6V6l-5 4 5 4v-3h8zM18 2h-7a2 2 0 00-2 2v3h2V4h7v12h-7v-3H9v3a2 2 0 002 2h7a2 2 0 002-2V4a2 2 0 00-2-2z"
    />
    <path
      v-else-if="props.name === 'plus'"
      fill-rule="evenodd"
      d="M10 3a1 1 0 011 1v5h5a1 1 0 110 2h-5v5a1 1 0 11-2 0v-5H4a1 1 0 110-2h5V4a1 1 0 011-1z"
      clip-rule="evenodd"
    />
    <path
      v-else-if="props.name === 'trash'"
      fill-rule="evenodd"
      d="M6 8a1 1 0 012 0v7a1 1 0 11-2 0V8zm5-1a1 1 0 00-1 1v7a1 1 0 102 0V8a1 1 0 00-1-1zM4 5h12v2H4V5zm3-2h6a1 1 0 011 1v1H6V4a1 1 0 011-1z"
      clip-rule="evenodd"
    />
    <path
      v-else-if="props.name === 'users'"
      d="M13 9a3 3 0 100-6 3 3 0 000 6zM7 9a3 3 0 100-6 3 3 0 000 6zm0 2c-2.7 0-5 1.3-5 3v2h10v-2c0-1.7-2.3-3-5-3zm6 0c-.4 0-.8 0-1.1.1 1.3.8 2.1 1.8 2.1 2.9v2h4v-2c0-1.7-2.3-3-5-3z"
    />
    <path
      v-else
      fill-rule="evenodd"
      d="M4.293 4.293a1 1 0 011.414 0L10 8.586l4.293-4.293a1 1 0 111.414 1.414L11.414 10l4.293 4.293a1 1 0 01-1.414 1.414L10 11.414l-4.293 4.293a1 1 0 01-1.414-1.414L8.586 10 4.293 5.707a1 1 0 010-1.414z"
      clip-rule="evenodd"
    />
  </svg>
</template>
