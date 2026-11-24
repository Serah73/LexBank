import { useState } from 'react'
import type { FormEvent } from 'react'
import { categoryNames, ruleErrorMessages, ruleSignNames } from './messages'
import type { Category, Rule, RuleError, RuleSign } from './messages'

export interface RuleOffer {
  description: string
  category: Category
  sign: RuleSign
}

function isRule(value: unknown): value is Rule {
  const rule = value as Rule | null
  return typeof rule?.id === 'number' && Object.hasOwn(categoryNames, rule.category)
}

function RuleOfferForm({ offer, onClose }: { offer: RuleOffer; onClose: () => void }) {
  const [pattern, setPattern] = useState(offer.description)
  const [ruleError, setRuleError] = useState<RuleError | null>(null)
  const [existing, setExisting] = useState<Rule | null>(null)

  async function handleReplace(rule: Rule) {
    setRuleError(null)
    try {
      const response = await fetch(`/api/rules/${rule.id}/category`, {
        method: 'PUT',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ category: offer.category }),
      })
      if (response.ok) onClose()
      else setRuleError('RuleUpdateFailed')
    } catch {
      setRuleError('RuleUpdateFailed')
    }
  }

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    setRuleError(null)
    let response: Response
    let result
    try {
      response = await fetch('/api/rules', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ pattern, category: offer.category, sign: offer.sign }),
      })
      if (!response.ok) result = await response.json()
    } catch {
      setRuleError('RuleCreationFailed')
      return
    }

    if (response.ok) onClose()
    else if (result?.error === 'RuleAlreadyExists' && isRule(result.rule) && result.rule.category !== offer.category)
      setExisting(result.rule)
    else setRuleError(Object.hasOwn(ruleErrorMessages, result?.error) ? result.error : 'RuleCreationFailed')
  }

  const patternError = ruleError === 'InvalidRulePattern' || ruleError === 'RuleAlreadyExists' ? ruleError : null
  const generalError = ruleError === 'RuleCreationFailed' || ruleError === 'RuleUpdateFailed' ? ruleError : null

  if (existing)
    return (
      <form aria-label="Crear regla" noValidate>
        <p>
          Ya existe una regla con ese patrón y ese signo, categorizada como «{categoryNames[existing.category]}». ¿Reemplazar
          su categoría por «{categoryNames[offer.category]}»?
        </p>
        {generalError && <p role="alert">{ruleErrorMessages[generalError]}</p>}
        <button type="button" onClick={() => handleReplace(existing)}>
          Reemplazar
        </button>
        <button
          type="button"
          onClick={() => {
            setExisting(null)
            setRuleError(null)
          }}
        >
          No reemplazar
        </button>
      </form>
    )

  return (
    <form aria-label="Crear regla" onSubmit={handleSubmit} noValidate>
      <p>
        ¿Categorizar como «{categoryNames[offer.category]}» los movimientos de tipo «{ruleSignNames[offer.sign]}» que
        coincidan con este patrón?
      </p>
      <label>
        Patrón
        <input
          value={pattern}
          onChange={(event) => setPattern(event.target.value)}
          aria-invalid={patternError !== null}
          aria-describedby={patternError ? 'pattern-error' : undefined}
        />
      </label>
      {patternError && <p id="pattern-error">{ruleErrorMessages[patternError]}</p>}
      {generalError && <p role="alert">{ruleErrorMessages[generalError]}</p>}
      <button type="submit">Crear regla</button>
      <button type="button" onClick={onClose}>
        Ahora no
      </button>
    </form>
  )
}

export default RuleOfferForm
