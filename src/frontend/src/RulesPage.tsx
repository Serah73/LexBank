import { useEffect, useState } from 'react'
import { useNavigate } from 'react-router'
import { categoryNames, describeRule } from './messages'
import type { Rule } from './messages'

type ListedRule = Pick<Rule, 'pattern' | 'sign' | 'category'>

function RulesPage() {
  const navigate = useNavigate()
  const [rules, setRules] = useState<ListedRule[]>([])

  useEffect(() => {
    fetch('/api/rules')
      .then((response) => (response.status === 401 ? null : response.json()))
      .then((listed) => {
        if (listed === null) navigate('/login', { replace: true })
        else if (Array.isArray(listed)) setRules(listed)
      })
  }, [navigate])

  return (
    <main>
      <h1>Reglas</h1>
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
