import type { FC } from 'react'

interface MedalDef {
  rim: string
  faceLight: string
  faceDark: string
  textColor: string
  label: string
  title: string
}

const MEDALS: Record<string, MedalDef> = {
  // ── Streak medals ──────────────────────────────────────────────
  STREAK_3: {
    rim: '#15803d', faceLight: '#bbf7d0', faceDark: '#4ade80',
    textColor: '#14532d', label: '3', title: '3 дни подред',
  },
  STREAK_5: {
    rim: '#0e7490', faceLight: '#cffafe', faceDark: '#22d3ee',
    textColor: '#164e63', label: '5', title: '5 дни подред',
  },
  SEVEN_DAY_STREAK: {
    rim: '#c2410c', faceLight: '#ffedd5', faceDark: '#fb923c',
    textColor: '#7c2d12', label: '7', title: '7 дни подред',
  },
  STREAK_15: {
    rim: '#a16207', faceLight: '#fefce8', faceDark: '#fde047',
    textColor: '#713f12', label: '15', title: '15 дни подред',
  },
  STREAK_30: {
    rim: '#78350f', faceLight: '#fef3c7', faceDark: '#d97706',
    textColor: '#451a03', label: '30', title: '30 дни подред',
  },
  STREAK_45: {
    rim: '#475569', faceLight: '#f1f5f9', faceDark: '#94a3b8',
    textColor: '#0f172a', label: '45', title: '45 дни подред',
  },
  STREAK_60: {
    rim: '#92400e', faceLight: '#fffbeb', faceDark: '#fcd34d',
    textColor: '#78350f', label: '60', title: '60 дни подред',
  },
  STREAK_90: {
    rim: '#075985', faceLight: '#e0f2fe', faceDark: '#38bdf8',
    textColor: '#0c4a6e', label: '90', title: '90 дни подред',
  },
  STREAK_180: {
    rim: '#6b21a8', faceLight: '#f3e8ff', faceDark: '#c084fc',
    textColor: '#3b0764', label: '180', title: '180 дни подред',
  },
  STREAK_360: {
    rim: '#9f1239', faceLight: '#fce7f3', faceDark: '#f472b6',
    textColor: '#500724', label: '360', title: '360 дни подред',
  },

  // ── Points medals ──────────────────────────────────────────────
  POINTS_100: {
    rim: '#92400e', faceLight: '#fef3c7', faceDark: '#d97706',
    textColor: '#451a03', label: '100', title: '100 точки',
  },
  POINTS_250: {
    rim: '#57534e', faceLight: '#f5f5f4', faceDark: '#a8a29e',
    textColor: '#1c1917', label: '250', title: '250 точки',
  },
  POINTS_500: {
    rim: '#475569', faceLight: '#f8fafc', faceDark: '#cbd5e1',
    textColor: '#1e293b', label: '500', title: '500 точки',
  },
  POINTS_1000: {
    rim: '#374151', faceLight: '#ffffff', faceDark: '#e5e7eb',
    textColor: '#111827', label: '1K', title: '1000 точки',
  },
  POINTS_1500: {
    rim: '#a16207', faceLight: '#fefce8', faceDark: '#fde047',
    textColor: '#713f12', label: '1.5K', title: '1500 точки',
  },
  POINTS_2000: {
    rim: '#b45309', faceLight: '#fffbeb', faceDark: '#fcd34d',
    textColor: '#78350f', label: '2K', title: '2000 точки',
  },
  POINTS_3000: {
    rim: '#334155', faceLight: '#f8fafc', faceDark: '#e2e8f0',
    textColor: '#0f172a', label: '3K', title: '3000 точки',
  },
  POINTS_5000: {
    rim: '#1d4ed8', faceLight: '#dbeafe', faceDark: '#60a5fa',
    textColor: '#1e3a8a', label: '5K', title: '5000 точки',
  },
  POINTS_7500: {
    rim: '#4338ca', faceLight: '#e0e7ff', faceDark: '#818cf8',
    textColor: '#1e1b4b', label: '7.5K', title: '7500 точки',
  },
  POINTS_10000: {
    rim: '#6d28d9', faceLight: '#f5f3ff', faceDark: '#c084fc',
    textColor: '#3b0764', label: '10K', title: '10 000 точки',
  },

  // ── Achievement badges ─────────────────────────────────────────
  FIRST_BOOK_COMPLETED: {
    rim: '#047857', faceLight: '#d1fae5', faceDark: '#34d399',
    textColor: '#064e3b', label: '1°', title: 'Първа книга',
  },
  READ_100_PAGES: {
    rim: '#0f766e', faceLight: '#ccfbf1', faceDark: '#2dd4bf',
    textColor: '#134e4a', label: '100', title: '100 страници',
  },
  COMPLETE_5_ASSIGNMENTS: {
    rim: '#4338ca', faceLight: '#e0e7ff', faceDark: '#818cf8',
    textColor: '#1e1b4b', label: '5', title: '5 задачи',
  },
  REACH_100_POINTS: {
    rim: '#b45309', faceLight: '#fef3c7', faceDark: '#d97706',
    textColor: '#451a03', label: '100', title: '100 точки',
  },
}

