import { useEffect, useState } from 'react'
import { useNavigate } from 'react-router'
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
    <main>
      <h1>Reglas</h1>
      {error && <p role="alert">{rulesLoadErrorMessages[error]}</p>}
      {rules.length > 0 && (
        <table>
          <thead>
            <tr>
              <th>Regla</th>
              <th>Categoría</th>
            </tr>
          </thead>
          <tbody>
            {rules.map((rule, index) => (
              <tr key={index}>
                <td>{describeRule(rule)}</td>
                <td>{categoryNames[rule.category]}</td>
              </tr>
            ))}
          </tbody>
        </table>
      )}
    </main>
  )
}

export default RulesPage
