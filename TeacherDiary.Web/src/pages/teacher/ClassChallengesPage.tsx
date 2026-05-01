import { useState } from 'react'
import { useParams } from 'react-router-dom'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { challengesApi } from '../../api/challenges'
import { Card, CardBody, CardHeader } from '../../components/ui/Card'
import { Button } from '../../components/ui/Button'
import { Input } from '../../components/ui/Input'
import { DateInput } from '../../components/ui/DateInput'
import { Spinner } from '../../components/ui/Spinner'
import { Badge } from '../../components/ui/Badge'
import type { ChallengeDto } from '../../types'
import { formatDate } from '../../utils/formatDate'

function EyeIcon() {
  return (
    <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor" className="w-4 h-4">
      <path d="M10 12.5a2.5 2.5 0 100-5 2.5 2.5 0 000 5z" />
      <path fillRule="evenodd" d="M.664 10.59a1.651 1.651 0 010-1.186A10.004 10.004 0 0110 3c4.257 0 7.893 2.66 9.336 6.41.147.381.146.804 0 1.186A10.004 10.004 0 0110 17c-4.257 0-7.893-2.66-9.336-6.41z" clipRule="evenodd" />
    </svg>
  )
}

function ChallengePreviewModal({ c, onClose }: { c: ChallengeDto; onClose: () => void }) {
  const targetLabel = c.targetDescription
    ? `${c.targetDescription}${c.targetValue ? ` · ${c.targetValue}` : ''}`
    : c.targetValue
    ? String(c.targetValue)
    : null

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center">
      <div className="absolute inset-0 bg-black/40" onClick={onClose} />
      <div className="relative bg-white rounded-xl shadow-lg p-6 max-w-md w-full mx-4 space-y-3">
        <h2 className="text-lg font-bold text-gray-900">{c.title}</h2>
        <div className="text-sm text-gray-600 space-y-1">
          <p><span className="font-medium text-gray-700">Начало:</span> {formatDate(c.startDate)}</p>
          <p><span className="font-medium text-gray-700">Краен срок:</span> {formatDate(c.endDate)}</p>
          {targetLabel && (
            <p><span className="font-medium text-gray-700">Цел:</span> {targetLabel}</p>
          )}
          {c.points > 0 && (
            <p><span className="font-medium text-gray-700">Точки за изпълнение:</span> {c.points} т.</p>
          )}
          {c.description && (
            <p><span className="font-medium text-gray-700">Описание:</span> {c.description}</p>
          )}
          <p>
            <span className="font-medium text-gray-700">Изпълнение:</span>{' '}
            {c.completedCount}/{c.totalStudents} ученика
          </p>
        </div>
        <div className="flex justify-end pt-2">
          <button
            onClick={onClose}
            className="px-4 py-2 text-sm font-medium rounded-lg bg-gray-100 text-gray-700 hover:bg-gray-200"
          >
            Затвори
          </button>
        </div>
      </div>
    </div>
  )
}

function challengeStudentStatusLabel(started: boolean, completed: boolean) {
  if (completed) return { text: 'Завършено', variant: 'green' as const }
  if (started) return { text: 'В процес', variant: 'blue' as const }
  return { text: 'Не е стартирано', variant: 'gray' as const }
}

interface ChallengeCardProps {
  c: ChallengeDto
  classId: string
  onPreview: (c: ChallengeDto) => void
}

