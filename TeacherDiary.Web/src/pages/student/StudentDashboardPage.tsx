import { useRef, useState } from 'react'
import { useTranslation } from 'react-i18next'
import type { TFunction } from 'i18next'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { studentApi } from '../../api/student'
import { Button } from '../../components/ui/Button'
import { Badge } from '../../components/ui/Badge'
import { Spinner } from '../../components/ui/Spinner'
import { CelebrationDialog } from '../../components/ui/CelebrationDialog'
import { AlertDialog } from '../../components/ui/AlertDialog'
import { MedalIcon } from '../../components/ui/MedalIcon'
import { GamificationHero } from '../../components/student/GamificationHero'
import type { ProgressStatus, StudentChallengeDto, ActivityCalendarDayDto } from '../../types'
import { formatDate } from '../../utils/formatDate'

// ─── helpers ────────────────────────────────────────────────────────────────

function statusVariant(s: ProgressStatus) {
  if (s === 'Completed') return 'green'
  if (s === 'InProgress') return 'blue'
  return 'gray'
}

function translateStatus(s: ProgressStatus, t: TFunction) {
  if (s === 'Completed') return t('status.completed')
  if (s === 'InProgress') return t('status.inProgress')
  return t('status.notStarted')
}

const FONT = "'Plus Jakarta Sans', sans-serif"

// ─── Streak Calendar ─────────────────────────────────────────────────────────

function calendarColor(day: ActivityCalendarDayDto, isToday: boolean): string {
  if (!day.hasActivity) return isToday ? 'rgba(124,58,237,0.08)' : 'rgba(200,200,210,0.25)'
  if (day.pointsEarned >= 50) return '#7c3aed'
  if (day.pointsEarned >= 20) return '#a78bfa'
  return '#c4b5fd'
}

