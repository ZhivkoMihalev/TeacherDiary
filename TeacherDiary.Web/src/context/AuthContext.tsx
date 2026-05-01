import { createContext, useContext, useState, type ReactNode } from 'react'
import type { AuthResponse } from '../types'

interface AuthContextValue {
  user: AuthResponse | null
  login: (data: AuthResponse) => void
  logout: () => void
  isTeacher: boolean
  isParent: boolean
  isStudent: boolean
}

const AuthContext = createContext<AuthContextValue | null>(null)

function loadUser(): AuthResponse | null {
  try {
    const raw = localStorage.getItem('user')
    return raw ? JSON.parse(raw) : null
  } catch {
    return null
  }
}

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<AuthResponse | null>(loadUser)

  function login(data: AuthResponse) {
    localStorage.setItem('token', data.token)
    localStorage.setItem('user', JSON.stringify(data))
    setUser(data)
  }

  function logout() {
    localStorage.removeItem('token')
    localStorage.removeItem('user')
    setUser(null)
  }

  return (
    <AuthContext.Provider
      value={{
        user,
        login,
        logout,
        isTeacher: user?.role === 'Teacher',
        isParent: user?.role === 'Parent',
        isStudent: user?.role === 'Student',
      }}
    >
      {children}
    </AuthContext.Provider>
  )
}

export function useAuth() {
  const ctx = useContext(AuthContext)
  if (!ctx) throw new Error('useAuth must be used inside AuthProvider')
  return ctx
}
