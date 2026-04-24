<script setup lang="ts">
import { computed } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useAuthStore } from '@/stores/auth'
import IconGlyph from '@/components/ui/IconGlyph.vue'

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
            <IconGlyph name="logout" />
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
            <IconGlyph name="list" class="!h-5 !w-5 mb-0.5" />
            <span>Listes</span>
          </RouterLink>
        </li>
        <li>
          <RouterLink
            to="/dishes"
            class="flex flex-col items-center justify-center h-16 text-xs"
            :class="activeClass('/dishes')"
          >
            <IconGlyph name="dish" class="!h-5 !w-5 mb-0.5" />
            <span>Plats</span>
          </RouterLink>
        </li>
        <li>
          <RouterLink
            to="/ingredients"
            class="flex flex-col items-center justify-center h-16 text-xs"
            :class="activeClass('/ingredients')"
          >
            <IconGlyph name="ingredient" class="!h-5 !w-5 mb-0.5" />
            <span>Ingrédients</span>
          </RouterLink>
        </li>
        <li v-if="isSuper">
          <RouterLink
            to="/users"
            class="flex flex-col items-center justify-center h-16 text-xs"
            :class="activeClass('/users')"
          >
            <IconGlyph name="users" class="!h-5 !w-5 mb-0.5" />
            <span>Utilisateurs</span>
          </RouterLink>
        </li>
      </ul>
    </nav>

    <footer class="border-t py-6 text-center text-sm text-gray-500 bg-white">
      © {{ new Date().getFullYear() }} ListeDeCourses — by
      <a
        class="underline decoration-dotted hover:text-gray-700"
        href="https://gilles.needemand.com/"
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
