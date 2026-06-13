import React from 'react'
import { useTranslation } from 'react-i18next'
import { useParams, NavLink, Outlet } from 'react-router-dom'
import { useQuery } from '@tanstack/react-query'
import { dashboardApi } from '../../api/dashboard'
import { Card, CardBody, CardHeader } from '../../components/ui/Card'
import { Spinner } from '../../components/ui/Spinner'
import { MedalIcon } from '../../components/ui/MedalIcon'

const tabs = [
  { to: 'dashboard', labelKey: 'teacher.classDashboard.tabDashboard' },
  { to: 'students', labelKey: 'teacher.classDashboard.tabStudents' },
  { to: 'reading', labelKey: 'teacher.classDashboard.tabReading' },
  { to: 'assignments', labelKey: 'teacher.classDashboard.tabAssignments' },
  { to: 'challenges', labelKey: 'teacher.classDashboard.tabChallenges' },
  { to: 'analytics', labelKey: 'teacher.classDashboard.tabAnalytics' },
]

export function ClassDashboardPage() {
  const { t } = useTranslation()
  const { classId } = useParams<{ classId: string }>()

  const { data, isLoading } = useQuery({
    queryKey: ['dashboard', classId],
    queryFn: () => dashboardApi.getClassDashboard(classId!),
  })

  if (isLoading) {
    return (
      <div className="flex justify-center items-center h-64">
        <Spinner className="text-indigo-600 h-8 w-8" />
      </div>
    )
  }

  return (
    <div className="p-6 max-w-6xl mx-auto">
      <div className="mb-6">
        <h1 className="text-3xl font-black text-[#1C1917]">{data?.className}</h1>
        <p className="text-[#78716C] text-sm font-semibold mt-0.5">{t('teacher.classes.studentsEnrolled', { count: data?.studentsCount ?? 0 })}</p>
      </div>

      <div className="flex gap-1 mb-6 bg-white rounded-2xl p-1.5 border border-[#EAE8E0] shadow-sm w-fit">
        {tabs.map(({ to, labelKey }) => (
          <NavLink
            key={to}
            to={to}
            className={({ isActive }) =>
              `px-4 py-2 text-sm font-bold rounded-xl transition-all ${
                isActive
                  ? 'bg-[#14532D] text-white shadow-sm'
                  : 'text-[#78716C] hover:text-[#1C1917] hover:bg-[#F7F5EF]'
              }`
            }
          >
            {t(labelKey)}
          </NavLink>
        ))}
      </div>

      <Outlet context={{ data, classId }} />
    </div>
  )
}

