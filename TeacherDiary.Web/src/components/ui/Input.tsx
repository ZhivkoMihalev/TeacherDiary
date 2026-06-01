import { type InputHTMLAttributes, forwardRef } from 'react'

interface Props extends InputHTMLAttributes<HTMLInputElement> {
  label?: string
  error?: string
}

export const Input = forwardRef<HTMLInputElement, Props>(
  ({ label, error, className = '', id, style, ...props }, ref) => {
    const inputId = id ?? label?.toLowerCase().replace(/\s+/g, '-')

    return (
      <div className="flex flex-col gap-1">
        {label && (
          <label
            htmlFor={inputId}
            style={{
              fontFamily: "'Plus Jakarta Sans', system-ui, sans-serif",
              fontSize: '0.8125rem',
              fontWeight: 600,
              color: '#4c1d95',
              letterSpacing: '0.005em',
            }}
          >
            {label}
          </label>
        )}
        <input
          ref={ref}
          id={inputId}
          className={`block w-full px-3 py-2 ${className}`}
          style={{
            fontFamily: "'Plus Jakarta Sans', system-ui, sans-serif",
            fontSize: '0.9rem',
            background: 'rgba(255,255,255,0.82)',
            backdropFilter: 'blur(8px)',
            WebkitBackdropFilter: 'blur(8px)',
            border: error ? '1px solid #fca5a5' : '1px solid rgba(124,58,237,0.18)',
            borderRadius: '9px',
            color: '#1e1b4b',
            outline: 'none',
            transition: 'border-color 0.2s, box-shadow 0.2s',
            ...style,
          }}
          onFocus={e => {
            e.currentTarget.style.borderColor = error ? '#f87171' : '#7c3aed'
            e.currentTarget.style.boxShadow = error
              ? '0 0 0 3px rgba(248,113,113,0.12)'
              : '0 0 0 3px rgba(124,58,237,0.1)'
          }}
          onBlur={e => {
            e.currentTarget.style.borderColor = error ? '#fca5a5' : 'rgba(124,58,237,0.18)'
            e.currentTarget.style.boxShadow = 'none'
          }}
          {...props}
        />
        {error && (
          <p style={{
            margin: 0, fontSize: '0.75rem', color: '#dc2626',
            fontFamily: "'Plus Jakarta Sans', system-ui, sans-serif",
          }}>
            {error}
          </p>
        )}
      </div>
    )
  }
)

Input.displayName = 'Input'
