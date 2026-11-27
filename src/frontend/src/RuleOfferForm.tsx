import { useState } from 'react'
import type { FormEvent } from 'react'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { ErrorMessage } from './Message'
import { categoryNames, ruleErrorMessages, ruleSignNames } from './messages'
import type { Category, Rule, RuleError, RuleSign } from './messages'

export interface RuleOffer {
  description: string
  category: Category
  sign: RuleSign
}

// The longest pattern the API accepts.
const maxPatternLength = 100

// A pattern matches whole words, so a word cut by the limit is left out of the suggestion.
function suggestPattern(description: string) {
  const cut = description.slice(0, maxPatternLength)
  const splitsAWord = /^[\p{L}\p{N}]/u.test(description.slice(maxPatternLength))
  return (splitsAWord ? cut.replace(/[\p{L}\p{N}]+$/u, '') : cut).trim()
}

function isRule(value: unknown): value is Rule {
  const rule = value as Rule | null
  return typeof rule?.id === 'number' && Object.hasOwn(categoryNames, rule.category)
}

function RuleOfferForm({ offer, onClose }: { offer: RuleOffer; onClose: () => void }) {
  const [pattern, setPattern] = useState(() => suggestPattern(offer.description))
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
      <form aria-label="Crear regla" noValidate className="flex flex-col gap-3">
        <p>
          Ya existe una regla con ese patrón y ese signo, categorizada como «{categoryNames[existing.category]}». ¿Reemplazar
          su categoría por «{categoryNames[offer.category]}»?
        </p>
        {generalError && <ErrorMessage role="alert">{ruleErrorMessages[generalError]}</ErrorMessage>}
        <div className="flex gap-2">
          <Button onClick={() => handleReplace(existing)}>Reemplazar</Button>
          <Button
            variant="outline"
            onClick={() => {
              setExisting(null)
              setRuleError(null)
            }}
          >
            No reemplazar
          </Button>
        </div>
      </form>
    )

  return (
    <form aria-label="Crear regla" onSubmit={handleSubmit} noValidate className="flex flex-col gap-3">
      <p>
        ¿Categorizar como «{categoryNames[offer.category]}» los movimientos de tipo «{ruleSignNames[offer.sign]}» que
        coincidan con este patrón?
      </p>
      <label className="flex max-w-md flex-col gap-1 text-sm font-medium">
        Patrón
        <Input
          value={pattern}
          onChange={(event) => setPattern(event.target.value)}
          aria-invalid={patternError !== null}
          aria-describedby={patternError ? 'pattern-error' : undefined}
        />
      </label>
      {patternError && <ErrorMessage id="pattern-error">{ruleErrorMessages[patternError]}</ErrorMessage>}
      {generalError && <ErrorMessage role="alert">{ruleErrorMessages[generalError]}</ErrorMessage>}
      <div className="flex gap-2">
        <Button type="submit">Crear regla</Button>
        <Button variant="outline" onClick={onClose}>
          Ahora no
        </Button>
      </div>
    </form>
  )
}

export default RuleOfferForm
