import type { ReactNode } from 'react'
import { Link } from 'react-router'

interface AuthLayoutProps {
  children: ReactNode
  link: { question: string; text: string; to: string }
}

// Centered frame of the pages without a session, with the link to the other one.
function AuthLayout({ children, link }: AuthLayoutProps) {
  return (
    <main className="flex min-h-screen flex-col items-center justify-center gap-8 p-8">
      <div className="flex items-center gap-3">
        <img src="/lexbank_icon_light.svg" alt="" className="size-14" />
        <h1 className="text-3xl font-semibold">LexBank</h1>
      </div>
      <div className="flex w-full max-w-sm flex-col gap-4">
        {children}
        <p className="text-center text-sm text-muted-foreground">
          {link.question}{' '}
          <Link to={link.to} className="font-medium text-tint hover:underline">
            {link.text}
          </Link>
        </p>
      </div>
    </main>
  )
}

export default AuthLayout
