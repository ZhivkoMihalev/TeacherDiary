import { Navigate } from 'react-router-dom'
import { useAuth } from '../context/AuthContext'
import type { ReactNode } from 'react'

interface Props {
  children: ReactNode
  role: 'Teacher' | 'Parent' | 'Student'
}

export function ProtectedRoute({ children, role }: Props) {
  const { user } = useAuth()

  if (!user) return <Navigate to="/login" replace />
  if (user.role !== role) return <Navigate to="/login" replace />

  return <>{children}</>
}
