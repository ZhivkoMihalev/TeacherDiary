import { useParams, useNavigate } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import { useQuery } from '@tanstack/react-query'
import { dashboardApi } from '../../api/dashboard'
import { Card, CardBody, CardHeader } from '../../components/ui/Card'
import { Badge } from '../../components/ui/Badge'
import { Spinner } from '../../components/ui/Spinner'
import { MedalIcon } from '../../components/ui/MedalIcon'
import type { ProgressStatus, StudentChallengeDto } from '../../types'
import { formatDate } from '../../utils/formatDate'
import { useLanguage } from '../../context/LanguageContext'

function statusVariant(s: ProgressStatus) {
  if (s === 'Completed') return 'green'
  if (s === 'InProgress') return 'blue'
  return 'gray'
}

function statusKey(s: ProgressStatus) {
  if (s === 'Completed') return 'status.completed'
  if (s === 'InProgress') return 'status.inProgress'
  return 'status.notStarted'
}

export function TeacherStudentPage() {
  const { studentId } = useParams<{ studentId: string }>()
  const navigate = useNavigate()
  const { translate } = useLanguage()
  const { t } = useTranslation()

  const { data, isLoading } = useQuery({
    queryKey: ['teacher-student', studentId],
    queryFn: () => dashboardApi.getStudentDetails(studentId!),
  })

  const { data: badges = [] } = useQuery({
    queryKey: ['teacher-student-badges', studentId],
    queryFn: () => dashboardApi.getStudentBadges(studentId!),
  })

  if (isLoading) {
    return (
      <div className="flex justify-center items-center h-64">
        <Spinner className="text-indigo-600 h-8 w-8" />
      </div>
    )
  }

  if (!data) return null

  const activeReading = data.reading.filter((r) => !r.isExpired)
  const archivedReading = data.reading.filter((r) => r.isExpired)
  const activeAssignments = data.assignments.filter((a) => !a.isExpired)
  const archivedAssignments = data.assignments.filter((a) => a.isExpired)
  const archivedActivities = data.learningActivities.filter((la) => la.isExpired)
  const activeChallenges = data.challenges.filter((c) => !c.completed)
  const completedChallenges = data.challenges.filter((c) => c.completed)
  const hasArchive = archivedReading.length > 0 || archivedAssignments.length > 0 || archivedActivities.length > 0

  function challengeStatusVariant(c: StudentChallengeDto) {
    if (c.completed) return 'green'
    if (c.started) return 'blue'
    return 'gray'
  }

  function challengeStatusLabel(c: StudentChallengeDto) {
    if (c.completed) return t('status.completed')
    if (c.started) return t('status.inProgress')
    return t('status.notStarted')
  }

  function challengeTargetLabel(c: StudentChallengeDto) {
    if (c.targetDescription) return `${c.targetDescription}${c.targetValue ? ` · ${c.currentValue} / ${c.targetValue}` : ''}`
    if (c.targetValue) return `${c.currentValue} / ${c.targetValue}`
    return null
  }

  return (
    <div className="p-8 max-w-4xl mx-auto space-y-6">
      <button
        onClick={() => navigate(-1)}
        className="text-sm text-gray-400 hover:text-gray-600"
      >
        {t('teacher.studentDetails.back')}
      </button>

      <div className="flex items-center gap-4">
        <div>
          <h1 className="text-2xl font-bold text-gray-900 flex items-center gap-2 flex-wrap">
            {data.studentName}
            {data.topMedalCode && <MedalIcon code={data.topMedalCode} size="md" />}
            {data.topPointsMedalCode && <MedalIcon code={data.topPointsMedalCode} size="md" />}
          </h1>
          <p className="text-gray-400 text-sm mt-0.5">
            {data.lastActivityAt
              ? t('teacher.studentDetails.lastActive', { date: formatDate(data.lastActivityAt) })
              : t('teacher.studentDetails.noActivity')}
          </p>
        </div>
        {!data.isActive && <Badge variant="gray">{t('teacher.studentDetails.inactive')}</Badge>}
      </div>

      {/* Статистика */}
      <div className="grid grid-cols-2 sm:grid-cols-3 gap-4">
        <Card>
          <CardBody>
            <p className="text-xs text-gray-400 mb-1">{t('teacher.studentDetails.pagesReadStat')}</p>
            <p className="text-2xl font-bold text-gray-900">{data.totalPagesRead}</p>
          </CardBody>
        </Card>
        <Card>
          <CardBody>
            <p className="text-xs text-gray-400 mb-1">{t('teacher.studentDetails.completedAssignmentsStat')}</p>
            <p className="text-2xl font-bold text-gray-900">{data.completedAssignments}</p>
          </CardBody>
        </Card>
        <Card>
          <CardBody>
            <p className="text-xs text-gray-400 mb-1">{t('teacher.studentDetails.pointsStat')}</p>
            <p className="text-2xl font-bold text-indigo-600">{data.totalPoints}</p>
          </CardBody>
        </Card>
      </div>

      {/* Активност последните 7 дни */}
      {data.activityLast7Days.length > 0 && (
        <Card>
          <CardHeader>
            <h2 className="font-semibold text-gray-800">{t('teacher.studentDetails.activity7d')}</h2>
          </CardHeader>
          <CardBody className="p-0">
            <table className="w-full text-sm">
              <thead className="text-xs text-gray-400 border-b border-gray-100">
                <tr>
                  <th className="text-left px-6 py-2">{t('teacher.studentDetails.colDate')}</th>
                  <th className="text-left px-6 py-2">{t('teacher.studentDetails.colActivity')}</th>
                  <th className="text-right px-6 py-2">{t('teacher.studentDetails.colPoints')}</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-gray-50">
                {data.activityLast7Days.map((entry, i) => (
                  <tr key={i}>
                    <td className="px-6 py-2.5 text-gray-500 whitespace-nowrap">{formatDate(entry.date)}</td>
                    <td className="px-6 py-2.5 text-gray-700">{translate(entry.description)}</td>
                    <td className="px-6 py-2.5 text-right font-medium text-indigo-600">
                      {entry.pointsEarned > 0 ? `+${entry.pointsEarned}` : '—'}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </CardBody>
        </Card>
      )}

      {/* Четене */}
      {activeReading.length > 0 && (
        <Card>
          <CardHeader>
            <h2 className="font-semibold text-gray-800">{t('teacher.studentDetails.reading')}</h2>
          </CardHeader>
          <div className="divide-y divide-gray-100">
            {activeReading.map((r) => {
              const pct = r.totalPages ? Math.round((r.currentPage / r.totalPages) * 100) : 0
              return (
                <div key={r.assignedBookId} className="px-6 py-4">
                  <div className="flex items-start justify-between mb-2">
                    <div>
                      <p className="font-medium text-gray-900">{r.bookTitle}</p>
                      <p className="text-sm text-gray-400">
                        {r.totalPages
                          ? t('teacher.studentDetails.pageOf', { current: r.currentPage, total: r.totalPages })
                          : t('teacher.studentDetails.pageCurrent', { current: r.currentPage })}
                      </p>
                    </div>
                    <Badge variant={statusVariant(r.status)}>{t(statusKey(r.status))}</Badge>
                  </div>
                  {r.totalPages && (
                    <div className="w-full bg-gray-100 rounded-full h-1.5">
                      <div
                        className="bg-indigo-500 h-1.5 rounded-full transition-all"
                        style={{ width: `${pct}%` }}
                      />
                    </div>
                  )}
                </div>
              )
            })}
          </div>
        </Card>
      )}

      {/* Задачи */}
      {activeAssignments.length > 0 && (
        <Card>
          <CardHeader>
            <h2 className="font-semibold text-gray-800">{t('teacher.studentDetails.assignments')}</h2>
          </CardHeader>
          <div className="divide-y divide-gray-100">
            {activeAssignments.map((a) => (
              <div key={a.assignmentId} className="flex items-center justify-between px-6 py-3">
                <div>
                  <p className="font-medium text-gray-900">{a.title}</p>
                  <p className="text-sm text-gray-400">
                    {a.subject}{a.dueDate ? ` · ${t('teacher.studentDetails.dueShort', { date: formatDate(a.dueDate) })}` : ''}
                  </p>
                </div>
                <Badge variant={statusVariant(a.status)}>{t(statusKey(a.status))}</Badge>
              </div>
            ))}
          </div>
        </Card>
      )}

      {/* Предизвикателства */}
      {data.challenges.length > 0 && (
        <Card>
          <CardHeader>
            <h2 className="font-semibold text-gray-800">{t('teacher.studentDetails.challenges')}</h2>
          </CardHeader>
          <div className="divide-y divide-gray-100">
            {activeChallenges.map((c) => (
              <div key={c.challengeId} className="flex items-center justify-between px-6 py-3">
                <div>
                  <p className="font-medium text-gray-900">{c.title}</p>
                  {challengeTargetLabel(c) && (
                    <p className="text-sm text-gray-400">{challengeTargetLabel(c)}</p>
                  )}
                  <p className="text-xs text-gray-400">{t('teacher.studentDetails.dueShort', { date: formatDate(c.endDate) })}</p>
                </div>
                <Badge variant={challengeStatusVariant(c)}>{challengeStatusLabel(c)}</Badge>
              </div>
            ))}
            {completedChallenges.map((c) => (
              <div key={c.challengeId} className="flex items-center justify-between px-6 py-3 opacity-60">
                <p className="font-medium text-gray-900">{c.title}</p>
                <Badge variant="green">{t('status.completed')}</Badge>
              </div>
            ))}
          </div>
        </Card>
      )}

      {/* Архив */}
      {hasArchive && (
        <Card>
          <CardHeader>
            <h2 className="font-semibold text-gray-500">{t('teacher.studentDetails.archive')}</h2>
          </CardHeader>
          <div className="divide-y divide-gray-50">
            {archivedReading.map((r) => {
              const pct = r.totalPages ? Math.round((r.currentPage / r.totalPages) * 100) : 0
              return (
                <div key={r.assignedBookId} className="px-6 py-4 opacity-60">
                  <div className="flex items-start justify-between mb-2">
                    <div>
                      <p className="font-medium text-gray-700">{r.bookTitle}</p>
                      <p className="text-sm text-gray-400">{r.totalPages ? t('teacher.studentDetails.pageOf', { current: r.currentPage, total: r.totalPages }) : t('teacher.studentDetails.pageCurrent', { current: r.currentPage })}</p>
                    </div>
                    <Badge variant={statusVariant(r.status)}>{t(statusKey(r.status))}</Badge>
                  </div>
                  {r.totalPages && (
                    <div className="w-full bg-gray-100 rounded-full h-1.5">
                      <div className="bg-gray-400 h-1.5 rounded-full" style={{ width: `${pct}%` }} />
                    </div>
                  )}
                </div>
              )
            })}
            {archivedAssignments.map((a) => (
              <div key={a.assignmentId} className="flex items-center justify-between px-6 py-3 opacity-60">
                <div>
                  <p className="font-medium text-gray-700">{a.title}</p>
                  <p className="text-sm text-gray-400">{a.subject}{a.dueDate ? ` · ${t('teacher.studentDetails.dueShort', { date: formatDate(a.dueDate) })}` : ''}</p>
                </div>
                <Badge variant={statusVariant(a.status)}>{t(statusKey(a.status))}</Badge>
              </div>
            ))}
            {archivedActivities.map((la) => (
              <div key={la.learningActivityId} className="flex items-center justify-between px-6 py-3 opacity-60">
                <div>
                  <p className="font-medium text-gray-700">{la.title}</p>
                  <p className="text-sm text-gray-400 capitalize">
                    {la.type}{la.targetValue ? ` · ${la.currentValue} / ${la.targetValue}` : ''}
                  </p>
                  {la.dueDateUtc && <p className="text-xs text-gray-400">{t('teacher.studentDetails.dueShort', { date: formatDate(la.dueDateUtc) })}</p>}
                </div>
                <Badge variant={statusVariant(la.status)}>{t(statusKey(la.status))}</Badge>
              </div>
            ))}
          </div>
        </Card>
      )}

      {/* Медали */}
      {badges.length > 0 && (
        <Card>
          <CardHeader>
            <h2 className="font-semibold text-gray-800">{t('teacher.studentDetails.badges')}</h2>
          </CardHeader>
          <div className="divide-y divide-gray-100">
            {badges.map((b) => (
              <div key={b.code} className="flex items-center gap-4 px-6 py-3">
                <MedalIcon code={b.code} size="lg" />
                <div>
                  <p className="font-medium text-gray-900">{b.name}</p>
                  <p className="text-sm text-gray-400">{b.description}</p>
                  <p className="text-xs text-gray-300 mt-0.5">
                    {formatDate(b.awardedAt)}
                  </p>
                </div>
              </div>
            ))}
          </div>
        </Card>
      )}
    </div>
  )
}
