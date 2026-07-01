import { companyOf } from './companies'

export function CompanyBadge({ symbol, size = 24 }: { symbol: string; size?: number }) {
  const company = companyOf(symbol)
  const color = company?.color ?? '#5d6c7b'
  const initials = company?.initials ?? symbol.slice(0, 2)

  return (
    <span
      className="company-badge"
      style={{ width: size, height: size, background: color, fontSize: size * 0.42 }}
      aria-hidden
    >
      {initials}
    </span>
  )
}
