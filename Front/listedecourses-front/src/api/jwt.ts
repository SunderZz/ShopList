function b64UrlDecode(input: string): string {
  input = input.replace(/-/g, '+').replace(/_/g, '/');
  const pad = input.length % 4;
  if (pad) input += '='.repeat(4 - pad);
  return atob(input);
}

export function parseJwt(token: string): { exp?: number } | null {
  try {
    const payload = token.split('.')[1];
    const json = b64UrlDecode(payload);
    return JSON.parse(json);
  } catch {
    return null;
  }
}

export function isExpired(token: string, skewSeconds = 0): boolean {
  const payload = parseJwt(token);
  if (!payload?.exp) return true;
  const now = Math.floor(Date.now() / 1000);
  return now >= (payload.exp - skewSeconds);
}

export function getExpiryMs(token: string): number | null {
  const payload = parseJwt(token);
  return payload?.exp ? payload.exp * 1000 : null;
}
