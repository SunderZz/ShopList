export type QuantityLike = {
  quantity: number | null
  unit: string | null
}

export const PIECE_UNIT = 'unit\u00E9'

export const UNIT_OPTIONS = ['g', 'kg', 'ml', 'cl', 'l', 'paquet', PIECE_UNIT] as const

const quantityFormatter = new Intl.NumberFormat('fr-FR', {
  maximumFractionDigits: 3,
})

export function formatQuantity(value: number | null | undefined) {
  if (typeof value !== 'number' || !Number.isFinite(value)) return '—'
  return quantityFormatter.format(value)
}

export function formatQuantityLine(line: QuantityLike) {
  if (line.quantity === null) return line.unit ?? '—'
  return line.unit ? `${formatQuantity(line.quantity)} ${line.unit}` : formatQuantity(line.quantity)
}

export function formatQuantitySummary(
  quantities: QuantityLike[] | null | undefined,
  fallback?: QuantityLike | null
) {
  const source =
    quantities && quantities.length
      ? quantities
      : fallback
        ? [fallback]
        : []

  return source.length ? source.map(formatQuantityLine).join(' + ') : '—'
}
