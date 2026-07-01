import { useEffect, useRef, useState } from 'react'
import { companyOf } from './companies'
import { CompanyBadge } from './CompanyBadge'

interface Props {
  symbols: string[]
  value: string
  onChange: (symbol: string) => void
}

export function SymbolSelect({ symbols, value, onChange }: Props) {
  const [open, setOpen] = useState(false)
  const containerRef = useRef<HTMLDivElement>(null)

  useEffect(() => {
    function handlePointerDown(event: MouseEvent) {
      if (containerRef.current && !containerRef.current.contains(event.target as Node)) {
        setOpen(false)
      }
    }
    document.addEventListener('mousedown', handlePointerDown)
    return () => document.removeEventListener('mousedown', handlePointerDown)
  }, [])

  function select(symbol: string) {
    onChange(symbol)
    setOpen(false)
  }

  return (
    <div className="symbol-select" ref={containerRef}>
      <button
        type="button"
        className="symbol-trigger"
        onClick={() => setOpen((previous) => !previous)}
        aria-haspopup="listbox"
        aria-expanded={open}
      >
        <span className="symbol-label">
          <CompanyBadge symbol={value} />
          <span className="symbol-ticker">{value}</span>
          <span className="symbol-company">{companyOf(value)?.name}</span>
        </span>
        <span className={open ? 'symbol-caret open' : 'symbol-caret'} aria-hidden>▾</span>
      </button>

      {open && (
        <ul className="symbol-list" role="listbox">
          {symbols.map((symbol) => (
            <li
              key={symbol}
              role="option"
              aria-selected={symbol === value}
              className={symbol === value ? 'symbol-item active' : 'symbol-item'}
              onClick={() => select(symbol)}
            >
              <CompanyBadge symbol={symbol} />
              <span className="symbol-ticker">{symbol}</span>
              <span className="symbol-company">{companyOf(symbol)?.name}</span>
            </li>
          ))}
        </ul>
      )}
    </div>
  )
}
