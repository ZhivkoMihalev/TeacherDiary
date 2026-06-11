import { useState } from 'react'
import { useParams } from 'react-router-dom'
import { useQuery } from '@tanstack/react-query'
import { analyticsApi } from '../../api/analytics'
import { dashboardApi } from '../../api/dashboard'
import { Badge } from '../../components/ui/Badge'
import { MedalIcon } from '../../components/ui/MedalIcon'
import { Spinner } from '../../components/ui/Spinner'
import type {
  DailyActivityDto,
  ProgressStatus,
  StudentChallengeDto,
  StudentEngagementDto,
  SubjectCompletionDto,
  ReadingStatsDto,
} from '../../types'
import { formatDate } from '../../utils/formatDate'
import { useLanguage } from '../../context/LanguageContext'

const FONT = "'Plus Jakarta Sans', sans-serif"

const CARD_STYLE: React.CSSProperties = {
  background: 'rgba(255,255,255,0.75)',
  backdropFilter: 'blur(20px)',
  WebkitBackdropFilter: 'blur(20px)',
  borderRadius: '14px',
  border: '1px solid rgba(124,58,237,0.1)',
  boxShadow: '0 2px 12px rgba(124,58,237,0.07)',
  padding: '20px 24px',
  fontFamily: FONT,
}

const CARD_HEADER_STYLE: React.CSSProperties = {
  fontSize: '15px',
  fontWeight: 700,
  color: '#1e1b4b',
  marginBottom: '16px',
  fontFamily: FONT,
}

// ── Helpers ──────────────────────────────────────────────────────────────────

function relativeTime(dateStr: string | null): string {
  if (!dateStr) return 'Никога'
  const diff = Date.now() - new Date(dateStr).getTime()
  const mins = Math.floor(diff / 60_000)
  if (mins < 1) return 'Сега'
  if (mins < 60) return `преди ${mins} мин`
  const hours = Math.floor(mins / 60)
  if (hours < 24) return `преди ${hours} ч`
  return `преди ${Math.floor(hours / 24)} дни`
}

function formatDateShort(dateStr: string): string {
  const d = new Date(dateStr + 'T00:00:00')
  return d.toLocaleDateString('bg-BG', { day: 'numeric', month: 'short' })
}

function statusVariant(s: ProgressStatus) {
  if (s === 'Completed') return 'green'
  if (s === 'InProgress') return 'blue'
  return 'gray'
}

function translateStatus(s: ProgressStatus) {
  if (s === 'Completed') return 'Завършено'
  if (s === 'InProgress') return 'В процес'
  return 'Не е стартирано'
}

// ── Student Detail Drawer ─────────────────────────────────────────────────────

