import { useEffect, type ReactNode } from 'react'
import * as signalR from '@microsoft/signalr'
import { useQueryClient } from '@tanstack/react-query'
import { NOTIFICATIONS_QUERY_KEY } from '../hooks/useNotifications'
import type { NotificationDto } from '../types'

export function NotificationsSignalRProvider({ children }: { children: ReactNode }) {
  const queryClient = useQueryClient()

  useEffect(() => {
    const token = localStorage.getItem('token')
    if (!token) return

    const connection = new signalR.HubConnectionBuilder()
      .withUrl('/hubs/notifications', { accessTokenFactory: () => token })
      .withAutomaticReconnect()
      .configureLogging(signalR.LogLevel.Warning)
      .build()

    connection.on('ReceiveNotification', (notification: NotificationDto) => {
      queryClient.setQueryData(NOTIFICATIONS_QUERY_KEY, (old: NotificationDto[] = []) =>
        [notification, ...old]
      )
    })

    let cancelled = false
    connection.start().then(() => {
      if (cancelled) connection.stop().catch(() => {})
    }).catch(() => {})

    return () => {
      cancelled = true
      if (connection.state !== signalR.HubConnectionState.Disconnected &&
          connection.state !== signalR.HubConnectionState.Disconnecting) {
        connection.stop().catch(() => {})
      }
    }
  }, [queryClient])

  return <>{children}</>
}
