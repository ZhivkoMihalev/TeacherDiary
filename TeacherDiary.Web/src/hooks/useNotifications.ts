import { useState, useEffect, useCallback } from 'react'
import * as signalR from '@microsoft/signalr'
import { notificationsApi } from '../api/notifications'
import type { NotificationDto } from '../types'

export function useNotifications() {
  const [notifications, setNotifications] = useState<NotificationDto[]>([])
  const [unreadCount, setUnreadCount] = useState(0)
  const [loading, setLoading] = useState(true)

  // Initial load
  useEffect(() => {
    notificationsApi.getAll().then(data => {
      setNotifications(data)
      setUnreadCount(data.filter(n => !n.isRead).length)
      setLoading(false)
    }).catch(() => setLoading(false))
  }, [])

  // SignalR real-time connection
  useEffect(() => {
    const token = localStorage.getItem('token')
    if (!token) return

    const connection = new signalR.HubConnectionBuilder()
      .withUrl('/hubs/notifications', {
        accessTokenFactory: () => token,
      })
      .withAutomaticReconnect()
      .configureLogging(signalR.LogLevel.Warning)
      .build()

    connection.on('ReceiveNotification', (notification: NotificationDto) => {
      setNotifications(prev => [notification, ...prev])
      setUnreadCount(prev => prev + 1)
    })

    connection.start().catch(() => {})

    return () => { connection.stop() }
  }, [])

  const markAsRead = useCallback(async (id: string) => {
    await notificationsApi.markAsRead(id)
    setNotifications(prev =>
      prev.map(n => n.id === id ? { ...n, isRead: true } : n)
    )
    setUnreadCount(prev => Math.max(0, prev - 1))
  }, [])

  const markAllAsRead = useCallback(async () => {
    await notificationsApi.markAllAsRead()
    setNotifications(prev => prev.map(n => ({ ...n, isRead: true })))
    setUnreadCount(0)
  }, [])

  return { notifications, unreadCount, loading, markAsRead, markAllAsRead }
}
