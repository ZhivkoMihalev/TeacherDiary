import { useRef, useState } from 'react'
import { useParams, Link } from 'react-router-dom'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { parentApi } from '../../api/parent'
import { Card, CardBody, CardHeader } from '../../components/ui/Card'
import { Button } from '../../components/ui/Button'
import { Badge } from '../../components/ui/Badge'
import { Spinner } from '../../components/ui/Spinner'
import { CelebrationDialog } from '../../components/ui/CelebrationDialog'
import { MedalIcon } from '../../components/ui/MedalIcon'
import { AlertDialog } from '../../components/ui/AlertDialog'
import type { ProgressStatus } from '../../types'
import { formatDate } from '../../utils/formatDate'

function statusVariant(s: ProgressStatus) {
  if (s === 'Completed') return 'green'
  if (s === 'InProgress') return 'blue'
  return 'gray'
}

function translateStatus(s: ProgressStatus) {
  if (s === 'Completed') return 'Завършено'
  if (s === 'InProgress') return 'В процес'
  return 'Не е стартирано'
}

export function StudentProgressPage() {
  const { studentId } = useParams<{ studentId: string }>()
  const qc = useQueryClient()
  const [readingPages, setReadingPages] = useState<Record<string, string>>({})
  const [celebration, setCelebration] = useState<{ studentName: string; bookTitle: string } | null>(null)
  const pendingCelebration = useRef<{ studentName: string; bookTitle: string } | null>(null)
  const [alertMessage, setAlertMessage] = useState('')

  const { data, isLoading } = useQuery({
    queryKey: ['parent-student', studentId],
    queryFn: () => parentApi.getStudent(studentId!),
  })

  const readingMutation = useMutation({
    mutationFn: ({ assignedBookId, page }: { assignedBookId: string; page: number }) =>
      parentApi.updateReadingProgress(studentId!, assignedBookId, page),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['parent-student', studentId] })
      setReadingPages({})
      if (pendingCelebration.current) {
        setCelebration(pendingCelebration.current)
        pendingCelebration.current = null
      }
    },
  })

  const assignmentMutation = useMutation({
    mutationFn: ({ assignmentId, markCompleted }: { assignmentId: string; markCompleted: boolean }) =>
      parentApi.updateAssignmentProgress(studentId!, assignmentId, markCompleted),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['parent-student', studentId] })
    },
  })

  if (isLoading) {
    return (
      <div className="flex justify-center items-center h-64">
        <Spinner className="text-emerald-600 h-8 w-8" />
      </div>
    )
  }

  if (!data) return null

  const activeReading = data.reading.filter((r) => !r.isExpired)
  const archivedReading = data.reading.filter((r) => r.isExpired)
  const activeAssignments = data.assignments.filter((a) => !a.isExpired)
  const archivedAssignments = data.assignments.filter((a) => a.isExpired)
  const activeActivities = data.learningActivities.filter((la) => !la.isExpired)
  const archivedActivities = data.learningActivities.filter((la) => la.isExpired)
  const hasArchive = archivedReading.length > 0 || archivedAssignments.length > 0 || archivedActivities.length > 0

  return (
    <div className="p-8 max-w-4xl mx-auto space-y-6">
      <Link to="/parent/students" className="text-sm text-gray-400 hover:text-gray-600">
        ← Назад
      </Link>

      <div className="flex items-center gap-4">
        <div>
          <h1 className="text-2xl font-bold text-gray-900 flex items-center gap-2 flex-wrap">
            {data.studentName}
            {data.topMedalCode && <MedalIcon code={data.topMedalCode} size="md" />}
            {data.topPointsMedalCode && <MedalIcon code={data.topPointsMedalCode} size="md" />}
          </h1>
          <p className="text-gray-400 text-sm mt-0.5">
            {data.lastActivityAt
              ? `Последно активен: ${formatDate(data.lastActivityAt)}`
              : 'Няма активност'}
          </p>
        </div>
        {!data.isActive && <Badge variant="gray">Неактивен</Badge>}
      </div>

      {/* Статистика */}
      <div className="grid grid-cols-2 sm:grid-cols-3 gap-4">
        <Card>
          <CardBody>
            <p className="text-xs text-gray-400 mb-1">Прочетени стр.</p>
            <p className="text-2xl font-bold text-gray-900">{data.totalPagesRead}</p>
          </CardBody>
        </Card>
        <Card>
          <CardBody>
            <p className="text-xs text-gray-400 mb-1">Изпълнени задачи</p>
            <p className="text-2xl font-bold text-gray-900">{data.completedAssignments}</p>
          </CardBody>
        </Card>
        <Card>
          <CardBody>
            <p className="text-xs text-gray-400 mb-1">Спечелени точки</p>
            <p className="text-2xl font-bold text-emerald-600">{data.totalPoints}</p>
          </CardBody>
        </Card>
      </div>

      {/* Активност последните 7 дни */}
      {data.activityLast7Days.length > 0 && (
        <Card>
          <CardHeader>
            <h2 className="font-semibold text-gray-800">Активност — последните 7 дни</h2>
          </CardHeader>
          <CardBody className="p-0">
            <table className="w-full text-sm">
              <thead className="text-xs text-gray-400 border-b border-gray-100">
                <tr>
                  <th className="text-left px-6 py-2">Дата</th>
                  <th className="text-right px-6 py-2">Стр.</th>
                  <th className="text-right px-6 py-2">Задачи</th>
                  <th className="text-right px-6 py-2">Точки</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-gray-50">
                {data.activityLast7Days.map((day) => (
                  <tr key={day.date}>
                    <td className="px-6 py-2.5 text-gray-700">{formatDate(day.date)}</td>
                    <td className="px-6 py-2.5 text-right text-gray-700">{day.pagesRead}</td>
                    <td className="px-6 py-2.5 text-right text-gray-700">{day.assignmentsCompleted}</td>
                    <td className="px-6 py-2.5 text-right font-medium text-emerald-600">{day.pointsEarned}</td>
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
            <h2 className="font-semibold text-gray-800">Четене</h2>
          </CardHeader>
          <div className="divide-y divide-gray-100">
            {activeReading.map((r) => {
              const pct = r.totalPages ? Math.round((r.currentPage / r.totalPages) * 100) : 0
              const pageInput = readingPages[r.assignedBookId] ?? ''

              return (
                <div key={r.assignedBookId} className="px-6 py-4">
                  <div className="flex items-start justify-between mb-2">
                    <div>
                      <p className="font-medium text-gray-900">{r.bookTitle}</p>
                      <p className="text-sm text-gray-400">
                        Стр. {r.currentPage}{r.totalPages ? ` от ${r.totalPages}` : ''}
                      </p>
                    </div>
                    <Badge variant={statusVariant(r.status)}>{translateStatus(r.status)}</Badge>
                  </div>

                  {r.totalPages && (
                    <div className="w-full bg-gray-100 rounded-full h-1.5 mb-3">
                      <div
                        className="bg-emerald-500 h-1.5 rounded-full transition-all"
                        style={{ width: `${pct}%` }}
                      />
                    </div>
                  )}

                  {r.isExpired && r.status !== 'Completed' && (
                    <p className="text-xs text-gray-400 mt-1">🔒 Срокът е изтекъл — въвеждането на напредък е заключено.</p>
                  )}

                  {!r.isExpired && r.status !== 'Completed' && (
                    <div className="flex flex-col gap-1">
                      <div className="flex items-center gap-2">
                        <input
                          type="number"
                          placeholder="Брой прочетени стр."
                          min={1}
                          className="w-36 rounded-lg border border-gray-300 px-3 py-1.5 text-sm focus:outline-none focus:ring-2 focus:ring-emerald-500"
                          value={pageInput}
                          onChange={(e) =>
                            setReadingPages((prev) => ({ ...prev, [r.assignedBookId]: e.target.value }))
                          }
                        />
                        <Button
                          size="sm"
                          variant="secondary"
                          loading={readingMutation.isPending}
                          disabled={!pageInput || Number(pageInput) < 1}
                          onClick={() => {
                            const newPage = r.currentPage + Number(pageInput)
                            if (r.totalPages && newPage > r.totalPages) {
                              setAlertMessage('Не можете да отбележите повече страници от броя на оставащите до края на книгата!')
                              return
                            }
                            if (r.totalPages && newPage >= r.totalPages) {
                              pendingCelebration.current = {
                                studentName: data.studentName,
                                bookTitle: r.bookTitle,
                              }
                            }
                            readingMutation.mutate({
                              assignedBookId: r.assignedBookId,
                              page: newPage,
                            })
                          }}
                        >
                          Актуализирай
                        </Button>
                      </div>
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
            <h2 className="font-semibold text-gray-800">Задачи</h2>
          </CardHeader>
          <div className="divide-y divide-gray-100">
            {activeAssignments.map((a) => (
              <div key={a.assignmentId} className="flex items-center justify-between px-6 py-3">
                <div>
                  <p className="font-medium text-gray-900">{a.title}</p>
                  <p className="text-sm text-gray-400">
                    {a.subject}{a.dueDate ? ` · Срок: ${formatDate(a.dueDate)}` : ''}
                  </p>
                </div>
                <div className="flex items-center gap-2">
                  <Badge variant={statusVariant(a.status)}>{translateStatus(a.status)}</Badge>
                  {a.isExpired && a.status !== 'Completed' && (
                    <span className="text-xs text-gray-400">🔒 Заключено</span>
                  )}
                  {!a.isExpired && a.status === 'NotStarted' && (
                    <Button
                      size="sm"
                      variant="success"
                      loading={assignmentMutation.isPending}
                      onClick={() => assignmentMutation.mutate({ assignmentId: a.assignmentId, markCompleted: false })}
                    >
                      Старт
                    </Button>
                  )}
                  {!a.isExpired && a.status === 'InProgress' && (
                    <Button
                      size="sm"
                      loading={assignmentMutation.isPending}
                      onClick={() => assignmentMutation.mutate({ assignmentId: a.assignmentId, markCompleted: true })}
                    >
                      Отбележи като изпълнено
                    </Button>
                  )}
                </div>
              </div>
            ))}
          </div>
        </Card>
      )}

      {/* Учебни задания */}
      {activeActivities.length > 0 && (
        <Card>
          <CardHeader>
            <h2 className="font-semibold text-gray-800">Учебни задания</h2>
          </CardHeader>
          <div className="divide-y divide-gray-100">
            {activeActivities.map((la) => (
              <div key={la.learningActivityId} className="flex items-center justify-between px-6 py-3">
                <div>
                  <p className="font-medium text-gray-900">{la.title}</p>
                  <p className="text-sm text-gray-400 capitalize">
                    {la.type}
                    {la.targetValue ? ` · ${la.currentValue} / ${la.targetValue}` : ''}
                  </p>
                  {la.dueDateUtc && (
                    <p className="text-xs text-gray-400">
                      Срок: {formatDate(la.dueDateUtc)}
                    </p>
                  )}
                </div>
                <Badge variant={statusVariant(la.status)}>{translateStatus(la.status)}</Badge>
              </div>
            ))}
          </div>
        </Card>
      )}
      {/* Архив */}
      {hasArchive && (
        <Card>
          <CardHeader>
            <h2 className="font-semibold text-gray-500">Архив</h2>
          </CardHeader>
          <div className="divide-y divide-gray-50">
            {archivedReading.map((r) => {
              const pct = r.totalPages ? Math.round((r.currentPage / r.totalPages) * 100) : 0
              return (
                <div key={r.assignedBookId} className="px-6 py-4 opacity-60">
                  <div className="flex items-start justify-between mb-2">
                    <div>
                      <p className="font-medium text-gray-700">{r.bookTitle}</p>
                      <p className="text-sm text-gray-400">Стр. {r.currentPage}{r.totalPages ? ` от ${r.totalPages}` : ''}</p>
                    </div>
                    <Badge variant={statusVariant(r.status)}>{translateStatus(r.status)}</Badge>
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
                  <p className="text-sm text-gray-400">{a.subject}{a.dueDate ? ` · Срок: ${formatDate(a.dueDate)}` : ''}</p>
                </div>
                <Badge variant={statusVariant(a.status)}>{translateStatus(a.status)}</Badge>
              </div>
            ))}
            {archivedActivities.map((la) => (
              <div key={la.learningActivityId} className="flex items-center justify-between px-6 py-3 opacity-60">
                <div>
                  <p className="font-medium text-gray-700">{la.title}</p>
                  <p className="text-sm text-gray-400 capitalize">
                    {la.type}{la.targetValue ? ` · ${la.currentValue} / ${la.targetValue}` : ''}
                  </p>
                  {la.dueDateUtc && <p className="text-xs text-gray-400">Срок: {formatDate(la.dueDateUtc)}</p>}
                </div>
                <Badge variant={statusVariant(la.status)}>{translateStatus(la.status)}</Badge>
              </div>
            ))}
          </div>
        </Card>
      )}

      {alertMessage && (
        <AlertDialog message={alertMessage} onClose={() => setAlertMessage('')} />
      )}

      {celebration && (
        <CelebrationDialog
          studentName={celebration.studentName}
          bookTitle={celebration.bookTitle}
          onClose={() => setCelebration(null)}
        />
      )}
    </div>
  )
}
