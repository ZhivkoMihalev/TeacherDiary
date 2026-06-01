import { useState, useEffect } from 'react'
import { NavLink, Link, Outlet, useNavigate, useLocation } from 'react-router-dom'
import { useQuery } from '@tanstack/react-query'
import { useAuth } from '../context/AuthContext'
import { classesApi } from '../api/classes'
import { studentsApi } from '../api/students'
import { messagesApi } from '../api/messages'
import type { ClassDto } from '../types'
import { AdSidebar } from '../components/AdSidebar'
import { NotificationBell } from '../components/NotificationBell'
import { Footer } from '../components/Footer'

function ChevronIcon({ open }: { open: boolean }) {
  return (
    <svg
      xmlns="http://www.w3.org/2000/svg"
      viewBox="0 0 20 20"
      fill="currentColor"
      style={{
        width: '13px', height: '13px', flexShrink: 0,
        transition: 'transform 0.2s ease',
        transform: open ? 'rotate(90deg)' : 'none',
      }}
    >
      <path fillRule="evenodd" d="M7.21 14.77a.75.75 0 01.02-1.06L11.168 10 7.23 6.29a.75.75 0 111.04-1.08l4.5 4.25a.75.75 0 010 1.08l-4.5 4.25a.75.75 0 01-1.06-.02z" clipRule="evenodd" />
    </svg>
  )
}

function ClassNavItem({ cls }: { cls: ClassDto }) {
  const [studentsOpen, setStudentsOpen] = useState(false)

  const { data: students = [] } = useQuery({
    queryKey: ['sidebar-students', cls.id],
    queryFn: () => studentsApi.getByClass(cls.id),
    enabled: studentsOpen,
  })

  const sorted = [...students].sort((a, b) =>
    `${a.lastName} ${a.firstName}`.localeCompare(`${b.lastName} ${b.firstName}`, 'bg')
  )

  return (
    <div>
      <div style={{ display: 'flex', alignItems: 'center' }}>
        <NavLink
          to={`/teacher/classes/${cls.id}`}
          className={({ isActive }) => `aurora-nav-sub ${isActive ? 'active' : ''}`}
          style={{ flexGrow: 1, marginRight: 0 }}
        >
          {cls.name}
        </NavLink>
        <button
          onClick={() => setStudentsOpen(v => !v)}
          style={{
            padding: '4px 8px',
            background: 'none',
            border: 'none',
            cursor: 'pointer',
            color: '#a78bfa',
            flexShrink: 0,
            borderRadius: '6px',
            transition: 'color 0.15s, background 0.15s',
          }}
          onMouseEnter={e => { e.currentTarget.style.color = '#7c3aed'; e.currentTarget.style.background = 'rgba(124,58,237,0.06)' }}
          onMouseLeave={e => { e.currentTarget.style.color = '#a78bfa'; e.currentTarget.style.background = 'none' }}
        >
          <ChevronIcon open={studentsOpen} />
        </button>
      </div>

      {studentsOpen && (
        <div style={{ marginLeft: '12px', borderLeft: '1.5px solid rgba(167,139,250,0.3)', paddingLeft: '4px' }}>
          {sorted.length === 0 ? (
            <p style={{ margin: 0, padding: '5px 10px', fontSize: '0.75rem', color: '#a78bfa', fontFamily: "'Plus Jakarta Sans', sans-serif" }}>
              Няма ученици
            </p>
          ) : (
            sorted.map(s => (
              <NavLink
                key={s.id}
                to={`/teacher/students/${s.id}`}
                className={({ isActive }) => `aurora-nav-sub ${isActive ? 'active' : ''}`}
                style={{ paddingLeft: '10px', fontSize: '0.77rem' }}
              >
                {s.lastName} {s.firstName}
              </NavLink>
            ))
          )}
        </div>
      )}
    </div>
  )
}

function SidebarDivider() {
  return <div style={{ height: '1px', background: 'linear-gradient(90deg, transparent, rgba(124,58,237,0.15), transparent)', margin: '8px 16px' }} />
}

