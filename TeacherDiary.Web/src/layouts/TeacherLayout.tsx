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

  const [sidebarOpen, setSidebarOpen] = useState(false)

  const onClassesRoute = location.pathname.startsWith('/teacher/classes')
  const [classesOpen, setClassesOpen] = useState(onClassesRoute)

  useEffect(() => {
    if (onClassesRoute) setClassesOpen(true)
  }, [onClassesRoute])

  // Close mobile sidebar on navigation
  useEffect(() => {
    setSidebarOpen(false)
  }, [location.pathname])

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

      {/* Mobile top bar */}
      <div
        className="lg:hidden fixed top-0 left-0 right-0 flex items-center justify-between px-4 z-30"
        style={{
          height: '52px',
          background: 'rgba(255,255,255,0.92)',
          backdropFilter: 'blur(12px)',
          WebkitBackdropFilter: 'blur(12px)',
          borderBottom: '1px solid rgba(124,58,237,0.1)',
        }}
      >
        <button
          onClick={() => setSidebarOpen(true)}
          className="p-2 rounded-lg text-violet-500 hover:bg-violet-50 transition-colors"
          aria-label="Отвори меню"
        >
          <svg xmlns="http://www.w3.org/2000/svg" className="w-5 h-5" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
            <line x1="3" y1="6" x2="21" y2="6" />
            <line x1="3" y1="12" x2="21" y2="12" />
            <line x1="3" y1="18" x2="21" y2="18" />
          </svg>
        </button>
        <Link to="/teacher/classes" className="aurora-brand">TeacherDiary</Link>
        <NotificationBell />
      </div>

      {/* Backdrop */}
      {sidebarOpen && (
        <div
          className="lg:hidden fixed inset-0 z-40 bg-black/30"
          style={{ backdropFilter: 'blur(2px)', WebkitBackdropFilter: 'blur(2px)' }}
          onClick={() => setSidebarOpen(false)}
        />
      )}

      {/* ── Floating Glass Sidebar ── */}
      <aside
        className={`aurora-sidebar fixed inset-y-0 left-0 lg:relative lg:inset-auto flex flex-col z-50 lg:z-20 transition-transform duration-300 ease-in-out ${sidebarOpen ? 'translate-x-0' : '-translate-x-full'} lg:translate-x-0`}
        style={{
          width: '230px',
          flexShrink: 0,
          background: 'rgba(255,255,255,0.78)',
          backdropFilter: 'blur(20px)',
          WebkitBackdropFilter: 'blur(20px)',
          border: '1px solid rgba(255,255,255,0.92)',
          boxShadow: '0 8px 32px rgba(124,58,237,0.1), 0 2px 8px rgba(0,0,0,0.04)',
        }}
      >

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
          <div style={{ display: 'flex', alignItems: 'center', gap: '4px' }}>
            <div className="hidden lg:block">
              <NotificationBell />
            </div>
            <button
              className="lg:hidden p-1.5 rounded-lg text-violet-400 hover:bg-violet-100 hover:text-violet-600 transition-colors"
              onClick={() => setSidebarOpen(false)}
              aria-label="Затвори меню"
            >
              <svg xmlns="http://www.w3.org/2000/svg" className="w-4 h-4" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.5" strokeLinecap="round" strokeLinejoin="round">
                <path d="M18 6 6 18M6 6l12 12" />
              </svg>
            </button>
          </div>
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
            to="/teacher/notifications"
            className={({ isActive }) => `aurora-nav-item ${isActive ? 'active' : ''}`}
          >
            <span style={{ fontSize: '1em' }}>🔔</span>
            Известия
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

      {/* ── Main ── */}
      <main className="flex-1 overflow-y-auto flex flex-col min-w-0 pt-[52px] lg:pt-3">
        <div style={{ flex: 1 }}>
          <Outlet />
        </div>
        <Footer />
      </main>

      <AdSidebar />
    </div>
  )
}
