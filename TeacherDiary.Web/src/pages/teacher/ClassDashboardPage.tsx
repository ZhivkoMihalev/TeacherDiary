import { useParams, NavLink, Outlet } from 'react-router-dom'
import { useQuery } from '@tanstack/react-query'
import { dashboardApi } from '../../api/dashboard'
import { Card, CardBody, CardHeader } from '../../components/ui/Card'
import { Spinner } from '../../components/ui/Spinner'
import { Badge } from '../../components/ui/Badge'
import { MedalIcon } from '../../components/ui/MedalIcon'

const tabs = [
  { to: 'dashboard', label: 'Дневник' },
  { to: 'students', label: 'Ученици' },
  { to: 'reading', label: 'Четене' },
  { to: 'assignments', label: 'Задачи' },
  { to: 'challenges', label: 'Предизвикателства' },
]

export function ClassDashboardPage() {
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
    <div className="p-8 max-w-6xl mx-auto">
      <div className="mb-6">
        <h1 className="text-2xl font-bold text-gray-900">{data?.className}</h1>
        <p className="text-gray-500 text-sm mt-0.5">{data?.studentsCount} записани ученика</p>
      </div>

      <div className="flex gap-1 mb-8 border-b border-gray-200">
        {tabs.map(({ to, label }) => (
          <NavLink
            key={to}
            to={to}
            className={({ isActive }) =>
              `px-4 py-2.5 text-sm font-medium border-b-2 -mb-px transition-colors ${
                isActive
                  ? 'border-indigo-600 text-indigo-600'
                  : 'border-transparent text-gray-500 hover:text-gray-800'
              }`
            }
          >
            {label}
          </NavLink>
        ))}
      </div>

      <Outlet context={{ data, classId }} />
    </div>
  )
}

