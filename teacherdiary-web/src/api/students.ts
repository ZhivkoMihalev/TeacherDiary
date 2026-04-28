import { client } from './client'
import type { PagedResult, StudentActivityDto, StudentBadgeDto, StudentDetailsDto, StudentDto, StudentSearchDto } from '../types'

export const studentsApi = {
  getByClass: (classId: string) =>
    client.get<StudentDto[]>(`/classes/${classId}/students`).then((r) => r.data),

  getDetails: (studentId: string) =>
    client.get<StudentDetailsDto>(`/students/${studentId}/details`).then((r) => r.data),

  addToClass: (classId: string, studentId: string) =>
    client.post(`/classes/${classId}/students/${studentId}`),

  removeFromClass: (studentId: string) =>
    client.delete(`/students/${studentId}/class`),

  search: (name: string, page = 1, pageSize = 20) =>
    client.get<PagedResult<StudentSearchDto>>('/students/search', { params: { name, page, pageSize } }).then((r) => r.data),

  getBadges: (studentId: string) =>
    client.get<StudentBadgeDto[]>(`/students/${studentId}/badges`).then((r) => r.data),

  getActivity: (classId: string) =>
    client.get<StudentActivityDto[]>(`/classes/${classId}/students/activity`).then((r) => r.data),
}
