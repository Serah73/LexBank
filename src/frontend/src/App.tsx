import { useState } from 'react'
import type { ChangeEvent } from 'react'
import { failedRowMessages, statementErrorMessages } from './messages'
import type { FailedRowReason, StatementError } from './messages'

interface Transaction {
  date: string
  description: string
  amount: number
  currency: string
  balance: number
}

interface FailedRow {
  rowNumber: number
  reason: FailedRowReason
}

function App() {
  const [transactions, setTransactions] = useState<Transaction[]>([])
  const [failedRows, setFailedRows] = useState<FailedRow[]>([])
  const [error, setError] = useState<StatementError | null>(null)

  function showError(code: StatementError) {
    setTransactions([])
    setFailedRows([])
    setError(code)
  }

  async function handleFileChange(event: ChangeEvent<HTMLInputElement>) {
    const file = event.target.files?.[0]
    if (!file) return
    event.target.value = ''

    const body = new FormData()
    body.append('file', file)
    let response: Response
    let result
    try {
      response = await fetch('/api/statements', { method: 'POST', body })
      result = await response.json()
    } catch {
      showError('ProcessingFailed')
      return
    }

    if (response.ok && Array.isArray(result?.transactions) && Array.isArray(result?.failedRows)) {
      setTransactions(result.transactions)
      setFailedRows(result.failedRows)
      setError(null)
    } else {
      showError(Object.hasOwn(statementErrorMessages, result?.error) ? result.error : 'ProcessingFailed')
    }
  }

  return (
    <main>
      <h1>LexBank</h1>
      <input type="file" accept=".xlsx" onChange={handleFileChange} />
      {error && <p role="alert">{statementErrorMessages[error]}</p>}
      {transactions.length > 0 && (
        <table>
          <thead>
            <tr>
              <th>Fecha</th>
              <th>Descripción</th>
              <th>Importe</th>
              <th>Moneda</th>
              <th>Saldo</th>
            </tr>
          </thead>
          <tbody>
            {transactions.map((t, index) => (
              <tr key={index}>
                <td>{t.date}</td>
                <td>{t.description}</td>
                <td>{t.amount}</td>
                <td>{t.currency}</td>
                <td>{t.balance}</td>
              </tr>
            ))}
          </tbody>
        </table>
      )}
      {failedRows.length > 0 && (
        <section>
          <h2>Filas que no se pudieron leer</h2>
          <ul>
            {failedRows.map((row) => (
              <li key={row.rowNumber}>
                Fila {row.rowNumber}: {failedRowMessages[row.reason]}
              </li>
            ))}
          </ul>
        </section>
      )}
    </main>
  )
}

export default App
