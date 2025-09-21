<script setup lang="ts">
import { computed, ref, onMounted, onUnmounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useAuthStore } from '@/stores/auth'

const auth = useAuthStore()
const route = useRoute()
const router = useRouter()

const isLoggedIn = computed(() => auth.isAuthenticated)
const isSuper = computed(() => auth.profile?.isSuperUser === true)

function logout() {
  auth.logout()
  router.push({ path: '/' })
}

function isActive(prefix: string) {
  return route.path.startsWith(prefix)
}
function activeClass(prefix: string) {
  return isActive(prefix)
    ? 'text-gray-900 font-medium bg-gray-100'
    : 'text-gray-600 hover:text-gray-900 hover:bg-gray-50'
}

function applyNoZoomViewport() {
  const content = 'width=device-width, initial-scale=1, maximum-scale=1, user-scalable=no'
  let meta = document.querySelector('meta[name="viewport"]') as HTMLMetaElement | null
  if (!meta) {
    meta = document.createElement('meta')
    meta.setAttribute('name', 'viewport')
    document.head.appendChild(meta)
  }
  meta.setAttribute('content', content)
}
const onGestureStart = (e: Event) => e.preventDefault()

onMounted(() => {
  applyNoZoomViewport()
  document.addEventListener('gesturestart', onGestureStart)
})
onUnmounted(() => {
  document.removeEventListener('gesturestart', onGestureStart)
})
</script>

