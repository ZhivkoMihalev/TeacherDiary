import { client } from './client'
import type { AssignmentCreateRequest, AssignmentDto, AssignmentStudentProgressDto } from '../types'

export const assignmentsApi = {
  getByClass: (classId: string) =>
    client.get<AssignmentDto[]>(`/classes/${classId}/assignments`).then((r) => r.data),

  create: (classId: string, data: AssignmentCreateRequest) =>
    client.post<{ assignmentId: string }>(`/classes/${classId}/assignments`, data).then((r) => r.data),

  update: (classId: string, assignmentId: string, data: AssignmentCreateRequest) =>
    client.patch(`/classes/${classId}/assignments/${assignmentId}`, data),

  getStudentProgress: (classId: string, assignmentId: string) =>
    client.get<AssignmentStudentProgressDto[]>(`/classes/${classId}/assignments/${assignmentId}/students`).then((r) => r.data),
}
