import { client } from './client'
import type { StudentDetailsDto, StudentDto } from '../types'

export const parentApi = {
  getMyStudents: () =>
    client.get<StudentDto[]>('/parent/students').then((r) => r.data),

  getStudent: (studentId: string) =>
    client.get<StudentDetailsDto>(`/parent/students/${studentId}`).then((r) => r.data),

  createStudent: (data: { firstName: string; lastName: string }) =>
    client.post<string>('/parent/students', data).then((r) => r.data),

  updateReadingProgress: (studentId: string, assignedBookId: string, currentPage: number) =>
    client.patch(`/parent/students/${studentId}/reading/${assignedBookId}`, { currentPage }),

  updateAssignmentProgress: (studentId: string, assignmentId: string, markCompleted: boolean) =>
    client.patch(`/parent/students/${studentId}/assignments/${assignmentId}`, { markCompleted }),

  startChallenge: (studentId: string, challengeId: string) =>
    client.patch(`/parent/students/${studentId}/challenges/${challengeId}/start`, {}),

  completeChallenge: (studentId: string, challengeId: string) =>
    client.patch(`/parent/students/${studentId}/challenges/${challengeId}/complete`, {}),

  deleteStudent: (studentId: string) =>
    client.delete(`/parent/students/${studentId}`),
}
