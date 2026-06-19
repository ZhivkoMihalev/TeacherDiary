import * as bg from './bg'
import * as en from './en'
import * as tr from './tr'
import * as ru from './ru'
import * as uk from './uk'

export interface Language {
  code: string
  name: string
  translate: (text: string) => string
}

export const LANGUAGES: Language[] = [
  { code: 'bg', name: bg.name, translate: bg.translate },
  { code: 'en', name: en.name, translate: en.translate },
  { code: 'tr', name: tr.name, translate: tr.translate },
  { code: 'ru', name: ru.name, translate: ru.translate },
  { code: 'uk', name: uk.name, translate: uk.translate },
]

export const DEFAULT_LANGUAGE = LANGUAGES[0]
