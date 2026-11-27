import { useEffect, useRef, useState } from 'react'
import type { ChangeEvent } from 'react'
import { useNavigate } from 'react-router'
import { Button } from '@/components/ui/button'
import { Card, CardContent } from '@/components/ui/card'
import { Input } from '@/components/ui/input'
import { Table, TableBody, TableCaption, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table'
import { formatAmount, formatDate, formatMovement } from './format'
import { applyRulesErrorMessages, bankNames, categoryNames, correctionErrorMessages, describeAccuracy, describeAppliedRules, describeHits, describeRule,failedRowMessages, statementErrorMessages } from './messages'
import type { ApplyRulesError, Bank, Category, CorrectionError, FailedRowReason, Rule, StatementError } from './messages'
import { ErrorMessage, SuccessMessage } from './Message'
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
    <Table>
      <TableCaption className="mt-0 mb-2 [caption-side:top] text-left">Por banco</TableCaption>
      <TableHeader>
        <TableRow>
          <TableHead>Banco</TableHead>
          <TableHead className="text-right">Aciertos</TableHead>
        </TableRow>
      </TableHeader>
      <TableBody>
        {banks.map((group) => (
          <TableRow key={group.bank}>
            <TableCell>{bankNames[group.bank]}</TableCell>
            <TableCell className="text-right tabular-nums">{describeHits(group.hits, group.total)}</TableCell>
          </TableRow>
        ))}
      </TableBody>
    </Table>
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
    // The bottom space lets the last rows be scrolled above the floating panel.
    <main className={`flex flex-col gap-4 ${correctionError || offer ? 'pb-80' : ''}`}>
      <h1 className="text-2xl font-semibold">Movimientos</h1>
      <div className="flex items-center gap-3">
        <Input type="file" accept=".xlsx,.csv" onChange={handleFileChange} className="h-9 w-80" />
        <Button size="lg" onClick={handleApplyRules}>
          Aplicar reglas
        </Button>
      </div>
      {applied !== null && <SuccessMessage role="status">{describeAppliedRules(applied)}</SuccessMessage>}
      {applyError && <ErrorMessage role="alert">{applyRulesErrorMessages[applyError]}</ErrorMessage>}
      {error && <ErrorMessage role="alert">{statementErrorMessages[error]}</ErrorMessage>}
      {skipped > 0 && (
        <p className="text-sm text-muted-foreground">Movimientos omitidos por estar ya importados: {skipped}</p>
      )}
      {/* Floating: the answer to a correction is in view from any row, without blocking the page. */}
      {(correctionError || offer) && (
        <div className="fixed right-6 bottom-6 z-10 w-96 rounded-xl border border-tint bg-card p-4 shadow-2xl">
          {correctionError && <ErrorMessage role="alert">{correctionErrorMessages[correctionError]}</ErrorMessage>}
          {offer && <RuleOfferForm key={`${offer.id}-${offer.category}`} offer={offer} onClose={() => setOffer(null)} />}
        </div>
      )}
      {accuracy && accuracy.total > 0 && (
        <Card className="w-full max-w-md">
          <CardContent className="flex flex-col gap-4">
            <p className="text-base font-medium">{describeAccuracy(accuracy.hits, accuracy.total)}</p>
            {Array.isArray(accuracy.byBank) && <BankAccuracyTable banks={accuracy.byBank} />}
          </CardContent>
        </Card>
      )}
      {transactions.length > 0 && (
        <Card className="overflow-visible py-0">
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead>Fecha</TableHead>
                <TableHead>Descripción</TableHead>
                <TableHead className="text-right">Importe</TableHead>
                <TableHead className="text-right">Saldo</TableHead>
                <TableHead>Banco</TableHead>
                <TableHead>Categoría</TableHead>
                <TableHead>Regla</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {transactions.map((t, index) => (
                <TableRow key={index}>
                  <TableCell className="tabular-nums">{formatDate(t.date)}</TableCell>
                  <TableCell className="whitespace-normal">{t.description}</TableCell>
                  <TableCell className={`text-right font-medium tabular-nums ${t.amount > 0 ? 'text-income' : ''}`}>
                    {formatMovement(t.amount)}
                  </TableCell>
                  <TableCell className="text-right text-muted-foreground tabular-nums">{formatAmount(t.balance)}</TableCell>
                  <TableCell>{bankNames[t.bank]}</TableCell>
                  <TableCell>
                    <select aria-label={`Categoría de ${t.description}`} value={t.category}
                      onChange={(event) => handleCategoryChange(t.id, event.target.value as Category)}
                      className="h-8 w-44 rounded-lg border border-input bg-background px-2 text-sm outline-none focus-visible:border-ring focus-visible:ring-3 focus-visible:ring-ring/50"
                    >
                      {(Object.keys(categoryNames) as Category[]).map((category) => (
                        <option key={category} value={category}>
                          {categoryNames[category]}
                        </option>
                      ))}
                    </select>
                  </TableCell>
                  <TableCell className="text-xs whitespace-normal text-muted-foreground">
                    {t.corrected ? 'Corregida por ti' : t.rule && describeRule(t.rule)}
                  </TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        </Card>
      )}
      {failedRows.length > 0 && (
        <Card>
          <CardContent>
            <section>
              <h2 className="mb-2 font-medium text-destructive">Filas que no se pudieron leer</h2>
              <ul className="list-inside list-disc text-sm">
                {failedRows.map((row) => (
                  <li key={row.rowNumber}>
                    Fila {row.rowNumber}: {failedRowMessages[row.reason]}
                  </li>
                ))}
              </ul>
            </section>
          </CardContent>
        </Card>
      )}
    </main>
  )
}

export default TransactionsPage
