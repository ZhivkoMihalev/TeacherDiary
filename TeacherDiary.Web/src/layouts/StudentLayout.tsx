import { useState, useEffect } from 'react'
import { NavLink, Link, Outlet, useNavigate, useLocation } from 'react-router-dom'
import { AdSidebar } from '../components/AdSidebar'
import { Footer } from '../components/Footer'
import { useQuery } from '@tanstack/react-query'
import { useAuth } from '../context/AuthContext'
import { messagesApi } from '../api/messages'
import { NotificationBell } from '../components/NotificationBell'

function SidebarDivider() {
  return <div style={{ height: '1px', background: 'linear-gradient(90deg, transparent, rgba(124,58,237,0.15), transparent)', margin: '8px 16px' }} />
}

export function StudentLayout() {
  const { user, logout } = useAuth()
  const navigate = useNavigate()
  const location = useLocation()

  const [sidebarOpen, setSidebarOpen] = useState(false)

  // Close mobile sidebar on navigation
  useEffect(() => {
    setSidebarOpen(false)
  }, [location.pathname])

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
          borderBottom: '1px solid rgba(3,105,161,0.1)',
        }}
      >
        <button
          onClick={() => setSidebarOpen(true)}
          className="p-2 rounded-lg text-sky-600 hover:bg-sky-50 transition-colors"
          aria-label="Отвори меню"
        >
          <svg xmlns="http://www.w3.org/2000/svg" className="w-5 h-5" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
            <line x1="3" y1="6" x2="21" y2="6" />
            <line x1="3" y1="12" x2="21" y2="12" />
            <line x1="3" y1="18" x2="21" y2="18" />
          </svg>
        </button>
        <Link to="/student/dashboard" className="aurora-brand">TeacherDiary</Link>
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

      {/* ── Floating Glass Sidebar (sky tint for student) ── */}
      <aside
        className={`aurora-sidebar fixed inset-y-0 left-0 lg:relative lg:inset-auto flex flex-col z-50 lg:z-20 transition-transform duration-300 ease-in-out ${sidebarOpen ? 'translate-x-0' : '-translate-x-full'} lg:translate-x-0`}
        style={{
          width: '230px',
          flexShrink: 0,
          background: 'rgba(255,255,255,0.78)',
          backdropFilter: 'blur(20px)',
          WebkitBackdropFilter: 'blur(20px)',
          border: '1px solid rgba(255,255,255,0.92)',
          boxShadow: '0 8px 32px rgba(3,105,161,0.1), 0 2px 8px rgba(0,0,0,0.04)',
        }}
      >

        <div style={{ padding: '20px 16px 14px', display: 'flex', alignItems: 'center', justifyContent: 'space-between', borderBottom: '1px solid rgba(124,58,237,0.1)' }}>
          <Link to="/student/dashboard" className="aurora-brand">
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

        <nav style={{ flex: 1, paddingTop: '10px', paddingBottom: '8px', overflowY: 'auto' }}>
          <NavLink to="/student/dashboard" className={({ isActive }) => `aurora-nav-item ${isActive ? 'active' : ''}`}>
            <span>⭐</span> Моят напредък
          </NavLink>

          <NavLink to="/student/badges" className={({ isActive }) => `aurora-nav-item ${isActive ? 'active' : ''}`}>
            <span>🏅</span> Значки
          </NavLink>

          <SidebarDivider />

          <NavLink to="/student/messages" className={({ isActive }) => `aurora-nav-item ${isActive ? 'active' : ''}`} style={{ justifyContent: 'space-between' }}>
            <span style={{ display: 'flex', alignItems: 'center', gap: '9px' }}>
              <span>💬</span> Съобщения
            </span>
            {unreadCount > 0 && (
              <span style={{ background: 'linear-gradient(135deg, #7c3aed, #db2777)', color: 'white', fontSize: '0.68rem', fontWeight: 700, borderRadius: '99px', minWidth: '19px', height: '19px', display: 'inline-flex', alignItems: 'center', justifyContent: 'center', padding: '0 5px' }}>
                {unreadCount}
              </span>
            )}
          </NavLink>
        </nav>

        <SidebarDivider />

        <div style={{ padding: '12px 16px 16px' }}>
          <div style={{ display: 'flex', alignItems: 'center', gap: '10px', marginBottom: '10px' }}>
            <div style={{ width: '34px', height: '34px', borderRadius: '10px', flexShrink: 0, display: 'flex', alignItems: 'center', justifyContent: 'center', fontSize: '0.72rem', fontWeight: 700, fontFamily: "'Plus Jakarta Sans', sans-serif", background: 'linear-gradient(135deg, #0369a1, #7c3aed)', color: 'white' }}>
              {initials}
            </div>
            <div style={{ minWidth: 0 }}>
              <p style={{ margin: 0, fontSize: '0.82rem', fontWeight: 600, fontFamily: "'Plus Jakarta Sans', sans-serif", color: '#1e1b4b', lineHeight: 1.25, overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap' }}>
                {user?.fullName}
              </p>
              <p style={{ margin: 0, fontSize: '0.72rem', color: '#a78bfa', fontFamily: "'Plus Jakarta Sans', sans-serif" }}>Ученик</p>
            </div>
          </div>
          <button onClick={handleLogout}
            style={{ fontSize: '0.75rem', fontFamily: "'Plus Jakarta Sans', sans-serif", fontWeight: 500, color: '#a78bfa', background: 'none', border: 'none', padding: 0, cursor: 'pointer', transition: 'color 0.15s' }}
            onMouseEnter={e => (e.currentTarget.style.color = '#dc2626')}
            onMouseLeave={e => (e.currentTarget.style.color = '#a78bfa')}>
            ← Изход
          </button>
        </div>
      </aside>

      <main className="flex-1 overflow-y-auto flex flex-col min-w-0 pt-[52px] lg:pt-3">
        <div style={{ flex: 1 }}><Outlet /></div>
        <Footer />
      </main>

      <AdSidebar />
    </div>
  )
}
