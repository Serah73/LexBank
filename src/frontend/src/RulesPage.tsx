import { useEffect, useState } from 'react'
import { useNavigate } from 'react-router'
import { Card } from '@/components/ui/card'
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table'
import { ErrorMessage } from './Message'
import { categoryNames, describeRule, rulesLoadErrorMessages } from './messages'
import type { Rule, RulesLoadError } from './messages'

type ListedRule = Pick<Rule, 'pattern' | 'sign' | 'category'>

function RulesPage() {
  const navigate = useNavigate()
  const [rules, setRules] = useState<ListedRule[]>([])
  const [error, setError] = useState<RulesLoadError | null>(null)

  useEffect(() => {
    fetch('/api/rules')
      .then((response) => {
        if (response.status === 401) return null
        if (!response.ok) throw new Error('The rules were not loaded')
        return response.json()
      })
      .then((listed) => {
        if (listed === null) navigate('/login', { replace: true })
        else if (Array.isArray(listed)) setRules(listed)
      })
      .catch(() => setError('RulesLoadFailed'))
  }, [navigate])

  return (
    <main className="flex flex-col gap-4">
      <h1 className="text-2xl font-semibold">Reglas</h1>
      {error && <ErrorMessage role="alert">{rulesLoadErrorMessages[error]}</ErrorMessage>}
      {rules.length > 0 && (
        <Card className="py-0">
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead>Regla</TableHead>
                <TableHead>Categoría</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {rules.map((rule, index) => (
                <TableRow key={index}>
                  <TableCell className="whitespace-normal">{describeRule(rule)}</TableCell>
                  <TableCell>{categoryNames[rule.category]}</TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        </Card>
      )}
    </main>
  )
}

export default RulesPage
