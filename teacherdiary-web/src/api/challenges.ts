import { client } from './client'
import type { ChallengeCreateRequest, ChallengeDto } from '../types'

export const challengesApi = {
  getByClass: (classId: string) =>
    client.get<ChallengeDto[]>(`/classes/${classId}/challenges`).then((r) => r.data),

  create: (classId: string, data: ChallengeCreateRequest) =>
    client.post<{ challengeId: string }>(`/classes/${classId}/challenges`, data).then((r) => r.data),

  extendDeadline: (classId: string, challengeId: string, endDate: string) =>
    client.patch(`/classes/${classId}/challenges/${challengeId}`, { endDate }),
}
