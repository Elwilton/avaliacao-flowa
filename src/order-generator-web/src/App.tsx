import { useCallback, useEffect, useMemo, useState } from 'react'
import {
  fetchHealth,
  fetchMeta,
  fetchPortfolio,
  submitOrder,
  Outcome,
  type Meta,
  type OrderResult,
  type Side,
} from './api'
import { ResultPanel } from './ResultPanel'
import { SymbolSelect } from './SymbolSelect'
import { PortfolioPanel, type SymbolState } from './PortfolioPanel'
import flowaLogo from './assets/flowa-logo.svg'
import './App.css'

const FALLBACK_META: Meta = {
  symbols: ['PETR4', 'VALE3', 'VIIA4'],
  sides: ['Buy', 'Sell'],
  maxQuantityExclusive: 100_000,
  maxPriceExclusive: 1_000,
  priceTick: 0.01,
  exposureLimitPerSymbol: 100_000_000,
}

function validate(
  meta: Meta,
  symbol: string,
  quantity: string,
  price: string,
): string[] {
  const errors: string[] = []

  if (!meta.symbols.includes(symbol)) errors.push('Selecione um símbolo válido.')

  const qty = Number(quantity)
  if (!Number.isInteger(qty) || qty <= 0 || qty >= meta.maxQuantityExclusive) {
    errors.push(`Quantidade deve ser um inteiro positivo menor que ${meta.maxQuantityExclusive.toLocaleString('pt-BR')}.`)
  }

  const prc = Number(price)
  if (!(prc > 0) || prc >= meta.maxPriceExclusive) {
    errors.push(`Preço deve ser positivo e menor que ${meta.maxPriceExclusive.toLocaleString('pt-BR')}.`)
  } else if (Math.round(prc * 100) !== Number((prc * 100).toFixed(4))) {
    errors.push('Preço deve ser múltiplo de 0,01.')
  }

  return errors
}

export default function App() {
  const [meta, setMeta] = useState<Meta>(FALLBACK_META)
  const [sessionReady, setSessionReady] = useState<boolean | null>(null)

  const [symbol, setSymbol] = useState('PETR4')
  const [side, setSide] = useState<Side>('Buy')
  const [quantity, setQuantity] = useState('1000')
  const [price, setPrice] = useState('30.00')

  const [submitting, setSubmitting] = useState(false)
  const [result, setResult] = useState<OrderResult | null>(null)
  const [serverErrors, setServerErrors] = useState<string[]>([])
  const [positions, setPositions] = useState<Record<string, SymbolState>>({})

  const refreshPortfolio = useCallback(() => {
    fetchPortfolio()
      .then((entries) => {
        const next: Record<string, SymbolState> = {}
        for (const entry of entries) {
          next[entry.symbol] = { exposure: entry.exposure, position: entry.position }
        }
        setPositions(next)
      })
      .catch(() => {})
  }, [])

  useEffect(() => {
    fetchMeta().then(setMeta).catch(() => setMeta(FALLBACK_META))
    refreshPortfolio()
  }, [refreshPortfolio])

  useEffect(() => {
    let active = true
    const tick = () =>
      fetchHealth()
        .then((h) => active && setSessionReady(h.fixSessionReady))
        .catch(() => active && setSessionReady(false))
    tick()
    const id = setInterval(tick, 4000)
    return () => {
      active = false
      clearInterval(id)
    }
  }, [])

  const clientErrors = useMemo(
    () => validate(meta, symbol, quantity, price),
    [meta, symbol, quantity, price],
  )

  const allErrors = serverErrors.length ? serverErrors : clientErrors
  const canSubmit = clientErrors.length === 0 && !submitting

  async function onSubmit(e: React.FormEvent) {
    e.preventDefault()
    setServerErrors([])
    setResult(null)
    if (clientErrors.length) return

    setSubmitting(true)
    try {
      const resp = await submitOrder({
        symbol,
        side,
        quantity: Number(quantity),
        price: Number(price),
      })

      if (resp.problem) {
        setServerErrors(resp.problem.errors?.order ?? ['Ordem inválida.'])
      } else if (resp.result) {
        setResult(resp.result)
        refreshPortfolio()
      }
    } catch {
      setResult({
        outcome: Outcome.Error,
        clOrdId: '—',
        text: 'Não foi possível contatar a API do OrderGenerator.',
        receivedAtUtc: new Date().toISOString(),
      })
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <div className="page">
      <header className="header">
        <img className="brand-logo" src={flowaLogo} alt="Flowa" />
        <h1>OrderGenerator</h1>
        <p className="subtitle">Envio de ordens FIX 4.4 ao OrderAccumulator</p>
        <SessionBadge ready={sessionReady} />
      </header>

      <main className="layout">
        <form className="card form" onSubmit={onSubmit}>
          <label className="field">
            <span>Símbolo</span>
            <SymbolSelect symbols={meta.symbols} value={symbol} onChange={setSymbol} />
          </label>

          <label className="field">
            <span>Lado</span>
            <div className="side-toggle">
              <button
                type="button"
                className={side === 'Buy' ? 'buy active' : 'buy'}
                onClick={() => setSide('Buy')}
              >
                Compra
              </button>
              <button
                type="button"
                className={side === 'Sell' ? 'sell active' : 'sell'}
                onClick={() => setSide('Sell')}
              >
                Venda
              </button>
            </div>
          </label>

          <label className="field">
            <span>Quantidade</span>
            <input
              type="number"
              min={1}
              max={meta.maxQuantityExclusive - 1}
              step={1}
              value={quantity}
              onChange={(e) => setQuantity(e.target.value)}
            />
            <small>Inteiro positivo &lt; {meta.maxQuantityExclusive.toLocaleString('pt-BR')}</small>
          </label>

          <label className="field">
            <span>Preço (R$)</span>
            <input
              type="number"
              min={meta.priceTick}
              max={meta.maxPriceExclusive - meta.priceTick}
              step={meta.priceTick}
              value={price}
              onChange={(e) => setPrice(e.target.value)}
            />
            <small>Múltiplo de 0,01 e &lt; {meta.maxPriceExclusive.toLocaleString('pt-BR')}</small>
          </label>

          {allErrors.length > 0 && (
            <ul className="errors">
              {allErrors.map((err) => (
                <li key={err}>{err}</li>
              ))}
            </ul>
          )}

          <button className={`submit ${side === 'Buy' ? 'buy' : 'sell'}`} type="submit" disabled={!canSubmit}>
            {submitting
              ? 'Enviando…'
              : side === 'Buy'
                ? 'Enviar ordem de Compra'
                : 'Enviar ordem de Venda'}
          </button>
        </form>

        <ResultPanel result={result} />
      </main>

      <PortfolioPanel symbols={meta.symbols} states={positions} />
    </div>
  )
}

function SessionBadge({ ready }: { ready: boolean | null }) {
  const label =
    ready === null ? 'Verificando…' : ready ? 'Sessão FIX conectada' : 'Acumulador offline'
  const cls = ready === null ? 'badge pending' : ready ? 'badge ok' : 'badge down'
  return <span className={cls}>{label}</span>
}
