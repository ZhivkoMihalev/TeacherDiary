import { useQuery } from '@tanstack/react-query'
import { studentApi } from '../../api/student'
import { Spinner } from '../../components/ui/Spinner'

// Badge tier data (mirrors BadgeCodes.cs)
const STREAK_TIERS = [3, 5, 7, 15, 30, 45, 60, 90, 180, 360]
const POINTS_TIERS = [100, 250, 500, 1000, 1500, 2000, 3000, 5000, 7500, 10000]

function nextTier(value: number, tiers: number[]): { current: number | null; next: number | null; pct: number } {
  const current = [...tiers].reverse().find(t => value >= t) ?? null
  const next = tiers.find(t => t > value) ?? null
  if (!next) return { current, next: null, pct: 100 }
  const base = current ?? 0
  const pct = Math.min(100, Math.round(((value - base) / (next - base)) * 100))
  return { current, next, pct }
}

const FONT = "'Plus Jakarta Sans', sans-serif"

export function StudentBadgesPage() {
  const { data: badges = [], isLoading: badgesLoading } = useQuery({
    queryKey: ['student-badges'],
    queryFn: studentApi.getMyBadges,
  })

  const { data: details, isLoading: detailsLoading } = useQuery({
    queryKey: ['student-me'],
    queryFn: studentApi.getMyDetails,
  })

  if (badgesLoading || detailsLoading) return <div className="flex items-center justify-center h-64"><Spinner /></div>

  const streak = details?.bestStreak ?? 0
  const points = details?.totalPoints ?? 0
  const streakProgress = nextTier(streak, STREAK_TIERS)
  const pointsProgress = nextTier(points, POINTS_TIERS)

  return (
    <div style={{ maxWidth: '720px', margin: '0 auto', padding: '24px 16px 48px' }}>
      {/* Header */}
      <div style={{ marginBottom: '22px' }}>
        <h1 style={{ margin: '0 0 3px', fontSize: '1.375rem', fontWeight: 700, fontFamily: "'Bricolage Grotesque', 'Plus Jakarta Sans', sans-serif", color: '#1e1b4b' }}>
          Моите значки
        </h1>
        <p style={{ margin: 0, fontSize: '0.82rem', color: '#a78bfa', fontFamily: FONT }}>
          Спечелени: <strong style={{ color: '#7c3aed' }}>{badges.length}</strong>
          {' '}· Чети и изпълнявай задачи, за да спечелиш повече!
        </p>
      </div>

      {/* Progress cards */}
      {details && (streakProgress.next !== null || pointsProgress.next !== null) && (
        <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fill, minmax(260px, 1fr))', gap: '12px', marginBottom: '24px' }}>

          {/* Streak progress */}
          {streakProgress.next !== null && (
            <div style={{
              background: 'rgba(255,255,255,0.75)',
              backdropFilter: 'blur(20px)',
              WebkitBackdropFilter: 'blur(20px)',
              borderRadius: '14px',
              border: '1px solid rgba(251,146,60,0.25)',
              boxShadow: '0 2px 12px rgba(251,146,60,0.1)',
              padding: '16px',
            }}>
              <div style={{ display: 'flex', alignItems: 'center', gap: '10px', marginBottom: '12px' }}>
                <div style={{ width: '36px', height: '36px', borderRadius: '10px', background: 'linear-gradient(135deg, #fb923c, #f97316)', display: 'flex', alignItems: 'center', justifyContent: 'center', fontSize: '1.1rem', flexShrink: 0 }}>🔥</div>
                <div>
                  <div style={{ fontSize: '0.82rem', fontWeight: 700, color: '#1e1b4b', fontFamily: FONT }}>Серия на активност</div>
                  <div style={{ fontSize: '0.72rem', color: '#a78bfa', fontFamily: FONT }}>
                    {streak} / {streakProgress.next} дни
                  </div>
                </div>
              </div>
              <div style={{ height: '7px', borderRadius: '4px', background: 'rgba(251,146,60,0.15)', overflow: 'hidden' }}>
                <div style={{ height: '100%', width: `${streakProgress.pct}%`, borderRadius: '4px', background: 'linear-gradient(90deg, #fb923c, #f97316)', transition: 'width 0.4s' }} />
              </div>
              <div style={{ display: 'flex', justifyContent: 'space-between', marginTop: '5px' }}>
                <span style={{ fontSize: '0.7rem', color: '#c2410c', fontFamily: FONT, fontWeight: 600 }}>{streakProgress.pct}%</span>
                <span style={{ fontSize: '0.7rem', color: '#a78bfa', fontFamily: FONT }}>
                  Следваща значка при {streakProgress.next} дни 🔥
                </span>
              </div>
            </div>
          )}

          {/* Points progress */}
          {pointsProgress.next !== null && (
            <div style={{
              background: 'rgba(255,255,255,0.75)',
              backdropFilter: 'blur(20px)',
              WebkitBackdropFilter: 'blur(20px)',
              borderRadius: '14px',
              border: '1px solid rgba(245,158,11,0.25)',
              boxShadow: '0 2px 12px rgba(245,158,11,0.1)',
              padding: '16px',
            }}>
              <div style={{ display: 'flex', alignItems: 'center', gap: '10px', marginBottom: '12px' }}>
                <div style={{ width: '36px', height: '36px', borderRadius: '10px', background: 'linear-gradient(135deg, #fbbf24, #f59e0b)', display: 'flex', alignItems: 'center', justifyContent: 'center', fontSize: '1.1rem', flexShrink: 0 }}>⚡</div>
                <div>
                  <div style={{ fontSize: '0.82rem', fontWeight: 700, color: '#1e1b4b', fontFamily: FONT }}>Точки</div>
                  <div style={{ fontSize: '0.72rem', color: '#a78bfa', fontFamily: FONT }}>
                    {points} / {pointsProgress.next} т
                  </div>
                </div>
              </div>
              <div style={{ height: '7px', borderRadius: '4px', background: 'rgba(245,158,11,0.15)', overflow: 'hidden' }}>
                <div style={{ height: '100%', width: `${pointsProgress.pct}%`, borderRadius: '4px', background: 'linear-gradient(90deg, #fbbf24, #f59e0b)', transition: 'width 0.4s' }} />
              </div>
              <div style={{ display: 'flex', justifyContent: 'space-between', marginTop: '5px' }}>
                <span style={{ fontSize: '0.7rem', color: '#b45309', fontFamily: FONT, fontWeight: 600 }}>{pointsProgress.pct}%</span>
                <span style={{ fontSize: '0.7rem', color: '#a78bfa', fontFamily: FONT }}>
                  Следваща значка при {pointsProgress.next} т ⭐
                </span>
              </div>
            </div>
          )}
        </div>
      )}

      {/* Badge grid */}
      {badges.length === 0 ? (
        <div style={{
          textAlign: 'center',
          padding: '64px 24px',
          background: 'rgba(255,255,255,0.72)',
          backdropFilter: 'blur(20px)',
          WebkitBackdropFilter: 'blur(20px)',
          borderRadius: '16px',
          border: '1px solid rgba(124,58,237,0.1)',
          boxShadow: '0 2px 12px rgba(124,58,237,0.07)',
        }}>
          <div style={{ fontSize: '3rem', marginBottom: '14px' }}>🏅</div>
          <p style={{ margin: 0, fontSize: '1rem', fontWeight: 700, color: '#1e1b4b', fontFamily: FONT }}>Все още нямаш значки</p>
          <p style={{ margin: '6px 0 0', fontSize: '0.85rem', color: '#a78bfa', fontFamily: FONT }}>
            Чети книги и изпълнявай задачи, за да спечелиш!
          </p>
        </div>
      ) : (
        <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fill, minmax(190px, 1fr))', gap: '14px' }}>
          {badges.map((b) => (
            <BadgeCard key={b.code} badge={b} />
          ))}
        </div>
      )}
    </div>
  )
}

