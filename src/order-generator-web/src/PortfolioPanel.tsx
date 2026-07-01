import { companyOf } from './companies'
import { CompanyBadge } from './CompanyBadge'

export interface SymbolState {
  exposure: number
  position: number
}

const BRL = new Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL' })
const QTY = new Intl.NumberFormat('pt-BR')

export function PortfolioPanel({
  symbols,
  states,
}: {
  symbols: string[]
  states: Record<string, SymbolState>
}) {
  return (
    <section className="card portfolio">
      <div className="portfolio-header">
        <h2>Carteira por símbolo</h2>
      </div>

      <div className="portfolio-grid portfolio-grid-head">
        <span>Ativo</span>
        <span className="num">Posição (qtd)</span>
        <span className="num">Exposição</span>
      </div>

      {symbols.map((symbol) => {
        const state = states[symbol] ?? { exposure: 0, position: 0 }
        const position = state.position
        return (
          <div className="portfolio-grid portfolio-row" key={symbol}>
            <span className="portfolio-asset">
              <CompanyBadge symbol={symbol} size={20} />
              <span className="symbol-ticker">{symbol}</span>
              <span className="symbol-company">{companyOf(symbol)?.name}</span>
            </span>
            <span className={`num position ${position > 0 ? 'pos' : position < 0 ? 'neg' : ''}`}>
              {position > 0 ? '+' : ''}
              {QTY.format(position)}
            </span>
            <span className="num">{BRL.format(state.exposure)}</span>
          </div>
        )
      })}
    </section>
  )
}
