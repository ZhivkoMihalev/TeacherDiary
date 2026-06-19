export const name = 'Русский'

export function translate(text: string): string {
  if (!text) return text

  // — Activity descriptions —
  const readMatch = text.match(/^Read (\d+) pages?$/)
  if (readMatch) return `Прочитал ${readMatch[1]} стр.`

  const activityMap: Record<string, string> = {
    'Completed assignment':        'Выполнил задание',
    'Started assignment':          'Начал задание',
    'Completed challenge':         'Выполнил испытание',
    'Updated challenge progress':  'Обновил прогресс испытания',
    'Completed learning activity': 'Завершил учебное задание',
    'Started learning activity':   'Начал учебное задание',
    'Activity':                    'Активность',
  }
  if (activityMap[text]) return activityMap[text]

  // — Notification messages (dynamic — use regex) —
  const notifPatterns: Array<[RegExp, (...args: string[]) => string]> = [
    [/^New message from (.+)$/,                           (n)    => `Новое сообщение от ${n}`],
    [/^You have a new assignment: (.+)\.$/,               (t)    => `У вас новое задание: ${t}.`],
    [/^You have a new book to read: (.+)\.$/,             (t)    => `Вам назначена новая книга: ${t}.`],
    [/^New challenge: (.+)\.$/,                           (t)    => `Новое испытание: ${t}.`],
    [/^You earned a badge: (.+)\.$/,                      (n)    => `Вы получили значок: ${n}.`],
    [/^(.+) completed assignment: (.+)\.$/,               (s, t) => `${s} выполнил задание: ${t}.`],
    [/^(.+) completed the book: (.+)\.$/,                 (s, t) => `${s} прочитал книгу: ${t}.`],
    [/^(.+) completed the challenge: (.+)\.$/,            (s, t) => `${s} выполнил испытание: ${t}.`],
    [/^(.+) joined the class\.$/,                         (s)    => `${s} присоединился к классу.`],
    [/^The deadline for assignment: (.+) has passed\.$/,  (t)    => `Срок выполнения задания истёк: ${t}.`],
    [/^The deadline for reading: (.+) has passed\.$/,     (t)    => `Срок чтения книги истёк: ${t}.`],
    [/^Don't forget! Study today to keep your streak\.$/, ()     => 'Не забудь! Занимайся сегодня, чтобы сохранить серию.'],
    [/^Your (\d+)-day streak was broken\. Keep studying every day!$/, (n) => `Твоя серия из ${n} дней прервана. Продолжай заниматься каждый день!`],
  ]
  for (const [pattern, fn] of notifPatterns) {
    const m = text.match(pattern)
    if (m) return fn(...m.slice(1))
  }

  // — Common —
  if (text === '[Image]')  return '[Изображение]'
  if (text === 'Unknown')  return 'Неизвестно'

  return text
}
