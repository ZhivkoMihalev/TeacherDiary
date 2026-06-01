import type { ReactNode, CSSProperties } from 'react'

type Variant = 'gray' | 'green' | 'yellow' | 'red' | 'indigo' | 'blue' | 'orange' | 'purple'

const variantStyles: Record<Variant, CSSProperties> = {
  gray:   { background: 'rgba(241,245,249,0.9)', color: '#475569', border: '1px solid #e2e8f0' },
  green:  { background: 'rgba(240,253,244,0.9)', color: '#166534', border: '1px solid #bbf7d0' },
  yellow: { background: 'rgba(255,251,235,0.9)', color: '#92400e', border: '1px solid #fde68a' },
  red:    { background: 'rgba(254,242,242,0.9)', color: '#991b1b', border: '1px solid #fecaca' },
  indigo: { background: 'rgba(238,242,255,0.9)', color: '#3730a3', border: '1px solid #c7d2fe' },
  blue:   { background: 'rgba(240,249,255,0.9)', color: '#0369a1', border: '1px solid #bae6fd' },
  orange: { background: 'rgba(255,247,237,0.9)', color: '#9a3412', border: '1px solid #fed7aa' },
  purple: { background: 'rgba(253,244,255,0.9)', color: '#6b21a8', border: '1px solid #e9d5ff' },
}

interface Props {
  children: ReactNode
  variant?: Variant
  className?: string
}

export function Badge({ children, variant = 'gray', className = '' }: Props) {
  return (
    <span
      className={`inline-flex items-center px-2.5 py-0.5 ${className}`}
      style={{
        ...variantStyles[variant],
        fontFamily: "'Plus Jakarta Sans', system-ui, sans-serif",
        fontSize: '0.73rem',
        fontWeight: 600,
        letterSpacing: '0.01em',
        borderRadius: '99px',
        lineHeight: 1.4,
      }}
    >
      {children}
    </span>
  )
}
