// The API sends dates as "YYYY-MM-DD": reordering the text avoids time zone shifts.
export function formatDate(date: string) {
  return date.split('-').reverse().join('/')
}

const euros = new Intl.NumberFormat('es-ES', { style: 'currency', currency: 'EUR' })

export function formatAmount(amount: number) {
  return euros.format(amount)
}
