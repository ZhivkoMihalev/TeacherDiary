import { useState } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { useMutation } from '@tanstack/react-query'
import { authApi } from '../../api/auth'
import { useAuth } from '../../context/AuthContext'
import { Input } from '../../components/ui/Input'
import { Button } from '../../components/ui/Button'

export function LoginPage() {
  const { login } = useAuth()
  const navigate = useNavigate()
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [error, setError] = useState('')

  const mutation = useMutation({
    mutationFn: () => authApi.login({ email, password }),
    onSuccess: (data) => {
      login(data)
      navigate(data.role === 'Teacher' ? '/teacher/classes' : '/parent/students', { replace: true })
    },
    onError: () => setError('Невалиден имейл или парола.'),
  })

  function handleSubmit(e: React.FormEvent) {
    e.preventDefault()
    setError('')
    mutation.mutate()
  }

  return (
    <div className="min-h-screen flex items-center justify-center bg-gray-50 px-4">
      <div className="w-full max-w-sm">
        <div className="text-center mb-8">
          <h1 className="text-2xl font-bold text-gray-900">TeacherDiary</h1>
          <p className="text-gray-500 mt-1 text-sm">Влезте в профила си</p>
        </div>

        <div className="bg-white rounded-2xl border border-gray-200 shadow-sm p-8">
          <form onSubmit={handleSubmit} className="space-y-4">
            <Input
              label="Имейл"
              type="email"
              value={email}
              onChange={(e) => setEmail(e.target.value)}
              placeholder="you@example.com"
              required
              autoFocus
            />
            <Input
              label="Парола"
              type="password"
              value={password}
              onChange={(e) => setPassword(e.target.value)}
              placeholder="••••••••"
              required
            />

            {error && (
              <p className="text-sm text-red-600 bg-red-50 border border-red-200 rounded-lg px-3 py-2">
                {error}
              </p>
            )}

            <Button
              type="submit"
              className="w-full"
              loading={mutation.isPending}
            >
              Вход
            </Button>
          </form>

          <div className="mt-6 pt-6 border-t border-gray-100 text-center text-sm text-gray-500 space-y-1">
            <p>
              Учител?{' '}
              <Link to="/register/teacher" className="text-indigo-600 hover:underline font-medium">
                Регистрирайте се тук
              </Link>
            </p>
            <p>
              Родител?{' '}
              <Link to="/register/parent" className="text-indigo-600 hover:underline font-medium">
                Регистрирайте се тук
              </Link>
            </p>
          </div>
        </div>
      </div>
    </div>
  )
}
