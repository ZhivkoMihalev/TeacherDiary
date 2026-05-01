import { client } from './client'
import type { NotificationDto } from '../types'

export const notificationsApi = {
  getAll: (page = 1, pageSize = 30) =>
    client.get<NotificationDto[]>('/notifications', { params: { page, pageSize } }).then(r => r.data),

  getUnreadCount: () =>
    client.get<number>('/notifications/unread-count').then(r => r.data),

  markAsRead: (id: string) =>
    client.put(`/notifications/${id}/read`),

  markAllAsRead: () =>
    client.put('/notifications/read-all'),
}