export function ClassOverview() {
  const { t } = useTranslation()
  const { classId } = useParams<{ classId: string }>()

  const { data, isLoading } = useQuery({
    queryKey: ['dashboard', classId],
    queryFn: () => dashboardApi.getClassDashboard(classId!),
  })

  if (isLoading) {
    return <div className="flex justify-center py-12"><Spinner className="text-indigo-600 h-8 w-8" /></div>
  }

  if (!data) return null

  return (
    <div className="space-y-6">
      <div className="grid grid-cols-2 sm:grid-cols-4 gap-4">
        <StatCard label={t('teacher.classDashboard.activeToday')} value={data.activeTodayCount} total={data.studentsCount} icon="people" accent="#4F46E5" />
        <StatCard label={t('teacher.classDashboard.pagesRead7d')} value={data.totalPagesReadLast7Days} icon="book" accent="#059669" />
        <StatCard label={t('teacher.classDashboard.completed7d')} value={data.completedAssignmentsLast7Days} icon="check" accent="#0284C7" />
        <StatCard label={t('teacher.classDashboard.activeActivities')} value={data.activeLearningActivitiesCount} icon="zap" accent="#D97706" />
      </div>

      <div className="grid sm:grid-cols-2 gap-6">
        {/* Топ 5 — Точки */}
        <Card>
          <CardHeader>
            <SectionTitle icon="trophy" color="#4F46E5" label={t('teacher.classDashboard.top5Points')} />
          </CardHeader>
          <CardBody className="p-0">
            {data.leaderboard.length === 0 ? (
              <p className="text-base text-gray-400 px-6 py-6 text-center">{t('common.noData')}</p>
            ) : (
              <ol className="divide-y divide-gray-100">
                {data.leaderboard.map((item, i) => (
                  <li key={item.studentId} className="flex items-center justify-between px-6 py-3 hover:bg-gray-50 transition-colors">
                    <div className="flex items-center gap-3">
                      <RankBadge rank={i + 1} />
                      <span className="text-base font-semibold text-gray-800 flex items-center gap-1.5">
                        {item.studentName}
                        {item.topMedalCode && <MedalIcon code={item.topMedalCode} size="sm" />}
                        {item.topPointsMedalCode && <MedalIcon code={item.topPointsMedalCode} size="sm" />}
                      </span>
                    </div>
                    <span className="text-base font-bold text-indigo-600 bg-indigo-50 px-2.5 py-1 rounded-lg">{t('teacher.classDashboard.pointsDisplay', { points: item.points })}</span>
                  </li>
                ))}
              </ol>
            )}
          </CardBody>
        </Card>

        {/* Топ читатели */}
        <Card>
          <CardHeader>
            <SectionTitle icon="book" color="#059669" label={t('teacher.classDashboard.topReaders')} />
          </CardHeader>
          <CardBody className="p-0">
            {data.topReaders.length === 0 ? (
              <p className="text-base text-gray-400 px-6 py-6 text-center">{t('teacher.classDashboard.noReading')}</p>
            ) : (
              <ol className="divide-y divide-gray-100">
                {data.topReaders.map((item, i) => (
                  <li key={item.studentId} className="flex items-center justify-between px-6 py-3 hover:bg-gray-50 transition-colors">
                    <div className="flex items-center gap-3">
                      <RankBadge rank={i + 1} />
                      <span className="text-base font-semibold text-gray-800">{item.studentName}</span>
                    </div>
                    <span className="text-base font-bold text-emerald-600 bg-emerald-50 px-2.5 py-1 rounded-lg">{item.pagesReadLast7Days} {t('common.pages')}</span>
                  </li>
                ))}
              </ol>
            )}
          </CardBody>
        </Card>

        {/* Най-добри серии */}
        <Card>
          <CardHeader>
            <SectionTitle icon="flame" color="#EA580C" label={t('teacher.classDashboard.bestStreaks')} />
          </CardHeader>
          <CardBody className="p-0">
            {data.bestStreaks.length === 0 ? (
              <p className="text-base text-gray-400 px-6 py-6 text-center">{t('teacher.classDashboard.noStreaks')}</p>
            ) : (
              <ol className="divide-y divide-gray-100">
                {data.bestStreaks.map((item, i) => (
                  <li key={item.studentId} className="flex items-center justify-between px-6 py-3 hover:bg-gray-50 transition-colors">
                    <div className="flex items-center gap-3">
                      <RankBadge rank={i + 1} />
                      <span className="text-base font-semibold text-gray-800">{item.studentName}</span>
                    </div>
                    <div className="flex items-center gap-4">
                      <div className="text-center">
                        <p className="text-xs text-gray-400 font-medium mb-0.5">{t('teacher.classDashboard.streakLabel')}</p>
                        <div className="flex items-center gap-1 bg-orange-50 border border-orange-200 rounded-lg px-2.5 py-1">
                          <svg viewBox="0 0 24 24" fill="none" stroke="#EA580C" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" className="w-3.5 h-3.5 shrink-0">
                            <path d="M8.5 14.5A2.5 2.5 0 0 0 11 12c0-1.38-.5-2-1-3-1.072-2.143-.224-4.054 2-6 .5 2.5 2 4.9 4 6.5 2 1.6 3 3.5 3 5.5a7 7 0 1 1-14 0c0-1.153.433-2.294 1-3a2.5 2.5 0 0 0 2.5 2.5z"/>
                          </svg>
                          <span className="text-sm font-black text-orange-600">{t('teacher.classDashboard.currentStreak', { count: item.currentStreak })}</span>
                        </div>
                      </div>
                      <div className="text-center">
                        <p className="text-xs text-gray-400 font-medium mb-0.5">{t('teacher.classDashboard.recordLabel')}</p>
                        <div className="flex items-center gap-1 bg-gray-50 border border-gray-200 rounded-lg px-2.5 py-1">
                          <span className="text-sm font-bold text-gray-600">{t('teacher.classDashboard.bestStreak', { count: item.bestStreak })}</span>
                        </div>
                      </div>
                    </div>
                  </li>
                ))}
              </ol>
            )}
          </CardBody>
        </Card>

        {/* Нови медали */}
        <Card>
          <CardHeader>
            <SectionTitle icon="award" color="#B45309" label={t('teacher.classDashboard.newBadges')} />
          </CardHeader>
          <CardBody className="p-0">
            {data.recentBadges.length === 0 ? (
              <p className="text-base text-gray-400 px-6 py-6 text-center">{t('teacher.classDashboard.noBadges')}</p>
            ) : (
              <ul className="divide-y divide-gray-100">
                {data.recentBadges.map((b, i) => (
                  <li key={i} className="flex items-center gap-3 px-6 py-3 hover:bg-gray-50 transition-colors">
                    <MedalIcon code={b.badgeCode} size="md" />
                    <div>
                      <p className="text-base font-semibold text-gray-800">{b.studentName}</p>
                      <p className="text-sm text-gray-500">{b.badgeName}</p>
                    </div>
                  </li>
                ))}
              </ul>
            )}
          </CardBody>
        </Card>
      </div>
    </div>
  )
}