interface BadgeData {
  code: string
  name: string
  description: string
  icon: string
  awardedAt: string | Date
}

function BadgeCard({ badge: b }: { badge: BadgeData }) {
  const gradient = pickGradient(b.code)

  return (
    <div
      style={{
        position: 'relative',
        display: 'flex',
        flexDirection: 'column',
        alignItems: 'center',
        borderRadius: '18px',
        padding: '20px 16px',
        textAlign: 'center',
        border: '1px solid rgba(255,255,255,0.6)',
        boxShadow: '0 4px 16px rgba(0,0,0,0.07)',
        background: gradient.bg,
        overflow: 'hidden',
        transition: 'transform 0.15s, box-shadow 0.15s',
        cursor: 'default',
      }}
      onMouseEnter={e => { (e.currentTarget as HTMLElement).style.transform = 'translateY(-2px)'; (e.currentTarget as HTMLElement).style.boxShadow = '0 8px 24px rgba(0,0,0,0.1)' }}
      onMouseLeave={e => { (e.currentTarget as HTMLElement).style.transform = 'none'; (e.currentTarget as HTMLElement).style.boxShadow = '0 4px 16px rgba(0,0,0,0.07)' }}
    >
      {/* Glow */}
      <div style={{ position: 'absolute', top: '6px', left: '50%', transform: 'translateX(-50%)', width: '80px', height: '80px', borderRadius: '50%', background: gradient.glow, opacity: 0.25, filter: 'blur(18px)', pointerEvents: 'none' }} />

      {/* Icon */}
      <div style={{ position: 'relative', zIndex: 1, marginBottom: '12px' }}>
        <div style={{ width: '64px', height: '64px', borderRadius: '18px', display: 'flex', alignItems: 'center', justifyContent: 'center', boxShadow: '0 4px 12px rgba(0,0,0,0.15)', background: gradient.iconBg }}>
          {b.icon && (b.icon.startsWith('/') || b.icon.startsWith('http')) ? (
            <img src={b.icon} alt={b.name} style={{ width: '40px', height: '40px', objectFit: 'contain' }} />
          ) : b.icon && /\P{ASCII}/u.test(b.icon) ? (
            <span style={{ fontSize: '1.75rem', lineHeight: 1 }}>{b.icon}</span>
          ) : (
            <span style={{ fontSize: '1.75rem', lineHeight: 1 }}>{gradient.fallbackEmoji}</span>
          )}
        </div>
      </div>

      <p style={{ position: 'relative', zIndex: 1, margin: '0 0 4px', fontSize: '0.845rem', fontWeight: 800, color: '#1e1b4b', fontFamily: FONT, lineHeight: 1.25 }}>{b.name}</p>
      <p style={{ position: 'relative', zIndex: 1, margin: 0, fontSize: '0.72rem', color: '#78716c', fontFamily: FONT, lineHeight: 1.4 }}>{b.description}</p>

      <div style={{ position: 'relative', zIndex: 1, marginTop: '10px', padding: '2px 10px', borderRadius: '99px', fontSize: '0.68rem', fontWeight: 700, background: gradient.dateBg, color: gradient.dateText, fontFamily: FONT }}>
        {new Date(b.awardedAt).toLocaleDateString('bg-BG')}
      </div>
    </div>
  )
}