function StreakCalendar({ days, currentStreak, bestStreak }: {
  days: ActivityCalendarDayDto[]
  currentStreak: number
  bestStreak: number
}) {
  const { t } = useTranslation()
  const [tooltip, setTooltip] = useState<{ day: ActivityCalendarDayDto; idx: number } | null>(null)
  const today = new Date().toISOString().slice(0, 10)

  return (
    <div style={{
      background: 'rgba(255,255,255,0.75)',
      backdropFilter: 'blur(20px)',
      WebkitBackdropFilter: 'blur(20px)',
      borderRadius: '16px',
      border: '1px solid rgba(124,58,237,0.1)',
      boxShadow: '0 2px 12px rgba(124,58,237,0.07)',
      padding: '18px 20px',
    }}>
      {/* Header row */}
      <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', marginBottom: '14px', flexWrap: 'wrap', gap: '8px' }}>
        <div style={{ display: 'flex', alignItems: 'center', gap: '8px' }}>
          <span style={{ fontSize: '0.9rem', fontWeight: 700, color: '#1e1b4b', fontFamily: FONT }}>
            {t('student.dashboard.activityStreak')}
          </span>
        </div>
        <div style={{ display: 'flex', gap: '14px' }}>
          <div style={{ textAlign: 'center' }}>
            <div style={{ fontSize: '1.25rem', fontWeight: 800, color: '#7c3aed', fontFamily: FONT, lineHeight: 1 }}>
              {currentStreak}
            </div>
            <div style={{ fontSize: '0.68rem', color: '#a78bfa', fontFamily: FONT, textTransform: 'uppercase', letterSpacing: '0.05em' }}>
              {t('student.dashboard.current')}
            </div>
          </div>
          <div style={{ width: '1px', background: 'rgba(124,58,237,0.15)' }} />
          <div style={{ textAlign: 'center' }}>
            <div style={{ fontSize: '1.25rem', fontWeight: 800, color: '#db2777', fontFamily: FONT, lineHeight: 1 }}>
              {bestStreak}
            </div>
            <div style={{ fontSize: '0.68rem', color: '#a78bfa', fontFamily: FONT, textTransform: 'uppercase', letterSpacing: '0.05em' }}>
              {t('student.dashboard.record')}
            </div>
          </div>
        </div>
      </div>

      {/* Calendar squares */}
      <div style={{ position: 'relative' }}>
        <div style={{ display: 'flex', gap: '4px', flexWrap: 'wrap' }}>
          {days.map((day, idx) => {
            const isToday = day.date === today
            return (
              <div
                key={day.date}
                onMouseEnter={(e) => { setTooltip({ day, idx }); e.currentTarget.style.transform = 'scale(1.25)' }}
                onMouseLeave={(e) => { setTooltip(null); e.currentTarget.style.transform = 'scale(1)' }}
                style={{
                  width: '20px',
                  height: '20px',
                  borderRadius: '4px',
                  background: calendarColor(day, isToday),
                  cursor: 'default',
                  outline: isToday ? '2px solid #7c3aed' : 'none',
                  outlineOffset: '1px',
                  transition: 'transform 0.1s',
                  flexShrink: 0,
                }}
              />
            )
          })}
        </div>

        {/* Tooltip */}
        {tooltip && (
          <div style={{
            position: 'absolute',
            bottom: '28px',
            left: `${Math.min(tooltip.idx * 24, 360)}px`,
            background: 'rgba(30,27,75,0.92)',
            backdropFilter: 'blur(8px)',
            borderRadius: '8px',
            padding: '7px 10px',
            zIndex: 10,
            pointerEvents: 'none',
            whiteSpace: 'nowrap',
            boxShadow: '0 4px 12px rgba(0,0,0,0.2)',
          }}>
            <div style={{ fontSize: '0.75rem', color: 'white', fontFamily: FONT, fontWeight: 600 }}>
              {new Date(tooltip.day.date + 'T00:00:00').toLocaleDateString('bg-BG', { day: 'numeric', month: 'short' })}
            </div>
            {tooltip.day.hasActivity ? (
              <div style={{ fontSize: '0.7rem', color: '#c4b5fd', fontFamily: FONT }}>
                {tooltip.day.pointsEarned > 0 && `+${tooltip.day.pointsEarned} т · `}
                {tooltip.day.pagesRead > 0 && `${tooltip.day.pagesRead} стр · `}
                {tooltip.day.activityCount} акт.
              </div>
            ) : (
              <div style={{ fontSize: '0.7rem', color: '#a78bfa', fontFamily: FONT }}>{t('student.dashboard.noActivity')}</div>
            )}
          </div>
        )}
      </div>

      {/* Legend */}
      <div style={{ display: 'flex', alignItems: 'center', gap: '6px', marginTop: '10px', flexWrap: 'wrap' }}>
        <span style={{ fontSize: '0.68rem', color: '#a78bfa', fontFamily: FONT }}>{t('student.dashboard.less')}</span>
        {['rgba(200,200,210,0.25)', '#c4b5fd', '#a78bfa', '#7c3aed'].map((c, i) => (
          <div key={i} style={{ width: '14px', height: '14px', borderRadius: '3px', background: c }} />
        ))}
        <span style={{ fontSize: '0.68rem', color: '#a78bfa', fontFamily: FONT }}>{t('student.dashboard.more')}</span>
      </div>
    </div>
  )
}

// ─── Leaderboard ─────────────────────────────────────────────────────────────

