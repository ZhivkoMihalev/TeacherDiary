import { createContext, useContext, useState, useCallback, type ReactNode } from 'react'
import { LANGUAGES, DEFAULT_LANGUAGE, type Language } from '../translations'

const STORAGE_KEY = 'app-language'

interface LanguageContextValue {
  language: Language
  setLanguage: (lang: Language) => void
  translate: (text: string) => string
}

const LanguageContext = createContext<LanguageContextValue | null>(null)

function loadLanguage(): Language {
  try {
    const code = localStorage.getItem(STORAGE_KEY)
    return LANGUAGES.find(l => l.code === code) ?? DEFAULT_LANGUAGE
  } catch {
    return DEFAULT_LANGUAGE
  }
}

export function LanguageProvider({ children }: { children: ReactNode }) {
  const [language, setLanguageState] = useState<Language>(loadLanguage)

  const setLanguage = useCallback((lang: Language) => {
    localStorage.setItem(STORAGE_KEY, lang.code)
    setLanguageState(lang)
  }, [])

  const translate = useCallback((text: string) => language.translate(text), [language])

  return (
    <LanguageContext.Provider value={{ language, setLanguage, translate }}>
      {children}
    </LanguageContext.Provider>
  )
}

export function useLanguage() {
  const ctx = useContext(LanguageContext)
  if (!ctx) throw new Error('useLanguage must be used inside LanguageProvider')
  return ctx
}
