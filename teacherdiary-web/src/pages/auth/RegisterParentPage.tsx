import { useState } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { useMutation } from '@tanstack/react-query'
import { authApi } from '../../api/auth'
import { useAuth } from '../../context/AuthContext'
import { Input } from '../../components/ui/Input'
import { Button } from '../../components/ui/Button'

export function RegisterParentPage() {
  const { login } = useAuth()
  const navigate = useNavigate()
  const [form, setForm] = useState({ firstName: '', lastName: '', email: '', password: '' })
  const [error, setError] = useState('')

  const mutation = useMutation({
    mutationFn: () => authApi.registerParent(form),
    onSuccess: (data) => {
      login(data)
      navigate('/parent/students', { replace: true })
    },
    onError: () => setError('Регистрацията е неуспешна. Имейлът може вече да се използва.'),
  })

  function handleSubmit(e: React.FormEvent) {
    e.preventDefault()
    setError('')
    mutation.mutate()
  }

  function set(field: keyof typeof form) {
    return (e: React.ChangeEvent<HTMLInputElement>) =>
      setForm((prev) => ({ ...prev, [field]: e.target.value }))
  }

  return (
    <div className="min-h-screen flex items-center justify-center bg-gray-50 px-4">
      <div className="w-full max-w-sm">
        <div className="text-center mb-8">
          <h1 className="text-2xl font-bold text-gray-900">TeacherDiary</h1>
          <p className="text-gray-500 mt-1 text-sm">Регистрация на родител</p>
        </div>

        <div className="bg-white rounded-2xl border border-gray-200 shadow-sm p-8">
          <form onSubmit={handleSubmit} className="space-y-4">
            <div className="grid grid-cols-2 gap-3">
              <Input label="Име" value={form.firstName} onChange={set('firstName')} required autoFocus />
              <Input label="Фамилия" value={form.lastName} onChange={set('lastName')} required />
            </div>
            <Input label="Имейл" type="email" value={form.email} onChange={set('email')} required />
            <Input label="Парола" type="password" value={form.password} onChange={set('password')} required />

            {error && (
              <p className="text-sm text-red-600 bg-red-50 border border-red-200 rounded-lg px-3 py-2">
                {error}
              </p>
            )}

            <Button type="submit" className="w-full" loading={mutation.isPending}>
              Създай профил
            </Button>
          </form>

          <p className="mt-6 pt-6 border-t border-gray-100 text-center text-sm text-gray-500">
            Вече имате профил?{' '}
            <Link to="/login" className="text-indigo-600 hover:underline font-medium">
              Вход
            </Link>
          </p>
        </div>
      </div>
    </div>
  )
}
