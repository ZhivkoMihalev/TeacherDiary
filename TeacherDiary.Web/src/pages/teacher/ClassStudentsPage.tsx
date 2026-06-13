import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { useParams, Link } from 'react-router-dom'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { studentsApi } from '../../api/students'
import { Card, CardBody, CardHeader } from '../../components/ui/Card'
import { Button } from '../../components/ui/Button'
import { Input } from '../../components/ui/Input'
import { Badge } from '../../components/ui/Badge'
import { Spinner } from '../../components/ui/Spinner'
import { ConfirmDialog } from '../../components/ui/ConfirmDialog'
import { MedalIcon } from '../../components/ui/MedalIcon'
import type { StudentSearchDto } from '../../types'

export function ClassStudentsPage() {
  const { t } = useTranslation()
  const { classId } = useParams<{ classId: string }>()
  const qc = useQueryClient()
  const [search, setSearch] = useState('')
  const [searchResults, setSearchResults] = useState<StudentSearchDto[]>([])
  const [searching, setSearching] = useState(false)
  const [removeId, setRemoveId] = useState<string | null>(null)

  const { data: students = [], isLoading } = useQuery({
    queryKey: ['students', classId],
    queryFn: () => studentsApi.getByClass(classId!),
  })

  const [addError, setAddError] = useState('')

  function invalidateClassQueries() {
    qc.invalidateQueries({ queryKey: ['students', classId] })
    qc.invalidateQueries({ queryKey: ['dashboard', classId] })
    qc.invalidateQueries({ queryKey: ['assigned-books', classId] })
    qc.invalidateQueries({ queryKey: ['assignments', classId] })
    qc.invalidateQueries({ queryKey: ['challenges', classId] })
  }

  const addMutation = useMutation({
    mutationFn: (studentId: string) => studentsApi.addToClass(classId!, studentId),
    onSuccess: () => {
      invalidateClassQueries()
      setSearchResults([])
      setSearch('')
      setAddError('')
    },
    onError: () => setAddError(t('teacher.classStudents.errorAdd')),
  })

  const removeMutation = useMutation({
    mutationFn: (studentId: string) => studentsApi.removeFromClass(studentId),
    onSuccess: () => {
      invalidateClassQueries()
      setRemoveId(null)
    },
  })

  async function handleSearch(e: React.FormEvent) {
    e.preventDefault()
    if (!search.trim()) return
    setSearching(true)
    try {
      const result = await studentsApi.search(search.trim())
      const enrolled = new Set(students.map((s) => s.id))
      setSearchResults(result.items.filter((s) => !enrolled.has(s.id)))
    } finally {
      setSearching(false)
    }
  }

  return (
    <div className="space-y-6">
      <Card>
        <CardHeader>
          <h2 className="font-semibold text-gray-800">{t('teacher.classStudents.enrollHeader')}</h2>
          <p className="text-xs text-gray-400 mt-0.5">{t('teacher.classStudents.enrollHint')}</p>
        </CardHeader>
        <CardBody>
          <form onSubmit={handleSearch} className="flex gap-3">
            <Input
              value={search}
              onChange={(e) => setSearch(e.target.value)}
              placeholder={t('teacher.classStudents.searchPlaceholder')}
              className="flex-1"
            />
            <Button type="submit" loading={searching} variant="secondary">{t('teacher.classStudents.searchButton')}</Button>
          </form>

          {searchResults.length > 0 && (
            <ul className="mt-4 divide-y divide-gray-100 rounded-lg border border-gray-200 overflow-hidden">
              {searchResults.map((s) => (
                <li key={s.id} className="flex items-center justify-between px-4 py-3">
                  <span className="text-sm font-medium text-gray-800">{s.firstName} {s.lastName}</span>
                  <Button
                    size="sm"
                    loading={addMutation.isPending}
                    onClick={() => addMutation.mutate(s.id)}
                  >
                    {t('teacher.classStudents.enrollButton')}
                  </Button>
                </li>
              ))}
            </ul>
          )}
          {searchResults.length === 0 && search && !searching && (
            <p className="text-sm text-gray-400 mt-3">{t('teacher.classStudents.noResults')}</p>
          )}
          {addError && <p className="text-sm text-red-600 mt-3">{addError}</p>}
        </CardBody>
      </Card>

      <Card>
        <CardHeader>
          <h2 className="font-semibold text-gray-800">{t('teacher.classStudents.enrolledHeader')}</h2>
        </CardHeader>
        {isLoading ? (
          <CardBody className="flex justify-center py-10">
            <Spinner className="text-indigo-600 h-6 w-6" />
          </CardBody>
        ) : students.length === 0 ? (
          <CardBody>
            <p className="text-sm text-gray-400 text-center py-8">{t('teacher.classStudents.noEnrolled')}</p>
          </CardBody>
        ) : (
          <div className="divide-y divide-gray-100">
            {students.map((s) => (
              <div key={s.id} className="flex items-center justify-between px-6 py-3">
                <div className="flex items-center gap-3">
                  <Link
                    to={`/teacher/students/${s.id}`}
                    className="text-sm font-medium text-gray-800 hover:text-indigo-600 transition-colors"
                  >
                    {s.firstName} {s.lastName}
                    {s.topMedalCode && <MedalIcon code={s.topMedalCode} size="sm" />}
                    {s.topPointsMedalCode && <MedalIcon code={s.topPointsMedalCode} size="sm" />}
                  </Link>
                  {!s.isActive && <Badge variant="gray">{t('teacher.classStudents.inactive')}</Badge>}
                </div>
                <Button size="sm" variant="ghost" onClick={() => setRemoveId(s.id)}>{t('common.remove')}</Button>
              </div>
            ))}
          </div>
        )}
      </Card>

      {removeId && (() => {
        const student = students.find((s) => s.id === removeId)
        return student ? (
          <ConfirmDialog
            message={t('teacher.classStudents.confirmRemove', { firstName: student.firstName, lastName: student.lastName })}
            loading={removeMutation.isPending}
            onConfirm={() => removeMutation.mutate(removeId)}
            onCancel={() => setRemoveId(null)}
          />
        ) : null
      })()}
    </div>
  )
}
