import { useState } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { booksApi } from '../../api/books'
import { Card, CardBody, CardHeader } from '../../components/ui/Card'
import { Button } from '../../components/ui/Button'
import { Input } from '../../components/ui/Input'
import { Spinner } from '../../components/ui/Spinner'
import type { BookDto } from '../../types'

type EditState = { id: string; title: string; author: string; gradeLevel: string; totalPages: string }

function PencilIcon() {
  return (
    <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor" className="w-4 h-4">
      <path d="M2.695 14.763l-1.262 3.154a.5.5 0 00.65.65l3.155-1.262a4 4 0 001.343-.885L17.5 5.5a2.121 2.121 0 00-3-3L3.58 13.42a4 4 0 00-.885 1.343z" />
    </svg>
  )
}

export function BooksPage() {
  const qc = useQueryClient()

  const [showForm, setShowForm] = useState(false)
  const [form, setForm] = useState({ title: '', author: '', gradeLevel: '', totalPages: '' })

  const [editing, setEditing] = useState<EditState | null>(null)
  const [editError, setEditError] = useState<string | null>(null)

  const { data: books = [], isLoading } = useQuery({
    queryKey: ['books'],
    queryFn: () => booksApi.getAll(),
  })

  const createMutation = useMutation({
    mutationFn: () =>
      booksApi.create({
        title: form.title,
        author: form.author,
        gradeLevel: Number(form.gradeLevel),
        totalPages: Number(form.totalPages),
      }),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['books'] })
      setShowForm(false)
      setForm({ title: '', author: '', gradeLevel: '', totalPages: '' })
    },
  })

  const updateMutation = useMutation({
    mutationFn: (data: EditState) =>
      booksApi.update(data.id, {
        title: data.title,
        author: data.author,
        gradeLevel: Number(data.gradeLevel),
        totalPages: Number(data.totalPages),
      }),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['books'] })
      setEditing(null)
      setEditError(null)
    },
    onError: (err: unknown) => {
      setEditError(err instanceof Error ? err.message : 'Неочаквана грешка')
    },
  })

  function openEdit(book: BookDto) {
    setEditing({
      id: book.id,
      title: book.title,
      author: book.author,
      gradeLevel: String(book.gradeLevel),
      totalPages: String(book.totalPages),
    })
    setEditError(null)
    setShowForm(false)
  }

  function setEditField(field: keyof Omit<EditState, 'id'>) {
    return (e: React.ChangeEvent<HTMLInputElement>) =>
      setEditing((prev) => prev ? { ...prev, [field]: e.target.value } : prev)
  }

  function set(field: keyof typeof form) {
    return (e: React.ChangeEvent<HTMLInputElement>) =>
      setForm((prev) => ({ ...prev, [field]: e.target.value }))
  }

  return (
    <div className="p-8 max-w-4xl mx-auto">
      <div className="flex items-center justify-between mb-6">
        <div>
          <h1 className="text-2xl font-bold text-gray-900">Каталог с книги</h1>
          <p className="text-gray-500 text-sm mt-0.5">Всички книги, достъпни за задаване на класа</p>
        </div>
        <Button onClick={() => { setShowForm(true); setEditing(null) }}>+ Добави книга</Button>
      </div>

      {showForm && (
        <Card className="mb-6">
          <CardHeader>
            <h2 className="font-semibold text-gray-800">Добавяне на книга</h2>
          </CardHeader>
          <CardBody>
            <form onSubmit={(e) => { e.preventDefault(); createMutation.mutate() }} className="space-y-4">
              <div className="grid grid-cols-2 gap-4">
                <Input label="Заглавие" value={form.title} onChange={set('title')} required autoFocus />
                <Input label="Автор" value={form.author} onChange={set('author')} required />
                <Input label="Клас" type="number" value={form.gradeLevel} onChange={set('gradeLevel')} required />
                <Input label="Брой страници" type="number" value={form.totalPages} onChange={set('totalPages')} required />
              </div>
              <div className="flex gap-2">
                <Button type="submit" loading={createMutation.isPending}>Добави книга</Button>
                <Button variant="secondary" type="button" onClick={() => setShowForm(false)}>Отказ</Button>
              </div>
            </form>
          </CardBody>
        </Card>
      )}

      {editing && (
        <Card className="mb-6">
          <CardHeader>
            <h2 className="font-semibold text-gray-800">Промяна на книга</h2>
          </CardHeader>
          {editError && (
            <div className="px-4 pb-2">
              <p className="text-sm text-red-600 bg-red-50 border border-red-200 rounded-lg px-3 py-2">{editError}</p>
            </div>
          )}
          <CardBody>
            <form
              onSubmit={(e) => { e.preventDefault(); updateMutation.mutate(editing) }}
              className="space-y-4"
            >
              <div className="grid grid-cols-2 gap-4">
                <Input label="Заглавие" value={editing.title} onChange={setEditField('title')} required autoFocus />
                <Input label="Автор" value={editing.author} onChange={setEditField('author')} required />
                <Input label="Клас" type="number" value={editing.gradeLevel} onChange={setEditField('gradeLevel')} required />
                <Input label="Брой страници" type="number" value={editing.totalPages} onChange={setEditField('totalPages')} required />
              </div>
              <div className="flex gap-2">
                <Button type="submit" loading={updateMutation.isPending}>Запази</Button>
                <Button variant="secondary" type="button" onClick={() => setEditing(null)}>Отказ</Button>
              </div>
            </form>
          </CardBody>
        </Card>
      )}

      {isLoading ? (
        <div className="flex justify-center py-16">
          <Spinner className="text-indigo-600 h-8 w-8" />
        </div>
      ) : books.length === 0 ? (
        <Card>
          <CardBody className="text-center py-16">
            <p className="text-gray-400 text-sm">Няма книги в каталога.</p>
          </CardBody>
        </Card>
      ) : (
        <Card>
          <div className="divide-y divide-gray-100">
            {books.map((book) => (
              <div
                key={book.id}
                className={`flex items-center justify-between px-6 py-4 ${editing?.id === book.id ? 'bg-indigo-50' : ''}`}
              >
                <div>
                  <p className="font-medium text-gray-900">{book.title}</p>
                  <p className="text-sm text-gray-400">{book.author} · {book.gradeLevel}. клас · {book.totalPages} стр.</p>
                </div>
                <button
                  onClick={() => openEdit(book)}
                  title="Промени"
                  className="p-2 rounded-lg text-gray-400 hover:text-indigo-600 hover:bg-indigo-50 transition-colors"
                >
                  <PencilIcon />
                </button>
              </div>
            ))}
          </div>
        </Card>
      )}
    </div>
  )
}
