import { client } from './client'
import type { ActivityCalendarDayDto, LeaderboardItemDto, StudentBadgeDto, StudentDetailsDto } from '../types'

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

  getActivityCalendar: (days = 30) =>
    client.get<ActivityCalendarDayDto[]>('/student/me/activity-calendar', { params: { days } }).then((r) => r.data),

  getMyLeaderboard: () =>
    client.get<LeaderboardItemDto[]>('/student/me/leaderboard').then((r) => r.data),
}