function Leaderboard({ items, myId }: { items: { studentId: string; studentName: string; points: number; topMedalCode?: string }[]; myId: string }) {
  const { t } = useTranslation()
  const medals = ['🥇', '🥈', '🥉']
  const myRank = items.findIndex(i => i.studentId === myId) + 1

  return (
    <div style={{
      background: 'rgba(255,255,255,0.75)',
      backdropFilter: 'blur(20px)',
      WebkitBackdropFilter: 'blur(20px)',
      borderRadius: '16px',
      border: '1px solid rgba(124,58,237,0.1)',
      boxShadow: '0 2px 12px rgba(124,58,237,0.07)',
      padding: '18px 0 6px',
    }}>
      <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', padding: '0 20px 12px', flexWrap: 'wrap', gap: '6px' }}>
        <div style={{ display: 'flex', alignItems: 'center', gap: '8px' }}>
          <span style={{ fontSize: '0.9rem', fontWeight: 700, color: '#1e1b4b', fontFamily: FONT }}>
            {t('student.dashboard.leaderboard')}
          </span>
        </div>
        {myRank > 0 && (
          <span style={{
            fontSize: '0.75rem',
            fontWeight: 600,
            color: '#7c3aed',
            background: 'rgba(124,58,237,0.08)',
            padding: '3px 10px',
            borderRadius: '99px',
            fontFamily: FONT,
          }}>
            {t('student.dashboard.yourRank', { rank: myRank })}
          </span>
        )}
      </div>

      {items.length === 0 ? (
        <div style={{ padding: '20px', textAlign: 'center', fontSize: '0.85rem', color: '#a78bfa', fontFamily: FONT }}>
          {t('student.dashboard.noLeaderboard')}
        </div>
      ) : (
        items.slice(0, 8).map((item, idx) => {
          const isMe = item.studentId === myId
          const maxPoints = items[0]?.points || 1
          const barPct = Math.max(4, Math.round((item.points / maxPoints) * 100))

          return (
            <div
              key={item.studentId}
              style={{
                display: 'flex',
                alignItems: 'center',
                gap: '12px',
                padding: '9px 20px',
                borderBottom: idx < Math.min(items.length, 8) - 1 ? '1px solid rgba(124,58,237,0.06)' : 'none',
                background: isMe ? 'rgba(124,58,237,0.05)' : 'transparent',
                borderLeft: isMe ? '3px solid #7c3aed' : '3px solid transparent',
              }}
            >
              {/* Rank */}
              <div style={{ width: '24px', textAlign: 'center', flexShrink: 0 }}>
                {idx < 3
                  ? <span style={{ fontSize: '1rem' }}>{medals[idx]}</span>
                  : <span style={{ fontSize: '0.8rem', fontWeight: 700, color: '#a78bfa', fontFamily: FONT }}>{idx + 1}</span>
                }
              </div>

              {/* Medal */}
              {item.topMedalCode && (
                <MedalIcon code={item.topMedalCode} size="sm" />
              )}

              {/* Name */}
              <div style={{ flex: 1, minWidth: 0 }}>
                <div style={{
                  fontSize: '0.845rem',
                  fontWeight: isMe ? 700 : 500,
                  color: isMe ? '#7c3aed' : '#1e1b4b',
                  fontFamily: FONT,
                  overflow: 'hidden',
                  textOverflow: 'ellipsis',
                  whiteSpace: 'nowrap',
                }}>
                  {item.studentName}{isMe ? t('student.dashboard.you') : ''}
                </div>

                {/* Progress bar */}
                <div style={{ marginTop: '3px', height: '4px', borderRadius: '2px', background: 'rgba(124,58,237,0.1)', overflow: 'hidden' }}>
                  <div style={{
                    height: '100%',
                    width: `${barPct}%`,
                    borderRadius: '2px',
                    background: idx === 0
                      ? 'linear-gradient(90deg, #f59e0b, #fbbf24)'
                      : isMe
                        ? 'linear-gradient(90deg, #7c3aed, #a78bfa)'
                        : 'rgba(124,58,237,0.35)',
                  }} />
                </div>
              </div>

              {/* Points */}
              <div style={{
                fontSize: '0.845rem',
                fontWeight: 700,
                color: idx === 0 ? '#d97706' : isMe ? '#7c3aed' : '#6b7280',
                fontFamily: FONT,
                flexShrink: 0,
              }}>
                {item.points} т
              </div>
            </div>
          )
        })
      )}
    </div>
  )
}

// ─── Main Page ───────────────────────────────────────────────────────────────

