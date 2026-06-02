import { client } from './client'
import type { ClassAnalyticsDto } from '../types'

export const analyticsApi = {
  getClassAnalytics: (classId: string, days = 30) =>
    client
      .get<ClassAnalyticsDto>(`/classes/${classId}/analytics`, { params: { days } })
      .then((r) => r.data),
}
