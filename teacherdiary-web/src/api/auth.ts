import { client } from './client'
import type { AuthResponse, LoginRequest, RegisterParentRequest, RegisterTeacherRequest } from '../types'

export const authApi = {
  registerTeacher: (data: RegisterTeacherRequest) =>
    client.post<AuthResponse>('/auth/register-teacher', data).then((r) => r.data),

  registerParent: (data: RegisterParentRequest) =>
    client.post<AuthResponse>('/auth/register-parent', data).then((r) => r.data),

  login: (data: LoginRequest) =>
    client.post<AuthResponse>('/auth/login', data).then((r) => r.data),
}
