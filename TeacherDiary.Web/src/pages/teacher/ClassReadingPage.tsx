import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { useParams } from 'react-router-dom'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { booksApi } from '../../api/books'
import { Card, CardBody, CardHeader } from '../../components/ui/Card'
import { Button } from '../../components/ui/Button'
import { DateInput } from '../../components/ui/DateInput'
import { Input } from '../../components/ui/Input'
import { Spinner } from '../../components/ui/Spinner'
import { Badge } from '../../components/ui/Badge'
import { ConfirmDialog } from '../../components/ui/ConfirmDialog'
import { formatDate } from '../../utils/formatDate'
import type { AssignedBookDto, AssignedBookStudentProgressDto } from '../../types'

function PencilIcon() {
  return (
    <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor" className="w-4 h-4">
      <path d="M2.695 14.763l-1.262 3.154a.5.5 0 00.65.65l3.155-1.262a4 4 0 001.343-.885L17.5 5.5a2.121 2.121 0 00-3-3L3.58 13.42a4 4 0 00-.885 1.343z" />
    </svg>
  )
}

function EyeIcon() {
  return (
    <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor" className="w-4 h-4">
      <path d="M10 12.5a2.5 2.5 0 100-5 2.5 2.5 0 000 5z" />
      <path fillRule="evenodd" d="M.664 10.59a1.651 1.651 0 010-1.186A10.004 10.004 0 0110 3c4.257 0 7.893 2.66 9.336 6.41.147.381.146.804 0 1.186A10.004 10.004 0 0110 17c-4.257 0-7.893-2.66-9.336-6.41z" clipRule="evenodd" />
    </svg>
  )
}

function BookPreviewModal({ ab, onClose }: { ab: AssignedBookDto; onClose: () => void }) {
  const { t } = useTranslation()
  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center">
      <div className="absolute inset-0 bg-black/40" onClick={onClose} />
      <div className="relative bg-white rounded-xl shadow-lg p-6 max-w-md w-full mx-4 space-y-3">
        <h2 className="text-lg font-bold text-gray-900">{ab.title}</h2>
        <div className="text-sm text-gray-600 space-y-1">
          <p><span className="font-medium text-gray-700">{t('teacher.classReading.previewAuthor')}</span> {ab.author}</p>
          <p><span className="font-medium text-gray-700">{t('teacher.classReading.previewPages')}</span> {ab.totalPages}</p>
          <p><span className="font-medium text-gray-700">{t('teacher.classReading.previewStart')}</span> {formatDate(ab.startDateUtc)}</p>
          <p><span className="font-medium text-gray-700">{t('teacher.classReading.previewDeadline')}</span> {formatDate(ab.endDateUtc)}</p>
          {ab.points > 0 && (
            <p><span className="font-medium text-gray-700">{t('teacher.classReading.previewPoints')}</span> {t('teacher.classDashboard.pointsDisplay', { points: ab.points })}</p>
          )}
          <p>
            <span className="font-medium text-gray-700">{t('teacher.classReading.previewProgress')}</span>{' '}
            {t('teacher.classReading.progressSummary', { completed: ab.completedCount, reading: ab.inProgressCount, notStarted: ab.notStartedCount })}
          </p>
        </div>
        <div className="flex justify-end pt-2">
          <button
            onClick={onClose}
            className="px-4 py-2 text-sm font-medium rounded-lg bg-gray-100 text-gray-700 hover:bg-gray-200"
          >
            {t('common.close')}
          </button>
        </div>
      </div>
    </div>
  )
}

function statusLabel(s: AssignedBookStudentProgressDto['status']) {
  if (s === 'Completed') return { key: 'status.completed', variant: 'green' as const }
  if (s === 'InProgress') return { key: 'status.reading', variant: 'blue' as const }
  return { key: 'status.notStartedReading', variant: 'gray' as const }
}

interface AssignedBookRowProps {
  ab: AssignedBookDto
  classId: string
  onRemove: (id: string) => void
  onPreview?: (ab: AssignedBookDto) => void
}

