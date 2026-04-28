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
import type { ChallengeDto, TargetType } from '../../types'
import { formatDate } from '../../utils/formatDate'

const targetTypeOptions: { value: TargetType; label: string }[] = [
  { value: 'Pages', label: 'Прочети X страници' },
  { value: 'Books', label: 'Завърши X книги' },
  { value: 'Assignments', label: 'Изпълни X задачи' },
]

interface ChallengeCardProps {
  c: ChallengeDto
  classId: string
}

function ChallengeCard({ c, classId }: ChallengeCardProps) {
  const [showExtend, setShowExtend] = useState(false)
  const [extendDate, setExtendDate] = useState('')
  const qc = useQueryClient()

  const extendMutation = useMutation({
    mutationFn: () => challengesApi.extendDeadline(classId, c.id, new Date(extendDate).toISOString()),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['challenges', classId] })
      setShowExtend(false)
      setExtendDate('')
    },
  })

  return (
    <Card>
      <CardBody>
        <div className="flex items-center justify-between flex-wrap gap-4">
          <div>
            <div className="flex items-center gap-2 mb-0.5">
              <p className="font-medium text-gray-900">{c.title}</p>
              {c.isExpired
                ? <Badge variant="gray">Приключило</Badge>
                : <Badge variant="green">Активно</Badge>
              }
            </div>
            <p className="text-sm text-gray-400">
              {c.targetType}: {c.targetValue} ·{' '}
              {formatDate(c.startDate)} – {formatDate(c.endDate)}
              {c.points ? ` · ${c.points} т.` : ''}
            </p>
            {c.description && <p className="text-sm text-gray-500 mt-1">{c.description}</p>}
          </div>
          <div className="flex items-center gap-2">
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
      </CardBody>
    </Card>
  )
}

export function ClassChallengesPage() {
  const { classId } = useParams<{ classId: string }>()
  const qc = useQueryClient()
  const [showForm, setShowForm] = useState(false)
  const [form, setForm] = useState({
    title: '',
    description: '',
    targetType: 'Pages' as TargetType,
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
        ...form,
        targetValue: Number(form.targetValue),
        points: Number(form.points),
        startDate: new Date(form.startDate).toISOString(),
        endDate: new Date(form.endDate).toISOString(),
      }),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['challenges', classId] })
      setShowForm(false)
      setForm({ title: '', description: '', targetType: 'Pages', targetValue: '', startDate: '', endDate: '', points: '' })
    },
  })

  function set(field: keyof typeof form) {
    return (e: React.ChangeEvent<HTMLInputElement | HTMLSelectElement | HTMLTextAreaElement>) =>
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
                <div className="flex flex-col gap-1">
                  <label className="text-sm font-medium text-gray-700">Тип цел</label>
                  <select
                    value={form.targetType}
                    onChange={set('targetType')}
                    className="block w-full rounded-lg border border-gray-300 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-indigo-500"
                  >
                    {targetTypeOptions.map((o) => (
                      <option key={o.value} value={o.value}>{o.label}</option>
                    ))}
                  </select>
                </div>
                <Input label="Целева стойност" type="number" value={form.targetValue} onChange={set('targetValue')} required />
                <Input label="Точки" type="number" value={form.points} onChange={set('points')} required />
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
                <ChallengeCard key={c.id} c={c} classId={classId!} />
              ))}
            </div>
          )}

          {expired.length > 0 && (
            <div className="space-y-3">
              <h3 className="text-sm font-semibold text-gray-500 uppercase tracking-wide">
                Приключили предизвикателства
              </h3>
              {expired.map((c) => (
                <ChallengeCard key={c.id} c={c} classId={classId!} />
              ))}
            </div>
          )}
        </>
      )}
    </div>
  )
}