<template>
  <div class="min-h-screen flex flex-col bg-gray-50">
    <a
      href="#main"
      class="sr-only focus:not-sr-only focus:absolute focus:top-2 focus:left-2 bg-emerald-600 text-white px-3 py-2 rounded-lg"
    >
      Aller au contenu
    </a>

    <header class="sticky top-0 z-40 border-b bg-white/95 backdrop-blur safe-pt">
      <nav class="container mx-auto px-4 py-3 flex gap-3 items-center">
        <RouterLink
          class="font-bold tracking-tight text-lg"
          to="/"
          aria-label="Accueil ListeDeCourses"
        >
          ListeDeCourses
        </RouterLink>

        <div class="flex-1" />

        <div v-if="isLoggedIn" class="hidden md:flex items-center gap-1">
          <RouterLink
            v-if="isSuper"
            class="px-3 py-2 rounded-lg transition"
            :class="activeClass('/users')"
            to="/users"
          >
            Utilisateurs
          </RouterLink>
          <RouterLink
            class="px-3 py-2 rounded-lg transition"
            :class="activeClass('/ingredients')"
            to="/ingredients"
          >
            Ingrédients
          </RouterLink>
          <RouterLink
            class="px-3 py-2 rounded-lg transition"
            :class="activeClass('/dishes')"
            to="/dishes"
          >
            Plats
          </RouterLink>
          <RouterLink
            class="px-3 py-2 rounded-lg transition"
            :class="activeClass('/lists')"
            to="/lists"
          >
            Listes
          </RouterLink>

          <button
            class="ml-2 inline-flex items-center gap-2 px-3 py-2 rounded-lg bg-gray-900 text-white hover:bg-gray-800 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-gray-400"
            @click="logout"
          >
            <svg class="h-4 w-4" viewBox="0 0 24 24" fill="currentColor" aria-hidden="true">
              <path
                d="M16 13v-2H7V8l-5 4 5 4v-3h9zM20 3h-8a2 2 0 00-2 2v4h2V5h8v14h-8v-4h-2v4a2 2 0 002 2h8a2 2 0 002-2V5a2 2 0 00-2-2z"
              />
            </svg>
            Se déconnecter
          </button>
        </div>

        <div class="md:hidden flex items-center gap-2">
          <button
            v-if="isLoggedIn"
            class="px-3 py-2 rounded-lg bg-gray-900 text-white text-sm focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-gray-400"
            @click="logout"
            aria-label="Se déconnecter"
          >
            Déconnexion
          </button>
        </div>
      </nav>
    </header>

    <main
      id="main"
      class="container mx-auto px-4 py-6 flex-1"
      :class="isLoggedIn ? 'pb-24 md:pb-6' : ''"
    >
      <RouterView v-slot="{ Component }">
        <Transition name="fade" mode="out-in">
          <component :is="Component" :key="route.fullPath" />
        </Transition>
      </RouterView>
    </main>

    <nav
      v-if="isLoggedIn"
      class="md:hidden fixed bottom-0 inset-x-0 border-t bg-white/95 backdrop-blur safe-pb"
      aria-label="Navigation principale"
    >
      <ul class="grid" :class="isSuper ? 'grid-cols-4' : 'grid-cols-3'">
        <li>
          <RouterLink
            to="/lists"
            class="flex flex-col items-center justify-center h-16 text-xs"
            :class="activeClass('/lists')"
            aria-current="page"
          >
            <svg class="h-5 w-5 mb-0.5" viewBox="0 0 24 24" fill="currentColor" aria-hidden="true">
              <path d="M7 5h14v2H7zM7 11h14v2H7zM7 17h14v2H7zM3 5h2v2H3zM3 11h2v2H3zM3 17h2v2H3z" />
            </svg>
            <span>Listes</span>
          </RouterLink>
        </li>
        <li>
          <RouterLink
            to="/dishes"
            class="flex flex-col items-center justify-center h-16 text-xs"
            :class="activeClass('/dishes')"
          >
            <svg class="h-5 w-5 mb-0.5" viewBox="0 0 24 24" fill="currentColor" aria-hidden="true">
              <path d="M12 3a9 9 0 100 18 9 9 0 000-18zm1 9h6a7 7 0 01-6 6v-6z" />
            </svg>
            <span>Plats</span>
          </RouterLink>
        </li>
        <li>
          <RouterLink
            to="/ingredients"
            class="flex flex-col items-center justify-center h-16 text-xs"
            :class="activeClass('/ingredients')"
          >
            <svg class="h-5 w-5 mb-0.5" viewBox="0 0 24 24" fill="currentColor" aria-hidden="true">
              <path d="M12 2l3 7h7l-5.5 4.5L18 22l-6-4-6 4 1.5-8.5L2 9h7z" />
            </svg>
            <span>Ingrédients</span>
          </RouterLink>
        </li>
        <li v-if="isSuper">
          <RouterLink
            to="/users"
            class="flex flex-col items-center justify-center h-16 text-xs"
            :class="activeClass('/users')"
          >
            <svg class="h-5 w-5 mb-0.5" viewBox="0 0 24 24" fill="currentColor" aria-hidden="true">
              <path
                d="M16 11c1.66 0 2.99-1.34 2.99-3S17.66 5 16 5s-3 1.34-3 3 1.34 3 3 3zm-8 0c1.66 0 2.99-1.34 2.99-3S9.66 5 8 5 5 6.34 5 8s1.34 3 3 3zm0 2c-2.33 0-7 1.17-7 3.5V20h14v-3.5C18 14.17 13.33 13 11 13zm8 0c-.29 0-.62.02-.97.05C19.16 14.2 22 15.45 22 17.5V20h-5v-3.5c0-.57-.13-1.1-.36-1.58.44-.06.86-.09 1.36-.09z"
              />
            </svg>
            <span>Utilisateurs</span>
          </RouterLink>
        </li>
      </ul>
    </nav>

    <footer class="border-t py-6 text-center text-sm text-gray-500 bg-white">
      © {{ new Date().getFullYear() }} ListeDeCourses — by
      <a
        class="underline decoration-dotted hover:text-gray-700"
        href="https://www.google.fr/index.html"
        target="_blank"
        rel="noopener noreferrer"
      >
        SunderZz
      </a>
    </footer>
  </div>
</template>

<style scoped>
.fade-enter-active,
.fade-leave-active {
  transition: opacity 0.15s ease;
}
.fade-enter-from,
.fade-leave-to {
  opacity: 0;
}
</style>
