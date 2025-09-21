<script setup lang="ts">
import { computed, ref, onMounted, watch } from 'vue'
import { useAuthStore } from '@/stores/auth'
import { useUsersStore } from '@/stores/users'
import BaseInput from '@/components/ui/BaseInput.vue'
import BaseButton from '@/components/ui/BaseButton.vue'

const auth = useAuthStore()
const users = useUsersStore()

const isLogged = computed(() => auth.isAuthenticated)
const showLogin = ref(false)
const mode = ref<'login' | 'register'>('login')

const email = ref('')
const pseudo = ref('')
const password = ref('')
const showPwd = ref(false)
const loading = ref(false)

const loginError = ref<string | null>(null)
const emailError = ref<string | null>(null)
const passwordError = ref<string | null>(null)

const EMAIL_RE = /^[^\s@]+@[^\s@]+\.[^\s@]+$/
const PASS_RE = /^(?=.*[A-Z])(?=.*\d).{7,}$/

onMounted(() => {
  showLogin.value = !isLogged.value
})

watch(isLogged, (v) => {
  if (v) showLogin.value = false
})

function resetErrors() {
  loginError.value = null
  emailError.value = null
  passwordError.value = null
}
function closeModal() {
  showLogin.value = false
  resetErrors()
}

watch([email, password, pseudo, mode, showLogin], () => {
  resetErrors()
})

async function submit() {
  resetErrors()

  if (mode.value === 'login') {
    if (!email.value.trim() || !password.value.trim()) {
      loginError.value = 'Email et mot de passe requis'
      return
    }
  } else {
    const e = email.value.trim()
    const p = password.value
    const ps = pseudo.value.trim()

    if (!e || !ps || !p) {
      if (!e) emailError.value = 'Email requis'
      if (!p) passwordError.value = 'Mot de passe requis'
      return
    }
    if (!EMAIL_RE.test(e)) {
      emailError.value = 'Format d’email invalide'
      return
    }
    if (!PASS_RE.test(p)) {
      passwordError.value =
        'Le mot de passe doit faire au moins 7 caractères, contenir 1 majuscule et 1 chiffre'
      return
    }
  }

  loading.value = true
  try {
    if (mode.value === 'login') {
      await auth.login({ email: email.value.trim(), password: password.value })
      closeModal()
    } else {
      await users.createOne({
        email: email.value.trim(),
        pseudo: pseudo.value.trim(),
        password: password.value,
        isSuperUser: false,
      })
      email.value = ''
      pseudo.value = ''
      password.value = ''
      closeModal()
    }
  } catch (e: any) {
    if (mode.value === 'login') {
      if (e?.response?.status === 401) {
        loginError.value = 'Email ou mot de passe invalide'
      } else {
        loginError.value = 'Une erreur est survenue'
      }
    } else {
      const status = e?.response?.status
      if (status === 409) {
        emailError.value = 'Email déjà utilisé'
      } else if (status === 400) {
        emailError.value = 'Données invalides'
      } else {
        emailError.value = 'Une erreur est survenue'
      }
    }
  } finally {
    loading.value = false
  }
}
</script>

