export const name = 'Türkçe'

export function translate(text: string): string {
  if (!text) return text

  // — Activity descriptions —
  const readMatch = text.match(/^Read (\d+) pages?$/)
  if (readMatch) return `${readMatch[1]} sf. okudu`

  const activityMap: Record<string, string> = {
    'Completed assignment':        'Ödevi tamamladı',
    'Started assignment':          'Ödeve başladı',
    'Completed challenge':         'Meydan okumayı tamamladı',
    'Updated challenge progress':  'Meydan okuma ilerlemesini güncelledi',
    'Completed learning activity': 'Öğrenme etkinliğini tamamladı',
    'Started learning activity':   'Öğrenme etkinliğine başladı',
    'Activity':                    'Aktivite',
  }
  if (activityMap[text]) return activityMap[text]

  // — Notification messages (dynamic — use regex) —
  const notifPatterns: Array<[RegExp, (...args: string[]) => string]> = [
    [/^New message from (.+)$/,                           (n)    => `${n} tarafından yeni mesaj`],
    [/^You have a new assignment: (.+)\.$/,               (t)    => `Yeni bir ödeviniz var: ${t}.`],
    [/^You have a new book to read: (.+)\.$/,             (t)    => `Okumak için yeni bir kitabınız var: ${t}.`],
    [/^New challenge: (.+)\.$/,                           (t)    => `Yeni meydan okuma: ${t}.`],
    [/^You earned a badge: (.+)\.$/,                      (n)    => `Rozet kazandınız: ${n}.`],
    [/^(.+) completed assignment: (.+)\.$/,               (s, t) => `${s} ödevi tamamladı: ${t}.`],
    [/^(.+) completed the book: (.+)\.$/,                 (s, t) => `${s} kitabı okudu: ${t}.`],
    [/^(.+) completed the challenge: (.+)\.$/,            (s, t) => `${s} meydan okumayı tamamladı: ${t}.`],
    [/^(.+) joined the class\.$/,                         (s)    => `${s} sınıfa katıldı.`],
    [/^The deadline for assignment: (.+) has passed\.$/,  (t)    => `Ödev son tarihi geçti: ${t}.`],
    [/^The deadline for reading: (.+) has passed\.$/,     (t)    => `Okuma son tarihi geçti: ${t}.`],
    [/^Don't forget! Study today to keep your streak\.$/, ()     => 'Unutma! Serinizi korumak için bugün de çalışın.'],
    [/^Your (\d+)-day streak was broken\. Keep studying every day!$/, (n) => `${n} günlük seriniz bozuldu. Her gün çalışmaya devam edin!`],
  ]
  for (const [pattern, fn] of notifPatterns) {
    const m = text.match(pattern)
    if (m) return fn(...m.slice(1))
  }

  // — Common —
  if (text === '[Image]')  return '[Görsel]'
  if (text === 'Unknown')  return 'Bilinmiyor'

  return text
}
