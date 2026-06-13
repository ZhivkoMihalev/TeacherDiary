import * as bg from './bg'
import * as en from './en'

export interface Language {
  code: string
  name: string
  translate: (text: string) => string
}

export const LANGUAGES: Language[] = [
  { code: 'bg', name: bg.name, translate: bg.translate },
  { code: 'en', name: en.name, translate: en.translate },
]

export const DEFAULT_LANGUAGE = LANGUAGES[0]
