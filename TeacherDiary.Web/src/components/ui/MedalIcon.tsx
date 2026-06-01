import type { FC } from 'react'

interface MedalDef {
  rim: string
  faceLight: string
  faceDark: string
  textColor: string
  label: string
  title: string
  category: 'streak' | 'points' | 'achievement'
}

const MEDALS: Record<string, MedalDef> = {
  // ── Streak medals ──────────────────────────────────────────────
  STREAK_3:        { rim: '#15803d', faceLight: '#bbf7d0', faceDark: '#4ade80',  textColor: '#14532d', label: '3д',   title: '3 дни подред',   category: 'streak' },
  STREAK_5:        { rim: '#0e7490', faceLight: '#cffafe', faceDark: '#22d3ee',  textColor: '#164e63', label: '5д',   title: '5 дни подред',   category: 'streak' },
  SEVEN_DAY_STREAK:{ rim: '#c2410c', faceLight: '#ffedd5', faceDark: '#fb923c',  textColor: '#7c2d12', label: '7д',   title: '7 дни подред',   category: 'streak' },
  STREAK_15:       { rim: '#a16207', faceLight: '#fefce8', faceDark: '#fde047',  textColor: '#713f12', label: '15д',  title: '15 дни подред',  category: 'streak' },
  STREAK_30:       { rim: '#b45309', faceLight: '#fef3c7', faceDark: '#fbbf24',  textColor: '#451a03', label: '30д',  title: '30 дни подред',  category: 'streak' },
  STREAK_45:       { rim: '#475569', faceLight: '#f1f5f9', faceDark: '#94a3b8',  textColor: '#0f172a', label: '45д',  title: '45 дни подред',  category: 'streak' },
  STREAK_60:       { rim: '#92400e', faceLight: '#fffbeb', faceDark: '#fcd34d',  textColor: '#78350f', label: '60д',  title: '60 дни подред',  category: 'streak' },
  STREAK_90:       { rim: '#075985', faceLight: '#e0f2fe', faceDark: '#38bdf8',  textColor: '#0c4a6e', label: '90д',  title: '90 дни подред',  category: 'streak' },
  STREAK_180:      { rim: '#6b21a8', faceLight: '#f3e8ff', faceDark: '#c084fc',  textColor: '#3b0764', label: '180д', title: '180 дни подред', category: 'streak' },
  STREAK_360:      { rim: '#9f1239', faceLight: '#fce7f3', faceDark: '#f472b6',  textColor: '#500724', label: '360д', title: '360 дни подред', category: 'streak' },

  // ── Points medals ──────────────────────────────────────────────
  POINTS_100:  { rim: '#b45309', faceLight: '#fef3c7', faceDark: '#f59e0b', textColor: '#451a03', label: '100',  title: '100 точки',    category: 'points' },
  POINTS_250:  { rim: '#0f766e', faceLight: '#ccfbf1', faceDark: '#14b8a6', textColor: '#134e4a', label: '250',  title: '250 точки',    category: 'points' },
  POINTS_500:  { rim: '#1d4ed8', faceLight: '#dbeafe', faceDark: '#3b82f6', textColor: '#1e3a8a', label: '500',  title: '500 точки',    category: 'points' },
  POINTS_1000: { rim: '#6d28d9', faceLight: '#ede9fe', faceDark: '#8b5cf6', textColor: '#2e1065', label: '1К',   title: '1000 точки',   category: 'points' },
  POINTS_1500: { rim: '#a16207', faceLight: '#fefce8', faceDark: '#eab308', textColor: '#713f12', label: '1.5К', title: '1500 точки',   category: 'points' },
  POINTS_2000: { rim: '#be185d', faceLight: '#fce7f3', faceDark: '#ec4899', textColor: '#500724', label: '2К',   title: '2000 точки',   category: 'points' },
  POINTS_3000: { rim: '#0f766e', faceLight: '#ccfbf1', faceDark: '#0d9488', textColor: '#134e4a', label: '3К',   title: '3000 точки',   category: 'points' },
  POINTS_5000: { rim: '#1d4ed8', faceLight: '#dbeafe', faceDark: '#2563eb', textColor: '#1e3a8a', label: '5К',   title: '5000 точки',   category: 'points' },
  POINTS_7500: { rim: '#4338ca', faceLight: '#e0e7ff', faceDark: '#6366f1', textColor: '#1e1b4b', label: '7.5К', title: '7500 точки',   category: 'points' },
  POINTS_10000:{ rim: '#7c2d12', faceLight: '#ffedd5', faceDark: '#ea580c', textColor: '#431407', label: '10К',  title: '10 000 точки', category: 'points' },

  // ── Achievement badges ─────────────────────────────────────────
  FIRST_BOOK_COMPLETED: { rim: '#047857', faceLight: '#d1fae5', faceDark: '#34d399', textColor: '#064e3b', label: '📖', title: 'Първа книга',   category: 'achievement' },
  READ_100_PAGES:       { rim: '#0f766e', faceLight: '#ccfbf1', faceDark: '#2dd4bf', textColor: '#134e4a', label: '📚', title: '100 страници',  category: 'achievement' },
  COMPLETE_5_ASSIGNMENTS:{ rim: '#4338ca', faceLight: '#e0e7ff', faceDark: '#818cf8', textColor: '#1e1b4b', label: '✅', title: '5 задачи',      category: 'achievement' },
  REACH_100_POINTS:     { rim: '#b45309', faceLight: '#fef3c7', faceDark: '#d97706', textColor: '#451a03', label: '⭐', title: '100 точки',     category: 'achievement' },
}


const SIZES = { sm: 28, md: 44, lg: 60 } as const
type Size = keyof typeof SIZES

