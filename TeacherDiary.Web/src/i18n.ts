import i18n from 'i18next'
import { initReactI18next } from 'react-i18next'
import bg from './locales/bg'
import en from './locales/en'

const savedLang = (() => {
  try {
    return localStorage.getItem('app-language') ?? 'bg'
  } catch {
    return 'bg'
  }
})()

i18n.use(initReactI18next).init({
  resources: {
    bg: { translation: bg },
    en: { translation: en },
  },
  lng: savedLang,
  fallbackLng: 'bg',
  interpolation: {
    escapeValue: false,
  },
})

export default i18n
