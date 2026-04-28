import { client } from './client'
import type { DashboardDto, LeaderboardItemDto, StudentDetailsDto, StudentBadgeDto } from '../types'

export const dashboardApi = {
  getClassDashboard: (classId: string) =>
    client.get<DashboardDto>(`/classes/${classId}/dashboard`).then((r) => r.data),

  getLeaderboard: (classId: string) =>
    client.get<LeaderboardItemDto[]>(`/classes/${classId}/leaderboard`).then((r) => r.data),

  getStudentDetails: (studentId: string) =>
    client.get<StudentDetailsDto>(`/students/${studentId}/details`).then((r) => r.data),

  getStudentBadges: (studentId: string) =>
    client.get<StudentBadgeDto[]>(`/students/${studentId}/badges`).then((r) => r.data),
}
