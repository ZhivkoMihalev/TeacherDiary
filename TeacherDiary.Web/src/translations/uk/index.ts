export const name = 'Українська'

export function translate(text: string): string {
  if (!text) return text

  // — Activity descriptions —
  const readMatch = text.match(/^Read (\d+) pages?$/)
  if (readMatch) return `Прочитав ${readMatch[1]} стор.`

  const activityMap: Record<string, string> = {
    'Completed assignment':        'Виконав завдання',
    'Started assignment':          'Розпочав завдання',
    'Completed challenge':         'Виконав виклик',
    'Updated challenge progress':  'Оновив прогрес виклику',
    'Completed learning activity': 'Завершив навчальне завдання',
    'Started learning activity':   'Розпочав навчальне завдання',
    'Activity':                    'Активність',
  }
  if (activityMap[text]) return activityMap[text]

  // — Notification messages (dynamic — use regex) —
  const notifPatterns: Array<[RegExp, (...args: string[]) => string]> = [
    [/^New message from (.+)$/,                           (n)    => `Нове повідомлення від ${n}`],
    [/^You have a new assignment: (.+)\.$/,               (t)    => `У вас нове завдання: ${t}.`],
    [/^You have a new book to read: (.+)\.$/,             (t)    => `Вам призначено нову книгу для читання: ${t}.`],
    [/^New challenge: (.+)\.$/,                           (t)    => `Новий виклик: ${t}.`],
    [/^You earned a badge: (.+)\.$/,                      (n)    => `Ви отримали значок: ${n}.`],
    [/^(.+) completed assignment: (.+)\.$/,               (s, t) => `${s} виконав завдання: ${t}.`],
    [/^(.+) completed the book: (.+)\.$/,                 (s, t) => `${s} прочитав книгу: ${t}.`],
    [/^(.+) completed the challenge: (.+)\.$/,            (s, t) => `${s} виконав виклик: ${t}.`],
    [/^(.+) joined the class\.$/,                         (s)    => `${s} приєднався до класу.`],
    [/^The deadline for assignment: (.+) has passed\.$/,  (t)    => `Термін виконання завдання минув: ${t}.`],
    [/^The deadline for reading: (.+) has passed\.$/,     (t)    => `Термін читання книги минув: ${t}.`],
    [/^Don't forget! Study today to keep your streak\.$/, ()     => 'Не забудь! Займайся сьогодні, щоб зберегти серію.'],
    [/^Your (\d+)-day streak was broken\. Keep studying every day!$/, (n) => `Твою серію з ${n} днів перервано. Продовжуй займатися щодня!`],
  ]
  for (const [pattern, fn] of notifPatterns) {
    const m = text.match(pattern)
    if (m) return fn(...m.slice(1))
  }

  // — Common —
  if (text === '[Image]')  return '[Зображення]'
  if (text === 'Unknown')  return 'Невідомо'

  return text
}
