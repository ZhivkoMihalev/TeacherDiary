import { useState, useRef, useEffect } from 'react'
import { useMutation } from '@tanstack/react-query'
import { isAxiosError } from 'axios'
import { translationApi } from '../api/messages'
import type { TranslationResult } from '../types'

const SUPPORTED_LANGUAGES = [
  { code: 'TR', label: 'Турски' },
  { code: 'EN', label: 'Английски' },
  { code: 'UK', label: 'Украински' },
  { code: 'RU', label: 'Руски' },
  { code: 'RO', label: 'Румънски' },
  { code: 'EL', label: 'Гръцки' },
  { code: 'DE', label: 'Немски' },
] as const

interface TranslateButtonProps {
  text: string
}

const FONT = "'Plus Jakarta Sans', sans-serif"
const VIOLET = '#7c3aed'

export function TranslateButton({ text }: TranslateButtonProps) {
  const [open, setOpen] = useState(false)
  const [result, setResult] = useState<TranslationResult | null>(null)
  const containerRef = useRef<HTMLDivElement>(null)

  const mutation = useMutation({
    mutationFn: (targetLanguage: string) => translationApi.translate(text, targetLanguage),
    onSuccess: (data, targetLanguage) => {
      setResult({ language: targetLanguage, translatedText: data.translatedText })
    },
  })

  useEffect(() => {
    if (!open) return
    function handleMouseDown(e: MouseEvent) {
      if (containerRef.current && !containerRef.current.contains(e.target as Node)) {
        setOpen(false)
      }
    }
    document.addEventListener('mousedown', handleMouseDown)
    return () => document.removeEventListener('mousedown', handleMouseDown)
  }, [open])

  function handleSelect(code: string) {
    setOpen(false)
    if (result?.language === code) return
    setResult(null)
    mutation.mutate(code)
  }

  function handleHide() {
    setResult(null)
    mutation.reset()
  }

  const errorMessage = mutation.isError
    ? (isAxiosError<{ error?: string }>(mutation.error) && mutation.error.response?.data?.error) ||
      'Преводът неуспешен. Опитайте отново.'
    : null

  const resultLabel = result
    ? SUPPORTED_LANGUAGES.find((l) => l.code === result.language)?.label ?? result.language
    : null

  return (
    <div ref={containerRef} style={{ fontFamily: FONT, position: 'relative' }}>
      <div style={{ position: 'relative', display: 'inline-block' }}>
        <button
          type="button"
          onClick={() => setOpen((v) => !v)}
          style={{
            display: 'inline-flex',
            alignItems: 'center',
            gap: '0.25rem',
            fontFamily: FONT,
            fontSize: '0.75rem',
            fontWeight: 600,
            color: VIOLET,
            background: 'none',
            border: 'none',
            padding: '2px 0',
            cursor: 'pointer',
            opacity: mutation.isPending ? 0.6 : 1,
          }}
          disabled={mutation.isPending}
        >
          <span aria-hidden>🌐</span>
          {mutation.isPending ? (
            <span style={{ display: 'inline-flex', alignItems: 'center', gap: '0.35rem' }}>
              <span
                style={{
                  width: '11px',
                  height: '11px',
                  border: `2px solid rgba(124,58,237,0.25)`,
                  borderTopColor: VIOLET,
                  borderRadius: '50%',
                  display: 'inline-block',
                  animation: 'translate-spin 0.6s linear infinite',
                }}
              />
              Превеждане...
            </span>
          ) : (
            'Преведи'
          )}
        </button>

        {open && (
          <div
            style={{
              position: 'absolute',
              top: 'calc(100% + 4px)',
              left: 0,
              zIndex: 30,
              minWidth: '160px',
              background: 'rgba(255,255,255,0.97)',
              backdropFilter: 'blur(12px)',
              WebkitBackdropFilter: 'blur(12px)',
              border: '1px solid rgba(124,58,237,0.15)',
              borderRadius: '10px',
              boxShadow: '0 4px 20px rgba(124,58,237,0.15)',
              padding: '4px',
              fontFamily: FONT,
            }}
          >
            {SUPPORTED_LANGUAGES.map((lang) => {
              const selected = result?.language === lang.code
              return (
                <button
                  key={lang.code}
                  type="button"
                  onClick={() => handleSelect(lang.code)}
                  style={{
                    display: 'flex',
                    alignItems: 'center',
                    justifyContent: 'space-between',
                    width: '100%',
                    textAlign: 'left',
                    fontFamily: FONT,
                    fontSize: '0.8rem',
                    color: '#374151',
                    background: 'transparent',
                    border: 'none',
                    borderRadius: '6px',
                    padding: '6px 10px',
                    cursor: 'pointer',
                  }}
                  onMouseEnter={(e) => {
                    e.currentTarget.style.background = 'rgba(124,58,237,0.06)'
                  }}
                  onMouseLeave={(e) => {
                    e.currentTarget.style.background = 'transparent'
                  }}
                >
                  <span>{lang.label}</span>
                  {selected && <span style={{ color: VIOLET, fontWeight: 700 }}>✓</span>}
                </button>
              )
            })}
          </div>
        )}
      </div>

      {errorMessage && !mutation.isPending && (
        <p
          style={{
            fontFamily: FONT,
            fontSize: '0.75rem',
            color: '#dc2626',
            marginTop: '4px',
          }}
        >
          {errorMessage}
        </p>
      )}

      {result && !mutation.isPending && (
        <div
          style={{
            marginTop: '6px',
            paddingTop: '6px',
            borderTop: '1px solid rgba(124,58,237,0.08)',
          }}
        >
          <div
            style={{
              display: 'flex',
              alignItems: 'center',
              justifyContent: 'space-between',
              gap: '0.5rem',
              marginBottom: '2px',
            }}
          >
            <span style={{ fontFamily: FONT, fontSize: '0.7rem', fontWeight: 600, color: VIOLET }}>
              🌐 Превод ({resultLabel}):
            </span>
            <button
              type="button"
              onClick={handleHide}
              style={{
                fontFamily: FONT,
                fontSize: '0.7rem',
                color: VIOLET,
                background: 'none',
                border: 'none',
                padding: 0,
                cursor: 'pointer',
                textDecoration: 'underline',
              }}
            >
              Скрий превода
            </button>
          </div>
          <p
            style={{
              fontFamily: FONT,
              fontSize: '0.85rem',
              fontStyle: 'italic',
              color: '#4b5563',
              margin: 0,
              whiteSpace: 'pre-wrap',
              wordBreak: 'break-word',
            }}
          >
            {result.translatedText}
          </p>
        </div>
      )}

      <style>{`@keyframes translate-spin { to { transform: rotate(360deg); } }`}</style>
    </div>
  )
}
