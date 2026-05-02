import { useRef, useState } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { studentApi } from '../../api/student'
import { Card, CardBody, CardHeader } from '../../components/ui/Card'
import { Button } from '../../components/ui/Button'
import { Badge } from '../../components/ui/Badge'
import { Spinner } from '../../components/ui/Spinner'
import { CelebrationDialog } from '../../components/ui/CelebrationDialog'
import { AlertDialog } from '../../components/ui/AlertDialog'
import type { ProgressStatus, StudentChallengeDto } from '../../types'
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

export function StudentDashboardPage() {
  const qc = useQueryClient()
  const [readingPages, setReadingPages] = useState<Record<string, string>>({})
  const [celebration, setCelebration] = useState<{ studentName: string; bookTitle: string } | null>(null)
  const pendingCelebration = useRef<{ studentName: string; bookTitle: string } | null>(null)
  const [alertMessage, setAlertMessage] = useState('')

  const { data, isLoading } = useQuery({
    queryKey: ['student-me'],
    queryFn: studentApi.getMyDetails,
  })

  const readingMutation = useMutation({
    mutationFn: ({ assignedBookId, page }: { assignedBookId: string; page: number }) =>
      studentApi.updateReadingProgress(assignedBookId, page),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['student-me'] })
      setReadingPages({})
      if (pendingCelebration.current) {
        setCelebration(pendingCelebration.current)
        pendingCelebration.current = null
      }
    },
    onError: (err: unknown) => {
      const msg = (err as { response?: { data?: { error?: string } } })?.response?.data?.error
      setAlertMessage(msg ?? 'Грешка при запис.')
    },
  })

  const startMutation = useMutation({
    mutationFn: (assignmentId: string) => studentApi.startAssignment(assignmentId),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['student-me'] }),
    onError: (err: unknown) => {
      const msg = (err as { response?: { data?: { error?: string } } })?.response?.data?.error
      setAlertMessage(msg ?? 'Грешка при запис.')
    },
  })

  const assignmentMutation = useMutation({
    mutationFn: (assignmentId: string) => studentApi.completeAssignment(assignmentId),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['student-me'] }),
    onError: (err: unknown) => {
      const msg = (err as { response?: { data?: { error?: string } } })?.response?.data?.error
      setAlertMessage(msg ?? 'Грешка при запис.')
    },
  })

  const startChallengeMutation = useMutation({
    mutationFn: (challengeId: string) => studentApi.startChallenge(challengeId),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['student-me'] }),
    onError: (err: unknown) => {
      const msg = (err as { response?: { data?: { error?: string } } })?.response?.data?.error
      setAlertMessage(msg ?? 'Грешка при запис.')
    },
  })

  const completeChallengewMutation = useMutation({
    mutationFn: (challengeId: string) => studentApi.completeChallenge(challengeId),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['student-me'] }),
    onError: (err: unknown) => {
      const msg = (err as { response?: { data?: { error?: string } } })?.response?.data?.error
      setAlertMessage(msg ?? 'Грешка при запис.')
    },
  })

  if (isLoading) return <div className="flex items-center justify-center h-64"><Spinner /></div>
  if (!data) return null

  const activeReading = data.reading.filter((r) => r.status !== 'Completed')
  const completedReading = data.reading.filter((r) => r.status === 'Completed')
  const activeAssignments = data.assignments.filter((a) => a.status !== 'Completed')
  const completedAssignments = data.assignments.filter((a) => a.status === 'Completed')
  const activeChallenges = data.challenges.filter((c) => !c.completed && !c.isExpired)
  const completedChallenges = data.challenges.filter((c) => c.completed)
  const expiredChallenges = data.challenges.filter((c) => !c.completed && c.isExpired)

  function challengeTargetLabel(c: StudentChallengeDto) {
    if (c.targetDescription) return `${c.targetDescription}${c.targetValue ? ` · ${c.currentValue} / ${c.targetValue}` : ''}`
    if (c.targetValue) return `${c.currentValue} / ${c.targetValue}`
    return null
  }

  return (
    <div className="p-6 space-y-6 max-w-4xl mx-auto">
      {/* Header */}
      <div className="bg-gradient-to-r from-indigo-600 to-indigo-500 rounded-2xl p-5 text-white">
        <div className="flex items-center justify-between flex-wrap gap-4">
          <div>
            <h1 className="text-2xl font-bold">{data.studentName}</h1>
            <p className="text-indigo-200 text-sm mt-0.5">Моят напредък</p>
          </div>
          <div className="flex gap-3 text-center">
            <div className="bg-white/15 rounded-xl px-4 py-2.5 min-w-[72px]">
              <p className="text-lg font-bold">🏆 {data.totalPoints}</p>
              <p className="text-xs text-indigo-200 mt-0.5">точки</p>
            </div>
            <div className="bg-white/15 rounded-xl px-4 py-2.5 min-w-[72px]">
              <p className="text-lg font-bold">📖 {data.totalPagesRead}</p>
              <p className="text-xs text-indigo-200 mt-0.5">стр.</p>
            </div>
            <div className="bg-white/15 rounded-xl px-4 py-2.5 min-w-[72px]">
              <p className="text-lg font-bold">✅ {data.completedAssignments}</p>
              <p className="text-xs text-indigo-200 mt-0.5">задачи</p>
            </div>
          </div>
        </div>
      </div>

      {/* Активност последните 7 дни */}
      {data.activityLast7Days.length > 0 && (
        <Card>
          <CardHeader>
            <span className="flex items-center gap-2">📅 Активност — последните 7 дни</span>
          </CardHeader>
          <CardBody className="p-0">
            <table className="w-full text-sm">
              <thead className="text-xs text-gray-400 border-b border-gray-100 bg-gray-50">
                <tr>
                  <th className="text-left px-6 py-2.5">Дата</th>
                  <th className="text-left px-6 py-2.5">Активност</th>
                  <th className="text-right px-6 py-2.5">Точки</th>
                </tr>
              </thead>
              <tbody>
                {data.activityLast7Days.map((entry, i) => (
                  <tr key={i} className={`border-b border-gray-50 hover:bg-gray-50 transition-colors ${i % 2 === 0 ? '' : 'bg-gray-50/40'}`}>
                    <td className="px-6 py-2.5 text-gray-500 whitespace-nowrap">{formatDate(entry.date)}</td>
                    <td className="px-6 py-2.5 text-gray-700">{entry.description}</td>
                    <td className="px-6 py-2.5 text-right">
                      {entry.pointsEarned > 0
                        ? <span className="font-semibold text-indigo-600 bg-indigo-50 px-2 py-0.5 rounded-lg">+{entry.pointsEarned}</span>
                        : <span className="text-gray-400">—</span>
                      }
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </CardBody>
        </Card>
      )}

      {/* Reading */}
      {data.reading.length > 0 && (
        <Card>
          <CardHeader><span className="flex items-center gap-2">📖 Четене</span></CardHeader>
          <CardBody className="space-y-4">
            {activeReading.map((r) => {
              const pagesInput = readingPages[r.assignedBookId] ?? ''
              const isExpired = r.isExpired && r.status !== 'Completed'
              return (
                <div key={r.assignedBookId} className="border border-gray-100 rounded-xl p-4 space-y-3">
                  <div className="flex items-start justify-between gap-2">
                    <div className="min-w-0">
                      <p className="font-medium text-gray-900 truncate">{r.bookTitle}</p>
                      <p className="text-xs text-gray-400 mt-0.5">
                        стр. {r.currentPage} / {r.totalPages ?? '?'}
                      </p>
                    </div>
                    <Badge variant={isExpired ? 'red' : statusVariant(r.status)}>
                      {isExpired ? 'Просрочено' : translateStatus(r.status)}
                    </Badge>
                  </div>
                  {r.totalPages && (
                    <div className="space-y-1">
                      <div className="w-full bg-gray-100 rounded-full h-2.5">
                        <div
                          className="bg-indigo-500 h-2.5 rounded-full transition-all"
                          style={{ width: `${Math.min(100, (r.currentPage / r.totalPages) * 100)}%` }}
                        />
                      </div>
                      <p className="text-xs text-gray-400 text-right">{Math.round((r.currentPage / r.totalPages) * 100)}%</p>
                    </div>
                  )}
                  {!isExpired && (
                    <div className="flex gap-2">
                      <input
                        type="number"
                        min={r.currentPage}
                        max={r.totalPages ?? undefined}
                        value={pagesInput}
                        onChange={(e) => setReadingPages((p) => ({ ...p, [r.assignedBookId]: e.target.value }))}
                        placeholder={`Текуща страница (${r.currentPage})`}
                        className="flex-1 border border-gray-200 rounded-lg px-3 py-1.5 text-sm focus:outline-none focus:ring-2 focus:ring-indigo-300"
                      />
                      <Button
                        size="sm"
                        disabled={!pagesInput || readingMutation.isPending}
                        loading={readingMutation.isPending}
                        onClick={() => {
                          const page = parseInt(pagesInput)
                          if (isNaN(page)) return
                          if (r.totalPages && page >= r.totalPages) {
                            pendingCelebration.current = { studentName: data.studentName, bookTitle: r.bookTitle }
                          }
                          readingMutation.mutate({ assignedBookId: r.assignedBookId, page })
                        }}
                      >
                        Запиши
                      </Button>
                    </div>
                  )}
                </div>
              )
            })}
            {completedReading.map((r) => (
              <div key={r.assignedBookId} className="border border-gray-100 rounded-xl p-4 flex items-center justify-between gap-2 opacity-60">
                <p className="font-medium text-gray-900 truncate">{r.bookTitle}</p>
                <Badge variant="green">Завършено</Badge>
              </div>
            ))}
          </CardBody>
        </Card>
      )}

      {/* Assignments */}
      {data.assignments.length > 0 && (
        <Card>
          <CardHeader><span className="flex items-center gap-2">📝 Задачи</span></CardHeader>
          <CardBody className="space-y-3">
            {activeAssignments.map((a) => {
              const isExpired = a.isExpired && a.status !== 'Completed'
              return (
                <div key={a.assignmentId} className="border border-gray-100 rounded-xl p-4 flex items-center justify-between gap-3">
                  <div className="min-w-0">
                    <p className="font-medium text-gray-900 truncate">{a.title}</p>
                    <p className="text-xs text-gray-400 mt-0.5">{a.subject}</p>
                  </div>
                  <div className="flex items-center gap-2 shrink-0">
                    <Badge variant={isExpired ? 'red' : statusVariant(a.status)}>
                      {isExpired ? 'Просрочено' : translateStatus(a.status)}
                    </Badge>
                    {!isExpired && a.status === 'NotStarted' && (
                      <Button
                        size="sm"
                        variant="secondary"
                        loading={startMutation.isPending}
                        onClick={() => startMutation.mutate(a.assignmentId)}
                      >
                        Стартирай
                      </Button>
                    )}
                    {!isExpired && a.status === 'InProgress' && (
                      <Button
                        size="sm"
                        variant="success"
                        loading={assignmentMutation.isPending}
                        onClick={() => assignmentMutation.mutate(a.assignmentId)}
                      >
                        Завърши
                      </Button>
                    )}
                  </div>
                </div>
              )
            })}
            {completedAssignments.map((a) => (
              <div key={a.assignmentId} className="border border-gray-100 rounded-xl p-4 flex items-center justify-between gap-2 opacity-60">
                <p className="font-medium text-gray-900 truncate">{a.title}</p>
                <Badge variant="green">Завършено</Badge>
              </div>
            ))}
          </CardBody>
        </Card>
      )}

      {/* Предизвикателства */}
      {data.challenges.length > 0 && (
        <Card>
          <CardHeader><span className="flex items-center gap-2">⚡ Предизвикателства</span></CardHeader>
          <CardBody className="space-y-3">
            {activeChallenges.map((c) => (
              <div key={c.challengeId} className="border border-gray-100 rounded-xl p-4 flex items-center justify-between gap-3">
                <div className="min-w-0">
                  <p className="font-medium text-gray-900 truncate">{c.title}</p>
                  {challengeTargetLabel(c) && (
                    <p className="text-xs text-gray-400 mt-0.5">{challengeTargetLabel(c)}</p>
                  )}
                  <p className="text-xs text-gray-400">Срок: {formatDate(c.endDate)}</p>
                </div>
                <div className="flex items-center gap-2 shrink-0">
                  {!c.started && (
                    <>
                      <Badge variant="gray">Не е стартирано</Badge>
                      <Button
                        size="sm"
                        variant="secondary"
                        loading={startChallengeMutation.isPending}
                        onClick={() => startChallengeMutation.mutate(c.challengeId)}
                      >
                        Стартирай
                      </Button>
                    </>
                  )}
                  {c.started && (
                    <>
                      <Badge variant="blue">В процес</Badge>
                      <Button
                        size="sm"
                        variant="success"
                        loading={completeChallengewMutation.isPending}
                        onClick={() => completeChallengewMutation.mutate(c.challengeId)}
                      >
                        Завърши
                      </Button>
                    </>
                  )}
                </div>
              </div>
            ))}
            {expiredChallenges.map((c) => (
              <div key={c.challengeId} className="border border-gray-100 rounded-xl p-4 flex items-center justify-between gap-2 opacity-60">
                <div className="min-w-0">
                  <p className="font-medium text-gray-900 truncate">{c.title}</p>
                  {challengeTargetLabel(c) && (
                    <p className="text-xs text-gray-400 mt-0.5">{challengeTargetLabel(c)}</p>
                  )}
                </div>
                <Badge variant="red">Просрочено</Badge>
              </div>
            ))}
            {completedChallenges.map((c) => (
              <div key={c.challengeId} className="border border-gray-100 rounded-xl p-4 flex items-center justify-between gap-2 opacity-60">
                <p className="font-medium text-gray-900 truncate">{c.title}</p>
                <Badge variant="green">Завършено</Badge>
              </div>
            ))}
          </CardBody>
        </Card>
      )}

      {data.reading.length === 0 && data.assignments.length === 0 && data.challenges.length === 0 && (
        <Card>
          <CardBody>
            <div className="text-center py-10">
              <div className="text-5xl mb-3">🎒</div>
              <p className="font-semibold text-gray-700">Все още нямаш задания</p>
              <p className="text-sm text-gray-400 mt-1">Свържи се с учителя си, за да те добави в клас.</p>
            </div>
          </CardBody>
        </Card>
      )}

      {celebration && (
        <CelebrationDialog
          studentName={celebration.studentName}
          bookTitle={celebration.bookTitle}
          onClose={() => setCelebration(null)}
        />
      )}

      {alertMessage && (
        <AlertDialog message={alertMessage} onClose={() => setAlertMessage('')} />
      )}
    </div>
  )
}