export function StudentDashboardPage() {
  const { t } = useTranslation()
  const qc = useQueryClient()
  const [readingPages, setReadingPages] = useState<Record<string, string>>({})
  const [celebration, setCelebration] = useState<{ studentName: string; bookTitle: string } | null>(null)
  const pendingCelebration = useRef<{ studentName: string; bookTitle: string } | null>(null)
  const [alertMessage, setAlertMessage] = useState('')

  const { data, isLoading } = useQuery({
    queryKey: ['student-me'],
    queryFn: studentApi.getMyDetails,
  })

  const { data: calendar = [] } = useQuery({
    queryKey: ['student-activity-calendar'],
    queryFn: () => studentApi.getActivityCalendar(30),
    enabled: !!data,
  })

  const { data: leaderboard = [] } = useQuery({
    queryKey: ['student-leaderboard'],
    queryFn: studentApi.getMyLeaderboard,
    enabled: !!data?.classId,
  })

  const readingMutation = useMutation({
    mutationFn: ({ assignedBookId, page }: { assignedBookId: string; page: number }) =>
      studentApi.updateReadingProgress(assignedBookId, page),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['student-me'] })
      qc.invalidateQueries({ queryKey: ['student-activity-calendar'] })
      qc.invalidateQueries({ queryKey: ['student-leaderboard'] })
      qc.invalidateQueries({ queryKey: ['student-gamification-summary'] })
      setReadingPages({})
      if (pendingCelebration.current) {
        setCelebration(pendingCelebration.current)
        pendingCelebration.current = null
      }
    },
    onError: (err: unknown) => {
      const msg = (err as { response?: { data?: { error?: string } } })?.response?.data?.error
      setAlertMessage(msg ?? t('student.dashboard.errorSave'))
    },
  })

  const startMutation = useMutation({
    mutationFn: (assignmentId: string) => studentApi.startAssignment(assignmentId),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['student-me'] }),
    onError: (err: unknown) => {
      const msg = (err as { response?: { data?: { error?: string } } })?.response?.data?.error
      setAlertMessage(msg ?? t('student.dashboard.errorSave'))
    },
  })

  const assignmentMutation = useMutation({
    mutationFn: (assignmentId: string) => studentApi.completeAssignment(assignmentId),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['student-me'] })
      qc.invalidateQueries({ queryKey: ['student-leaderboard'] })
      qc.invalidateQueries({ queryKey: ['student-gamification-summary'] })
    },
    onError: (err: unknown) => {
      const msg = (err as { response?: { data?: { error?: string } } })?.response?.data?.error
      setAlertMessage(msg ?? t('student.dashboard.errorSave'))
    },
  })

  const startChallengeMutation = useMutation({
    mutationFn: (challengeId: string) => studentApi.startChallenge(challengeId),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['student-me'] }),
    onError: (err: unknown) => {
      const msg = (err as { response?: { data?: { error?: string } } })?.response?.data?.error
      setAlertMessage(msg ?? t('student.dashboard.errorSave'))
    },
  })

  const completeChallengewMutation = useMutation({
    mutationFn: (challengeId: string) => studentApi.completeChallenge(challengeId),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['student-me'] })
      qc.invalidateQueries({ queryKey: ['student-leaderboard'] })
      qc.invalidateQueries({ queryKey: ['student-gamification-summary'] })
    },
    onError: (err: unknown) => {
      const msg = (err as { response?: { data?: { error?: string } } })?.response?.data?.error
      setAlertMessage(msg ?? t('student.dashboard.errorSave'))
    },
  })

  if (isLoading) return <div className="flex items-center justify-center h-64"><Spinner /></div>
  if (!data) return null

  const activeReading = data.reading.filter((r) => r.status !== 'Completed')
  const completedReading = data.reading.filter((r) => r.status === 'Completed')
  const activeAssignments = data.assignments.filter((a) => a.status !== 'Completed')
  const completedAssignments = data.assignments.filter((a) => a.status === 'Completed')
  const activeChallenges = data.challenges.filter((c) => !c.completed && !c.isExpired)
  const completedChallenges = data.challenges.filter((c) => c.completed)
  const expiredChallenges = data.challenges.filter((c) => !c.completed && c.isExpired)
  const hasTasks = data.reading.length > 0 || data.assignments.length > 0 || data.challenges.length > 0

  function challengeTargetLabel(c: StudentChallengeDto) {
    if (c.targetDescription) return `${c.targetDescription}${c.targetValue ? ` · ${c.currentValue} / ${c.targetValue}` : ''}`
    if (c.targetValue) return `${c.currentValue} / ${c.targetValue}`
    return null
  }

  // class rank from leaderboard
  const myRankIdx = leaderboard.findIndex(i => i.studentId === data.studentId)
  const myRank = myRankIdx >= 0 ? myRankIdx + 1 : null

  return (
    <div style={{ maxWidth: '820px', margin: '0 auto', padding: '20px 16px 48px' }}>

      {/* ── Gamification Hero (streak / точки / класиране / награди) ──────── */}
      <GamificationHero />

      {/* ── Hero ─────────────────────────────────────────────────────────── */}
      <div style={{
        borderRadius: '20px',
        overflow: 'hidden',
        marginBottom: '20px',
        background: 'linear-gradient(135deg, #3b0764 0%, #1e2640 50%, #0c4a6e 100%)',
        position: 'relative',
      }}>
        {/* Decorative glow */}
        <div style={{
          position: 'absolute', top: '-40px', right: '-40px',
          width: '200px', height: '200px',
          background: 'radial-gradient(circle, rgba(124,58,237,0.4) 0%, transparent 70%)',
          pointerEvents: 'none',
        }} />

        <div style={{ padding: '24px 24px 20px', position: 'relative' }}>
          <p style={{ margin: '0 0 2px', fontSize: '0.8rem', fontWeight: 700, color: '#a78bfa', fontFamily: FONT, letterSpacing: '0.04em', textTransform: 'uppercase' }}>
            {t('student.dashboard.welcome')}
          </p>
          <h1 style={{ margin: '0 0 18px', fontSize: '1.8rem', fontWeight: 900, color: 'white', fontFamily: "'Bricolage Grotesque', 'Plus Jakarta Sans', sans-serif", letterSpacing: '-0.02em' }}>
            {data.studentName}
          </h1>

          {/* Stat chips */}
          <div style={{ display: 'flex', gap: '10px', flexWrap: 'wrap' }}>
            {/* Points */}
            <div style={{ display: 'flex', alignItems: 'center', gap: '10px', background: 'rgba(255,255,255,0.1)', backdropFilter: 'blur(8px)', borderRadius: '14px', padding: '10px 14px', border: '1px solid rgba(255,255,255,0.1)' }}>
              <div style={{ width: '34px', height: '34px', borderRadius: '10px', background: 'linear-gradient(135deg, #f59e0b, #fbbf24)', display: 'flex', alignItems: 'center', justifyContent: 'center', fontSize: '1.15rem', flexShrink: 0 }}>⚡</div>
              <div>
                <div style={{ fontSize: '1.5rem', fontWeight: 900, color: 'white', fontFamily: FONT, lineHeight: 1 }}>{data.totalPoints}</div>
                <div style={{ fontSize: '0.7rem', color: '#a78bfa', fontFamily: FONT, fontWeight: 600 }}>{t('common.points')}</div>
              </div>
            </div>

            {/* Streak */}
            <div style={{ display: 'flex', alignItems: 'center', gap: '10px', background: 'rgba(255,255,255,0.1)', backdropFilter: 'blur(8px)', borderRadius: '14px', padding: '10px 14px', border: '1px solid rgba(255,255,255,0.1)' }}>
              <div style={{ width: '34px', height: '34px', borderRadius: '10px', background: 'linear-gradient(135deg, #ef4444, #f97316)', display: 'flex', alignItems: 'center', justifyContent: 'center', fontSize: '1.15rem', flexShrink: 0 }}>🔥</div>
              <div>
                <div style={{ fontSize: '1.5rem', fontWeight: 900, color: 'white', fontFamily: FONT, lineHeight: 1 }}>{data.currentStreak}</div>
                <div style={{ fontSize: '0.7rem', color: '#a78bfa', fontFamily: FONT, fontWeight: 600 }}>{t('common.daysStreak')}</div>
              </div>
            </div>

            {/* Rank */}
            {myRank !== null && (
              <div style={{ display: 'flex', alignItems: 'center', gap: '10px', background: 'rgba(255,255,255,0.1)', backdropFilter: 'blur(8px)', borderRadius: '14px', padding: '10px 14px', border: '1px solid rgba(255,255,255,0.1)' }}>
                <div style={{ width: '34px', height: '34px', borderRadius: '10px', background: 'linear-gradient(135deg, #7c3aed, #db2777)', display: 'flex', alignItems: 'center', justifyContent: 'center', fontSize: '1.15rem', flexShrink: 0 }}>🏆</div>
                <div>
                  <div style={{ fontSize: '1.5rem', fontWeight: 900, color: 'white', fontFamily: FONT, lineHeight: 1 }}>#{myRank}</div>
                  <div style={{ fontSize: '0.7rem', color: '#a78bfa', fontFamily: FONT, fontWeight: 600 }}>{t('common.inClass')}</div>
                </div>
              </div>
            )}

            {/* Pages read */}
            <div style={{ display: 'flex', alignItems: 'center', gap: '10px', background: 'rgba(255,255,255,0.1)', backdropFilter: 'blur(8px)', borderRadius: '14px', padding: '10px 14px', border: '1px solid rgba(255,255,255,0.1)' }}>
              <div style={{ width: '34px', height: '34px', borderRadius: '10px', background: 'linear-gradient(135deg, #059669, #34d399)', display: 'flex', alignItems: 'center', justifyContent: 'center', fontSize: '1.15rem', flexShrink: 0 }}>📖</div>
              <div>
                <div style={{ fontSize: '1.5rem', fontWeight: 900, color: 'white', fontFamily: FONT, lineHeight: 1 }}>{data.totalPagesRead}</div>
                <div style={{ fontSize: '0.7rem', color: '#a78bfa', fontFamily: FONT, fontWeight: 600 }}>{t('common.pagesRead')}</div>
              </div>
            </div>
          </div>
        </div>
      </div>

      {/* ── Streak Calendar ──────────────────────────────────────────────── */}
      {calendar.length > 0 && (
        <div style={{ marginBottom: '16px' }}>
          <StreakCalendar
            days={calendar}
            currentStreak={data.currentStreak}
            bestStreak={data.bestStreak}
          />
        </div>
      )}

      {/* ── Leaderboard ──────────────────────────────────────────────────── */}
      {leaderboard.length > 0 && (
        <div style={{ marginBottom: '16px' }}>
          <Leaderboard items={leaderboard} myId={data.studentId} />
        </div>
      )}

      {/* ── Active Tasks ─────────────────────────────────────────────────── */}
      {hasTasks && (
        <div style={{ marginBottom: '6px' }}>
          <div style={{ fontSize: '0.72rem', fontWeight: 700, textTransform: 'uppercase', letterSpacing: '0.08em', color: '#a78bfa', fontFamily: FONT, marginBottom: '10px', paddingLeft: '4px' }}>
            {t('student.dashboard.activeTasks')}
          </div>

          <div style={{ display: 'flex', flexDirection: 'column', gap: '12px' }}>

            {/* Reading */}
            {data.reading.length > 0 && (
              <div style={{ background: 'rgba(255,255,255,0.75)', backdropFilter: 'blur(20px)', WebkitBackdropFilter: 'blur(20px)', borderRadius: '14px', border: '1px solid rgba(124,58,237,0.1)', boxShadow: '0 2px 10px rgba(124,58,237,0.07)', overflow: 'hidden' }}>
                <div style={{ display: 'flex', alignItems: 'center', gap: '8px', padding: '12px 16px 10px', borderBottom: '1px solid rgba(124,58,237,0.08)' }}>
                  <span style={{ fontSize: '0.85rem', fontWeight: 700, color: '#1e1b4b', fontFamily: FONT }}>{t('student.dashboard.reading')}</span>
                </div>
                <div style={{ padding: '12px 16px', display: 'flex', flexDirection: 'column', gap: '12px' }}>
                  {activeReading.map((r) => {
                    const pagesInput = readingPages[r.assignedBookId] ?? ''
                    const isExpired = r.isExpired && r.status !== 'Completed'
                    const pct = r.totalPages ? Math.min(100, Math.round((r.currentPage / r.totalPages) * 100)) : null
                    return (
                      <div key={r.assignedBookId} style={{ display: 'flex', flexDirection: 'column', gap: '8px' }}>
                        <div style={{ display: 'flex', alignItems: 'flex-start', justifyContent: 'space-between', gap: '8px' }}>
                          <div style={{ minWidth: 0 }}>
                            <p style={{ margin: 0, fontSize: '0.87rem', fontWeight: 600, color: '#1e1b4b', fontFamily: FONT, overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap' }}>{r.bookTitle}</p>
                            <p style={{ margin: '2px 0 0', fontSize: '0.75rem', color: '#a78bfa', fontFamily: FONT }}>{t('student.dashboard.pagesProgress', { current: r.currentPage, total: r.totalPages ?? '?' })}</p>
                          </div>
                          <Badge variant={isExpired ? 'red' : statusVariant(r.status)}>
                            {isExpired ? t('status.overdue') : translateStatus(r.status, t)}
                          </Badge>
                        </div>
                        {pct !== null && (
                          <div>
                            <div style={{ width: '100%', height: '6px', borderRadius: '3px', background: 'rgba(124,58,237,0.1)', overflow: 'hidden' }}>
                              <div style={{ height: '100%', width: `${pct}%`, borderRadius: '3px', background: 'linear-gradient(90deg, #7c3aed, #a78bfa)', transition: 'width 0.3s' }} />
                            </div>
                            <p style={{ margin: '2px 0 0', fontSize: '0.7rem', color: '#a78bfa', fontFamily: FONT, textAlign: 'right' }}>{pct}%</p>
                          </div>
                        )}
                        {!isExpired && (
                          <div style={{ display: 'flex', gap: '8px' }}>
                            <input
                              type="number"
                              min={r.currentPage}
                              max={r.totalPages ?? undefined}
                              value={pagesInput}
                              onChange={(e) => setReadingPages((p) => ({ ...p, [r.assignedBookId]: e.target.value }))}
                              placeholder={t('student.dashboard.currentPagePlaceholder', { current: r.currentPage })}
                              style={{ flex: 1, border: '1px solid rgba(124,58,237,0.2)', borderRadius: '8px', padding: '7px 10px', fontSize: '0.82rem', outline: 'none', fontFamily: FONT, color: '#1e1b4b' }}
                            />
                            <Button
                              size="sm"
                              disabled={!pagesInput || readingMutation.isPending}
                              loading={readingMutation.isPending}
                              onClick={() => {
                                const page = parseInt(pagesInput)
                                if (isNaN(page)) return
                                if (r.totalPages && page >= r.totalPages)
                                  pendingCelebration.current = { studentName: data.studentName, bookTitle: r.bookTitle }
                                readingMutation.mutate({ assignedBookId: r.assignedBookId, page })
                              }}
                            >{t('common.save2')}</Button>
                          </div>
                        )}
                      </div>
                    )
                  })}
                  {completedReading.map((r) => (
                    <div key={r.assignedBookId} style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', gap: '8px', opacity: 0.55 }}>
                      <p style={{ margin: 0, fontSize: '0.87rem', color: '#1e1b4b', fontFamily: FONT, overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap' }}>{r.bookTitle}</p>
                      <Badge variant="green">{t('status.completed')}</Badge>
                    </div>
                  ))}
                </div>
              </div>
            )}

            {/* Assignments */}
            {data.assignments.length > 0 && (
              <div style={{ background: 'rgba(255,255,255,0.75)', backdropFilter: 'blur(20px)', WebkitBackdropFilter: 'blur(20px)', borderRadius: '14px', border: '1px solid rgba(124,58,237,0.1)', boxShadow: '0 2px 10px rgba(124,58,237,0.07)', overflow: 'hidden' }}>
                <div style={{ display: 'flex', alignItems: 'center', gap: '8px', padding: '12px 16px 10px', borderBottom: '1px solid rgba(124,58,237,0.08)' }}>
                  <span style={{ fontSize: '0.85rem', fontWeight: 700, color: '#1e1b4b', fontFamily: FONT }}>{t('student.dashboard.assignments')}</span>
                </div>
                <div style={{ padding: '8px 0' }}>
                  {activeAssignments.map((a) => {
                    const isExpired = a.isExpired && a.status !== 'Completed'
                    return (
                      <div key={a.assignmentId} style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', gap: '10px', padding: '9px 16px', borderBottom: '1px solid rgba(124,58,237,0.06)' }}>
                        <div style={{ minWidth: 0 }}>
                          <p style={{ margin: 0, fontSize: '0.87rem', fontWeight: 600, color: '#1e1b4b', fontFamily: FONT, overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap' }}>{a.title}</p>
                          <p style={{ margin: '1px 0 0', fontSize: '0.72rem', color: '#a78bfa', fontFamily: FONT }}>{a.subject}</p>
                        </div>
                        <div style={{ display: 'flex', alignItems: 'center', gap: '6px', flexShrink: 0 }}>
                          <Badge variant={isExpired ? 'red' : statusVariant(a.status)}>
                            {isExpired ? t('status.overdue') : translateStatus(a.status, t)}
                          </Badge>
                          {!isExpired && a.status === 'NotStarted' && (
                            <Button size="sm" variant="secondary" loading={startMutation.isPending} onClick={() => startMutation.mutate(a.assignmentId)}>{t('common.start')}</Button>
                          )}
                          {!isExpired && a.status === 'InProgress' && (
                            <Button size="sm" variant="success" loading={assignmentMutation.isPending} onClick={() => assignmentMutation.mutate(a.assignmentId)}>{t('common.complete')}</Button>
                          )}
                        </div>
                      </div>
                    )
                  })}
                  {completedAssignments.map((a) => (
                    <div key={a.assignmentId} style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', gap: '8px', padding: '9px 16px', opacity: 0.55 }}>
                      <p style={{ margin: 0, fontSize: '0.87rem', color: '#1e1b4b', fontFamily: FONT, overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap' }}>{a.title}</p>
                      <Badge variant="green">{t('status.completed')}</Badge>
                    </div>
                  ))}
                </div>
              </div>
            )}

            {/* Challenges */}
            {data.challenges.length > 0 && (
              <div style={{ background: 'rgba(255,255,255,0.75)', backdropFilter: 'blur(20px)', WebkitBackdropFilter: 'blur(20px)', borderRadius: '14px', border: '1px solid rgba(124,58,237,0.1)', boxShadow: '0 2px 10px rgba(124,58,237,0.07)', overflow: 'hidden' }}>
                <div style={{ display: 'flex', alignItems: 'center', gap: '8px', padding: '12px 16px 10px', borderBottom: '1px solid rgba(124,58,237,0.08)' }}>
                  <span style={{ fontSize: '0.85rem', fontWeight: 700, color: '#1e1b4b', fontFamily: FONT }}>{t('student.dashboard.challenges')}</span>
                </div>
                <div style={{ padding: '8px 0' }}>
                  {activeChallenges.map((c) => (
                    <div key={c.challengeId} style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', gap: '10px', padding: '9px 16px', borderBottom: '1px solid rgba(124,58,237,0.06)' }}>
                      <div style={{ minWidth: 0 }}>
                        <p style={{ margin: 0, fontSize: '0.87rem', fontWeight: 600, color: '#1e1b4b', fontFamily: FONT, overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap' }}>{c.title}</p>
                        {challengeTargetLabel(c) && <p style={{ margin: '1px 0 0', fontSize: '0.72rem', color: '#a78bfa', fontFamily: FONT }}>{challengeTargetLabel(c)}</p>}
                        <p style={{ margin: '1px 0 0', fontSize: '0.72rem', color: '#a78bfa', fontFamily: FONT }}>{t('common.dueDate')} {formatDate(c.endDate)}</p>
                      </div>
                      <div style={{ display: 'flex', alignItems: 'center', gap: '6px', flexShrink: 0 }}>
                        {!c.started && (
                          <>
                            <Badge variant="gray">{t('status.notStarted')}</Badge>
                            <Button size="sm" variant="secondary" loading={startChallengeMutation.isPending} onClick={() => startChallengeMutation.mutate(c.challengeId)}>{t('common.start')}</Button>
                          </>
                        )}
                        {c.started && (
                          <>
                            <Badge variant="blue">{t('status.inProgress')}</Badge>
                            <Button size="sm" variant="success" loading={completeChallengewMutation.isPending} onClick={() => completeChallengewMutation.mutate(c.challengeId)}>{t('common.complete')}</Button>
                          </>
                        )}
                      </div>
                    </div>
                  ))}
                  {expiredChallenges.map((c) => (
                    <div key={c.challengeId} style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', gap: '8px', padding: '9px 16px', opacity: 0.55 }}>
                      <div style={{ minWidth: 0 }}>
                        <p style={{ margin: 0, fontSize: '0.87rem', color: '#1e1b4b', fontFamily: FONT, overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap' }}>{c.title}</p>
                        {challengeTargetLabel(c) && <p style={{ margin: '1px 0 0', fontSize: '0.72rem', color: '#a78bfa', fontFamily: FONT }}>{challengeTargetLabel(c)}</p>}
                      </div>
                      <Badge variant="red">{t('status.overdue')}</Badge>
                    </div>
                  ))}
                  {completedChallenges.map((c) => (
                    <div key={c.challengeId} style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', gap: '8px', padding: '9px 16px', opacity: 0.55 }}>
                      <p style={{ margin: 0, fontSize: '0.87rem', color: '#1e1b4b', fontFamily: FONT, overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap' }}>{c.title}</p>
                      <Badge variant="green">{t('status.completed')}</Badge>
                    </div>
                  ))}
                </div>
              </div>
            )}
          </div>
        </div>
      )}

      {/* Empty state */}
      {!hasTasks && (
        <div style={{ textAlign: 'center', padding: '48px 24px', background: 'rgba(255,255,255,0.72)', backdropFilter: 'blur(20px)', borderRadius: '16px', border: '1px solid rgba(124,58,237,0.1)' }}>
          <div style={{ fontSize: '3rem', marginBottom: '12px' }}>🎒</div>
          <p style={{ margin: 0, fontSize: '1rem', fontWeight: 600, color: '#1e1b4b', fontFamily: FONT }}>{t('student.dashboard.noTasks')}</p>
          <p style={{ margin: '6px 0 0', fontSize: '0.85rem', color: '#a78bfa', fontFamily: FONT }}>{t('student.dashboard.noTasksHint')}</p>
        </div>
      )}

      {celebration && (
        <CelebrationDialog studentName={celebration.studentName} bookTitle={celebration.bookTitle} onClose={() => setCelebration(null)} />
      )}
      {alertMessage && (
        <AlertDialog message={alertMessage} onClose={() => setAlertMessage('')} />
      )}
    </div>
  )
}