function starPath(cx: number, cy: number, outerR: number, innerR: number): string {
  const pts: string[] = []
  for (let i = 0; i < 10; i++) {
    const r = i % 2 === 0 ? outerR : innerR
    const a = (Math.PI * i) / 5 - Math.PI / 2
    pts.push(`${(cx + r * Math.cos(a)).toFixed(2)},${(cy + r * Math.sin(a)).toFixed(2)}`)
  }
  return `M${pts.join(' L')} Z`
}

export const MedalIcon: FC<{
  code: string
  size?: Size
  className?: string
}> = ({ code, size = 'md', className }) => {
  const def = MEDALS[code]
  if (!def) return null

  const px = SIZES[size]
  const isSm = size === 'sm'
  const uid = `mdl-${code}-${size}`

  // For sm: small star shape (same as md/lg but compact, no bail)
  if (isSm) {
    const cx = px / 2
    const cy = px / 2
    const outerR = px / 2 - 1.5
    const innerR = outerR * 0.44
    const faceR  = outerR * 0.65
    const fontSize = px * 0.24
    return (
      <svg
        width={px} height={px}
        viewBox={`0 0 ${px} ${px}`}
        xmlns="http://www.w3.org/2000/svg"
        role="img" aria-label={def.title}
        style={{ display: 'inline-block', verticalAlign: 'middle', flexShrink: 0 }}
        className={className}
      >
        <title>{def.title}</title>
        <defs>
          <radialGradient id={`${uid}-fg`} cx="35%" cy="28%" r="70%">
            <stop offset="0%"   stopColor={def.faceLight} />
            <stop offset="100%" stopColor={def.faceDark} />
          </radialGradient>
          <filter id={`${uid}-sh`} x="-30%" y="-30%" width="160%" height="160%">
            <feDropShadow dx="0" dy="1.5" stdDeviation="1.5"
              floodColor={def.rim} floodOpacity="0.65" />
          </filter>
        </defs>
        {/* Star rim */}
        <path d={starPath(cx, cy, outerR, innerR)} fill={def.rim} filter={`url(#${uid}-sh)`} />
        {/* Face circle */}
        <circle cx={cx} cy={cy} r={faceR} fill={`url(#${uid}-fg)`} />
        {/* Label */}
        <text
          x={cx} y={cy + 0.5}
          textAnchor="middle" dominantBaseline="central"
          fill={def.textColor} fontSize={fontSize}
          fontWeight="900" fontFamily="'Plus Jakarta Sans', system-ui, sans-serif"
          style={{ userSelect: 'none' }}
        >
          {def.label}
        </text>
      </svg>
    )
  }

  // md / lg: star shape with bail
  const bailR    = size === 'md' ? 3.8 : 5.2
  const bailCy   = bailR + 1.5
  const starCx   = px / 2
  const starTop  = bailCy + bailR + 1
  const starCy   = starTop + (px - starTop) / 2
  const outerR   = (px - starTop) / 2 - 2
  const innerR   = outerR * 0.42
  const faceR    = outerR * 0.64
  const glossR   = faceR * 0.3
  const fontSize = px * 0.185

  return (
    <svg
      width={px} height={px}
      viewBox={`0 0 ${px} ${px}`}
      xmlns="http://www.w3.org/2000/svg"
      role="img" aria-label={def.title}
      style={{ display: 'inline-block', verticalAlign: 'middle', flexShrink: 0 }}
      className={className}
    >
      <title>{def.title}</title>
      <defs>
        <radialGradient id={`${uid}-fg`} cx="35%" cy="28%" r="70%">
          <stop offset="0%"   stopColor={def.faceLight} />
          <stop offset="100%" stopColor={def.faceDark} />
        </radialGradient>
        <radialGradient id={`${uid}-rg`} cx="30%" cy="25%" r="80%">
          <stop offset="0%"   stopColor={def.rim} stopOpacity="0.75" />
          <stop offset="100%" stopColor={def.rim} />
        </radialGradient>
        <filter id={`${uid}-sh`} x="-25%" y="-25%" width="150%" height="150%">
          <feDropShadow dx="0" dy="2" stdDeviation="2"
            floodColor={def.rim} floodOpacity="0.5" />
        </filter>
      </defs>

      {/* ── Bail: open ring (transparent fill, only stroke) ── */}
      <path
        d={`M ${starCx - bailR} ${bailCy}
            A ${bailR} ${bailR} 0 1 1 ${starCx + bailR} ${bailCy}`}
        fill="none"
        stroke={def.rim}
        strokeWidth="2.5"
        strokeLinecap="round"
      />

      {/* ── Star (rim) ── */}
      <path
        d={starPath(starCx, starCy, outerR, innerR)}
        fill={`url(#${uid}-rg)`}
        filter={`url(#${uid}-sh)`}
      />

      {/* ── Face circle ── */}
      <circle cx={starCx} cy={starCy} r={faceR} fill={`url(#${uid}-fg)`} />

      {/* ── Gloss ── */}
      <ellipse
        cx={starCx - faceR * 0.18} cy={starCy - faceR * 0.26}
        rx={glossR} ry={glossR * 0.48}
        fill="white" opacity="0.6"
        transform={`rotate(-35 ${starCx - faceR * 0.18} ${starCy - faceR * 0.26})`}
      />

      {/* ── Label ── */}
      <text
        x={starCx} y={starCy + 0.5}
        textAnchor="middle" dominantBaseline="central"
        fill={def.textColor} fontSize={fontSize}
        fontWeight="900" fontFamily="'Plus Jakarta Sans', system-ui, sans-serif"
        style={{ userSelect: 'none' }}
      >
        {def.label}
      </text>
    </svg>
  )
}
