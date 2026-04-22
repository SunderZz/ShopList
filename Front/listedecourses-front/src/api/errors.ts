import { isAxiosError } from 'axios'

type ApiErrorBody = {
  message?: unknown
  title?: unknown
}

export function getApiErrorMessage(error: unknown, fallback: string) {
  if (!isAxiosError<unknown>(error)) return fallback

  const data = error.response?.data
  if (isApiErrorBody(data)) {
    if (typeof data.message === 'string' && data.message.trim()) return data.message
    if (typeof data.title === 'string' && data.title.trim()) return data.title
  }

  if (typeof data === 'string' && data.trim()) return data

  return fallback
}

function isApiErrorBody(value: unknown): value is ApiErrorBody {
  return typeof value === 'object' && value !== null
}
