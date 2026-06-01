import { useState, useRef, useEffect } from 'react'
import { NavLink, useNavigate } from 'react-router-dom'
import { useQueryClient } from '@tanstack/react-query'
import * as signalR from '@microsoft/signalr'
import { useNotifications, NOTIFICATIONS_QUERY_KEY } from '../hooks/useNotifications'
import { useAuth } from '../context/AuthContext'
import type { NotificationDto } from '../types'

function formatRelativeTime(dateStr: string): string {
  const diff = Date.now() - new Date(dateStr).getTime()
  const mins = Math.floor(diff / 60_000)
  if (mins < 1) return 'Сега'
  if (mins < 60) return `преди ${mins} мин`
  const hours = Math.floor(mins / 60)
  if (hours < 24) return `преди ${hours} ч`
  const days = Math.floor(hours / 24)
  return `преди ${days} дни`
}

export function NotificationBell() {
  const [open, setOpen] = useState(false)
  const ref = useRef<HTMLDivElement>(null)
  const navigate = useNavigate()
  const queryClient = useQueryClient()
  const { user } = useAuth()
  const { notifications, unreadCount, markAsRead, markAllAsRead } = useNotifications()

  const notificationsUrl =
    user?.role === 'Teacher' ? '/teacher/notifications' :
    user?.role === 'Parent'  ? '/parent/notifications'  :
                               '/student/notifications'

  // SignalR connection — lives here so there's exactly one per mounted layout
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

    connection.start().catch(() => {})
    return () => { connection.stop() }
  }, [queryClient])

  // Close dropdown when clicking outside
  useEffect(() => {
    function handleClick(e: MouseEvent) {
      if (ref.current && !ref.current.contains(e.target as Node))
        setOpen(false)
    }
    document.addEventListener('mousedown', handleClick)
    return () => document.removeEventListener('mousedown', handleClick)
  }, [])

  async function handleNotificationClick(id: string, url: string | null) {
    await markAsRead(id)
    setOpen(false)
    if (url) navigate(url)
  }

  return (
    <div ref={ref} className="relative">
      {/* Bell button */}
      <button
        onClick={() => setOpen(v => !v)}
        className="relative p-1.5 rounded-lg text-violet-400 hover:bg-violet-100 hover:text-violet-600 transition-colors"
        title="Известия"
      >
        <svg xmlns="http://www.w3.org/2000/svg" className="w-5 h-5" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
          <path d="M18 8A6 6 0 0 0 6 8c0 7-3 9-3 9h18s-3-2-3-9" />
          <path d="M13.73 21a2 2 0 0 1-3.46 0" />
        </svg>
        {unreadCount > 0 && (
          <span className="absolute -top-0.5 -right-0.5 bg-red-500 text-white text-[10px] font-bold rounded-full min-w-[16px] h-4 flex items-center justify-center px-1 leading-none">
            {unreadCount > 99 ? '99+' : unreadCount}
          </span>
        )}
      </button>

      {/* Dropdown */}
      {open && (
        <div
          className="absolute left-0 top-11 w-80 flex flex-col max-h-[480px] overflow-hidden"
          style={{
            background: 'rgba(255,255,255,0.97)',
            backdropFilter: 'blur(20px)',
            WebkitBackdropFilter: 'blur(20px)',
            border: '1px solid rgba(124,58,237,0.15)',
            borderRadius: '14px',
            boxShadow: '0 8px 40px rgba(124,58,237,0.18), 0 2px 12px rgba(0,0,0,0.08)',
            zIndex: 9999,
          }}
        >
          {/* Header */}
          <div
            className="flex items-center justify-between px-4 py-3 flex-shrink-0"
            style={{ borderBottom: '1px solid rgba(124,58,237,0.1)' }}
          >
            <span style={{ fontSize: '0.875rem', fontWeight: 600, color: '#1e1b4b', fontFamily: "'Plus Jakarta Sans', sans-serif" }}>
              Известия
            </span>
            {unreadCount > 0 && (
              <button
                onClick={() => markAllAsRead()}
                style={{
                  fontSize: '0.75rem',
                  color: '#7c3aed',
                  background: 'none',
                  border: 'none',
                  cursor: 'pointer',
                  fontFamily: "'Plus Jakarta Sans', sans-serif",
                  fontWeight: 500,
                }}
                onMouseEnter={e => (e.currentTarget.style.color = '#4c1d95')}
                onMouseLeave={e => (e.currentTarget.style.color = '#7c3aed')}
              >
                Маркирай всички
              </button>
            )}
          </div>

          {/* List */}
          <div className="overflow-y-auto flex-1">
            {notifications.length === 0 ? (
              <div className="px-4 py-8 text-center" style={{ fontSize: '0.875rem', color: '#a78bfa', fontFamily: "'Plus Jakarta Sans', sans-serif" }}>
                Няма известия
              </div>
            ) : (
              notifications.slice(0, 20).map(n => (
                <button
                  key={n.id}
                  onClick={() => handleNotificationClick(n.id, n.navigationUrl)}
                  className="w-full text-left flex gap-3 transition-colors"
                  style={{
                    padding: '12px 16px',
                    background: !n.isRead ? 'rgba(124,58,237,0.04)' : 'transparent',
                    cursor: 'pointer',
                    borderTop: 'none',
                    borderRight: 'none',
                    borderLeft: 'none',
                    borderBottom: '1px solid rgba(124,58,237,0.06)',
                  }}
                  onMouseEnter={e => (e.currentTarget.style.background = 'rgba(124,58,237,0.07)')}
                  onMouseLeave={e => (e.currentTarget.style.background = !n.isRead ? 'rgba(124,58,237,0.04)' : 'transparent')}
                >
                  <span
                    className="mt-1.5 flex-shrink-0 w-2 h-2 rounded-full"
                    style={{ background: !n.isRead ? '#7c3aed' : 'transparent', flexShrink: 0, marginTop: '6px' }}
                  />
                  <div className="min-w-0 flex-1">
                    <p style={{ margin: 0, fontSize: '0.8125rem', color: '#1e1b4b', lineHeight: 1.4, fontFamily: "'Plus Jakarta Sans', sans-serif" }}>
                      {n.message}
                    </p>
                    <p style={{ margin: '2px 0 0', fontSize: '0.72rem', color: '#a78bfa', fontFamily: "'Plus Jakarta Sans', sans-serif" }}>
                      {formatRelativeTime(n.createdAt)}
                    </p>
                  </div>
                </button>
              ))
            )}
          </div>

          {/* Footer — link to full page */}
          <div style={{ borderTop: '1px solid rgba(124,58,237,0.1)', padding: '8px 10px', flexShrink: 0 }}>
            <NavLink
              to={notificationsUrl}
              onClick={() => setOpen(false)}
              style={{
                display: 'block',
                textAlign: 'center',
                fontSize: '0.8rem',
                color: '#7c3aed',
                fontFamily: "'Plus Jakarta Sans', sans-serif",
                fontWeight: 500,
                textDecoration: 'none',
                padding: '5px',
                borderRadius: '7px',
                transition: 'background 0.15s',
              }}
              onMouseEnter={e => (e.currentTarget.style.background = 'rgba(124,58,237,0.08)')}
              onMouseLeave={e => (e.currentTarget.style.background = 'none')}
            >
              Виж всички известия →
            </NavLink>
          </div>
        </div>
      )}
    </div>
  )
}
