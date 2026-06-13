import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { Link } from 'react-router-dom'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { parentApi } from '../../api/parent'
import { Card, CardBody } from '../../components/ui/Card'
import { Button } from '../../components/ui/Button'
import { Input } from '../../components/ui/Input'
import { Spinner } from '../../components/ui/Spinner'
import { ConfirmDialog } from '../../components/ui/ConfirmDialog'
import { MedalIcon } from '../../components/ui/MedalIcon'

export function MyStudentsPage() {
  const { t } = useTranslation()
  const qc = useQueryClient()
  const [showForm, setShowForm] = useState(false)
  const [firstName, setFirstName] = useState('')
  const [lastName, setLastName] = useState('')
  const [confirmDeleteId, setConfirmDeleteId] = useState<string | null>(null)
  const [deleteError, setDeleteError] = useState('')

  const { data: students = [], isLoading } = useQuery({
    queryKey: ['parent-students'],
    queryFn: parentApi.getMyStudents,
  })

  const createMutation = useMutation({
    mutationFn: () => parentApi.createStudent({ firstName, lastName }),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['parent-students'] })
      setShowForm(false)
      setFirstName('')
      setLastName('')
    },
  })

  const deleteMutation = useMutation({
    mutationFn: (studentId: string) => parentApi.deleteStudent(studentId),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['parent-students'] })
      setConfirmDeleteId(null)
      setDeleteError('')
    },
    onError: (err: unknown) => {
      const msg = (err as { response?: { data?: { error?: string } } })?.response?.data?.error
      setConfirmDeleteId(null)
      setDeleteError(msg ?? t('parent.myStudents.errorDelete'))
    },
  })

  function handleSubmit(e: React.FormEvent) {
    e.preventDefault()
    createMutation.mutate()
  }

  return (
    <div className="p-8 max-w-2xl mx-auto">
      <div className="flex items-center justify-between mb-6">
        <div>
          <h1 className="text-2xl font-bold text-gray-900">{t('parent.myStudents.title')}</h1>
          <p className="text-gray-500 text-sm mt-0.5">{t('parent.myStudents.subtitle')}</p>
        </div>
        <Button onClick={() => setShowForm(true)}>{t('parent.myStudents.addButton')}</Button>
      </div>

      {showForm && (
        <Card className="mb-6">
          <CardBody>
            <form onSubmit={handleSubmit} className="flex items-end gap-3">
              <Input
                label={t('parent.myStudents.formFirstName')}
                value={firstName}
                onChange={(e) => setFirstName(e.target.value)}
                required
                autoFocus
              />
              <Input
                label={t('parent.myStudents.formLastName')}
                value={lastName}
                onChange={(e) => setLastName(e.target.value)}
                required
              />
              <div className="flex gap-2 pb-px">
                <Button type="submit" loading={createMutation.isPending}>{t('parent.myStudents.submit')}</Button>
                <Button variant="secondary" type="button" onClick={() => setShowForm(false)}>{t('common.cancel')}</Button>
              </div>
            </form>
            <p className="text-xs text-gray-400 mt-3">
              {t('parent.myStudents.helpText')}
            </p>
          </CardBody>
        </Card>
      )}

      {isLoading ? (
        <div className="flex justify-center py-16">
          <Spinner className="text-emerald-600 h-8 w-8" />
        </div>
      ) : students.length === 0 ? (
        <Card>
          <CardBody className="text-center py-16">
            <div className="text-5xl mb-3">👶</div>
            <p className="font-semibold text-gray-700">{t('parent.myStudents.noChildren')}</p>
            <p className="text-sm text-gray-400 mt-1">{t('parent.myStudents.noChildrenHint')}</p>
          </CardBody>
        </Card>
      ) : (
        <div className="space-y-3">
          {students.map((s) => (
            <Card key={s.id} className="hover:shadow-md transition-shadow">
              <CardBody>
                <div className="flex items-center justify-between gap-4">
                  <div className="flex items-center gap-4">
                    <div className={`w-11 h-11 rounded-full flex items-center justify-center text-lg font-bold shrink-0 ${s.classId ? 'bg-emerald-100 text-emerald-700' : 'bg-amber-100 text-amber-700'}`}>
                      {s.firstName.charAt(0)}
                    </div>
                    <div>
                      <p className="font-semibold text-gray-900 flex items-center gap-1">
                        {s.firstName} {s.lastName}
                        {s.topMedalCode && <MedalIcon code={s.topMedalCode} size="sm" />}
                        {s.topPointsMedalCode && <MedalIcon code={s.topPointsMedalCode} size="sm" />}
                      </p>
                      <p className="text-sm mt-0.5">
                        {s.classId
                          ? <span className="text-emerald-600">{t('parent.myStudents.enrolled')}</span>
                          : <span className="text-amber-500">{t('parent.myStudents.waiting')}</span>
                        }
                      </p>
                    </div>
                  </div>
                  <div className="flex items-center gap-2 shrink-0">
                    <Link to={`/parent/students/${s.id}`}>
                      <Button size="sm" variant="secondary">{t('parent.myStudents.viewProgress')}</Button>
                    </Link>
                    <Button size="sm" variant="ghost" onClick={() => { setConfirmDeleteId(s.id); setDeleteError('') }}>
                      {t('common.remove')}
                    </Button>
                  </div>
                </div>
              </CardBody>
            </Card>
          ))}
        </div>
      )}

      {deleteError && (
        <p className="text-sm text-red-600 text-center mt-2">{deleteError}</p>
      )}

      {confirmDeleteId && (() => {
        const student = students.find((s) => s.id === confirmDeleteId)
        return student ? (
          <ConfirmDialog
            message={t('parent.myStudents.confirmDelete', { firstName: student.firstName, lastName: student.lastName })}
            loading={deleteMutation.isPending}
            onConfirm={() => deleteMutation.mutate(confirmDeleteId)}
            onCancel={() => { setConfirmDeleteId(null); setDeleteError('') }}
          />
        ) : null
      })()}
    </div>
  )
}
