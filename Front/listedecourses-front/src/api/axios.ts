import axios from 'axios'
import { useAuthStore } from '@/stores/auth'
import router from '@/router'
import { endpoints } from '@/api/endpoints'

export const api = axios.create({
  baseURL: import.meta.env.DEV
    ? '/api'
    : import.meta.env.VITE_API_BASE_URL ?? 'https://shoplist-33qz.onrender.com/',
  withCredentials: false,
})

api.interceptors.response.use((response) => {
  if (import.meta.env.DEV) {
    const url = response?.config?.url ?? ''
    const isLogin = typeof url === 'string' && url.endsWith(endpoints.auth.login)
    if (isLogin) {
      const token = response?.data?.token

      try {
        sessionStorage.setItem('__token_sample__', JSON.stringify(token))
      } catch {}
    }
  }
  return response
})

api.interceptors.request.use((config) => {
  const token = localStorage.getItem('ldc_token')
  if (token) {
    config.headers = config.headers ?? {}
    config.headers.Authorization = `Bearer ${token}`
  }
  return config
})

let handling = false
api.interceptors.response.use(
  (r) => r,
  async (error) => {
    const status = error?.response?.status
    if ((status === 401 || status === 403) && !handling) {
      handling = true
      try {
        const auth = useAuthStore()
        auth.logout()
        const current = router.currentRoute.value
        const redirect =
          current?.fullPath && current.fullPath !== '/' ? current.fullPath : undefined
        await router.push({ path: '/', query: redirect ? { redirect } : undefined })
      } finally {
        handling = false
      }
    }
    return Promise.reject(error)
  }
)
