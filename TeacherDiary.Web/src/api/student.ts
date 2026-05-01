import { client } from './client'
import type { StudentBadgeDto, StudentDetailsDto } from '../types'

export const studentApi = {
  getMyDetails: () =>
    client.get<StudentDetailsDto>('/student/me').then((r) => r.data),

  updateReadingProgress: (assignedBookId: string, currentPage: number) =>
    client.patch(`/student/me/reading/${assignedBookId}`, { currentPage }).then((r) => r.data),

  startAssignment: (assignmentId: string) =>
    client.patch(`/student/me/assignments/${assignmentId}/start`, {}).then((r) => r.data),

  completeAssignment: (assignmentId: string) =>
    client.patch(`/student/me/assignments/${assignmentId}/complete`, {}).then((r) => r.data),

  startChallenge: (challengeId: string) =>
    client.patch(`/student/me/challenges/${challengeId}/start`, {}).then((r) => r.data),

  completeChallenge: (challengeId: string) =>
    client.patch(`/student/me/challenges/${challengeId}/complete`, {}).then((r) => r.data),

  getMyBadges: () =>
    client.get<StudentBadgeDto[]>('/student/me/badges').then((r) => r.data),
}