function ChallengeCard({ c, classId, onPreview }: ChallengeCardProps) {
  const [expanded, setExpanded] = useState(false)
  const [showExtend, setShowExtend] = useState(false)
  const [extendDate, setExtendDate] = useState('')
  const qc = useQueryClient()

  const { data: students = [], isLoading: studentsLoading } = useQuery({
    queryKey: ['challenge-student-progress', classId, c.id],
    queryFn: () => challengesApi.getStudentProgress(classId, c.id),
    enabled: expanded,
  })

  const extendMutation = useMutation({
    mutationFn: () => challengesApi.extendDeadline(classId, c.id, new Date(extendDate).toISOString()),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['challenges', classId] })
      setShowExtend(false)
      setExtendDate('')
    },
  })

  const targetLabel = c.targetDescription
    ? `${c.targetDescription}${c.targetValue ? ` · ${c.targetValue}` : ''}`
    : c.targetValue
    ? String(c.targetValue)
    : null

  return (
    <Card>
      <CardBody>
        <div className="flex items-center justify-between flex-wrap gap-4">
          <button
            className="flex items-center gap-2 text-left group flex-1 min-w-0"
            onClick={() => setExpanded((v) => !v)}
          >
            <span
              className="text-gray-400 text-sm transition-transform duration-200 shrink-0"
              style={{ display: 'inline-block', transform: expanded ? 'rotate(90deg)' : 'rotate(0deg)' }}
            >
              ▶
            </span>
            <div className="min-w-0">
              <div className="flex items-center gap-2 mb-0.5">
                <p className="font-medium text-gray-900 group-hover:text-indigo-600 transition-colors">
                  {c.title}
                </p>
                {c.isExpired
                  ? <Badge variant="gray">Приключило</Badge>
                  : <Badge variant="green">Активно</Badge>
                }
              </div>
              <p className="text-sm text-gray-400">
                {targetLabel ? `${targetLabel} · ` : ''}
                {formatDate(c.startDate)} – {formatDate(c.endDate)}
                {c.points ? ` · ${c.points} т.` : ''}
              </p>
              {c.description && <p className="text-sm text-gray-500 mt-0.5">{c.description}</p>}
            </div>
          </button>

          <div className="flex items-center gap-2 shrink-0">
            <div className="relative group">
              <button
                onClick={() => onPreview(c)}
                className="p-1 rounded text-gray-400 hover:text-emerald-600 hover:bg-emerald-50 transition-colors"
              >
                <EyeIcon />
              </button>
              <span className="pointer-events-none absolute left-1/2 -translate-x-1/2 -top-8 whitespace-nowrap rounded bg-gray-800 px-2 py-1 text-xs text-white opacity-0 group-hover:opacity-100 transition-opacity">
                Преглед
              </span>
            </div>
            <Badge variant={c.completedCount === c.totalStudents && c.totalStudents > 0 ? 'green' : 'gray'}>
              {c.completedCount}/{c.totalStudents} завършили
            </Badge>
            {c.isExpired && (
              <Button size="sm" variant="secondary" onClick={() => setShowExtend((v) => !v)}>
                Промени срок
              </Button>
            )}
          </div>
        </div>

        {showExtend && (
          <div className="mt-3 flex items-end gap-2 border-t border-gray-100 pt-3">
            <DateInput
              label="Нова крайна дата"
              value={extendDate}
              onChange={setExtendDate}
              className="w-48"
            />
            <Button
              size="sm"
              loading={extendMutation.isPending}
              disabled={!extendDate}
              onClick={() => extendMutation.mutate()}
            >
              Запази
            </Button>
            <Button size="sm" variant="secondary" onClick={() => setShowExtend(false)}>
              Отказ
            </Button>
          </div>
        )}

        {expanded && (
          <div className="mt-4 border-t border-gray-100 pt-4">
            {studentsLoading ? (
              <div className="flex justify-center py-4">
                <Spinner className="text-indigo-600 h-5 w-5" />
              </div>
            ) : students.length === 0 ? (
              <p className="text-sm text-gray-400 text-center py-2">Няма ученици.</p>
            ) : (
              <div className="divide-y divide-gray-50">
                {students.map((s) => {
                  const { text, variant } = challengeStudentStatusLabel(s.started, s.completed)
                  return (
                    <div key={s.studentId} className="py-3 flex items-center justify-between gap-4">
                      <p className="text-sm font-medium text-gray-800">{s.studentName}</p>
                      <Badge variant={variant}>{text}</Badge>
                    </div>
                  )
                })}
              </div>
            )}
          </div>
        )}
      </CardBody>
    </Card>
  )
}

