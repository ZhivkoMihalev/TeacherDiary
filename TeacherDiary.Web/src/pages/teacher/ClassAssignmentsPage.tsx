import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { useParams } from 'react-router-dom'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { assignmentsApi } from '../../api/assignments'
import { Card, CardBody, CardHeader } from '../../components/ui/Card'
import { Button } from '../../components/ui/Button'
import { Input } from '../../components/ui/Input'
import { DateInput } from '../../components/ui/DateInput'
import { Spinner } from '../../components/ui/Spinner'
import { Badge } from '../../components/ui/Badge'
import { ConfirmDialog } from '../../components/ui/ConfirmDialog'
import { AlertDialog } from '../../components/ui/AlertDialog'
import { formatDate } from '../../utils/formatDate'
import type { AssignmentDto, AssignmentStudentProgressDto } from '../../types'

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

function statusLabel(s: AssignmentStudentProgressDto['status']) {
  if (s === 'Completed') return { key: 'status.completed', variant: 'green' as const }
  if (s === 'InProgress') return { key: 'status.inProgress', variant: 'blue' as const }
  return { key: 'status.notStarted', variant: 'gray' as const }
}

type EditForm = { title: string; subject: string; description: string; dueDate: string; points: string }

interface AssignmentRowProps {
  a: AssignmentDto
  classId: string
  onEdit: (a: AssignmentDto) => void
  onPreview: (a: AssignmentDto) => void
}

