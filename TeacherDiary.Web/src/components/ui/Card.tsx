import type { ReactNode } from 'react'

interface CardProps {
  children: ReactNode
  className?: string
}

export function Card({ children, className = '' }: CardProps) {
  return (
    <div
      className={className}
      style={{
        background: 'rgba(255,255,255,0.88)',
        backdropFilter: 'blur(12px)',
        WebkitBackdropFilter: 'blur(12px)',
        border: '1px solid rgba(255,255,255,0.95)',
        borderRadius: '14px',
        boxShadow: '0 4px 20px rgba(124,58,237,0.07), 0 1px 6px rgba(0,0,0,0.04)',
        transition: 'box-shadow 0.25s ease, transform 0.25s ease',
      }}
      onMouseEnter={e => {
        const el = e.currentTarget as HTMLDivElement
        el.style.boxShadow = '0 8px 32px rgba(124,58,237,0.12), 0 2px 8px rgba(0,0,0,0.05)'
        el.style.transform = 'translateY(-1px)'
      }}
      onMouseLeave={e => {
        const el = e.currentTarget as HTMLDivElement
        el.style.boxShadow = '0 4px 20px rgba(124,58,237,0.07), 0 1px 6px rgba(0,0,0,0.04)'
        el.style.transform = 'translateY(0)'
      }}
    >
      {children}
    </div>
  )
}

export function CardHeader({ children, className = '' }: CardProps) {
  return (
    <div
      className={className}
      style={{
        padding: '14px 20px',
        borderBottom: '1px solid rgba(124,58,237,0.08)',
        fontFamily: "'Bricolage Grotesque', system-ui, sans-serif",
        fontWeight: 700,
        fontSize: '0.92rem',
        letterSpacing: '-0.01em',
        color: '#1e1b4b',
      }}
    >
      {children}
    </div>
  )
}

export function CardBody({ children, className = '' }: CardProps) {
  return (
    <div
      className={className}
      style={{ padding: '16px 20px' }}
    >
      {children}
    </div>
  )
}