const RANK_STYLES = [
  'text-amber-500 bg-amber-50 border-amber-200',
  'text-gray-500 bg-gray-100 border-gray-200',
  'text-orange-500 bg-orange-50 border-orange-200',
]

function RankBadge({ rank }: { rank: number }) {
  const style = rank <= 3 ? RANK_STYLES[rank - 1] : 'text-gray-400 bg-transparent border-transparent'
  return (
    <span className={`text-xs font-bold w-6 h-6 flex items-center justify-center rounded-full border ${style}`}>
      {rank}
    </span>
  )
}

// ── Section title with SVG icon ──────────────────────────────────
type SectionIconType = 'trophy' | 'book' | 'flame' | 'award'

function SectionSvg({ type, className, style }: { type: SectionIconType; className?: string; style?: React.CSSProperties }) {
  const base = { className, style, viewBox: '0 0 24 24', fill: 'none', stroke: 'currentColor', strokeLinecap: 'round' as const, strokeLinejoin: 'round' as const }
  if (type === 'trophy') return (
    <svg {...base} strokeWidth="2">
      <path d="M6 9H4.5a2.5 2.5 0 0 1 0-5H6"/>
      <path d="M18 9h1.5a2.5 2.5 0 0 0 0-5H18"/>
      <path d="M4 22h16"/>
      <path d="M10 14.66V17c0 .55-.47.98-.97 1.21C7.85 18.75 7 20.24 7 22"/>
      <path d="M14 14.66V17c0 .55.47.98.97 1.21C16.15 18.75 17 20.24 17 22"/>
      <path d="M18 2H6v7a6 6 0 0 0 12 0V2Z"/>
    </svg>
  )
  if (type === 'book') return (
    <svg {...base} strokeWidth="2">
      <path d="M4 19.5A2.5 2.5 0 0 1 6.5 17H20"/>
      <path d="M6.5 2H20v20H6.5A2.5 2.5 0 0 1 4 19.5v-15A2.5 2.5 0 0 1 6.5 2z"/>
      <line x1="12" y1="6" x2="16" y2="6"/>
      <line x1="12" y1="10" x2="16" y2="10"/>
    </svg>
  )
  if (type === 'flame') return (
    <svg {...base} strokeWidth="2">
      <path d="M8.5 14.5A2.5 2.5 0 0 0 11 12c0-1.38-.5-2-1-3-1.072-2.143-.224-4.054 2-6 .5 2.5 2 4.9 4 6.5 2 1.6 3 3.5 3 5.5a7 7 0 1 1-14 0c0-1.153.433-2.294 1-3a2.5 2.5 0 0 0 2.5 2.5z"/>
    </svg>
  )
  // award
  return (
    <svg {...base} strokeWidth="2">
      <circle cx="12" cy="8" r="6"/>
      <path d="M15.477 12.89 17 22l-5-3-5 3 1.523-9.11"/>
    </svg>
  )
}

