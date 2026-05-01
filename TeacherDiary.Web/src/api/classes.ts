import { client } from './client'
import type { ClassCreateRequest, ClassDto } from '../types'

export const classesApi = {
  getMine: () =>
    client.get<ClassDto[]>('/classes').then((r) => r.data),

  create: (data: ClassCreateRequest) =>
    client.post<ClassDto>('/classes', data).then((r) => r.data),

  update: (classId: string, data: { name: string; grade: number; schoolYear: string }) =>
    client.patch(`/classes/${classId}`, data),

  delete: (classId: string) =>
    client.delete(`/classes/${classId}`),
}