export function ClassChallengesPage() {
  const { classId } = useParams<{ classId: string }>()
  const qc = useQueryClient()
  const [showForm, setShowForm] = useState(false)
  const [previewChallenge, setPreviewChallenge] = useState<ChallengeDto | null>(null)
  const [form, setForm] = useState({
    title: '',
    description: '',
    targetDescription: '',
    targetValue: '',
    startDate: '',
    endDate: '',
    points: '',
  })

  const { data: challenges = [], isLoading } = useQuery({
    queryKey: ['challenges', classId],
    queryFn: () => challengesApi.getByClass(classId!),
  })

  const createMutation = useMutation({
    mutationFn: () =>
      challengesApi.create(classId!, {
        title: form.title,
        description: form.description,
        targetDescription: form.targetDescription || undefined,
        targetType: 'None',
        targetValue: form.targetValue ? Number(form.targetValue) : 0,
        points: form.points ? Number(form.points) : 0,
        startDate: new Date(form.startDate).toISOString(),
        endDate: new Date(form.endDate).toISOString(),
      }),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['challenges', classId] })
      setShowForm(false)
      setForm({ title: '', description: '', targetDescription: '', targetValue: '', startDate: '', endDate: '', points: '' })
    },
  })

  function set(field: keyof typeof form) {
    return (e: React.ChangeEvent<HTMLInputElement | HTMLTextAreaElement>) =>
      setForm((prev) => ({ ...prev, [field]: e.target.value }))
  }

  const active = challenges.filter((c) => !c.isExpired)
  const expired = challenges.filter((c) => c.isExpired)

  return (
    <div className="space-y-6">
      <div className="flex justify-end">
        <Button onClick={() => setShowForm(true)}>+ Ново предизвикателство</Button>
      </div>

      {showForm && (
        <Card>
          <CardHeader>
            <h2 className="font-semibold text-gray-800">Създаване на предизвикателство</h2>
          </CardHeader>
          <CardBody>
            <form onSubmit={(e) => { e.preventDefault(); createMutation.mutate() }} className="space-y-4">
              <Input label="Заглавие" value={form.title} onChange={set('title')} required autoFocus />
              <div className="flex flex-col gap-1">
                <label className="text-sm font-medium text-gray-700">Описание</label>
                <textarea
                  value={form.description}
                  onChange={set('description')}
                  rows={2}
                  className="block w-full rounded-lg border border-gray-300 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-indigo-500 resize-none"
                />
              </div>
              <div className="grid grid-cols-2 gap-4">
                <Input
                  label="Тип цел"
                  placeholder="напр. Прочети книга, Реши задачи..."
                  value={form.targetDescription}
                  onChange={set('targetDescription')}
                />
                <Input
                  label="Целева стойност"
                  type="number"
                  placeholder="напр. 5"
                  value={form.targetValue}
                  onChange={set('targetValue')}
                />
                <Input label="Точки" type="number" value={form.points} onChange={set('points')} />
                <DateInput label="Начална дата" value={form.startDate} onChange={(v) => setForm((p) => ({ ...p, startDate: v }))} required />
                <DateInput label="Крайна дата" value={form.endDate} onChange={(v) => setForm((p) => ({ ...p, endDate: v }))} required />
              </div>
              <div className="flex gap-2">
                <Button type="submit" loading={createMutation.isPending}>Създай</Button>
                <Button variant="secondary" type="button" onClick={() => setShowForm(false)}>Отказ</Button>
              </div>
            </form>
          </CardBody>
        </Card>
      )}

      {isLoading ? (
        <div className="flex justify-center py-10">
          <Spinner className="text-indigo-600 h-7 w-7" />
        </div>
      ) : (
        <>
          {active.length === 0 && expired.length === 0 && (
            <Card>
              <CardBody className="text-center py-12">
                <p className="text-gray-400 text-sm">Няма предизвикателства.</p>
              </CardBody>
            </Card>
          )}

          {active.length > 0 && (
            <div className="space-y-3">
              {active.map((c) => (
                <ChallengeCard key={c.id} c={c} classId={classId!} onPreview={setPreviewChallenge} />
              ))}
            </div>
          )}

          {expired.length > 0 && (
            <div className="space-y-3">
              <h3 className="text-sm font-semibold text-gray-500 uppercase tracking-wide">
                Приключили предизвикателства
              </h3>
              {expired.map((c) => (
                <ChallengeCard key={c.id} c={c} classId={classId!} onPreview={setPreviewChallenge} />
              ))}
            </div>
          )}
        </>
      )}

      {previewChallenge && (
        <ChallengePreviewModal c={previewChallenge} onClose={() => setPreviewChallenge(null)} />
      )}
    </div>
  )
}
