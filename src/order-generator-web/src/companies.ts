export interface Company {
  name: string
  color: string
  initials: string
}

export const COMPANIES: Record<string, Company> = {
  PETR4: { name: 'Petrobras', color: '#00A859', initials: 'PE' },
  VALE3: { name: 'Vale', color: '#008F83', initials: 'VA' },
  VIIA4: { name: 'Via', color: '#E5006D', initials: 'VI' },
}

export function companyOf(symbol: string): Company | undefined {
  return COMPANIES[symbol]
}
