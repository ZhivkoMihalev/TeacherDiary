import { client } from './client'
import type { AssignBookRequest, AssignedBookDto, AssignedBookStudentProgressDto, BookCreateRequest, BookDto, BookUpdateRequest } from '../types'

export const booksApi = {
  getAll: (gradeLevel?: number) =>
    client.get<BookDto[]>('/books', { params: gradeLevel ? { gradeLevel } : {} }).then((r) => r.data),

  create: (data: BookCreateRequest) =>
    client.post<{ bookId: string }>('/books', data).then((r) => r.data),

  assignToClass: (classId: string, data: AssignBookRequest) =>
    client.post<{ assignedBookId: string }>(`/reading/${classId}/assigned-books`, data).then((r) => r.data),

  getAssigned: (classId: string) =>
    client.get<AssignedBookDto[]>(`/reading/${classId}/books`).then((r) => r.data),

  removeAssigned: (classId: string, assignedBookId: string) =>
    client.delete(`/reading/${classId}/assigned-books/${assignedBookId}`),

  getStudentProgress: (classId: string, assignedBookId: string) =>
    client.get<AssignedBookStudentProgressDto[]>(
      `/reading/${classId}/assigned-books/${assignedBookId}/students`
    ).then((r) => r.data),

  updateAssigned: (classId: string, assignedBookId: string, startDateUtc: string, endDateUtc: string, points: number) =>
    client.patch(`/reading/${classId}/assigned-books/${assignedBookId}`, { startDateUtc, endDateUtc, points }),

  update: (bookId: string, data: BookUpdateRequest) =>
    client.patch(`/books/${bookId}`, data),
}
