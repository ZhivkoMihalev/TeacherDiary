import { useEffect } from 'react'
import { useQuery } from '@tanstack/react-query'
import { gamificationSummaryApi } from '../../api/student'
import { MedalIcon } from '../ui/MedalIcon'
import type { GamificationSummaryDto } from '../../types'

const FONT = "'Plus Jakarta Sans', sans-serif"

const HERO_STYLES = `
  @keyframes gamHeroPulse {
    0%, 100% { transform: scale(1); }
    50% { transform: scale(1.12); }
  }
  @media (min-width: 720px) {
    .gam-hero-grid { grid-template-columns: repeat(4, minmax(0, 1fr)) !important; }
  }
`

function useHeroStyles() {
  useEffect(() => {
    const id = 'gam-hero-styles'
    if (document.getElementById(id)) return
    const el = document.createElement('style')
    el.id = id
    el.textContent = HERO_STYLES
    document.head.appendChild(el)
    return () => { el.remove() }
  }, [])
}

interface StatBoxProps {
  icon: string
  iconGradient: string
  value: string
  label: string
  highlight?: boolean
  sub?: string
}

function StatBox({ icon, iconGradient, value, label, highlight, sub }: StatBoxProps) {
  return (
    <div
      style={{
        display: 'flex',
        alignItems: 'center',
        gap: '12px',
        background: 'rgba(255,255,255,0.7)',
        backdropFilter: 'blur(20px)',
        WebkitBackdropFilter: 'blur(20px)',
        borderRadius: '16px',
        border: highlight ? '1px solid rgba(124,58,237,0.35)' : '1px solid rgba(124,58,237,0.12)',
        boxShadow: highlight
          ? '0 4px 18px rgba(124,58,237,0.18)'
          : '0 2px 10px rgba(124,58,237,0.07)',
        padding: '14px 16px',
        minWidth: 0,
      }}
    >
      <div
        style={{
          width: '40px',
          height: '40px',
          borderRadius: '12px',
          background: iconGradient,
          display: 'flex',
          alignItems: 'center',
          justifyContent: 'center',
          fontSize: '1.3rem',
          flexShrink: 0,
          animation: highlight ? 'gamHeroPulse 1.6s ease-in-out infinite' : undefined,
        }}
      >
        {icon}
      </div>
      <div style={{ minWidth: 0 }}>
        <div
          style={{
            fontSize: '1.55rem',
            fontWeight: 900,
            color: '#1e1b4b',
            fontFamily: FONT,
            lineHeight: 1,
            whiteSpace: 'nowrap',
          }}
        >
          {value}
        </div>
        <div
          style={{
            marginTop: '3px',
            fontSize: '0.72rem',
            fontWeight: 600,
            color: '#7c3aed',
            fontFamily: FONT,
          }}
        >
          {label}
        </div>
        {sub && (
          <div style={{ marginTop: '1px', fontSize: '0.68rem', color: '#a78bfa', fontFamily: FONT }}>
            {sub}
          </div>
        )}
      </div>
    </div>
  )
}

