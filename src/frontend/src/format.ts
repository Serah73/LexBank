// The API sends dates as "YYYY-MM-DD": reordering the text avoids time zone shifts.
export function formatDate(date: string) {
  return date.split('-').reverse().join('/')
}

const euros = new Intl.NumberFormat('es-ES', { style: 'currency', currency: 'EUR' })

export function formatAmount(amount: number) {
  return euros.format(amount)
}

const signedEuros = new Intl.NumberFormat('es-ES', { style: 'currency', currency: 'EUR', signDisplay: 'exceptZero' })

// For movements: the sign tells income from expenses without relying on color.
export function formatMovement(amount: number) {
  return signedEuros.format(amount)
}
