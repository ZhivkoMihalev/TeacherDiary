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
        <StatCard label="Активни днес" value={data.activeTodayCount} total={data.studentsCount} />
        <StatCard label="Прочетени страници (7 дни)" value={data.totalPagesReadLast7Days} />
        <StatCard label="Изпълнени задачи (7 дни)" value={data.completedAssignmentsLast7Days} />
        <StatCard label="Активни задания към класа" value={data.activeLearningActivitiesCount} />
      </div>

      <div className="grid sm:grid-cols-2 gap-6">
        <Card>
          <CardHeader>
            <h2 className="font-semibold text-gray-800">Топ 5 — Точки</h2>
          </CardHeader>
          <CardBody className="p-0">
            {data.leaderboard.length === 0 ? (
              <p className="text-sm text-gray-400 px-6 py-4">Няма данни.</p>
            ) : (
              <ol className="divide-y divide-gray-100">
                {data.leaderboard.map((item, i) => (
                  <li key={item.studentId} className="flex items-center justify-between px-6 py-3">
                    <div className="flex items-center gap-3">
                      <span className="text-sm font-bold text-gray-400 w-4">{i + 1}</span>
                      <span className="text-sm font-medium text-gray-800 flex items-center gap-1">
                        {item.studentName}
                        {item.topMedalCode && <MedalIcon code={item.topMedalCode} size="sm" />}
                        {item.topPointsMedalCode && <MedalIcon code={item.topPointsMedalCode} size="sm" />}
                      </span>
                    </div>
                    <span className="text-sm font-semibold text-indigo-600">{item.points} т.</span>
                  </li>
                ))}
              </ol>
            )}
          </CardBody>
        </Card>

        <Card>
          <CardHeader>
            <h2 className="font-semibold text-gray-800">Топ читатели (7 дни)</h2>
          </CardHeader>
          <CardBody className="p-0">
            {data.topReaders.length === 0 ? (
              <p className="text-sm text-gray-400 px-6 py-4">Няма активност по четене.</p>
            ) : (
              <ol className="divide-y divide-gray-100">
                {data.topReaders.map((item, i) => (
                  <li key={item.studentId} className="flex items-center justify-between px-6 py-3">
                    <div className="flex items-center gap-3">
                      <span className="text-sm font-bold text-gray-400 w-4">{i + 1}</span>
                      <span className="text-sm font-medium text-gray-800">{item.studentName}</span>
                    </div>
                    <span className="text-sm text-gray-500">{item.pagesReadLast7Days} стр.</span>
                  </li>
                ))}
              </ol>
            )}
          </CardBody>
        </Card>

        <Card>
          <CardHeader>
            <h2 className="font-semibold text-gray-800">Най-добри серии</h2>
          </CardHeader>
          <CardBody className="p-0">
            {data.bestStreaks.length === 0 ? (
              <p className="text-sm text-gray-400 px-6 py-4">Няма данни за серии.</p>
            ) : (
              <ol className="divide-y divide-gray-100">
                {data.bestStreaks.map((item, i) => (
                  <li key={item.studentId} className="flex items-center justify-between px-6 py-3">
                    <div className="flex items-center gap-3">
                      <span className="text-sm font-bold text-gray-400 w-4">{i + 1}</span>
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
            <h2 className="font-semibold text-gray-800">Нови медали (7 дни)</h2>
          </CardHeader>
          <CardBody className="p-0">
            {data.recentBadges.length === 0 ? (
              <p className="text-sm text-gray-400 px-6 py-4">Няма присъдени медали наскоро.</p>
            ) : (
              <ul className="divide-y divide-gray-100">
                {data.recentBadges.map((b, i) => (
                  <li key={i} className="flex items-center gap-3 px-6 py-3">
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

function StatCard({ label, value, total }: { label: string; value: number; total?: number }) {
  return (
    <Card>
      <CardBody>
        <p className="text-xs text-gray-500 mb-1">{label}</p>
        <p className="text-2xl font-bold text-gray-900">
          {value}
          {total !== undefined && (
            <span className="text-sm font-normal text-gray-400"> / {total}</span>
          )}
        </p>
      </CardBody>
    </Card>
  )
}
