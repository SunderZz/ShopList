export function normalizeToken(raw: unknown): string | null {
  if (raw == null) return null;

  if (typeof raw === 'string' && raw.includes('.') && raw.split('.').length === 3) return raw;

  if (typeof raw === 'string' && raw.trim().startsWith('[')) {
    try {
      const arr = JSON.parse(raw);
      if (Array.isArray(arr) && arr.length === 3 && arr.every(x => typeof x === 'string')) {
        return arr.join('.');
      }
    } catch {}
  }

  if (Array.isArray(raw) && raw.length === 3 && raw.every(x => typeof x === 'string')) {
    return raw.join('.');
  }

  return String(raw);
}
