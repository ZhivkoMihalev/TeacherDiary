import { useState, useRef, useEffect } from 'react'
import { useLanguage } from '../context/LanguageContext'
import { LANGUAGES } from '../translations'

export function LanguageSelector() {
  const { language, setLanguage } = useLanguage()
  const [open, setOpen] = useState(false)
  const containerRef = useRef<HTMLDivElement>(null)

  useEffect(() => {
    if (!open) return
    function handleClickOutside(e: MouseEvent) {
      if (containerRef.current && !containerRef.current.contains(e.target as Node)) {
        setOpen(false)
      }
    }
    document.addEventListener('mousedown', handleClickOutside)
    return () => document.removeEventListener('mousedown', handleClickOutside)
  }, [open])

  return (
    <div ref={containerRef} style={{ position: 'relative', fontFamily: "'Plus Jakarta Sans', sans-serif" }}>
      <button
        type="button"
        onClick={() => setOpen(v => !v)}
        style={{
          display: 'flex',
          alignItems: 'center',
          gap: '8px',
          width: '100%',
          padding: '8px 10px',
          fontSize: '0.78rem',
          fontWeight: 600,
          color: '#1e1b4b',
          background: 'rgba(124,58,237,0.06)',
          border: '1px solid rgba(124,58,237,0.12)',
          borderRadius: '10px',
          cursor: 'pointer',
          transition: 'background 0.15s, border-color 0.15s',
        }}
        onMouseEnter={e => { e.currentTarget.style.background = 'rgba(124,58,237,0.1)'; e.currentTarget.style.borderColor = 'rgba(124,58,237,0.22)' }}
        onMouseLeave={e => { e.currentTarget.style.background = 'rgba(124,58,237,0.06)'; e.currentTarget.style.borderColor = 'rgba(124,58,237,0.12)' }}
      >
        <span style={{ fontSize: '0.95em' }}>🌐</span>
        <span style={{ flex: 1, textAlign: 'left', overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap' }}>
          {language.name}
        </span>
        <svg
          xmlns="http://www.w3.org/2000/svg"
          viewBox="0 0 20 20"
          fill="currentColor"
          style={{
            width: '12px', height: '12px', flexShrink: 0, color: '#a78bfa',
            transition: 'transform 0.2s ease',
            transform: open ? 'rotate(180deg)' : 'none',
          }}
        >
          <path fillRule="evenodd" d="M5.23 7.21a.75.75 0 011.06.02L10 11.168l3.71-3.938a.75.75 0 111.08 1.04l-4.25 4.5a.75.75 0 01-1.08 0l-4.25-4.5a.75.75 0 01.02-1.06z" clipRule="evenodd" />
        </svg>
      </button>

      {open && (
        <div
          style={{
            position: 'absolute',
            bottom: 'calc(100% + 6px)',
            left: 0,
            right: 0,
            background: 'rgba(255,255,255,0.92)',
            backdropFilter: 'blur(20px)',
            WebkitBackdropFilter: 'blur(20px)',
            border: '1px solid rgba(255,255,255,0.92)',
            borderRadius: '12px',
            boxShadow: '0 8px 32px rgba(124,58,237,0.16), 0 2px 8px rgba(0,0,0,0.06)',
            padding: '5px',
            zIndex: 60,
          }}
        >
          {LANGUAGES.map(lang => {
            const isActive = lang.code === language.code
            return (
              <button
                key={lang.code}
                type="button"
                onClick={() => { setLanguage(lang); setOpen(false) }}
                style={{
                  display: 'flex',
                  alignItems: 'center',
                  gap: '8px',
                  width: '100%',
                  padding: '8px 10px',
                  fontSize: '0.78rem',
                  fontWeight: isActive ? 700 : 500,
                  color: isActive ? '#7c3aed' : '#1e1b4b',
                  background: isActive ? 'rgba(124,58,237,0.08)' : 'none',
                  border: 'none',
                  borderRadius: '8px',
                  cursor: 'pointer',
                  textAlign: 'left',
                  transition: 'background 0.15s',
                }}
                onMouseEnter={e => { if (!isActive) e.currentTarget.style.background = 'rgba(124,58,237,0.05)' }}
                onMouseLeave={e => { if (!isActive) e.currentTarget.style.background = 'none' }}
              >
                <span style={{ flex: 1 }}>{lang.name}</span>
                {isActive && <span style={{ color: '#7c3aed', fontWeight: 700 }}>✓</span>}
              </button>
            )
          })}
        </div>
      )}
    </div>
  )
}