<template>
  <section class="container mx-auto px-4 py-10 safe-pt safe-pb min-h-[60vh] flex items-center">
    <div class="relative isolate w-full">
      <div
        class="pointer-events-none absolute -top-24 -right-10 h-56 w-56 rounded-full blur-3xl opacity-20 bg-emerald-400"
      ></div>
      <div
        class="pointer-events-none absolute -bottom-24 -left-10 h-64 w-64 rounded-full blur-3xl opacity-20 bg-amber-300"
      ></div>

      <div
        class="max-w-3xl mx-auto text-center space-y-5 transition-opacity duration-300"
        :class="showLogin ? 'pointer-events-none select-none opacity-0' : 'opacity-100'"
      >
        <h1 class="text-4xl sm:text-5xl font-extrabold tracking-tight">
          Bienvenue sur <span class="text-emerald-600">ListeDeCourses</span>
        </h1>
        <p class="text-base sm:text-lg text-gray-600">
          Gérez vos ingrédients, vos plats et composez vos listes en quelques clics.
        </p>

        <div v-if="!isLogged" class="pt-6 flex flex-wrap items-center justify-center gap-3">
          <BaseButton
            class="!h-11 !px-5 !rounded-xl !shadow-sm hover:!shadow-md focus-visible:ring-2 focus-visible:ring-emerald-500/60"
            @click=";(mode = 'login'), (showLogin = true), resetErrors()"
          >
            Se connecter
          </BaseButton>

          <BaseButton
            class="!bg-emerald-600 hover:!bg-emerald-700 !h-11 !px-5 !rounded-xl !shadow-sm focus-visible:ring-2 focus-visible:ring-emerald-600/60"
            @click=";(mode = 'register'), (showLogin = true), resetErrors()"
          >
            Créer un compte
          </BaseButton>
        </div>

        <div v-else class="pt-4 flex items-center justify-center">
          <span
            class="inline-flex items-center gap-2 text-sm text-emerald-700 bg-emerald-50 border border-emerald-200 px-3 py-1.5 rounded-full"
          >
            <span class="h-2.5 w-2.5 rounded-full bg-emerald-500 animate-pulse"></span>
            Connecté — utilisez le menu pour naviguer
          </span>
        </div>
      </div>
    </div>

    <Transition name="modal" appear>
      <div
        v-if="showLogin"
        class="fixed inset-0 z-50"
        role="dialog"
        aria-modal="true"
        aria-labelledby="login-title"
        tabindex="-1"
        @keydown.esc="closeModal"
      >
        <div class="absolute inset-0 bg-black/50 backdrop-blur-sm" @click="closeModal"></div>

        <div
          class="relative z-10 mt-auto w-full bg-white text-black shadow-2xl flex flex-col rounded-t-2xl safe-pb sm:my-16 sm:rounded-2xl sm:max-w-md sm:w-full sm:mx-auto sm:h-auto transition-all duration-200"
        >
          <div class="mx-auto mt-2 h-1.5 w-10 rounded-full bg-gray-300 sm:hidden"></div>

          <div class="px-5 py-4 border-b border-gray-200 flex items-center justify-between">
            <h2 id="login-title" class="font-semibold text-lg">
              {{ mode === 'login' ? 'Connexion' : 'Créer un compte' }}
            </h2>
            <button
              class="inline-flex items-center justify-center h-9 w-9 rounded-lg border border-transparent hover:bg-gray-100 active:bg-gray-200 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-gray-400"
              @click="closeModal"
              title="Fermer"
            >
              <svg
                class="h-5 w-5 text-gray-700"
                viewBox="0 0 20 20"
                fill="currentColor"
                aria-hidden="true"
              >
                <path
                  fill-rule="evenodd"
                  d="M4.293 4.293a1 1 0 011.414 0L10 8.586l4.293-4.293a1 1 0
                111.414 1.414L11.414 10l4.293 4.293a1 1 0 01-1.414 1.414L10 11.414l-4.293
                4.293a1 1 0 01-1.414-1.414L8.586 10 4.293 5.707a1 1 0 010-1.414z"
                  clip-rule="evenodd"
                />
              </svg>
              <span class="sr-only">Fermer</span>
            </button>
          </div>

          <form class="p-5 space-y-4" @submit.prevent="submit">
            <div class="space-y-1.5">
              <BaseInput
                label="Email"
                type="email"
                v-model="email"
                autocomplete="username"
                placeholder="ex: jean@mail.com"
                autofocus
              />
              <p v-if="emailError" class="text-xs text-red-600">{{ emailError }}</p>
            </div>

            <div v-if="mode === 'register'" class="space-y-1.5">
              <BaseInput
                label="Pseudo"
                v-model="pseudo"
                autocomplete="nickname"
                placeholder="ex: Jean"
              />
            </div>

            <div class="space-y-1.5 relative">
              <BaseInput
                :label="mode === 'login' ? 'Mot de passe' : 'Choisir un mot de passe'"
                :type="showPwd ? 'text' : 'password'"
                v-model="password"
                :autocomplete="mode === 'login' ? 'current-password' : 'new-password'"
                placeholder="••••••••"
              />
              <button
                type="button"
                class="absolute right-2 top-9 text-gray-500 hover:text-gray-700 p-1 rounded focus-visible:ring-2 focus-visible:ring-emerald-500"
                @click="showPwd = !showPwd"
              >
                <span class="text-xs">{{ showPwd ? 'Masquer' : 'Afficher' }}</span>
                <span class="sr-only">Basculer la visibilité du mot de passe</span>
              </button>
              <p v-if="passwordError" class="text-xs text-red-600">{{ passwordError }}</p>
              <p v-else-if="mode === 'register'" class="text-xs text-gray-500">
                Min 7 caractères, 1 majuscule, 1 chiffre.
              </p>
            </div>

            <p v-if="loginError" class="text-sm text-red-600">{{ loginError }}</p>

            <div class="pt-2">
              <BaseButton
                type="submit"
                :disabled="loading"
                class="!h-11 !rounded-xl !px-5 !shadow-sm hover:!shadow-md"
              >
                <span v-if="!loading">
                  {{ mode === 'login' ? 'Se connecter' : 'Créer un compte' }}
                </span>
                <span v-else class="inline-flex items-center gap-2">
                  <svg
                    class="h-4 w-4 animate-spin"
                    viewBox="0 0 24 24"
                    fill="none"
                    aria-hidden="true"
                  >
                    <circle
                      cx="12"
                      cy="12"
                      r="10"
                      stroke="currentColor"
                      stroke-opacity=".25"
                      stroke-width="4"
                    />
                    <path d="M22 12a10 10 0 0 1-10 10" stroke="currentColor" stroke-width="4" />
                  </svg>
                  {{ mode === 'login' ? 'Connexion…' : 'Création…' }}
                </span>
              </BaseButton>
            </div>
          </form>
        </div>
      </div>
    </Transition>
  </section>
</template>

<style scoped>
.modal-enter-from .relative {
  transform: scale(0.98);
  opacity: 0;
}
.modal-enter-active .relative,
.modal-leave-active .relative {
  transition: all 0.2s ease;
}
.modal-leave-to .relative {
  transform: scale(0.98);
  opacity: 0;
}
</style>
