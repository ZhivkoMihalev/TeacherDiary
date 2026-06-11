export const name = 'Български'

export function translate(text: string): string {
  if (!text) return text

  // — Activity descriptions —
  const readMatch = text.match(/^Read (\d+) pages?$/)
  if (readMatch) return `Прочел ${readMatch[1]} стр.`

  const activityMap: Record<string, string> = {
    'Completed assignment':        'Завърши задача',
    'Started assignment':          'Стартира задача',
    'Completed challenge':         'Завърши предизвикателство',
    'Updated challenge progress':  'Актуализира предизвикателство',
    'Completed learning activity': 'Завърши учебна дейност',
    'Started learning activity':   'Стартира учебна дейност',
    'Activity':                    'Активност',
  }
  if (activityMap[text]) return activityMap[text]

  // — Notification messages (dynamic — use regex) —
  const notifPatterns: Array<[RegExp, (...args: string[]) => string]> = [
    [/^New message from (.+)$/,                      (n)    => `Ново съобщение от ${n}`],
    [/^You have a new assignment: (.+)\.$/, (t)    => `Получихте нова задача: ${t}.`],
    [/^You have a new book to read: (.+)\.$/, (t)  => `Получихте нова книга за четене: ${t}.`],
    [/^New challenge: (.+)\.$/, (t)                => `Ново предизвикателство: ${t}.`],
    [/^You earned a badge: (.+)\.$/, (n)           => `Получихте медал: ${n}.`],
    [/^(.+) completed assignment: (.+)\.$/, (s, t) => `${s} завърши задача: ${t}.`],
    [/^(.+) completed the book: (.+)\.$/, (s, t)   => `${s} прочете книгата: ${t}.`],
    [/^(.+) completed the challenge: (.+)\.$/, (s, t) => `${s} завърши предизвикателството: ${t}.`],
    [/^(.+) joined the class\.$/, (s)              => `${s} се присъедини към класа.`],
    [/^The deadline for assignment: (.+) has passed\.$/, (t) => `Времето за завършване на задача: ${t} изтече.`],
    [/^The deadline for reading: (.+) has passed\.$/, (t)    => `Времето за прочитане на книга: ${t} изтече.`],
    [/^Don't forget! Study today to keep your streak\.$/, () => 'Не забравяй! Учи и днес, за да запазиш серията си.'],
    [/^Your (\d+)-day streak was broken\. Keep studying every day!$/, (n) => `Серията ти от ${n} дни беше прекъсната. Продължи да учиш ежедневно!`],
  ]
  for (const [pattern, fn] of notifPatterns) {
    const m = text.match(pattern)
    if (m) return fn(...m.slice(1))
  }

  // — Common —
  if (text === '[Image]')  return '[Снимка]'
  if (text === 'Unknown')  return 'Непознат'

  return text
}