function StudentDetailDrawer({
  studentId,
  onClose,
}: {
  studentId: string
  onClose: () => void
}) {
  const { translate } = useLanguage()
  const { data, isLoading } = useQuery({
    queryKey: ['teacher-student', studentId],
    queryFn: () => dashboardApi.getStudentDetails(studentId),
  })

  const { data: badges = [] } = useQuery({
    queryKey: ['teacher-student-badges', studentId],
    queryFn: () => dashboardApi.getStudentBadges(studentId),
  })

  function challengeTargetLabel(c: StudentChallengeDto) {
    if (c.targetDescription)
      return `${c.targetDescription}${c.targetValue ? ` · ${c.currentValue} / ${c.targetValue}` : ''}`
    if (c.targetValue) return `${c.currentValue} / ${c.targetValue}`
    return null
  }

  return (
    <>
      {/* Backdrop */}
      <div
        onClick={onClose}
        style={{
          position: 'fixed',
          inset: 0,
          background: 'rgba(30,27,75,0.25)',
          backdropFilter: 'blur(2px)',
          WebkitBackdropFilter: 'blur(2px)',
          zIndex: 900,
        }}
      />

      {/* Drawer panel */}
      <div
        style={{
          position: 'fixed',
          top: 0,
          right: 0,
          bottom: 0,
          width: 'min(480px, 100vw)',
          background: 'rgba(255,255,255,0.97)',
          backdropFilter: 'blur(24px)',
          WebkitBackdropFilter: 'blur(24px)',
          borderLeft: '1px solid rgba(124,58,237,0.12)',
          boxShadow: '-8px 0 40px rgba(124,58,237,0.12)',
          zIndex: 901,
          display: 'flex',
          flexDirection: 'column',
          overflowY: 'hidden',
          fontFamily: FONT,
        }}
      >
        {/* Header */}
        <div
          style={{
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'space-between',
            padding: '20px 20px 16px',
            borderBottom: '1px solid rgba(124,58,237,0.08)',
            flexShrink: 0,
          }}
        >
          <div style={{ display: 'flex', alignItems: 'center', gap: '8px', flexWrap: 'wrap' }}>
            {isLoading || !data ? (
              <span style={{ fontSize: '1rem', fontWeight: 700, color: '#1e1b4b' }}>Зареждане…</span>
            ) : (
              <>
                <span style={{ fontSize: '1.05rem', fontWeight: 700, color: '#1e1b4b' }}>
                  {data.studentName}
                </span>
                {data.topMedalCode && <MedalIcon code={data.topMedalCode} size="sm" />}
                {data.topPointsMedalCode && <MedalIcon code={data.topPointsMedalCode} size="sm" />}
              </>
            )}
          </div>
          <button
            onClick={onClose}
            style={{
              width: '30px',
              height: '30px',
              borderRadius: '8px',
              border: 'none',
              background: 'rgba(124,58,237,0.07)',
              cursor: 'pointer',
              display: 'flex',
              alignItems: 'center',
              justifyContent: 'center',
              color: '#7c3aed',
              fontSize: '1rem',
              flexShrink: 0,
              transition: 'background 0.15s',
            }}
            onMouseEnter={e => (e.currentTarget.style.background = 'rgba(124,58,237,0.14)')}
            onMouseLeave={e => (e.currentTarget.style.background = 'rgba(124,58,237,0.07)')}
            aria-label="Затвори"
          >
            ✕
          </button>
        </div>

        {/* Scrollable body */}
        <div style={{ flex: 1, overflowY: 'auto', padding: '16px 20px 32px' }}>
          {isLoading && (
            <div style={{ display: 'flex', justifyContent: 'center', padding: '48px 0' }}>
              <Spinner />
            </div>
          )}

          {!isLoading && data && (
            <div style={{ display: 'flex', flexDirection: 'column', gap: '14px' }}>

              {/* Stat chips */}
              <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr 1fr', gap: '8px' }}>
                {[
                  { label: 'Точки', value: data.totalPoints, color: '#7c3aed' },
                  { label: 'Стр.', value: data.totalPagesRead, color: '#059669' },
                  { label: 'Задачи', value: data.completedAssignments, color: '#d97706' },
                ].map(s => (
                  <div
                    key={s.label}
                    style={{
                      background: 'rgba(124,58,237,0.05)',
                      borderRadius: '10px',
                      padding: '10px 12px',
                      textAlign: 'center',
                    }}
                  >
                    <div style={{ fontSize: '1.4rem', fontWeight: 800, color: s.color, lineHeight: 1 }}>
                      {s.value}
                    </div>
                    <div style={{ fontSize: '0.7rem', color: '#a78bfa', fontWeight: 600, marginTop: '3px' }}>
                      {s.label}
                    </div>
                  </div>
                ))}
              </div>

              {/* Last activity */}
              <div style={{ fontSize: '0.78rem', color: '#a78bfa', textAlign: 'right' }}>
                {data.lastActivityAt
                  ? `Последна активност: ${relativeTime(data.lastActivityAt)}`
                  : 'Няма активност'}
              </div>

              {/* Reading */}
              {data.reading.length > 0 && (
                <DrawerSection title="Четене">
                  {data.reading.map(r => {
                    const pct = r.totalPages ? Math.min(100, Math.round((r.currentPage / r.totalPages) * 100)) : null
                    const isExpiredActive = r.isExpired && r.status !== 'Completed'
                    return (
                      <div key={r.assignedBookId} style={{ padding: '10px 0', borderBottom: '1px solid rgba(124,58,237,0.06)' }}>
                        <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start', gap: '8px', marginBottom: pct !== null ? '6px' : 0 }}>
                          <div style={{ minWidth: 0 }}>
                            <p style={{ margin: 0, fontSize: '0.84rem', fontWeight: 600, color: '#1e1b4b', overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap' }}>
                              {r.bookTitle}
                            </p>
                            <p style={{ margin: '2px 0 0', fontSize: '0.72rem', color: '#a78bfa' }}>
                              стр. {r.currentPage} / {r.totalPages ?? '?'}
                            </p>
                          </div>
                          <Badge variant={isExpiredActive ? 'red' : statusVariant(r.status)}>
                            {isExpiredActive ? 'Просрочено' : translateStatus(r.status)}
                          </Badge>
                        </div>
                        {pct !== null && (
                          <div>
                            <div style={{ width: '100%', height: '5px', borderRadius: '3px', background: 'rgba(124,58,237,0.1)', overflow: 'hidden' }}>
                              <div style={{ height: '100%', width: `${pct}%`, borderRadius: '3px', background: 'linear-gradient(90deg, #7c3aed, #a78bfa)' }} />
                            </div>
                            <p style={{ margin: '2px 0 0', fontSize: '0.68rem', color: '#a78bfa', textAlign: 'right' }}>{pct}%</p>
                          </div>
                        )}
                      </div>
                    )
                  })}
                </DrawerSection>
              )}

              {/* Assignments */}
              {data.assignments.length > 0 && (
                <DrawerSection title="Задачи">
                  {data.assignments.map(a => {
                    const isExpiredActive = a.isExpired && a.status !== 'Completed'
                    return (
                      <div
                        key={a.assignmentId}
                        style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', gap: '8px', padding: '8px 0', borderBottom: '1px solid rgba(124,58,237,0.06)' }}
                      >
                        <div style={{ minWidth: 0 }}>
                          <p style={{ margin: 0, fontSize: '0.84rem', fontWeight: 600, color: '#1e1b4b', overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap' }}>
                            {a.title}
                          </p>
                          <p style={{ margin: '2px 0 0', fontSize: '0.72rem', color: '#a78bfa' }}>
                            {a.subject}{a.dueDate ? ` · Срок: ${formatDate(a.dueDate)}` : ''}
                          </p>
                        </div>
                        <Badge variant={isExpiredActive ? 'red' : statusVariant(a.status)}>
                          {isExpiredActive ? 'Просрочено' : translateStatus(a.status)}
                        </Badge>
                      </div>
                    )
                  })}
                </DrawerSection>
              )}

              {/* Challenges */}
              {data.challenges.length > 0 && (
                <DrawerSection title="Предизвикателства">
                  {data.challenges.map(c => {
                    const variant = c.completed ? 'green' : c.started ? 'blue' : 'gray'
                    const label = c.completed ? 'Завършено' : c.started ? 'В процес' : 'Не е стартирано'
                    return (
                      <div
                        key={c.challengeId}
                        style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', gap: '8px', padding: '8px 0', borderBottom: '1px solid rgba(124,58,237,0.06)', opacity: c.isExpired && !c.completed ? 0.6 : 1 }}
                      >
                        <div style={{ minWidth: 0 }}>
                          <p style={{ margin: 0, fontSize: '0.84rem', fontWeight: 600, color: '#1e1b4b', overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap' }}>
                            {c.title}
                          </p>
                          {challengeTargetLabel(c) && (
                            <p style={{ margin: '2px 0 0', fontSize: '0.72rem', color: '#a78bfa' }}>{challengeTargetLabel(c)}</p>
                          )}
                          <p style={{ margin: '2px 0 0', fontSize: '0.68rem', color: '#a78bfa' }}>Срок: {formatDate(c.endDate)}</p>
                        </div>
                        <Badge variant={variant}>{label}</Badge>
                      </div>
                    )
                  })}
                </DrawerSection>
              )}

              {/* Activity last 7 days */}
              {data.activityLast7Days.length > 0 && (
                <DrawerSection title="Последни 7 дни">
                  {data.activityLast7Days.map((entry, i) => (
                    <div key={i} style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'baseline', padding: '6px 0', borderBottom: '1px solid rgba(124,58,237,0.06)', fontSize: '0.8rem' }}>
                      <span style={{ color: '#6b7280', flexShrink: 0, marginRight: '8px' }}>{formatDate(entry.date)}</span>
                      <span style={{ color: '#1e1b4b', flex: 1, textAlign: 'center' }}>{translate(entry.description)}</span>
                      <span style={{ color: '#7c3aed', fontWeight: 600, flexShrink: 0, marginLeft: '8px' }}>
                        {entry.pointsEarned > 0 ? `+${entry.pointsEarned}` : '—'}
                      </span>
                    </div>
                  ))}
                </DrawerSection>
              )}

              {/* Badges */}
              {badges.length > 0 && (
                <DrawerSection title="Значки">
                  <div style={{ display: 'flex', flexWrap: 'wrap', gap: '10px', paddingTop: '4px' }}>
                    {badges.map(b => (
                      <div
                        key={b.code}
                        title={`${b.name} — ${b.description}`}
                        style={{ display: 'flex', flexDirection: 'column', alignItems: 'center', gap: '4px' }}
                      >
                        <MedalIcon code={b.code} size="md" />
                        <span style={{ fontSize: '0.65rem', color: '#a78bfa', textAlign: 'center', maxWidth: '56px', overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap' }}>
                          {b.name}
                        </span>
                      </div>
                    ))}
                  </div>
                </DrawerSection>
              )}

            </div>
          )}
        </div>
      </div>
    </>
  )
}

function DrawerSection({ title, children }: { title: string; children: React.ReactNode }) {
  return (
    <div>
      <h3 style={{ margin: '0 0 8px', fontSize: '0.72rem', fontWeight: 700, textTransform: 'uppercase', letterSpacing: '0.07em', color: '#a78bfa', fontFamily: FONT }}>
        {title}
      </h3>
      {children}
    </div>
  )
}

// ── A. Activity Bar Chart (SVG) ───────────────────────────────────────────────

function ActivityBarChart({ data }: { data: DailyActivityDto[] }) {
  const BAR_W = 16
  const BAR_GAP = 6
  const CHART_H = 120
  const AXIS_BOTTOM = 20
  const AXIS_LEFT = 32
  const PADDING_TOP = 8

  const maxVal = Math.max(...data.map((d) => d.activeStudents), 1)
  const totalW = data.length * (BAR_W + BAR_GAP) + AXIS_LEFT
  const yTicks = [...new Set([0, Math.round(maxVal / 2), maxVal])]

  return (
    <div style={{ overflowX: 'auto' }}>
      <svg
        width={Math.max(totalW, 300)}
        height={CHART_H + AXIS_BOTTOM + PADDING_TOP}
        style={{ fontFamily: FONT, overflow: 'visible' }}
      >
        {yTicks.map((tick) => {
          const y = PADDING_TOP + CHART_H - (tick / maxVal) * CHART_H
          return (
            <g key={tick}>
              <line x1={AXIS_LEFT - 4} y1={y} x2={totalW} y2={y} stroke="rgba(124,58,237,0.08)" strokeWidth={1} />
              <text x={AXIS_LEFT - 6} y={y + 4} fontSize={9} fill="#a78bfa" textAnchor="end">{tick}</text>
            </g>
          )
        })}

        {data.map((item, i) => {
          const barH = Math.max(2, (item.activeStudents / maxVal) * CHART_H)
          const x = AXIS_LEFT + i * (BAR_W + BAR_GAP)
          const y = PADDING_TOP + CHART_H - barH
          const showLabel = data.length <= 10 || i % Math.ceil(data.length / 6) === 0

          return (
            <g key={item.date}>
              <title>
                {formatDateShort(item.date)}{'\n'}
                Активни ученици: {item.activeStudents}{'\n'}
                Прочетени страници: {item.pagesRead}{'\n'}
                Спечелени точки: {item.pointsEarned}
              </title>
              <rect
                x={x} y={y} width={BAR_W} height={barH} rx={3}
                fill="#7c3aed" opacity={0.85}
                style={{ cursor: 'pointer', transition: 'opacity 0.15s' }}
                onMouseEnter={(e) => { (e.target as SVGRectElement).style.opacity = '1' }}
                onMouseLeave={(e) => { (e.target as SVGRectElement).style.opacity = '0.85' }}
              />
              {showLabel && (
                <text x={x + BAR_W / 2} y={PADDING_TOP + CHART_H + AXIS_BOTTOM - 4} fontSize={9} fill="#a78bfa" textAnchor="middle">
                  {formatDateShort(item.date)}
                </text>
              )}
            </g>
          )
        })}
      </svg>
      <p style={{ fontSize: '11px', color: '#a78bfa', marginTop: '4px', fontFamily: FONT }}>
        Задръжте върху лента за детайли
      </p>
    </div>
  )
}

// ── B. Student Engagement ────────────────────────────────────────────────────

function ActivityDots({ count }: { count: number }) {
  return (
    <div style={{ display: 'flex', gap: '3px', alignItems: 'center' }}>
      {Array.from({ length: 7 }).map((_, i) => (
        <span
          key={i}
          style={{
            display: 'inline-block',
            width: '10px',
            height: '10px',
            borderRadius: '50%',
            background: i < count ? '#7c3aed' : 'rgba(124,58,237,0.15)',
            flexShrink: 0,
          }}
        />
      ))}
    </div>
  )
}

function EngagementTable({
  students,
  onStudentClick,
}: {
  students: StudentEngagementDto[]
  onStudentClick: (studentId: string) => void
}) {
  if (students.length === 0) {
    return (
      <p style={{ color: '#a78bfa', fontSize: '13px', textAlign: 'center', padding: '24px 0', fontFamily: FONT }}>
        Няма данни за ученици.
      </p>
    )
  }

  return (
    <div style={{ overflowX: 'auto' }}>
      <table style={{ width: '100%', borderCollapse: 'collapse', fontFamily: FONT, fontSize: '13px' }}>
        <thead>
          <tr style={{ borderBottom: '1px solid rgba(124,58,237,0.1)' }}>
            {['Ученик', 'Активни дни (7)', 'Точки', 'Серия', 'Последна активност'].map((h) => (
              <th
                key={h}
                style={{
                  padding: '8px 10px',
                  textAlign: 'left',
                  color: '#a78bfa',
                  fontWeight: 600,
                  fontSize: '11px',
                  textTransform: 'uppercase',
                  letterSpacing: '0.04em',
                  whiteSpace: 'nowrap',
                }}
              >
                {h}
              </th>
            ))}
          </tr>
        </thead>
        <tbody>
          {students.map((s) => {
            const isInactive = s.daysActiveLast7 === 0
            const rowBg = s.activeToday
              ? 'rgba(5,150,105,0.05)'
              : isInactive
              ? 'rgba(239,68,68,0.05)'
              : 'transparent'

            return (
              <tr
                key={s.studentId}
                onClick={() => onStudentClick(s.studentId)}
                style={{
                  background: rowBg,
                  borderBottom: '1px solid rgba(124,58,237,0.05)',
                  cursor: 'pointer',
                  transition: 'background 0.12s',
                }}
                onMouseEnter={(e) => {
                  (e.currentTarget as HTMLTableRowElement).style.background = 'rgba(124,58,237,0.06)'
                }}
                onMouseLeave={(e) => {
                  (e.currentTarget as HTMLTableRowElement).style.background = rowBg
                }}
              >
                <td style={{ padding: '10px 10px', whiteSpace: 'nowrap' }}>
                  <div style={{ display: 'flex', alignItems: 'center', gap: '6px' }}>
                    {s.activeToday ? (
                      <span style={{ width: '7px', height: '7px', borderRadius: '50%', background: '#059669', flexShrink: 0, display: 'inline-block' }} />
                    ) : isInactive ? (
                      <span style={{ width: '7px', height: '7px', borderRadius: '50%', background: '#ef4444', flexShrink: 0, display: 'inline-block' }} />
                    ) : null}
                    <span style={{ fontWeight: s.activeToday ? 700 : 500, color: '#7c3aed', textDecoration: 'underline', textDecorationColor: 'rgba(124,58,237,0.3)', textUnderlineOffset: '3px' }}>
                      {s.studentName}
                    </span>
                  </div>
                </td>
                <td style={{ padding: '10px 10px' }}>
                  <ActivityDots count={s.daysActiveLast7} />
                </td>
                <td style={{ padding: '10px 10px', whiteSpace: 'nowrap', color: '#7c3aed', fontWeight: 600 }}>
                  ⚡ {s.totalPoints}
                </td>
                <td style={{ padding: '10px 10px', whiteSpace: 'nowrap', color: '#1e1b4b' }}>
                  {s.currentStreak > 0 ? `🔥 ${s.currentStreak}` : '—'}
                </td>
                <td style={{ padding: '10px 10px', color: '#6b7280', whiteSpace: 'nowrap' }}>
                  {relativeTime(s.lastActivityAt)}
                </td>
              </tr>
            )
          })}
        </tbody>
      </table>
    </div>
  )
}

// ── C. Subject Completion ────────────────────────────────────────────────────

function SubjectCompletionSection({ subjects }: { subjects: SubjectCompletionDto[] }) {
  if (subjects.length === 0) {
    return (
      <p style={{ color: '#a78bfa', fontSize: '13px', textAlign: 'center', padding: '16px 0', fontFamily: FONT }}>
        Няма задачи в този клас.
      </p>
    )
  }

  return (
    <div style={{ display: 'flex', flexDirection: 'column', gap: '14px' }}>
      {subjects.map((s) => {
        const pct = Math.round(s.completionRate * 100)
        return (
          <div key={`${s.subject}-${s.totalRows}`}>
            <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'baseline', marginBottom: '6px', fontFamily: FONT }}>
              <span style={{ fontWeight: 600, color: '#1e1b4b', fontSize: '13px' }}>{s.subject}</span>
              <span style={{ fontSize: '12px', color: '#6b7280' }}>
                {s.completedRows} / {s.totalRows} &nbsp;
                <span style={{ color: '#7c3aed', fontWeight: 700 }}>{pct}%</span>
              </span>
            </div>
            <div style={{ height: '8px', borderRadius: '4px', background: 'rgba(124,58,237,0.1)', overflow: 'hidden' }}>
              <div
                style={{
                  height: '100%',
                  borderRadius: '4px',
                  width: `${pct}%`,
                  background: 'linear-gradient(90deg, #7c3aed, #db2777)',
                  transition: 'width 0.5s ease',
                }}
              />
            </div>
          </div>
        )
      })}
    </div>
  )
}

// ── D. Reading Stats ─────────────────────────────────────────────────────────

function ReadingStatsSection({ stats }: { stats: ReadingStatsDto }) {
  return (
    <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '16px', fontFamily: FONT }}>
      <div style={{ display: 'flex', flexDirection: 'column', gap: '10px' }}>
        <StatChip label="Не започнато" value={stats.notStartedCount} color="#d97706" bg="rgba(217,119,6,0.08)" />
        <StatChip label="В процес" value={stats.inProgressCount} color="#2563eb" bg="rgba(37,99,235,0.08)" />
        <StatChip label="Завършено" value={stats.completedCount} color="#059669" bg="rgba(5,150,105,0.08)" />
      </div>
      <div style={{ display: 'flex', flexDirection: 'column', gap: '10px' }}>
        <BigStat label="📖 Стр. последните 7 дни" value={stats.pagesReadLast7Days} />
        <BigStat label="📖 Стр. последните 30 дни" value={stats.pagesReadLast30Days} />
      </div>
    </div>
  )
}

function StatChip({ label, value, color, bg }: { label: string; value: number; color: string; bg: string }) {
  return (
    <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', padding: '8px 12px', borderRadius: '8px', background: bg, fontFamily: FONT }}>
      <span style={{ fontSize: '13px', color: '#1e1b4b', fontWeight: 500 }}>{label}</span>
      <span style={{ fontSize: '18px', fontWeight: 800, color }}>{value}</span>
    </div>
  )
}

function BigStat({ label, value }: { label: string; value: number }) {
  return (
    <div style={{ padding: '12px 16px', borderRadius: '10px', background: 'rgba(124,58,237,0.06)', fontFamily: FONT }}>
      <p style={{ fontSize: '11px', color: '#a78bfa', fontWeight: 600, marginBottom: '4px' }}>{label}</p>
      <p style={{ fontSize: '28px', fontWeight: 800, color: '#7c3aed', lineHeight: 1 }}>{value}</p>
    </div>
  )
}

// ── Main Page ────────────────────────────────────────────────────────────────

const DAYS = 30

export function ClassAnalyticsPage() {
  const { classId } = useParams<{ classId: string }>()
  const [selectedStudentId, setSelectedStudentId] = useState<string | null>(null)

  const { data, isLoading, isError } = useQuery({
    queryKey: ['class-analytics', classId],
    queryFn: () => analyticsApi.getClassAnalytics(classId!, DAYS),
    enabled: !!classId,
  })

  if (isLoading) {
    return (
      <div className="flex items-center justify-center h-64">
        <Spinner className="text-indigo-600 h-8 w-8" />
      </div>
    )
  }

  if (isError) {
    return (
      <div style={{ textAlign: 'center', padding: '64px 16px', color: '#ef4444', fontFamily: FONT, fontSize: '0.9rem' }}>
        Грешка при зареждане на аналитиките.
      </div>
    )
  }

  if (!data) return null

  const hasTimeline = data.activityTimeline.length > 0

  return (
    <>
      <div
        style={{
          maxWidth: '900px',
          margin: '0 auto',
          padding: '24px 16px 48px',
          display: 'flex',
          flexDirection: 'column',
          gap: '16px',
          fontFamily: FONT,
        }}
      >
        {/* A. Activity Timeline */}
        <div style={CARD_STYLE}>
          <h2 style={CARD_HEADER_STYLE}>📈 Активност — последните {DAYS} дни</h2>
          {hasTimeline ? (
            <ActivityBarChart data={data.activityTimeline} />
          ) : (
            <p style={{ color: '#a78bfa', fontSize: '13px', textAlign: 'center', padding: '24px 0' }}>
              Няма данни за избрания период.
            </p>
          )}
        </div>

        {/* B. Student Engagement */}
        <div style={CARD_STYLE}>
          <h2 style={CARD_HEADER_STYLE}>👥 Активност на учениците (последните 7 дни)</h2>
          <EngagementTable
            students={data.studentEngagement}
            onStudentClick={setSelectedStudentId}
          />
        </div>

        {/* C. Subject Completion */}
        <div style={CARD_STYLE}>
          <h2 style={CARD_HEADER_STYLE}>📝 Изпълнение по предмети</h2>
          <SubjectCompletionSection subjects={data.subjectCompletion} />
        </div>

        {/* D. Reading Stats */}
        <div style={CARD_STYLE}>
          <h2 style={CARD_HEADER_STYLE}>📚 Статистика за четене</h2>
          <ReadingStatsSection stats={data.readingStats} />
        </div>
      </div>

      {/* Student detail drawer */}
      {selectedStudentId && (
        <StudentDetailDrawer
          studentId={selectedStudentId}
          onClose={() => setSelectedStudentId(null)}
        />
      )}
    </>
  )
}
