import { useState } from 'react'
import type { FormEvent } from 'react'
import { categoryNames, ruleErrorMessages, ruleSignNames } from './messages'
import type { Category, RuleError, RuleSign } from './messages'

export interface RuleOffer {
  description: string
  category: Category
  sign: RuleSign
}

function RuleOfferForm({ offer, onClose }: { offer: RuleOffer; onClose: () => void }) {
  const [pattern, setPattern] = useState(offer.description)
  const [ruleError, setRuleError] = useState<RuleError | null>(null)

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
    else setRuleError(Object.hasOwn(ruleErrorMessages, result?.error) ? result.error : 'RuleCreationFailed')
  }

  const patternError = ruleError === 'InvalidRulePattern' || ruleError === 'RuleAlreadyExists' ? ruleError : null
  const generalError = ruleError === 'RuleCreationFailed' ? ruleError : null

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
