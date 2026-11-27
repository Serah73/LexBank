import { useEffect, useRef, useState } from 'react'
import type { ChangeEvent } from 'react'
import { useNavigate } from 'react-router'
import { formatAmount, formatDate } from './format'
import { applyRulesErrorMessages, bankNames, categoryNames, correctionErrorMessages, describeAccuracy, describeAppliedRules, describeHits, describeRule,failedRowMessages, statementErrorMessages } from './messages'
import type { ApplyRulesError, Bank, Category, CorrectionError, FailedRowReason, Rule, StatementError } from './messages'
import RuleOfferForm from './RuleOfferForm'
import type { RuleOffer } from './RuleOfferForm'

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

interface Hits {
  hits: number
  total: number
}

interface Accuracy extends Hits {
  byBank?: (Hits & { bank: Bank })[]
}

interface FailedRow {
  rowNumber: number
  reason: FailedRowReason
}

// Resolves to null when there is no session (the API answers 401).
async function fetchStoredTransactions(): Promise<Transaction[] | null> {
  const response = await fetch('/api/transactions')
  if (response.status === 401) return null
  const stored = response.ok ? await response.json() : null
  if (!Array.isArray(stored)) throw new Error('The transactions were not loaded')
  return stored
}

// Returns null when the value is not an accuracy.
function toAccuracy(value: Partial<Accuracy> | null | undefined): Accuracy | null {
  return Number.isInteger(value?.hits) && Number.isInteger(value?.total) ? (value as Accuracy) : null
}

async function fetchAccuracy(): Promise<Accuracy | null> {
  const response = await fetch('/api/transactions/accuracy')
  return toAccuracy(response.ok ? await response.json() : null)
}

function BankAccuracyTable({ banks }: { banks: NonNullable<Accuracy['byBank']> }) {
  return (
    <table>
      <caption>Por banco</caption>
      <thead>
        <tr>
          <th>Banco</th>
          <th>Aciertos</th>
        </tr>
      </thead>
      <tbody>
        {banks.map((group) => (
          <tr key={group.bank}>
            <td>{bankNames[group.bank]}</td>
            <td>{describeHits(group.hits, group.total)}</td>
          </tr>
        ))}
      </tbody>
    </table>
  )
}

function TransactionsPage() {
  const navigate = useNavigate()
  const [transactions, setTransactions] = useState<Transaction[]>([])
  const [failedRows, setFailedRows] = useState<FailedRow[]>([])
  const [error, setError] = useState<StatementError | null>(null)
  const [skipped, setSkipped] = useState(0)
  const [offer, setOffer] = useState<{ id: number } & RuleOffer | null>(null)
  const [correctionError, setCorrectionError] = useState<CorrectionError | null>(null)

  const [applied, setApplied] = useState<number | null>(null)
  const [applyError, setApplyError] = useState<ApplyRulesError | null>(null)

  const [accuracy, setAccuracy] = useState<Accuracy | null>(null)

  const uploaded = useRef(false)

  useEffect(() => {
    fetchStoredTransactions()
      .then((stored) => {
        if (stored === null) navigate('/login', { replace: true })
        else if (!uploaded.current) setTransactions(stored)
      })
      .catch(() => {})
    fetchAccuracy()
      .then((stored) => {
        if (!uploaded.current) setAccuracy(stored)
      })
      .catch(() => {})
  }, [navigate])

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
    setApplied(null)
    setApplyError(null)
    setCorrectionError(null)
    setOffer(null)

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
      setAccuracy(toAccuracy(result.accuracy))
      setFailedRows(result.failedRows)
      setSkipped(Number.isInteger(result.skippedTransactions) ? result.skippedTransactions : 0)
      setError(null)
    } else {
      showError(Object.hasOwn(statementErrorMessages, result?.error) ? result.error : 'ProcessingFailed')
    }
  }

  async function handleCategoryChange(id: number, category: Category) {
    setApplied(null)
    setApplyError(null)
    setCorrectionError(null)
    setOffer(null)
    try {
      const response = await fetch(`/api/transactions/${id}/category`, {
        method: 'PUT',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ category }),
      })
      const result = await response.json()
      const updated: Transaction = result?.transaction
      if (!response.ok || updated?.id !== id) throw new Error('The correction was not saved')
      setTransactions((current) => current.map((t) => (t.id === id ? updated : t)))
      setAccuracy(toAccuracy(result.accuracy))
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

  async function handleApplyRules() {
    setApplied(null)
    setApplyError(null)
    try {
      const response = await fetch('/api/transactions/apply-rules', { method: 'POST' })
      const result = await response.json()
      if (!response.ok || !Number.isInteger(result?.changed)) throw new Error('The rules were not applied')
      setApplied(result.changed)
      setAccuracy(toAccuracy(result.accuracy))
    } catch {
      setApplyError('ApplyRulesFailed')
      return
    }
    await reloadTransactions()
  }

  async function reloadTransactions() {
    try {
      const stored = await fetchStoredTransactions()
      if (stored === null) navigate('/login', { replace: true })
      else setTransactions(stored)
    } catch {
      setApplyError('ReloadFailed')
    }
  }

  return (
    <main>
      <h1>LexBank</h1>
      <input type="file" accept=".xlsx,.csv" onChange={handleFileChange} />
      <button type="button" onClick={handleApplyRules}>
        Aplicar reglas
      </button>
      {applied !== null && <p role="status">{describeAppliedRules(applied)}</p>}
      {applyError && <p role="alert">{applyRulesErrorMessages[applyError]}</p>}
      {error && <p role="alert">{statementErrorMessages[error]}</p>}
      {correctionError && <p role="alert">{correctionErrorMessages[correctionError]}</p>}
      {offer && <RuleOfferForm key={`${offer.id}-${offer.category}`} offer={offer} onClose={() => setOffer(null)} />}
      {skipped > 0 && <p>Movimientos omitidos por estar ya importados: {skipped}</p>}
      {accuracy && accuracy.total > 0 && (
        <>
          <p>{describeAccuracy(accuracy.hits, accuracy.total)}</p>
          {Array.isArray(accuracy.byBank) && <BankAccuracyTable banks={accuracy.byBank} />}
        </>
      )}
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
                <td>{formatDate(t.date)}</td>
                <td>{t.description}</td>
                <td>{formatAmount(t.amount)}</td>
                <td>{formatAmount(t.balance)}</td>
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

export default TransactionsPage
