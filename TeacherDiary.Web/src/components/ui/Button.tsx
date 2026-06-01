import { type ButtonHTMLAttributes, type CSSProperties } from 'react'
import { Spinner } from './Spinner'

type Variant = 'primary' | 'secondary' | 'danger' | 'ghost' | 'success'

const variantStyles: Record<Variant, CSSProperties> = {
  primary:   { background: 'linear-gradient(135deg, #7c3aed 0%, #db2777 100%)', color: '#ffffff', border: 'none' },
  secondary: { background: 'rgba(255,255,255,0.8)', color: '#3730a3', border: '1px solid rgba(124,58,237,0.2)' },
  danger:    { background: '#dc2626', color: '#ffffff', border: 'none' },
  ghost:     { background: 'transparent', color: '#7c3aed', border: '1px solid transparent' },
  success:   { background: '#059669', color: '#ffffff', border: 'none' },
}

const hoverBg: Record<Variant, string> = {
  primary:   'linear-gradient(135deg, #6d28d9 0%, #be185d 100%)',
  secondary: 'rgba(255,255,255,0.95)',
  danger:    '#b91c1c',
  ghost:     'rgba(124,58,237,0.06)',
  success:   '#047857',
}

interface Props extends ButtonHTMLAttributes<HTMLButtonElement> {
  variant?: Variant
  loading?: boolean
  size?: 'sm' | 'md'
}

export function Button({
  variant = 'primary',
  loading = false,
  size = 'md',
  className = '',
  children,
  disabled,
  style: externalStyle,
  onMouseEnter,
  onMouseLeave,
  ...props
}: Props) {
  const sizeClass = size === 'sm' ? 'px-3 py-1.5' : 'px-4 py-2'

  const baseStyle: CSSProperties = {
    fontFamily: "'Plus Jakarta Sans', system-ui, sans-serif",
    fontSize: size === 'sm' ? '0.8125rem' : '0.875rem',
    fontWeight: 600,
    letterSpacing: '0.005em',
    borderRadius: '9px',
    lineHeight: 1.4,
    cursor: 'pointer',
    ...variantStyles[variant],
    ...externalStyle,
  }

  return (
    <button
      disabled={disabled || loading}
      className={`inline-flex items-center gap-2 justify-center transition-all focus:outline-none focus:ring-2 focus:ring-offset-1 focus:ring-violet-400 disabled:opacity-50 disabled:cursor-not-allowed active:scale-[0.97] ${sizeClass} ${className}`}
      style={baseStyle}
      onMouseEnter={e => {
        if (!disabled && !loading) e.currentTarget.style.background = hoverBg[variant]
        onMouseEnter?.(e)
      }}
      onMouseLeave={e => {
        const s = variantStyles[variant]
        e.currentTarget.style.background = (s.background as string) ?? ''
        onMouseLeave?.(e)
      }}
      {...props}
    >
      {loading && <Spinner className="h-4 w-4" />}
      {children}
    </button>
  )
}
