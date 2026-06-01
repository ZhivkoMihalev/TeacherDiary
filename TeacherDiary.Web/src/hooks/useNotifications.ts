import { useCallback } from 'react'
import { useQuery, useQueryClient } from '@tanstack/react-query'
import { notificationsApi } from '../api/notifications'
import type { NotificationDto } from '../types'

export const NOTIFICATIONS_QUERY_KEY = ['notifications']

export function useNotifications() {
  const queryClient = useQueryClient()

  const { data: notifications = [], isLoading: loading } = useQuery({
    queryKey: NOTIFICATIONS_QUERY_KEY,
    queryFn: () => notificationsApi.getAll(1, 50),
  })

  const unreadCount = notifications.filter(n => !n.isRead).length

  const markAsRead = useCallback(async (id: string) => {
    await notificationsApi.markAsRead(id)
    queryClient.setQueryData(NOTIFICATIONS_QUERY_KEY, (old: NotificationDto[] = []) =>
      old.map(n => n.id === id ? { ...n, isRead: true } : n)
    )
  }, [queryClient])

  const markAllAsRead = useCallback(async () => {
    await notificationsApi.markAllAsRead()
    queryClient.setQueryData(NOTIFICATIONS_QUERY_KEY, (old: NotificationDto[] = []) =>
      old.map(n => ({ ...n, isRead: true }))
    )
  }, [queryClient])

  return { notifications, unreadCount, loading, markAsRead, markAllAsRead }
}
