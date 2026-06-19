import { useState } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { useMutation } from '@tanstack/react-query'
import { useTranslation } from 'react-i18next'
import { authApi } from '../../api/auth'
import { useAuth } from '../../context/AuthContext'
import { Input } from '../../components/ui/Input'
import { LanguageSelector } from '../../components/LanguageSelector'
import heroImage from '../../assets/hero-roles.png'

type RoleKey = 'Teacher' | 'Student' | 'Parent'

const ROLE_CONFIG: {
  key: RoleKey
  registerPath: string
  accentBg: string
  accentText: string
  ring: string
}[] = [
  { key: 'Teacher', registerPath: '/register/teacher', accentBg: 'bg-indigo-600', accentText: 'text-indigo-600', ring: 'ring-indigo-400' },
  { key: 'Student', registerPath: '/register/student', accentBg: 'bg-emerald-600', accentText: 'text-emerald-600', ring: 'ring-emerald-400' },
  { key: 'Parent',  registerPath: '/register/parent',  accentBg: 'bg-amber-500',   accentText: 'text-amber-500',   ring: 'ring-amber-400'   },
]

export function LoginPage() {
  const { t } = useTranslation()
  const { login } = useAuth()
  const navigate = useNavigate()
  const [selectedRole, setSelectedRole] = useState<RoleKey | null>(null)
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [error, setError] = useState('')

  const ROLES = ROLE_CONFIG.map(r => ({
    ...r,
    label:    t(`auth.roles.${r.key}.label`),
    sublabel: t(`auth.roles.${r.key}.sublabel`),
  }))

  const role = ROLES.find(r => r.key === selectedRole)

  const mutation = useMutation({
    mutationFn: () => authApi.login({ email, password }),
    onSuccess: (data) => {
      if (selectedRole && data.role !== selectedRole) {
        setError(t('auth.wrongRole', { role: role?.label }))
        return
      }
      login(data)
      navigate(
        data.role === 'Teacher' ? '/teacher/classes'
          : data.role === 'Student' ? '/student/dashboard'
          : '/parent/students',
        { replace: true },
      )
    },
    onError: () => setError(t('auth.invalidCredentials')),
  })

  function handleRoleClick(key: RoleKey) {
    setSelectedRole(key)
    setEmail('')
    setPassword('')
    setError('')
  }

  function handleClose() {
    setSelectedRole(null)
    setEmail('')
    setPassword('')
    setError('')
  }

  return (
    <div className="min-h-screen flex flex-col items-center justify-center bg-gradient-to-b from-sky-50 to-indigo-50 px-4 py-10">

      {/* Language selector — top right */}
      <div style={{ position: 'fixed', top: '16px', right: '16px', width: '140px', zIndex: 50 }}>
        <LanguageSelector dropUp={false} />
      </div>

      {/* Brand */}
      <span className="text-4xl font-extrabold tracking-tight text-indigo-700 mb-2">
        {t('auth.brandTitle')}
      </span>
      <p className="text-gray-500 text-sm mb-8 text-center">
        {t('auth.brandSubtitle')}
      </p>

      {/* Prompt */}
      <h1 className="text-2xl sm:text-3xl font-bold text-gray-800 mb-6 text-center">
        {t('auth.greeting')}{' '}
        <span className="text-indigo-500">?</span>
      </h1>

      {/* Interactive image */}
      <div className="relative w-full max-w-3xl rounded-2xl overflow-hidden shadow-xl border border-white/60">
        <img
          src={heroImage}
          alt={t('auth.greeting')}
          className="w-full object-cover"
          draggable={false}
        />

        {/* Clickable overlay panels */}
        <div className="absolute inset-0 flex">
          {ROLES.map((r, idx) => (
            <button
              key={r.key}
              onClick={() => handleRoleClick(r.key)}
              className={[
                'flex-1 relative focus:outline-none',
                selectedRole === r.key
                  ? `ring-4 ring-inset ${r.ring} bg-white/15`
                  : 'hover:bg-white/10 transition-colors duration-200',
                idx === 0 ? 'rounded-l-2xl' : idx === 2 ? 'rounded-r-2xl' : '',
              ].join(' ')}
            >
              {selectedRole === r.key && (
                <span
                  className={`absolute top-3 left-1/2 -translate-x-1/2 text-xs font-bold text-white px-3 py-1 rounded-full shadow ${r.accentBg}`}
                >
                  {t('auth.selected')}
                </span>
              )}
            </button>
          ))}
        </div>
      </div>

      {/* Role label buttons */}
      <div className="w-full max-w-3xl grid grid-cols-3 mt-3 mb-8 gap-2">
        {ROLES.map(r => (
          <button
            key={r.key}
            onClick={() => handleRoleClick(r.key)}
            className={[
              'rounded-xl px-3 py-2.5 text-center border-2 focus:outline-none transition-all duration-200',
              selectedRole === r.key
                ? `border-transparent text-white shadow-md ${r.accentBg}`
                : 'border-gray-200 bg-white text-gray-700 hover:border-gray-300 hover:shadow-sm',
            ].join(' ')}
          >
            <p className="font-semibold text-sm">{r.label}</p>
            <p className={`text-xs mt-0.5 ${selectedRole === r.key ? 'text-white/80' : 'text-gray-400'}`}>
              {r.sublabel}
            </p>
          </button>
        ))}
      </div>

      {/* Login panel */}
      {selectedRole && role && (
        <div className="w-full max-w-sm">
          <div className="bg-white rounded-2xl border border-gray-200 shadow-lg p-7">
            <div className="flex items-center justify-between mb-5">
              <h2 className="text-lg font-bold text-gray-900">
                {t('auth.loginAs')}{' '}
                <span className={role.accentText}>{role.label}</span>
              </h2>
              <button
                onClick={handleClose}
                className="text-gray-400 hover:text-gray-600 transition-colors text-lg leading-none"
              >
                ✕
              </button>
            </div>

            <form
              onSubmit={(e) => { e.preventDefault(); setError(''); mutation.mutate() }}
              className="space-y-4"
            >
              <Input
                label={t('auth.email')}
                type="email"
                value={email}
                onChange={(e) => setEmail(e.target.value)}
                placeholder="you@example.com"
                required
                autoFocus
              />
              <Input
                label={t('auth.password')}
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

              <button
                type="submit"
                disabled={mutation.isPending}
                className={[
                  'w-full py-2.5 rounded-xl text-sm font-semibold text-white transition-opacity hover:opacity-90',
                  role.accentBg,
                  mutation.isPending ? 'opacity-60 cursor-not-allowed' : '',
                ].join(' ')}
              >
                {mutation.isPending ? t('auth.submitting') : t('auth.submit')}
              </button>
            </form>

            <div className="mt-5 pt-5 border-t border-gray-100 text-center text-sm text-gray-500">
              <p>{t('auth.noAccount')}</p>
              <Link
                to={role.registerPath}
                className={`font-semibold hover:underline mt-1 inline-block ${role.accentText}`}
              >
                {t('auth.registerLink')}
              </Link>
            </div>
          </div>
        </div>
      )}
    </div>
  )
}