const SIZES = { sm: 24, md: 38, lg: 54 } as const

type Size = keyof typeof SIZES

export const MedalIcon: FC<{
  code: string
  size?: Size
  className?: string
}> = ({ code, size = 'md', className }) => {
  const def = MEDALS[code]
  if (!def) return null

  const px = SIZES[size]
  const cx = px / 2
  const outerR = cx - 1.5
  const innerR = outerR * 0.75
  const fontSize = px * (size === 'sm' ? 0.285 : size === 'md' ? 0.265 : 0.25)
  // Unique ID per code+size combo — safe because identical medals share identical defs
  const uid = `mdl-${code}-${size}`

  return (
    <svg
      width={px}
      height={px}
      viewBox={`0 0 ${px} ${px}`}
      xmlns="http://www.w3.org/2000/svg"
      className={className}
      role="img"
      aria-label={def.title}
      style={{ display: 'inline-block', verticalAlign: 'middle', flexShrink: 0 }}
    >
      <title>{def.title}</title>
      <defs>
        {/* Radial gradient for the face — light from top-left */}
        <radialGradient id={`${uid}-g`} cx="36%" cy="30%" r="72%">
          <stop offset="0%" stopColor={def.faceLight} />
          <stop offset="100%" stopColor={def.faceDark} />
        </radialGradient>
        {/* Drop shadow */}
        <filter id={`${uid}-f`} x="-15%" y="-15%" width="130%" height="130%">
          <feDropShadow dx="0" dy="1" stdDeviation="1.2"
            floodColor={def.rim} floodOpacity="0.45" />
        </filter>
      </defs>

      {/* Outer rim — coloured ring */}
      <circle
        cx={cx} cy={cx} r={outerR}
        fill={def.rim}
        filter={`url(#${uid}-f)`}
      />

      {/* Medal face with radial gradient */}
      <circle
        cx={cx} cy={cx} r={innerR}
        fill={`url(#${uid}-g)`}
      />

      {/* Specular gloss — small bright ellipse near top-left */}
      <ellipse
        cx={cx - innerR * 0.22}
        cy={cx - innerR * 0.28}
        rx={innerR * 0.30}
        ry={innerR * 0.15}
        fill="white"
        opacity="0.50"
        transform={`rotate(-35 ${cx - innerR * 0.22} ${cx - innerR * 0.28})`}
      />

      {/* Label */}
      <text
        x={cx}
        y={cx + 0.5}
        textAnchor="middle"
        dominantBaseline="central"
        fill={def.textColor}
        fontSize={fontSize}
        fontWeight="800"
        fontFamily="-apple-system, BlinkMacSystemFont, 'Segoe UI', system-ui, sans-serif"
        style={{ userSelect: 'none' }}
      >
        {def.label}
      </text>
    </svg>
  )
}
