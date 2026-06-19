import i18n from 'i18next'
import { initReactI18next } from 'react-i18next'
import bg from './locales/bg'
import en from './locales/en'
import tr from './locales/tr'
import ru from './locales/ru'
import uk from './locales/uk'

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
    tr: { translation: tr },
    ru: { translation: ru },
    uk: { translation: uk },
  },
  lng: savedLang,
  fallbackLng: 'bg',
  interpolation: {
    escapeValue: false,
  },
})

export default i18n
