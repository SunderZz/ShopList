import { defineStore } from 'pinia'
import { ref, computed } from 'vue'
import { api } from '@/api/axios'
import { endpoints } from '@/api/endpoints'
import type { LoginRequest, LoginResponse, User } from '@/api/types'
import { isExpired, getExpiryMs } from '@/api/jwt'

const TOKEN_KEY = 'ldc_token'
let logoutTimer: number | null = null

function safeReloadOnce(tag = 'auth-expired', ttlMs = 3000) {
  const key = `__reload_once__:${tag}`
  const last = Number(sessionStorage.getItem(key) || 0)
  const now = Date.now()
  if (now - last < ttlMs) return
  sessionStorage.setItem(key, String(now))
  window.location.reload()
}

export const useAuthStore = defineStore('auth', () => {
  const token = ref<string | null>(null)
  const profile = ref<User | null>(null)
  const isAuthenticated = computed(() => !!token.value && !isExpired(token.value))

  function clearLogoutTimer() {
    if (logoutTimer !== null) {
      window.clearTimeout(logoutTimer)
      logoutTimer = null
    }
  }

  function scheduleAutoLogout(t: string | null) {
    clearLogoutTimer()
    if (!t) return
    const expMs = getExpiryMs(t)
    if (!expMs) return
    const delay = Math.max(0, expMs - Date.now())
    logoutTimer = window.setTimeout(() => {
      logout({ reload: true })
    }, delay)
  }

  function setToken(t: string | null) {
    token.value = t

    if (t) {
      localStorage.setItem(TOKEN_KEY, t)
      api.defaults.headers = api.defaults.headers ?? {}
      ;(api.defaults.headers as any).Authorization = `Bearer ${t}`
      scheduleAutoLogout(t)

      if (import.meta.env.DEV) {
        const looksLikeJwt = typeof t === 'string' && t.split('.').length === 3
        if (!looksLikeJwt) {
          console.warn('[auth] Token inattendu (non-JWT)')
        }
      }
    } else {
      localStorage.removeItem(TOKEN_KEY)
      if (api.defaults.headers) delete (api.defaults.headers as any).Authorization
      clearLogoutTimer()
    }
  }

  async function loadProfile() {
    if (!token.value) return
    if (isExpired(token.value)) {
      logout({ reload: true })
      return
    }

    try {
      const { data: user } = await api.get<User>(endpoints.auth.profile)
      profile.value = user
    } catch {
      logout({ reload: true })
    }
  }

  async function login(payload: LoginRequest) {
    const { data } = await api.post<LoginResponse>(endpoints.auth.login, payload)
    setToken(data.token)
    profile.value = data.user
  }

 
  function logout(opts: { reload?: boolean } = {}) {
    setToken(null)
    profile.value = null
    if (opts.reload) safeReloadOnce('auth-expired')
  }

  function initFromLocalStorage() {
    const raw = localStorage.getItem(TOKEN_KEY)
    if (!raw) {
      logout() 
      return
    }
    if (isExpired(raw)) {
      logout() 
      return
    }
    setToken(raw)
  }

  return {
    token,
    profile,
    isAuthenticated,
    login,
    logout,
    loadProfile,
    setToken,
    initFromLocalStorage,
  }
})