export function TeacherLayout() {
  const { user, logout } = useAuth()
  const navigate = useNavigate()
  const location = useLocation()

  const onClassesRoute = location.pathname.startsWith('/teacher/classes')
  const [classesOpen, setClassesOpen] = useState(onClassesRoute)

  useEffect(() => {
    if (onClassesRoute) setClassesOpen(true)
  }, [onClassesRoute])

  const { data: classes = [] } = useQuery({
    queryKey: ['classes'],
    queryFn: classesApi.getMine,
  })

  const { data: unreadCount = 0 } = useQuery({
    queryKey: ['unread-count'],
    queryFn: messagesApi.getUnreadCount,
    refetchInterval: 5_000,
  })

  function handleLogout() {
    logout()
    navigate('/login')
  }

  const initials = user?.fullName
    ? user.fullName.split(' ').map(n => n[0]).join('').toUpperCase().slice(0, 2)
    : '?'

  return (
    <div style={{ display: 'flex', height: '100vh', overflow: 'hidden' }}>

      {/* ── Floating Glass Sidebar ── */}
      <aside style={{
        width: '230px',
        flexShrink: 0,
        margin: '12px 6px 12px 12px',
        display: 'flex',
        flexDirection: 'column',
        background: 'rgba(255,255,255,0.78)',
        backdropFilter: 'blur(20px)',
        WebkitBackdropFilter: 'blur(20px)',
        borderRadius: '18px',
        border: '1px solid rgba(255,255,255,0.92)',
        boxShadow: '0 8px 32px rgba(124,58,237,0.1), 0 2px 8px rgba(0,0,0,0.04)',
        position: 'relative',
        zIndex: 20,
      }}>

        {/* Brand */}
        <div style={{
          padding: '20px 16px 14px',
          display: 'flex',
          alignItems: 'center',
          justifyContent: 'space-between',
          borderBottom: '1px solid rgba(124,58,237,0.1)',
        }}>
          <Link to="/teacher/classes" className="aurora-brand">
            TeacherDiary
          </Link>
          <NotificationBell />
        </div>

        {/* Nav */}
        <nav style={{ flex: 1, paddingTop: '10px', paddingBottom: '8px', overflowY: 'auto' }}>
          {/* Класове */}
          <div>
            <button
              onClick={() => { navigate('/teacher/classes'); setClassesOpen(v => !v) }}
              className={`aurora-nav-item ${onClassesRoute ? 'active' : ''}`}
              style={{ justifyContent: 'space-between' }}
            >
              <span style={{ display: 'flex', alignItems: 'center', gap: '9px' }}>
                <span style={{ fontSize: '1em' }}>🏫</span>
                Класове
              </span>
              <ChevronIcon open={classesOpen} />
            </button>

            {classesOpen && classes.length > 0 && (
              <div style={{ marginLeft: '10px', borderLeft: '1.5px solid rgba(167,139,250,0.3)', paddingLeft: '4px', marginBottom: '4px' }}>
                {classes.map(c => <ClassNavItem key={c.id} cls={c} />)}
              </div>
            )}
          </div>

          <SidebarDivider />

          <NavLink
            to="/teacher/books"
            className={({ isActive }) => `aurora-nav-item ${isActive ? 'active' : ''}`}
          >
            <span style={{ fontSize: '1em' }}>📚</span>
            Книги
          </NavLink>

          <NavLink
            to="/teacher/messages"
            className={({ isActive }) => `aurora-nav-item ${isActive ? 'active' : ''}`}
            style={{ justifyContent: 'space-between' }}
          >
            <span style={{ display: 'flex', alignItems: 'center', gap: '9px' }}>
              <span style={{ fontSize: '1em' }}>💬</span>
              Съобщения
            </span>
            {unreadCount > 0 && (
              <span style={{
                background: 'linear-gradient(135deg, #7c3aed, #db2777)',
                color: 'white',
                fontSize: '0.68rem',
                fontWeight: 700,
                borderRadius: '99px',
                minWidth: '19px',
                height: '19px',
                display: 'inline-flex',
                alignItems: 'center',
                justifyContent: 'center',
                padding: '0 5px',
              }}>
                {unreadCount}
              </span>
            )}
          </NavLink>
        </nav>

        <SidebarDivider />

        {/* User */}
        <div style={{ padding: '12px 16px 16px' }}>
          <div style={{ display: 'flex', alignItems: 'center', gap: '10px', marginBottom: '10px' }}>
            <div style={{
              width: '34px',
              height: '34px',
              borderRadius: '10px',
              flexShrink: 0,
              display: 'flex',
              alignItems: 'center',
              justifyContent: 'center',
              fontSize: '0.72rem',
              fontWeight: 700,
              fontFamily: "'Plus Jakarta Sans', sans-serif",
              letterSpacing: '0.04em',
              background: 'linear-gradient(135deg, #7c3aed, #059669)',
              color: 'white',
            }}>
              {initials}
            </div>
            <div style={{ minWidth: 0 }}>
              <p style={{
                margin: 0,
                fontSize: '0.82rem',
                fontWeight: 600,
                fontFamily: "'Plus Jakarta Sans', sans-serif",
                color: '#1e1b4b',
                lineHeight: 1.25,
                overflow: 'hidden',
                textOverflow: 'ellipsis',
                whiteSpace: 'nowrap',
              }}>
                {user?.fullName}
              </p>
              <p style={{ margin: 0, fontSize: '0.72rem', color: '#a78bfa', fontFamily: "'Plus Jakarta Sans', sans-serif" }}>
                Учител
              </p>
            </div>
          </div>

          <button
            onClick={handleLogout}
            style={{
              fontSize: '0.75rem',
              fontFamily: "'Plus Jakarta Sans', sans-serif",
              fontWeight: 500,
              color: '#a78bfa',
              background: 'none',
              border: 'none',
              padding: 0,
              cursor: 'pointer',
              transition: 'color 0.15s',
            }}
            onMouseEnter={e => (e.currentTarget.style.color = '#dc2626')}
            onMouseLeave={e => (e.currentTarget.style.color = '#a78bfa')}
          >
            ← Изход
          </button>
        </div>
      </aside>

      {/* ── Main (transparent — aurora shows through) ── */}
      <main style={{ flex: 1, overflowY: 'auto', display: 'flex', flexDirection: 'column', minWidth: 0, padding: '12px 0 0 0' }}>
        <div style={{ flex: 1 }}>
          <Outlet />
        </div>
        <Footer />
      </main>

      <AdSidebar />
    </div>
  )
}