function SectionTitle({ icon, color, label }: { icon: SectionIconType; color: string; label: string }) {
  return (
    <div className="flex items-center gap-3">
      <div
        className="w-8 h-8 rounded-xl flex items-center justify-center shrink-0"
        style={{ background: `${color}18` }}
      >
        <SectionSvg type={icon} className="w-4 h-4" style={{ color }} />
      </div>
      <h2 className="font-bold text-[#1C1917]">{label}</h2>
    </div>
  )
}


type IconType = 'people' | 'book' | 'check' | 'zap'

function StatIcon({ type, className, style }: { type: IconType; className?: string; style?: React.CSSProperties }) {
  const props = { className, style, viewBox: '0 0 24 24', fill: 'none', stroke: 'currentColor', strokeLinecap: 'round' as const, strokeLinejoin: 'round' as const }
  if (type === 'people') return (
    <svg {...props} strokeWidth="2">
      <path d="M16 21v-2a4 4 0 0 0-4-4H6a4 4 0 0 0-4 4v2"/>
      <circle cx="9" cy="7" r="4"/>
      <path d="M22 21v-2a4 4 0 0 0-3-3.87"/>
      <path d="M16 3.13a4 4 0 0 1 0 7.75"/>
    </svg>
  )
  if (type === 'book') return (
    <svg {...props} strokeWidth="2">
      <path d="M4 19.5A2.5 2.5 0 0 1 6.5 17H20"/>
      <path d="M6.5 2H20v20H6.5A2.5 2.5 0 0 1 4 19.5v-15A2.5 2.5 0 0 1 6.5 2z"/>
      <line x1="12" y1="6" x2="16" y2="6"/>
      <line x1="12" y1="10" x2="16" y2="10"/>
    </svg>
  )
  if (type === 'check') return (
    <svg {...props} strokeWidth="2.5">
      <path d="M22 11.08V12a10 10 0 1 1-5.93-9.14"/>
      <polyline points="22 4 12 14.01 9 11.01"/>
    </svg>
  )
  return (
    <svg {...props} strokeWidth="2">
      <polygon points="13 2 3 14 12 14 11 22 21 10 12 10 13 2"/>
    </svg>
  )
}

function StatCard({ label, value, total, icon, accent }: {
  label: string
  value: number
  total?: number
  icon: IconType
  accent: string
}) {
  const pct = total ? Math.min(100, Math.round((value / total) * 100)) : null
  // Derive a light tint from accent — use opacity trick via inline style
  return (
    <div className="bg-white rounded-2xl p-5 shadow-sm border border-white/80 flex flex-col gap-3 stat-card hover:shadow-md transition-shadow">
      {/* Icon container */}
      <div
        className="w-12 h-12 rounded-2xl flex items-center justify-center"
        style={{ background: `${accent}18` }}
      >
        <StatIcon type={icon} className="w-6 h-6" style={{ color: accent }} />
      </div>

      {/* Number */}
      <div>
        <p className="text-4xl font-black leading-none" style={{ color: accent }}>
          {value}
          {total !== undefined && (
            <span className="text-lg font-semibold text-gray-300 ml-0.5">/{total}</span>
          )}
        </p>
        <p className="text-sm text-gray-500 font-semibold mt-2 leading-tight">{label}</p>
      </div>

      {/* Progress bar (only if total provided) */}
      {pct !== null && (
        <div className="h-1.5 bg-gray-100 rounded-full overflow-hidden">
          <div
            className="h-full rounded-full transition-all duration-500"
            style={{ width: `${pct}%`, background: accent }}
          />
        </div>
      )}
    </div>
  )
}
