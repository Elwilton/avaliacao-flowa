export type Side = 'Buy' | 'Sell'

export interface Meta {
  symbols: string[]
  sides: Side[]
  maxQuantityExclusive: number
  maxPriceExclusive: number
  priceTick: number
  exposureLimitPerSymbol: number
}

export interface OrderRequest {
  symbol: string
  side: Side
  quantity: number
  price: number
}

export enum Outcome {
  New = 0,
  Rejected = 1,
  Error = 2,
}

export interface OrderResult {
  outcome: Outcome
  clOrdId: string
  symbol?: string
  side?: string
  quantity?: number
  price?: number
  execType?: string
  ordStatus?: string
  orderId?: string
  execId?: string
  text?: string
  exposureAfter?: number
  positionAfter?: number
  receivedAtUtc: string
}

export interface ValidationProblem {
  title?: string
  errors?: Record<string, string[]>
}

export interface SubmitResponse {
  status: number
  result?: OrderResult
  problem?: ValidationProblem
}

export async function fetchMeta(): Promise<Meta> {
  const res = await fetch('/api/meta')
  if (!res.ok) throw new Error('Falha ao carregar metadados da API')
  return res.json()
}

export async function fetchHealth(): Promise<{ fixSessionReady: boolean }> {
  const res = await fetch('/api/health')
  if (!res.ok) throw new Error('Falha ao consultar saúde da API')
  return res.json()
}

export interface PortfolioEntry {
  symbol: string
  exposure: number
  position: number
}

export async function fetchPortfolio(): Promise<PortfolioEntry[]> {
  const res = await fetch('/api/portfolio')
  if (!res.ok) throw new Error('Falha ao carregar a carteira')
  return res.json()
}

export async function submitOrder(order: OrderRequest): Promise<SubmitResponse> {
  const res = await fetch('/api/orders', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(order),
  })

  const body = await res.json().catch(() => undefined)

  if (res.status === 400) {
    return { status: res.status, problem: body as ValidationProblem }
  }
  return { status: res.status, result: body as OrderResult }
}
