import { Outcome, type OrderResult } from './api'

const BRL = new Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL' })
const QTY = new Intl.NumberFormat('pt-BR')

function formatSide(side?: string): string {
  if (side === 'Buy') return 'Compra'
  if (side === 'Sell') return 'Venda'
  return '—'
}

function outcomeMeta(outcome: Outcome): { label: string; cls: string } {
  switch (outcome) {
    case Outcome.New:
      return { label: 'Aceita', cls: 'result accepted' }
    case Outcome.Rejected:
      return { label: 'Rejeitada', cls: 'result rejected' }
    default:
      return { label: 'Erro', cls: 'result error' }
  }
}

export function ResultPanel({ result }: { result: OrderResult | null }) {
  if (!result) {
    return (
      <aside className="card placeholder">
        <p>Preencha o formulário e envie uma ordem para ver aqui o ExecutionReport retornado pelo OrderAccumulator.</p>
      </aside>
    )
  }

  const meta = outcomeMeta(result.outcome)
  const rows: Array<[string, string]> = [
    ['ClOrdID', result.clOrdId],
    ['Símbolo', result.symbol ?? '—'],
    ['Lado', formatSide(result.side)],
    ['Quantidade', result.quantity != null ? result.quantity.toLocaleString('pt-BR') : '—'],
    ['Preço', result.price != null ? BRL.format(result.price) : '—'],
    ['ExecType', result.execType ?? '—'],
    ['OrdStatus', result.ordStatus ?? '—'],
    ['OrderID', result.orderId ?? '—'],
    ['ExecID', result.execId ?? '—'],
    ['Posição do símbolo', result.positionAfter != null ? QTY.format(result.positionAfter) : '—'],
    ['Exposição do símbolo', result.exposureAfter != null ? BRL.format(result.exposureAfter) : '—'],
  ]

  return (
    <aside className={`card ${meta.cls}`}>
      <div className="result-header">
        <h2>{meta.label}</h2>
        <time>{new Date(result.receivedAtUtc).toLocaleString('pt-BR')}</time>
      </div>

      {result.text && <p className="result-text">{result.text}</p>}

      <dl className="result-grid">
        {rows.map(([k, v]) => (
          <div key={k} className="result-row">
            <dt>{k}</dt>
            <dd>{v}</dd>
          </div>
        ))}
      </dl>
    </aside>
  )
}