export function ClassOverview() {
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
        <StatCard label="Активни днес" value={data.activeTodayCount} total={data.studentsCount} icon="👥" color="indigo" />
        <StatCard label="Прочетени страници (7 дни)" value={data.totalPagesReadLast7Days} icon="📖" color="emerald" />
        <StatCard label="Изпълнени задачи (7 дни)" value={data.completedAssignmentsLast7Days} icon="✅" color="sky" />
        <StatCard label="Активни задания" value={data.activeLearningActivitiesCount} icon="🎯" color="amber" />
      </div>

      <div className="grid sm:grid-cols-2 gap-6">
        <Card>
          <CardHeader>
            <h2 className="font-semibold text-gray-800 flex items-center gap-2">
              <span className="text-base">🏆</span> Топ 5 — Точки
            </h2>
          </CardHeader>
          <CardBody className="p-0">
            {data.leaderboard.length === 0 ? (
              <p className="text-sm text-gray-400 px-6 py-6 text-center">Няма данни.</p>
            ) : (
              <ol className="divide-y divide-gray-100">
                {data.leaderboard.map((item, i) => (
                  <li key={item.studentId} className="flex items-center justify-between px-6 py-3 hover:bg-gray-50 transition-colors">
                    <div className="flex items-center gap-3">
                      <RankBadge rank={i + 1} />
                      <span className="text-sm font-medium text-gray-800 flex items-center gap-1">
                        {item.studentName}
                        {item.topMedalCode && <MedalIcon code={item.topMedalCode} size="sm" />}
                        {item.topPointsMedalCode && <MedalIcon code={item.topPointsMedalCode} size="sm" />}
                      </span>
                    </div>
                    <span className="text-sm font-bold text-indigo-600 bg-indigo-50 px-2 py-0.5 rounded-lg">{item.points} т.</span>
                  </li>
                ))}
              </ol>
            )}
          </CardBody>
        </Card>

        <Card>
          <CardHeader>
            <h2 className="font-semibold text-gray-800 flex items-center gap-2">
              <span className="text-base">📚</span> Топ читатели (7 дни)
            </h2>
          </CardHeader>
          <CardBody className="p-0">
            {data.topReaders.length === 0 ? (
              <p className="text-sm text-gray-400 px-6 py-6 text-center">Няма активност по четене.</p>
            ) : (
              <ol className="divide-y divide-gray-100">
                {data.topReaders.map((item, i) => (
                  <li key={item.studentId} className="flex items-center justify-between px-6 py-3 hover:bg-gray-50 transition-colors">
                    <div className="flex items-center gap-3">
                      <RankBadge rank={i + 1} />
                      <span className="text-sm font-medium text-gray-800">{item.studentName}</span>
                    </div>
                    <span className="text-sm font-semibold text-emerald-600 bg-emerald-50 px-2 py-0.5 rounded-lg">{item.pagesReadLast7Days} стр.</span>
                  </li>
                ))}
              </ol>
            )}
          </CardBody>
        </Card>

        <Card>
          <CardHeader>
            <h2 className="font-semibold text-gray-800 flex items-center gap-2">
              <span className="text-base">🔥</span> Най-добри серии
            </h2>
          </CardHeader>
          <CardBody className="p-0">
            {data.bestStreaks.length === 0 ? (
              <p className="text-sm text-gray-400 px-6 py-6 text-center">Няма данни за серии.</p>
            ) : (
              <ol className="divide-y divide-gray-100">
                {data.bestStreaks.map((item, i) => (
                  <li key={item.studentId} className="flex items-center justify-between px-6 py-3 hover:bg-gray-50 transition-colors">
                    <div className="flex items-center gap-3">
                      <RankBadge rank={i + 1} />
                      <span className="text-sm font-medium text-gray-800">{item.studentName}</span>
                    </div>
                    <div className="flex items-center gap-2">
                      <Badge variant="yellow">🔥 {item.currentStreak}д</Badge>
                      <span className="text-xs text-gray-400">рекорд {item.bestStreak}д</span>
                    </div>
                  </li>
                ))}
              </ol>
            )}
          </CardBody>
        </Card>

        <Card>
          <CardHeader>
            <h2 className="font-semibold text-gray-800 flex items-center gap-2">
              <span className="text-base">🎖️</span> Нови медали (7 дни)
            </h2>
          </CardHeader>
          <CardBody className="p-0">
            {data.recentBadges.length === 0 ? (
              <p className="text-sm text-gray-400 px-6 py-6 text-center">Няма присъдени медали наскоро.</p>
            ) : (
              <ul className="divide-y divide-gray-100">
                {data.recentBadges.map((b, i) => (
                  <li key={i} className="flex items-center gap-3 px-6 py-3 hover:bg-gray-50 transition-colors">
                    <MedalIcon code={b.badgeCode} size="md" />
                    <div>
                      <p className="text-sm font-medium text-gray-800">{b.studentName}</p>
                      <p className="text-xs text-gray-400">{b.badgeName}</p>
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

type StatColor = 'indigo' | 'emerald' | 'sky' | 'amber'

const COLOR_MAP: Record<StatColor, { bg: string; text: string; icon: string }> = {
  indigo: { bg: 'bg-indigo-50', text: 'text-indigo-700', icon: 'bg-indigo-100' },
  emerald: { bg: 'bg-emerald-50', text: 'text-emerald-700', icon: 'bg-emerald-100' },
  sky: { bg: 'bg-sky-50', text: 'text-sky-700', icon: 'bg-sky-100' },
  amber: { bg: 'bg-amber-50', text: 'text-amber-700', icon: 'bg-amber-100' },
}

function StatCard({ label, value, total, icon, color }: { label: string; value: number; total?: number; icon: string; color: StatColor }) {
  const c = COLOR_MAP[color]
  return (
    <div className={`${c.bg} rounded-2xl p-4 flex flex-col gap-2`}>
      <div className={`${c.icon} w-8 h-8 rounded-xl flex items-center justify-center text-base`}>{icon}</div>
      <p className={`text-2xl font-bold ${c.text}`}>
        {value}
        {total !== undefined && (
          <span className="text-sm font-normal text-gray-400"> / {total}</span>
        )}
      </p>
      <p className="text-xs text-gray-500 leading-tight">{label}</p>
    </div>
  )
}
