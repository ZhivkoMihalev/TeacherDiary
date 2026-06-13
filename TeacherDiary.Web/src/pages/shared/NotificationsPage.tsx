import { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import { useNotifications } from '../../hooks/useNotifications'
import type { NotificationDto, NotificationType } from '../../types'
import { useLanguage } from '../../context/LanguageContext'

function getIcon(type: NotificationType): string {
  switch (type) {
    case 'AssignmentCreated':   return '📝'
    case 'AssignmentCompleted': return '✅'
    case 'AssignmentOverdue':   return '⏰'
    case 'BookAssigned':        return '📚'
    case 'BookCompleted':       return '📖'
    case 'BookOverdue':         return '⏰'
    case 'ChallengeCreated':    return '🏆'
    case 'ChallengeCompleted':  return '🎉'
    case 'BadgeEarned':         return '🏅'
    case 'StreakReminder':      return '🔥'
    case 'StreakBroken':        return '💔'
    case 'StudentJoinedClass':  return '👋'
    default:                    return '🔔'
  }
}

function getIconBg(type: NotificationType): string {
  switch (type) {
    case 'AssignmentCompleted':
    case 'BookCompleted':
    case 'ChallengeCompleted':  return 'rgba(16,185,129,0.12)'
    case 'AssignmentOverdue':
    case 'BookOverdue':
    case 'StreakBroken':        return 'rgba(239,68,68,0.12)'
    case 'BadgeEarned':
    case 'StreakReminder':      return 'rgba(245,158,11,0.12)'
    case 'StudentJoinedClass':  return 'rgba(59,130,246,0.12)'
    default:                    return 'rgba(124,58,237,0.12)'
  }
}

function groupByDate(
  items: NotificationDto[],
  todayLabel: string,
  yesterdayLabel: string,
  locale: string,
): { label: string; items: NotificationDto[] }[] {
  const todayMs  = new Date().setHours(0, 0, 0, 0)
  const yesterMs = todayMs - 86_400_000
  const order: string[] = []
  const map   = new Map<string, NotificationDto[]>()

  for (const n of items) {
    const dayMs = new Date(n.createdAt).setHours(0, 0, 0, 0)
    const label =
      dayMs === todayMs  ? todayLabel :
      dayMs === yesterMs ? yesterdayLabel :
      new Date(n.createdAt).toLocaleDateString(locale, { day: 'numeric', month: 'long', year: 'numeric' })

    if (!map.has(label)) { map.set(label, []); order.push(label) }
    map.get(label)!.push(n)
  }

  return order.map(label => ({ label, items: map.get(label)! }))
}

function useRelativeTime() {
  const { t, i18n } = useTranslation()
  const locale = i18n.language === 'bg' ? 'bg-BG' : 'en-GB'

  return (dateStr: string): string => {
    const diff = Date.now() - new Date(dateStr).getTime()
    const mins = Math.floor(diff / 60_000)
    if (mins < 1)  return t('notifications.justNow')
    if (mins < 60) return t('notifications.minutesAgo', { count: mins })
    const hours = Math.floor(mins / 60)
    if (hours < 24) return t('notifications.hoursAgo', { count: hours })
    const days = Math.floor(hours / 24)
    if (days < 7)  return t('notifications.daysAgo', { count: days })
    return new Date(dateStr).toLocaleDateString(locale, { day: 'numeric', month: 'short' })
  }
}

const FONT = "'Plus Jakarta Sans', sans-serif"

export function NotificationsPage() {
  const { notifications, unreadCount, loading, markAsRead, markAllAsRead } = useNotifications()
  const { translate } = useLanguage()
  const { t, i18n } = useTranslation()
  const navigate = useNavigate()
  const [filter, setFilter] = useState<'all' | 'unread'>('all')
  const relativeTime = useRelativeTime()
  const locale = i18n.language === 'bg' ? 'bg-BG' : 'en-GB'

  const filtered = filter === 'unread' ? notifications.filter(n => !n.isRead) : notifications
  const groups   = groupByDate(filtered, t('notifications.today'), t('notifications.yesterday'), locale)

  async function handleClick(n: NotificationDto) {
    if (!n.isRead) await markAsRead(n.id)
    if (n.navigationUrl) navigate(n.navigationUrl)
  }

  return (
    <div style={{ maxWidth: '680px', margin: '0 auto', padding: '24px 16px 48px' }}>

      {/* Page header */}
      <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', marginBottom: '22px', flexWrap: 'wrap', gap: '10px' }}>
        <div style={{ display: 'flex', alignItems: 'center', gap: '10px' }}>
          <h1 style={{ margin: 0, fontSize: '1.375rem', fontWeight: 700, fontFamily: "'Bricolage Grotesque', 'Plus Jakarta Sans', sans-serif", color: '#1e1b4b' }}>
            {t('notifications.title')}
          </h1>
          {unreadCount > 0 && (
            <span style={{
              background: 'linear-gradient(135deg, #7c3aed, #db2777)',
              color: 'white',
              fontSize: '0.72rem',
              fontWeight: 700,
              borderRadius: '99px',
              minWidth: '22px',
              height: '22px',
              display: 'inline-flex',
              alignItems: 'center',
              justifyContent: 'center',
              padding: '0 7px',
            }}>
              {unreadCount}
            </span>
          )}
        </div>
        {unreadCount > 0 && (
          <button
            onClick={() => markAllAsRead()}
            style={{
              fontSize: '0.8rem',
              color: '#7c3aed',
              background: 'none',
              border: 'none',
              cursor: 'pointer',
              fontFamily: FONT,
              fontWeight: 500,
              padding: '6px 12px',
              borderRadius: '8px',
              transition: 'background 0.15s',
            }}
            onMouseEnter={e => (e.currentTarget.style.background = 'rgba(124,58,237,0.08)')}
            onMouseLeave={e => (e.currentTarget.style.background = 'none')}
          >
            {t('notifications.markAllRead')}
          </button>
        )}
      </div>

      {/* Filter tabs */}
      <div style={{
        display: 'flex',
        gap: '4px',
        marginBottom: '22px',
        background: 'rgba(124,58,237,0.06)',
        borderRadius: '10px',
        padding: '3px',
      }}>
        {(['all', 'unread'] as const).map(f => (
          <button
            key={f}
            onClick={() => setFilter(f)}
            style={{
              flex: 1,
              padding: '7px 0',
              borderRadius: '8px',
              border: 'none',
              cursor: 'pointer',
              fontSize: '0.82rem',
              fontFamily: FONT,
              fontWeight: 600,
              transition: 'all 0.15s',
              background: filter === f ? 'white' : 'transparent',
              color:      filter === f ? '#7c3aed' : '#a78bfa',
              boxShadow:  filter === f ? '0 1px 4px rgba(124,58,237,0.12)' : 'none',
            }}
          >
            {f === 'all'
              ? t('notifications.all')
              : unreadCount > 0 ? t('notifications.unread', { count: unreadCount }) : t('notifications.unreadTab')}
          </button>
        ))}
      </div>

      {/* Body */}
      {loading ? (
        <div style={{ textAlign: 'center', padding: '64px 0', color: '#a78bfa', fontFamily: FONT, fontSize: '0.9rem' }}>
          {t('notifications.loading')}
        </div>
      ) : filtered.length === 0 ? (
        <div style={{
          textAlign: 'center',
          padding: '64px 24px',
          background: 'rgba(255,255,255,0.72)',
          backdropFilter: 'blur(20px)',
          WebkitBackdropFilter: 'blur(20px)',
          borderRadius: '16px',
          border: '1px solid rgba(124,58,237,0.1)',
          boxShadow: '0 2px 12px rgba(124,58,237,0.07)',
        }}>
          <div style={{ fontSize: '2.75rem', marginBottom: '14px' }}>🔔</div>
          <p style={{ margin: 0, fontSize: '1rem', fontWeight: 600, color: '#1e1b4b', fontFamily: FONT }}>
            {filter === 'unread' ? t('notifications.noUnread') : t('notifications.noNotifications')}
          </p>
          <p style={{ margin: '6px 0 0', fontSize: '0.85rem', color: '#a78bfa', fontFamily: FONT }}>
            {filter === 'unread' ? t('notifications.allRead') : t('notifications.noActivity')}
          </p>
        </div>
      ) : (
        <div style={{ display: 'flex', flexDirection: 'column', gap: '22px' }}>
          {groups.map(group => (
            <div key={group.label}>
              {/* Date label */}
              <div style={{
                fontSize: '0.72rem',
                fontWeight: 700,
                textTransform: 'uppercase',
                letterSpacing: '0.08em',
                color: '#a78bfa',
                fontFamily: FONT,
                marginBottom: '8px',
                paddingLeft: '4px',
              }}>
                {group.label}
              </div>

              {/* Card */}
              <div style={{
                background: 'rgba(255,255,255,0.75)',
                backdropFilter: 'blur(20px)',
                WebkitBackdropFilter: 'blur(20px)',
                borderRadius: '14px',
                border: '1px solid rgba(124,58,237,0.1)',
                boxShadow: '0 2px 12px rgba(124,58,237,0.08)',
                overflow: 'hidden',
              }}>
                {group.items.map((n, idx) => (
                  <button
                    key={n.id}
                    onClick={() => handleClick(n)}
                    style={{
                      width: '100%',
                      textAlign: 'left',
                      display: 'flex',
                      alignItems: 'flex-start',
                      gap: '13px',
                      padding: '14px 16px 14px 13px',
                      borderTop: 'none',
                      borderRight: 'none',
                      borderBottom: idx < group.items.length - 1 ? '1px solid rgba(124,58,237,0.07)' : 'none',
                      borderLeft: !n.isRead ? '3px solid #7c3aed' : '3px solid transparent',
                      background: !n.isRead ? 'rgba(124,58,237,0.03)' : 'transparent',
                      cursor: 'pointer',
                      transition: 'background 0.15s',
                    }}
                    onMouseEnter={e => (e.currentTarget.style.background = 'rgba(124,58,237,0.06)')}
                    onMouseLeave={e => (e.currentTarget.style.background = !n.isRead ? 'rgba(124,58,237,0.03)' : 'transparent')}
                  >
                    {/* Type icon */}
                    <div style={{
                      width: '38px',
                      height: '38px',
                      borderRadius: '10px',
                      flexShrink: 0,
                      display: 'flex',
                      alignItems: 'center',
                      justifyContent: 'center',
                      fontSize: '1.15rem',
                      background: getIconBg(n.type),
                    }}>
                      {getIcon(n.type)}
                    </div>

                    {/* Text */}
                    <div style={{ flex: 1, minWidth: 0 }}>
                      <p style={{
                        margin: 0,
                        fontSize: '0.875rem',
                        color: '#1e1b4b',
                        lineHeight: 1.45,
                        fontFamily: FONT,
                        fontWeight: n.isRead ? 400 : 500,
                      }}>
                        {translate(n.message)}
                      </p>
                      <p style={{ margin: '3px 0 0', fontSize: '0.75rem', color: '#a78bfa', fontFamily: FONT }}>
                        {relativeTime(n.createdAt)}
                      </p>
                    </div>

                    {/* Unread dot */}
                    {!n.isRead && (
                      <div style={{
                        width: '8px',
                        height: '8px',
                        borderRadius: '50%',
                        background: '#7c3aed',
                        flexShrink: 0,
                        marginTop: '6px',
                      }} />
                    )}
                  </button>
                ))}
              </div>
            </div>
          ))}
        </div>
      )}
    </div>
  )
}
