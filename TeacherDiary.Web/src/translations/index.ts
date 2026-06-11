import * as bg from './bg'

export interface Language {
  code: string
  name: string
  translate: (text: string) => string
}

export const LANGUAGES: Language[] = [
  { code: 'bg', name: bg.name, translate: bg.translate },
]

export const DEFAULT_LANGUAGE = LANGUAGES[0]
