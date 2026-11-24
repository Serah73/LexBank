import { useEffect, useRef, useState } from 'react'
import type { ChangeEvent } from 'react'
import LogInForm from './LogInForm'
import { bankNames, categoryNames, correctionErrorMessages, describeRule, failedRowMessages, statementErrorMessages } from './messages'
import type { Bank, Category, CorrectionError, FailedRowReason, Rule, StatementError } from './messages'
import RuleOfferForm from './RuleOfferForm'
import type { RuleOffer } from './RuleOfferForm'
import SignUpForm from './SignUpForm'

interface Transaction {
  id: number
  date: string
  description: string
  amount: number
  balance: number
  bank: Bank
  category: Category
  rule: Rule | null
  corrected: boolean
}

interface FailedRow {
  rowNumber: number
  reason: FailedRowReason
}

// Resolves to null when there is no session (the API answers 401).
async function fetchStoredTransactions(): Promise<Transaction[] | null> {
  const response = await fetch('/api/transactions')
  return response.status === 401 ? null : response.json()
}

function App() {
  const [transactions, setTransactions] = useState<Transaction[]>([])
  const [failedRows, setFailedRows] = useState<FailedRow[]>([])
  const [error, setError] = useState<StatementError | null>(null)
  const [skipped, setSkipped] = useState(0)
  const [signedOut, setSignedOut] = useState(false)
  const [offer, setOffer] = useState<{ id: number } & RuleOffer | null>(null)
  const [correctionError, setCorrectionError] = useState<CorrectionError | null>(null)

  const uploaded = useRef(false)

  useEffect(() => {
    fetchStoredTransactions()
      .then((stored) => {
        if (stored === null) setSignedOut(true)
        else if (!uploaded.current) setTransactions(stored)
      })
      .catch(() => {})
  }, [])

  function handleLoggedIn() {
    setSignedOut(false)
    fetchStoredTransactions()
      .then((stored) => {
        if (stored) setTransactions(stored)
      })
      .catch(() => {})
  }

  function showError(code: StatementError) {
    uploaded.current = true
    setTransactions([])
    setFailedRows([])
    setSkipped(0)
    setError(code)
  }

  async function handleFileChange(event: ChangeEvent<HTMLInputElement>) {
    const file = event.target.files?.[0]
    if (!file) return
    event.target.value = ''
    setCorrectionError(null)

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
      uploaded.current = true
      setTransactions(result.transactions)
      setFailedRows(result.failedRows)
      setSkipped(Number.isInteger(result.skippedTransactions) ? result.skippedTransactions : 0)
      setError(null)
    } else {
      showError(Object.hasOwn(statementErrorMessages, result?.error) ? result.error : 'ProcessingFailed')
    }
  }

  async function handleCategoryChange(id: number, category: Category) {
    setCorrectionError(null)
    try {
      const response = await fetch(`/api/transactions/${id}/category`, {
        method: 'PUT',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ category }),
      })
      const updated: Transaction = await response.json()
      if (!response.ok || updated?.id !== id) throw new Error('The correction was not saved')
      setTransactions((current) => current.map((t) => (t.id === id ? updated : t)))
      setOffer(
        updated.category === 'Uncategorized'
          ? null
          : {
              id,
              description: updated.description,
              category: updated.category,
              sign: updated.amount < 0 ? 'Expense' : updated.amount > 0 ? 'Income' : 'Any',
            },
      )
    } catch {
      setCorrectionError('CorrectionFailed')
    }
  }

  async function handleLogOut() {
    await fetch('/api/session', { method: 'DELETE' })
    uploaded.current = false
    setTransactions([])
    setFailedRows([])
    setSkipped(0)
    setError(null)
    setCorrectionError(null)
    setOffer(null)
    setSignedOut(true)
  }

  return (
    <main>
      <h1>LexBank</h1>
      {signedOut && (
        <>
          <SignUpForm />
          <LogInForm onLoggedIn={handleLoggedIn} />
        </>
      )}
      {!signedOut && (
        <>
          <button type="button" onClick={handleLogOut}>
            Cerrar sesión
          </button>
          <input type="file" accept=".xlsx,.csv" onChange={handleFileChange} />
        </>
      )}
      {error && <p role="alert">{statementErrorMessages[error]}</p>}
      {correctionError && <p role="alert">{correctionErrorMessages[correctionError]}</p>}
      {offer && <RuleOfferForm key={`${offer.id}-${offer.category}`} offer={offer} onClose={() => setOffer(null)} />}
      {skipped > 0 && <p>Movimientos omitidos por estar ya importados: {skipped}</p>}
      {transactions.length > 0 && (
        <table>
          <thead>
            <tr>
              <th>Fecha</th>
              <th>Descripción</th>
              <th>Importe</th>
              <th>Saldo</th>
              <th>Banco</th>
              <th>Categoría</th>
              <th>Regla</th>
            </tr>
          </thead>
          <tbody>
            {transactions.map((t, index) => (
              <tr key={index}>
                <td>{t.date}</td>
                <td>{t.description}</td>
                <td>{t.amount}</td>
                <td>{t.balance}</td>
                <td>{bankNames[t.bank]}</td>
                <td>
                  <select aria-label={`Categoría de ${t.description}`} value={t.category}
                    onChange={(event) => handleCategoryChange(t.id, event.target.value as Category)}
                  >
                    {(Object.keys(categoryNames) as Category[]).map((category) => (
                      <option key={category} value={category}>
                        {categoryNames[category]}
                      </option>
                    ))}
                  </select>
                </td>
                <td>{t.corrected ? 'Corregida por ti' : t.rule && describeRule(t.rule)}</td>
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