interface GradientDef {
  bg: string
  glow: string
  iconBg: string
  dateBg: string
  dateText: string
  fallbackEmoji: string
}

function pickGradient(code: string): GradientDef {
  const c = code.toLowerCase()
  if (c.includes('streak') || c.includes('day')) return { bg: 'linear-gradient(145deg,#FFF7ED,#FFEDD5)', glow: '#FB923C', iconBg: 'linear-gradient(135deg,#FB923C,#F97316)', dateBg: '#FFEDD5', dateText: '#C2410C', fallbackEmoji: '🔥' }
  if (c.includes('points') || c.includes('reach')) return { bg: 'linear-gradient(145deg,#FFFBEB,#FEF3C7)', glow: '#FBBF24', iconBg: 'linear-gradient(135deg,#FBBF24,#F59E0B)', dateBg: '#FEF3C7', dateText: '#B45309', fallbackEmoji: '⭐' }
  if (c.includes('pages') || c.includes('read_')) return { bg: 'linear-gradient(145deg,#ECFDF5,#D1FAE5)', glow: '#34D399', iconBg: 'linear-gradient(135deg,#34D399,#10B981)', dateBg: '#D1FAE5', dateText: '#065F46', fallbackEmoji: '📜' }
  if (c.includes('book')) return { bg: 'linear-gradient(145deg,#ECFDF5,#D1FAE5)', glow: '#34D399', iconBg: 'linear-gradient(135deg,#34D399,#10B981)', dateBg: '#D1FAE5', dateText: '#065F46', fallbackEmoji: '📖' }
  if (c.includes('assignment') || c.includes('complete')) return { bg: 'linear-gradient(145deg,#EEF2FF,#E0E7FF)', glow: '#818CF8', iconBg: 'linear-gradient(135deg,#818CF8,#6366F1)', dateBg: '#E0E7FF', dateText: '#3730A3', fallbackEmoji: '✅' }
  return { bg: 'linear-gradient(145deg,#F0F9FF,#E0F2FE)', glow: '#38BDF8', iconBg: 'linear-gradient(135deg,#38BDF8,#0EA5E9)', dateBg: '#E0F2FE', dateText: '#0369A1', fallbackEmoji: '🏅' }
}