export function GamificationHero() {
  useHeroStyles()

  const { data, isLoading, isError, error } = useQuery<GamificationSummaryDto>({
    queryKey: ['student-gamification-summary'],
    queryFn: gamificationSummaryApi.get,
    staleTime: 30_000,
  })

  if (isLoading) {
    return (
      <div
        role="status"
        aria-busy="true"
        aria-label="Зареждане на напредък"
        style={{
          borderRadius: '20px',
          marginBottom: '20px',
          background: 'linear-gradient(135deg, rgba(124,58,237,0.12) 0%, rgba(168,85,247,0.1) 50%, rgba(99,102,241,0.12) 100%)',
          border: '1px solid rgba(124,58,237,0.1)',
          height: '128px',
        }}
      />
    )
  }

  if (isError) {
    const status = (error as { response?: { status?: number } })?.response?.status
    // 404 means student is not enrolled in a class — expected state, hide hero silently
    if (status === 404) return null
    // Other errors — show a minimal non-blocking fallback
    return (
      <div
        style={{
          borderRadius: '20px',
          marginBottom: '20px',
          padding: '14px 18px',
          background: 'rgba(124,58,237,0.05)',
          border: '1px solid rgba(124,58,237,0.1)',
          fontSize: '0.8rem',
          color: '#a78bfa',
          fontFamily: FONT,
          textAlign: 'center',
        }}
      >
        Данните за напредъка не могат да се заредят в момента.
      </div>
    )
  }

  if (!data) return null

  const streakHighlight = data.currentStreak > 0

  return (
    <div
      style={{
        position: 'relative',
        borderRadius: '20px',
        overflow: 'hidden',
        marginBottom: '20px',
        background:
          'linear-gradient(135deg, rgba(124,58,237,0.16) 0%, rgba(168,85,247,0.12) 45%, rgba(99,102,241,0.16) 100%)',
        border: '1px solid rgba(124,58,237,0.15)',
        padding: '18px',
      }}
    >
      {/* Decorative glow */}
      <div
        aria-hidden="true"
        style={{
          position: 'absolute',
          top: '-60px',
          right: '-60px',
          width: '220px',
          height: '220px',
          background: 'radial-gradient(circle, rgba(124,58,237,0.25) 0%, transparent 70%)',
          pointerEvents: 'none',
        }}
      />

      {/* Header + recent badge pill */}
      <div
        style={{
          position: 'relative',
          display: 'flex',
          alignItems: 'center',
          justifyContent: 'space-between',
          flexWrap: 'wrap',
          gap: '8px',
          marginBottom: '14px',
        }}
      >
        <div style={{ display: 'flex', alignItems: 'center', gap: '8px' }}>
          <span aria-hidden="true" style={{ fontSize: '1.05rem' }}>🎮</span>
          <span style={{ fontSize: '0.9rem', fontWeight: 800, color: '#1e1b4b', fontFamily: FONT }}>
            Твоят напредък
          </span>
        </div>

        {data.recentBadge && (
          <div
            style={{
              display: 'flex',
              alignItems: 'center',
              gap: '7px',
              background: 'rgba(255,255,255,0.75)',
              border: '1px solid rgba(124,58,237,0.18)',
              borderRadius: '99px',
              padding: '4px 12px 4px 6px',
              boxShadow: '0 2px 8px rgba(124,58,237,0.1)',
            }}
            title={`Получена награда: ${data.recentBadge.name}`}
          >
            <MedalIcon code={data.recentBadge.code} size="sm" />
            <div style={{ minWidth: 0 }}>
              <div
                style={{
                  fontSize: '0.6rem',
                  fontWeight: 700,
                  color: '#a78bfa',
                  fontFamily: FONT,
                  textTransform: 'uppercase',
                  letterSpacing: '0.05em',
                  lineHeight: 1,
                }}
              >
                Нова награда
              </div>
              <div
                style={{
                  fontSize: '0.8rem',
                  fontWeight: 700,
                  color: '#7c3aed',
                  fontFamily: FONT,
                  lineHeight: 1.2,
                  overflow: 'hidden',
                  textOverflow: 'ellipsis',
                  whiteSpace: 'nowrap',
                  maxWidth: '180px',
                }}
              >
                {data.recentBadge.name}
              </div>
            </div>
          </div>
        )}
      </div>

      {/* Stat boxes — 4 в ред на десктоп, 2×2 на мобилен */}
      <div
        className="gam-hero-grid"
        style={{
          position: 'relative',
          display: 'grid',
          gridTemplateColumns: 'repeat(2, minmax(0, 1fr))',
          gap: '10px',
        }}
      >
        <StatBox
          icon="🔥"
          iconGradient="linear-gradient(135deg, #ef4444, #f97316)"
          value={String(data.currentStreak)}
          label="дни серия"
          sub={`Рекорд: ${data.bestStreak} дни`}
          highlight={streakHighlight}
        />
        <StatBox
          icon="⭐"
          iconGradient="linear-gradient(135deg, #f59e0b, #fbbf24)"
          value={String(data.totalPoints)}
          label="точки"
        />
        <StatBox
          icon="🏆"
          iconGradient="linear-gradient(135deg, #7c3aed, #db2777)"
          value={data.classSize > 0 ? `#${data.classRank} / ${data.classSize}` : `#${data.classRank}`}
          label="в класа"
        />
        <StatBox
          icon="🎖️"
          iconGradient="linear-gradient(135deg, #6366f1, #a78bfa)"
          value={String(data.totalBadges)}
          label="награди"
        />
      </div>
    </div>
  )
}