function AssignedBookRow({ ab, classId, onRemove, onPreview }: AssignedBookRowProps) {
  const { t } = useTranslation()
  const [expanded, setExpanded] = useState(false)
  const [showEdit, setShowEdit] = useState(false)
  const [editStart, setEditStart] = useState('')
  const [editEnd, setEditEnd] = useState('')
  const [editPoints, setEditPoints] = useState('')
  const qc = useQueryClient()
  const total = ab.notStartedCount + ab.inProgressCount + ab.completedCount

  const { data: students = [], isLoading } = useQuery({
    queryKey: ['book-student-progress', classId, ab.id],
    queryFn: () => booksApi.getStudentProgress(classId, ab.id),
    enabled: expanded,
  })

  const updateMutation = useMutation({
    mutationFn: () =>
      booksApi.updateAssigned(
        classId,
        ab.id,
        new Date(editStart).toISOString(),
        new Date(editEnd).toISOString(),
        Number(editPoints),
      ),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['assigned-books', classId] })
      setShowEdit(false)
    },
  })

  function openEdit() {
    setEditStart(ab.startDateUtc ? ab.startDateUtc.split('T')[0] : '')
    setEditEnd(ab.endDateUtc ? ab.endDateUtc.split('T')[0] : '')
    setEditPoints(String(ab.points ?? 0))
    setShowEdit(true)
  }

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
            <div>
              <div className="flex items-center gap-2">
                <p className="font-medium text-gray-900 group-hover:text-indigo-600 transition-colors">
                  {ab.title}
                </p>
                {ab.isExpired && <Badge variant="gray">{t('status.expired')}</Badge>}
              </div>
              <p className="text-sm text-gray-400">{ab.author} · {ab.totalPages} {t('common.pages')}{ab.points ? ` · ${t('teacher.classDashboard.pointsDisplay', { points: ab.points })}` : ''}</p>
              <p className="text-xs text-gray-400 mt-0.5">
                {formatDate(ab.startDateUtc)} – {formatDate(ab.endDateUtc)}
              </p>
            </div>
          </button>

          <div className="flex items-center gap-2 flex-wrap shrink-0">
            <Badge variant="gray">{t('teacher.classReading.notStartedBadge', { count: ab.notStartedCount, total })}</Badge>
            <Badge variant="blue">{t('teacher.classReading.readingBadge', { count: ab.inProgressCount, total })}</Badge>
            <Badge variant="green">{t('teacher.classReading.completedBadge', { count: ab.completedCount, total })}</Badge>

            <div className="relative group">
              <button
                onClick={() => onPreview?.(ab)}
                className="p-1 rounded text-gray-400 hover:text-emerald-600 hover:bg-emerald-50 transition-colors"
              >
                <EyeIcon />
              </button>
              <span className="pointer-events-none absolute left-1/2 -translate-x-1/2 -top-8 whitespace-nowrap rounded bg-gray-800 px-2 py-1 text-xs text-white opacity-0 group-hover:opacity-100 transition-opacity">
                {t('common.preview')}
              </span>
            </div>

            <div className="relative group">
              <button
                onClick={openEdit}
                className="p-1 rounded text-gray-400 hover:text-indigo-600 hover:bg-indigo-50 transition-colors"
              >
                <PencilIcon />
              </button>
              <span className="pointer-events-none absolute left-1/2 -translate-x-1/2 -top-8 whitespace-nowrap rounded bg-gray-800 px-2 py-1 text-xs text-white opacity-0 group-hover:opacity-100 transition-opacity">
                {t('common.edit')}
              </span>
            </div>

            {!ab.isExpired && (
              <Button variant="secondary" size="sm" onClick={() => onRemove(ab.id)}>
                {t('common.remove')}
              </Button>
            )}
          </div>
        </div>

        {showEdit && (
          <div className="mt-4 border-t border-gray-100 pt-4">
            <p className="text-sm font-medium text-gray-700 mb-3">{t('teacher.classReading.editParamsHeader')}</p>
            <div className="flex items-end gap-3 flex-wrap">
              <DateInput
                label={t('teacher.classReading.startDateLabel')}
                value={editStart}
                onChange={setEditStart}
                className="w-48"
              />
              <DateInput
                label={t('teacher.classReading.endDateLabel')}
                value={editEnd}
                onChange={setEditEnd}
                className="w-48"
              />
              <Input
                label={t('teacher.classReading.pointsShort')}
                type="number"
                value={editPoints}
                onChange={(e) => setEditPoints(e.target.value)}
                className="w-28"
              />
              <div className="flex gap-2 pb-0.5">
                <Button
                  size="sm"
                  loading={updateMutation.isPending}
                  disabled={!editStart || !editEnd}
                  onClick={() => updateMutation.mutate()}
                >
                  {t('common.save')}
                </Button>
                <Button size="sm" variant="secondary" onClick={() => setShowEdit(false)}>
                  {t('common.cancel')}
                </Button>
              </div>
            </div>
            {updateMutation.isError && (
              <p className="text-sm text-red-600 mt-2">
                {(updateMutation.error as { response?: { data?: { error?: string } } })?.response?.data?.error ?? t('common.error')}
              </p>
            )}
          </div>
        )}

        {expanded && (
          <div className="mt-4 border-t border-gray-100 pt-4">
            {isLoading ? (
              <div className="flex justify-center py-4">
                <Spinner className="text-indigo-600 h-5 w-5" />
              </div>
            ) : students.length === 0 ? (
              <p className="text-sm text-gray-400 text-center py-2">{t('teacher.classReading.noStudents')}</p>
            ) : (
              <div className="divide-y divide-gray-50">
                {students.map((s) => {
                  const pct = s.totalPages ? Math.round((s.currentPage / s.totalPages) * 100) : 0
                  const { key, variant } = statusLabel(s.status)
                  return (
                    <div key={s.studentId} className="py-3 flex items-center gap-4">
                      <div className="w-40 shrink-0">
                        <p className="text-sm font-medium text-gray-800">{s.studentName}</p>
                        <p className="text-xs text-gray-400">
                          {t('teacher.classReading.pageLabel')} {s.currentPage}{s.totalPages ? ` / ${s.totalPages}` : ''}
                        </p>
                      </div>
                      <div className="flex-1 min-w-0">
                        <div className="w-full bg-gray-100 rounded-full h-1.5">
                          <div
                            className="bg-indigo-500 h-1.5 rounded-full transition-all"
                            style={{ width: `${pct}%` }}
                          />
                        </div>
                        <p className="text-xs text-gray-400 mt-0.5">{pct}%</p>
                      </div>
                      <Badge variant={variant}>{t(key)}</Badge>
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

export function ClassReadingPage() {
  const { t } = useTranslation()
  const { classId } = useParams<{ classId: string }>()
  const qc = useQueryClient()
  const [showForm, setShowForm] = useState(false)
  const [bookId, setBookId] = useState('')
  const [startDate, setStartDate] = useState('')
  const [endDate, setEndDate] = useState('')
  const [points, setPoints] = useState('')
  const [formError, setFormError] = useState('')
  const [confirmRemoveId, setConfirmRemoveId] = useState<string | null>(null)
  const [previewBook, setPreviewBook] = useState<AssignedBookDto | null>(null)

  const { data: assigned = [], isLoading } = useQuery({
    queryKey: ['assigned-books', classId],
    queryFn: () => booksApi.getAssigned(classId!),
  })

  const { data: catalog = [] } = useQuery({
    queryKey: ['books'],
    queryFn: () => booksApi.getAll(),
    enabled: showForm,
  })

  const removeMutation = useMutation({
    mutationFn: (assignedBookId: string) => booksApi.removeAssigned(classId!, assignedBookId),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['assigned-books', classId] })
      setConfirmRemoveId(null)
    },
  })

  const assignMutation = useMutation({
    mutationFn: () =>
      booksApi.assignToClass(classId!, {
        bookId,
        startDateUtc: new Date(startDate).toISOString(),
        endDateUtc: new Date(endDate).toISOString(),
        points: Number(points),
      }),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['assigned-books', classId] })
      setShowForm(false)
      setBookId('')
      setStartDate('')
      setEndDate('')
      setPoints('')
      setFormError('')
    },
    onError: (err: unknown) => {
      const msg = (err as { response?: { data?: { message?: string } } })?.response?.data?.message
      setFormError(msg ?? t('teacher.classReading.errorAssign'))
    },
  })

  const active = assigned.filter((ab) => !ab.isExpired)
  const expired = assigned.filter((ab) => ab.isExpired)

  return (
    <div className="space-y-6">
      <div className="flex justify-end">
        <Button onClick={() => setShowForm(true)}>{t('teacher.classReading.assignButton')}</Button>
      </div>

      {showForm && (
        <Card>
          <CardHeader>
            <h2 className="font-semibold text-gray-800">{t('teacher.classReading.formHeader')}</h2>
          </CardHeader>
          <CardBody>
            <form onSubmit={(e) => { e.preventDefault(); assignMutation.mutate() }} className="space-y-4">
              <div className="flex flex-col gap-1">
                <label className="text-sm font-medium text-gray-700">{t('teacher.classReading.bookLabel')}</label>
                <select
                  value={bookId}
                  onChange={(e) => setBookId(e.target.value)}
                  required
                  className="block w-full rounded-lg border border-gray-300 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-indigo-500"
                >
                  <option value="">{t('teacher.classReading.bookPlaceholder')}</option>
                  {catalog.map((b) => (
                    <option key={b.id} value={b.id}>{b.title} — {b.author}</option>
                  ))}
                </select>
              </div>
              <div className="grid grid-cols-2 gap-4">
                <DateInput label={t('teacher.classReading.startDateLabel')} value={startDate} onChange={setStartDate} required />
                <DateInput label={t('teacher.classReading.endDateLabel')} value={endDate} onChange={setEndDate} required />
              </div>
              <Input
                label={t('teacher.classReading.pointsLabel')}
                type="number"
                value={points}
                onChange={(e) => setPoints(e.target.value)}
                required
                className="w-48"
              />
              {formError && <p className="text-sm text-red-600">{formError}</p>}
              <div className="flex gap-2">
                <Button type="submit" loading={assignMutation.isPending}>{t('common.assign')}</Button>
                <Button variant="secondary" type="button" onClick={() => { setShowForm(false); setFormError('') }}>{t('common.cancel')}</Button>
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
                <p className="text-gray-400 text-sm">{t('teacher.classReading.noAssignedBooks')}</p>
              </CardBody>
            </Card>
          )}

          {active.length > 0 && (
            <div className="space-y-3">
              {active.map((ab) => (
                <AssignedBookRow
                  key={ab.id}
                  ab={ab}
                  classId={classId!}
                  onRemove={setConfirmRemoveId}
                  onPreview={setPreviewBook}
                />
              ))}
            </div>
          )}

          {expired.length > 0 && (
            <div className="space-y-3">
              <h3 className="text-sm font-semibold text-gray-500 uppercase tracking-wide">
                {t('teacher.classReading.completedSection')}
              </h3>
              {expired.map((ab) => (
                <AssignedBookRow
                  key={ab.id}
                  ab={ab}
                  classId={classId!}
                  onRemove={setConfirmRemoveId}
                />
              ))}
            </div>
          )}
        </>
      )}

      {previewBook && (
        <BookPreviewModal ab={previewBook} onClose={() => setPreviewBook(null)} />
      )}

      {confirmRemoveId && (
        <ConfirmDialog
          message={t('teacher.classReading.confirmRemove')}
          loading={removeMutation.isPending}
          onConfirm={() => removeMutation.mutate(confirmRemoveId)}
          onCancel={() => setConfirmRemoveId(null)}
        />
      )}
    </div>
  )
}