function AssignmentRow({ a, classId, onEdit, onPreview }: AssignmentRowProps) {
  const { t } = useTranslation()
  const [expanded, setExpanded] = useState(false)
  const [showExtend, setShowExtend] = useState(false)
  const [extendDate, setExtendDate] = useState('')
  const qc = useQueryClient()

  const { data: students = [], isLoading } = useQuery({
    queryKey: ['assignment-student-progress', classId, a.id],
    queryFn: () => assignmentsApi.getStudentProgress(classId, a.id),
    enabled: expanded,
  })

  const extendMutation = useMutation({
    mutationFn: () => assignmentsApi.update(classId, a.id, {
      title: a.title,
      subject: a.subject,
      description: a.description,
      dueDate: new Date(extendDate).toISOString(),
      points: a.points,
    }),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['assignments', classId] })
      setShowExtend(false)
      setExtendDate('')
    },
  })

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
              <div className="flex items-center gap-2">
                <p className="font-medium text-gray-900 group-hover:text-indigo-600 transition-colors">
                  {a.title}
                </p>
                {a.isExpired && <Badge variant="gray">{t('status.expired')}</Badge>}
              </div>
              <p className="text-sm text-gray-400">
                {a.subject}{a.dueDate ? ` · ${t('teacher.classAssignments.dueShort', { date: formatDate(a.dueDate) })}` : ''}{a.points ? ` · ${t('teacher.classDashboard.pointsDisplay', { points: a.points })}` : ''}
              </p>
              {a.description && (
                <p className="text-sm text-gray-500 mt-0.5">{a.description}</p>
              )}
            </div>
          </button>

          <div className="flex items-center gap-2 shrink-0">
            <Badge variant={a.completedCount === a.totalStudents && a.totalStudents > 0 ? 'green' : 'gray'}>
              {t('teacher.classAssignments.completedBadge', { count: a.completedCount, total: a.totalStudents })}
            </Badge>
            {a.isExpired ? (
              <Button size="sm" variant="secondary" onClick={() => setShowExtend((v) => !v)}>
                {t('teacher.classAssignments.changeDeadline')}
              </Button>
            ) : (
              <>
                <div className="relative group">
                  <button
                    onClick={() => onEdit(a)}
                    className="p-1 rounded text-gray-400 hover:text-indigo-600 hover:bg-indigo-50 transition-colors"
                  >
                    <PencilIcon />
                  </button>
                  <span className="pointer-events-none absolute left-1/2 -translate-x-1/2 -top-8 whitespace-nowrap rounded bg-gray-800 px-2 py-1 text-xs text-white opacity-0 group-hover:opacity-100 transition-opacity">
                    {t('common.edit')}
                  </span>
                </div>
                <div className="relative group">
                  <button
                    onClick={() => onPreview(a)}
                    className="p-1 rounded text-gray-400 hover:text-emerald-600 hover:bg-emerald-50 transition-colors"
                  >
                    <EyeIcon />
                  </button>
                  <span className="pointer-events-none absolute left-1/2 -translate-x-1/2 -top-8 whitespace-nowrap rounded bg-gray-800 px-2 py-1 text-xs text-white opacity-0 group-hover:opacity-100 transition-opacity">
                    {t('common.preview')}
                  </span>
                </div>
              </>
            )}
          </div>
        </div>

        {showExtend && (
          <div className="mt-3 flex items-end gap-2 border-t border-gray-100 pt-3">
            <DateInput
              label={t('teacher.classAssignments.newDueDateLabel')}
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
              {t('common.save')}
            </Button>
            <Button size="sm" variant="secondary" onClick={() => setShowExtend(false)}>
              {t('common.cancel')}
            </Button>
          </div>
        )}

        {expanded && (
          <div className="mt-4 border-t border-gray-100 pt-4">
            {isLoading ? (
              <div className="flex justify-center py-4">
                <Spinner className="text-indigo-600 h-5 w-5" />
              </div>
            ) : students.length === 0 ? (
              <p className="text-sm text-gray-400 text-center py-2">{t('teacher.classAssignments.noStudents')}</p>
            ) : (
              <div className="divide-y divide-gray-50">
                {students.map((s) => {
                  const { key, variant } = statusLabel(s.status)
                  return (
                    <div key={s.studentId} className="py-3 flex items-center justify-between gap-4">
                      <p className="text-sm font-medium text-gray-800">{s.studentName}</p>
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

function PreviewModal({ a, onClose }: { a: AssignmentDto; onClose: () => void }) {
  const { t } = useTranslation()
  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center">
      <div className="absolute inset-0 bg-black/40" onClick={onClose} />
      <div className="relative bg-white rounded-xl shadow-lg p-6 max-w-md w-full mx-4 space-y-3">
        <h2 className="text-lg font-bold text-gray-900">{a.title}</h2>
        <div className="text-sm text-gray-600 space-y-1">
          <p><span className="font-medium text-gray-700">{t('teacher.classAssignments.previewSubject')}</span> {a.subject}</p>
          <p><span className="font-medium text-gray-700">{t('teacher.classAssignments.previewDeadline')}</span> {formatDate(a.dueDate)}</p>
          {a.points > 0 && (
            <p><span className="font-medium text-gray-700">{t('teacher.classAssignments.previewPoints')}</span> {t('teacher.classDashboard.pointsDisplay', { points: a.points })}</p>
          )}
          {a.description && (
            <p><span className="font-medium text-gray-700">{t('teacher.classAssignments.previewDescription')}</span> {a.description}</p>
          )}
          <p>
            <span className="font-medium text-gray-700">{t('teacher.classAssignments.previewCompletion')}</span>{' '}
            {t('teacher.classAssignments.previewStudents', { completed: a.completedCount, total: a.totalStudents })}
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

const emptyEdit: EditForm = { title: '', subject: '', description: '', dueDate: '', points: '' }

export function ClassAssignmentsPage() {
  const { t } = useTranslation()
  const { classId } = useParams<{ classId: string }>()
  const qc = useQueryClient()

  const [showForm, setShowForm] = useState(false)
  const [form, setForm] = useState({ title: '', subject: '', description: '', dueDate: '', points: '' })

  const [editingId, setEditingId] = useState<string | null>(null)
  const [editForm, setEditForm] = useState<EditForm>(emptyEdit)
  const [originalEdit, setOriginalEdit] = useState<EditForm>(emptyEdit)

  const [showSaveConfirm, setShowSaveConfirm] = useState(false)
  const [showCancelConfirm, setShowCancelConfirm] = useState(false)
  const [showSuccess, setShowSuccess] = useState(false)

  const [previewAssignment, setPreviewAssignment] = useState<AssignmentDto | null>(null)

  const hasChanges = JSON.stringify(editForm) !== JSON.stringify(originalEdit)

  const { data: assignments = [], isLoading } = useQuery({
    queryKey: ['assignments', classId],
    queryFn: () => assignmentsApi.getByClass(classId!),
  })

  const createMutation = useMutation({
    mutationFn: () => assignmentsApi.create(classId!, { ...form, points: Number(form.points) }),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['assignments', classId] })
      setShowForm(false)
      setForm({ title: '', subject: '', description: '', dueDate: '', points: '' })
    },
  })

  const updateMutation = useMutation({
    mutationFn: () => assignmentsApi.update(classId!, editingId!, { ...editForm, points: Number(editForm.points) }),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['assignments', classId] })
      setShowSaveConfirm(false)
      setShowSuccess(true)
    },
  })

  function set(field: keyof typeof form) {
    return (e: React.ChangeEvent<HTMLInputElement | HTMLTextAreaElement>) =>
      setForm((prev) => ({ ...prev, [field]: e.target.value }))
  }

  function setEdit(field: keyof EditForm) {
    return (e: React.ChangeEvent<HTMLInputElement | HTMLTextAreaElement>) =>
      setEditForm((prev) => ({ ...prev, [field]: e.target.value }))
  }

  function openEdit(a: AssignmentDto) {
    const initial: EditForm = {
      title: a.title,
      subject: a.subject,
      description: a.description ?? '',
      dueDate: a.dueDate ? a.dueDate.split('T')[0] : '',
      points: String(a.points ?? 0),
    }
    setEditingId(a.id)
    setEditForm(initial)
    setOriginalEdit(initial)
  }

  function handleCancelEdit() {
    if (hasChanges) {
      setShowCancelConfirm(true)
    } else {
      closeEdit()
    }
  }

  function closeEdit() {
    setEditingId(null)
    setShowCancelConfirm(false)
    setShowSuccess(false)
  }

  const active = assignments.filter((a) => !a.isExpired)
  const expired = assignments.filter((a) => a.isExpired)

  return (
    <div className="space-y-6">
      <div className="flex justify-end">
        <Button onClick={() => setShowForm(true)}>{t('teacher.classAssignments.newButton')}</Button>
      </div>

      {showForm && (
        <Card>
          <CardHeader>
            <h2 className="font-semibold text-gray-800">{t('teacher.classAssignments.createHeader')}</h2>
          </CardHeader>
          <CardBody>
            <form onSubmit={(e) => { e.preventDefault(); createMutation.mutate() }} className="space-y-4">
              <div className="grid grid-cols-2 gap-4">
                <Input label={t('teacher.classAssignments.titleLabel')} value={form.title} onChange={set('title')} required autoFocus />
                <Input label={t('teacher.classAssignments.subjectLabel')} value={form.subject} onChange={set('subject')} required />
              </div>
              <div className="flex flex-col gap-1">
                <label className="text-sm font-medium text-gray-700">{t('teacher.classAssignments.descriptionLabel')}</label>
                <textarea
                  value={form.description}
                  onChange={set('description')}
                  rows={3}
                  className="block w-full rounded-lg border border-gray-300 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-indigo-500 resize-none"
                />
              </div>
              <div className="flex items-end gap-4">
                <DateInput label={t('teacher.classAssignments.dueDateLabel')} value={form.dueDate} onChange={(v) => setForm((p) => ({ ...p, dueDate: v }))} required className="w-48" />
                <Input label={t('teacher.classAssignments.pointsLabel')} type="number" value={form.points} onChange={set('points')} required className="w-28" />
              </div>
              <div className="flex gap-2">
                <Button type="submit" loading={createMutation.isPending}>{t('common.create')}</Button>
                <Button variant="secondary" type="button" onClick={() => setShowForm(false)}>{t('common.cancel')}</Button>
              </div>
            </form>
          </CardBody>
        </Card>
      )}

      {editingId && (
        <Card>
          <CardHeader>
            <h2 className="font-semibold text-gray-800">{t('teacher.classAssignments.editHeader')}</h2>
          </CardHeader>
          <CardBody>
            <form onSubmit={(e) => { e.preventDefault(); if (hasChanges) setShowSaveConfirm(true); else closeEdit() }} className="space-y-4">
              <div className="grid grid-cols-2 gap-4">
                <Input label={t('teacher.classAssignments.titleLabel')} value={editForm.title} onChange={setEdit('title')} required autoFocus />
                <Input label={t('teacher.classAssignments.subjectLabel')} value={editForm.subject} onChange={setEdit('subject')} required />
              </div>
              <div className="flex flex-col gap-1">
                <label className="text-sm font-medium text-gray-700">{t('teacher.classAssignments.descriptionLabel')}</label>
                <textarea
                  value={editForm.description}
                  onChange={setEdit('description')}
                  rows={3}
                  className="block w-full rounded-lg border border-gray-300 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-indigo-500 resize-none"
                />
              </div>
              <div className="flex items-end gap-4">
                <DateInput label={t('teacher.classAssignments.dueDateLabel')} value={editForm.dueDate} onChange={(v) => setEditForm((p) => ({ ...p, dueDate: v }))} required className="w-48" />
                <Input label={t('teacher.classAssignments.pointsLabel')} type="number" value={editForm.points} onChange={setEdit('points')} required className="w-28" />
              </div>
              <div className="flex gap-2">
                <Button type="submit">{t('common.save')}</Button>
                <Button variant="secondary" type="button" onClick={handleCancelEdit}>{t('common.cancel')}</Button>
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
                <p className="text-gray-400 text-sm">{t('teacher.classAssignments.noAssignments')}</p>
              </CardBody>
            </Card>
          )}

          {active.length > 0 && (
            <div className="space-y-3">
              {active.map((a) => (
                <AssignmentRow
                  key={a.id}
                  a={a}
                  classId={classId!}
                  onEdit={openEdit}
                  onPreview={setPreviewAssignment}
                />
              ))}
            </div>
          )}

          {expired.length > 0 && (
            <div className="space-y-3">
              <h3 className="text-sm font-semibold text-gray-500 uppercase tracking-wide">
                {t('teacher.classAssignments.completedSection')}
              </h3>
              {expired.map((a) => (
                <AssignmentRow
                  key={a.id}
                  a={a}
                  classId={classId!}
                  onEdit={openEdit}
                  onPreview={setPreviewAssignment}
                />
              ))}
            </div>
          )}
        </>
      )}

      {showSaveConfirm && (
        <ConfirmDialog
          message={t('teacher.classAssignments.confirmSave')}
          loading={updateMutation.isPending}
          onConfirm={() => updateMutation.mutate()}
          onCancel={() => setShowSaveConfirm(false)}
        />
      )}

      {showCancelConfirm && (
        <ConfirmDialog
          message={t('teacher.classAssignments.confirmDiscard')}
          onConfirm={closeEdit}
          onCancel={() => setShowCancelConfirm(false)}
        />
      )}

      {showSuccess && (
        <AlertDialog
          message={t('teacher.classAssignments.savedSuccess')}
          onClose={closeEdit}
        />
      )}

      {previewAssignment && (
        <PreviewModal
          a={previewAssignment}
          onClose={() => setPreviewAssignment(null)}
        />
      )}
    </div>
  )
}
