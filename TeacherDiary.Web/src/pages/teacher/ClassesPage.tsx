import { useState } from 'react'
import { Link } from 'react-router-dom'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { classesApi } from '../../api/classes'
import { Card, CardBody, CardHeader } from '../../components/ui/Card'
import { Button } from '../../components/ui/Button'
import { Input } from '../../components/ui/Input'
import { Spinner } from '../../components/ui/Spinner'
import { ConfirmDialog } from '../../components/ui/ConfirmDialog'
import type { ClassDto } from '../../types'

function currentSchoolYear() {
  const now = new Date()
  const y = now.getFullYear()
  return now.getMonth() >= 8 ? `${y}/${y + 1}` : `${y - 1}/${y}`
}

function generateSchoolYears() {
  const now = new Date()
  const baseYear = now.getMonth() >= 8 ? now.getFullYear() : now.getFullYear() - 1
  const years: string[] = []
  for (let i = 2; i >= -2; i--) {
    const y = baseYear - i
    years.push(`${y}/${y + 1}`)
  }
  return years
}

const schoolYearOptions = generateSchoolYears()

type EditState = { id: string; name: string; grade: string; schoolYear: string }

export function ClassesPage() {
  const qc = useQueryClient()

  const [showForm, setShowForm] = useState(false)
  const [name, setName] = useState('')
  const [grade, setGrade] = useState('')
  const [schoolYear, setSchoolYear] = useState(currentSchoolYear)

  const [editing, setEditing] = useState<EditState | null>(null)
  const [originalEdit, setOriginalEdit] = useState<EditState | null>(null)
  const [showSaveConfirm, setShowSaveConfirm] = useState(false)
  const [showCancelConfirm, setShowCancelConfirm] = useState(false)

  const [confirmDelete, setConfirmDelete] = useState<{ id: string; name: string } | null>(null)
  const [updateError, setUpdateError] = useState<string | null>(null)

  const hasChanges =
    editing !== null &&
    originalEdit !== null &&
    (editing.name !== originalEdit.name ||
      editing.grade !== originalEdit.grade ||
      editing.schoolYear !== originalEdit.schoolYear)

  const { data: classes = [], isLoading } = useQuery({
    queryKey: ['classes'],
    queryFn: classesApi.getMine,
  })

  const createMutation = useMutation({
    mutationFn: () => classesApi.create({ name, grade: Number(grade), schoolYear }),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['classes'] })
      setShowForm(false)
      setName('')
      setGrade('')
      setSchoolYear(currentSchoolYear)
    },
  })

  const updateMutation = useMutation({
    mutationFn: (data: EditState) =>
      classesApi.update(data.id, {
        name: data.name,
        grade: Number(data.grade),
        schoolYear: data.schoolYear,
      }),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['classes'] })
      setEditing(null)
      setOriginalEdit(null)
      setShowSaveConfirm(false)
      setUpdateError(null)
    },
    onError: (err: unknown) => {
      const msg = err instanceof Error ? err.message : 'Неочаквана грешка'
      setUpdateError(msg)
      setShowSaveConfirm(false)
    },
  })

  const deleteMutation = useMutation({
    mutationFn: (id: string) => classesApi.delete(id),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['classes'] })
      setConfirmDelete(null)
    },
  })

  function openEdit(cls: ClassDto) {
    const state = { id: cls.id, name: cls.name, grade: String(cls.grade), schoolYear: cls.schoolYear }
    setEditing(state)
    setOriginalEdit(state)
    setUpdateError(null)
    setShowForm(false)
  }

  function handleSave() {
    if (hasChanges) {
      setShowSaveConfirm(true)
    } else {
      setEditing(null)
      setOriginalEdit(null)
    }
  }

  function handleCancelEdit() {
    if (hasChanges) {
      setShowCancelConfirm(true)
    } else {
      setEditing(null)
      setOriginalEdit(null)
    }
  }

  function confirmCancel() {
    setEditing(null)
    setOriginalEdit(null)
    setShowCancelConfirm(false)
  }

  function setEditField(field: keyof Omit<EditState, 'id'>) {
    return (e: React.ChangeEvent<HTMLInputElement | HTMLSelectElement>) =>
      setEditing((prev) => prev ? { ...prev, [field]: e.target.value } : prev)
  }

  return (
    <div className="p-8 max-w-4xl mx-auto">
      <div className="flex items-center justify-between mb-6">
        <div>
          <h1 className="text-2xl font-bold text-gray-900">Моите класове</h1>
          <p className="text-gray-500 text-sm mt-0.5">Управлявайте класовете и учениците</p>
        </div>
        <Button onClick={() => { setShowForm(true); setEditing(null) }}>+ Нов клас</Button>
      </div>

      {/* Create form */}
      {showForm && (
        <Card className="mb-6">
          <CardHeader>
            <h2 className="font-semibold text-gray-800">Създаване на клас</h2>
          </CardHeader>
          <CardBody>
            <form onSubmit={(e) => { e.preventDefault(); createMutation.mutate() }} className="flex items-end gap-3 flex-wrap">
              <Input
                label="Наименование"
                value={name}
                onChange={(e) => setName(e.target.value)}
                placeholder="напр. 3А"
                required
                autoFocus
              />
              <Input
                label="Клас (ниво)"
                type="number"
                value={grade}
                onChange={(e) => setGrade(e.target.value)}
                placeholder="3"
                required
                className="w-28"
              />
              <div className="flex flex-col gap-1">
                <label className="text-sm font-medium text-gray-700">Випуск</label>
                <select
                  value={schoolYear}
                  onChange={(e) => setSchoolYear(e.target.value)}
                  required
                  className="block rounded-lg border border-gray-300 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-indigo-500 w-36"
                >
                  {schoolYearOptions.map((y) => (
                    <option key={y} value={y}>{y}</option>
                  ))}
                </select>
              </div>
              <div className="flex gap-2 pb-px">
                <Button type="submit" loading={createMutation.isPending}>Създай</Button>
                <Button variant="secondary" type="button" onClick={() => setShowForm(false)}>Отказ</Button>
              </div>
            </form>
          </CardBody>
        </Card>
      )}

      {/* Edit form */}
      {editing && (
        <Card className="mb-6">
          <CardHeader>
            <h2 className="font-semibold text-gray-800">Промяна на клас</h2>
          </CardHeader>
          {updateError && (
            <div className="px-4 pb-2">
              <p className="text-sm text-red-600 bg-red-50 border border-red-200 rounded-lg px-3 py-2">{updateError}</p>
            </div>
          )}
          <CardBody>
            <form onSubmit={(e) => { e.preventDefault(); handleSave() }} className="flex items-end gap-3 flex-wrap">
              <Input
                label="Наименование"
                value={editing.name}
                onChange={setEditField('name')}
                required
                autoFocus
              />
              <Input
                label="Клас (ниво)"
                type="number"
                value={editing.grade}
                onChange={setEditField('grade')}
                required
                className="w-28"
              />
              <div className="flex flex-col gap-1">
                <label className="text-sm font-medium text-gray-700">Випуск</label>
                <select
                  value={editing.schoolYear}
                  onChange={setEditField('schoolYear')}
                  required
                  className="block rounded-lg border border-gray-300 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-indigo-500 w-36"
                >
                  {schoolYearOptions.map((y) => (
                    <option key={y} value={y}>{y}</option>
                  ))}
                </select>
              </div>
              <div className="flex gap-2 pb-px">
                <Button type="submit" loading={updateMutation.isPending}>Запази</Button>
                <Button variant="secondary" type="button" onClick={handleCancelEdit}>Отказ</Button>
              </div>
            </form>
          </CardBody>
        </Card>
      )}

      {isLoading ? (
        <div className="flex justify-center py-16">
          <Spinner className="text-indigo-600 h-8 w-8" />
        </div>
      ) : classes.length === 0 ? (
        <Card>
          <CardBody className="text-center py-16">
            <div className="text-5xl mb-3">🏫</div>
            <p className="font-semibold text-gray-700">Нямате добавени класове</p>
            <p className="text-sm text-gray-400 mt-1">Създайте първия клас, за да започнете.</p>
          </CardBody>
        </Card>
      ) : (
        <div className="grid gap-4 sm:grid-cols-2">
          {classes.map((cls) => (
            <Card
              key={cls.id}
              className={`hover:shadow-md transition-shadow ${editing?.id === cls.id ? 'ring-2 ring-indigo-300' : ''}`}
            >
              <CardBody className="flex items-center justify-between gap-4">
                <div className="flex items-center gap-4">
                  <div className="w-12 h-12 rounded-xl bg-indigo-100 flex items-center justify-center shrink-0">
                    <span className="text-xl font-extrabold text-indigo-600">{cls.grade}</span>
                  </div>
                  <div>
                    <Link
                      to={`/teacher/classes/${cls.id}/dashboard`}
                      className="font-semibold text-gray-900 hover:text-indigo-600 transition-colors"
                    >
                      {cls.name}
                    </Link>
                    <p className="text-sm text-gray-400 mt-0.5">{cls.grade}. клас · {cls.schoolYear}</p>
                  </div>
                </div>
                <div className="flex items-center gap-2 shrink-0">
                  <Link to={`/teacher/classes/${cls.id}/dashboard`}>
                    <Button size="sm" variant="secondary">Отвори</Button>
                  </Link>
                  <Button size="sm" variant="secondary" onClick={() => openEdit(cls)}>
                    Промени
                  </Button>
                  <Button size="sm" variant="ghost" onClick={() => setConfirmDelete({ id: cls.id, name: cls.name })}>
                    Изтрий
                  </Button>
                </div>
              </CardBody>
            </Card>
          ))}
        </div>
      )}

      {showSaveConfirm && (
        <ConfirmDialog
          message="Сигурни ли сте, че желаете да направите тази промяна?"
          loading={updateMutation.isPending}
          onConfirm={() => editing && updateMutation.mutate(editing)}
          onCancel={() => setShowSaveConfirm(false)}
        />
      )}

      {showCancelConfirm && (
        <ConfirmDialog
          message="Сигурни ли сте, че желаете да откажете направените промени?"
          onConfirm={confirmCancel}
          onCancel={() => setShowCancelConfirm(false)}
        />
      )}

      {confirmDelete && (
        <ConfirmDialog
          message={`Сигурни ли сте, че желаете да изтриете клас ${confirmDelete.name}?`}
          loading={deleteMutation.isPending}
          onConfirm={() => deleteMutation.mutate(confirmDelete.id)}
          onCancel={() => setConfirmDelete(null)}
        />
      )}
    </div>
  )
}
