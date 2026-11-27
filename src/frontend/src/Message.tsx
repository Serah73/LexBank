import { CircleAlert, CircleCheck } from 'lucide-react'
import type { ComponentProps } from 'react'

// Errors and successes carry an icon, so they do not rely on color alone.
// The caller sets the role: an error under its own field has none.
export function ErrorMessage({ children, ...props }: ComponentProps<'p'>) {
  return (
    <p className="flex items-center gap-2 text-sm text-destructive" {...props}>
      <CircleAlert aria-hidden="true" className="size-4 shrink-0" />
      {children}
    </p>
  )
}

export function SuccessMessage({ children, ...props }: ComponentProps<'p'>) {
  return (
    <p className="flex items-center gap-2 text-sm text-income" {...props}>
      <CircleCheck aria-hidden="true" className="size-4 shrink-0" />
      {children}
    </p>
  )
}
