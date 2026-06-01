import { NavLink, Link, Outlet, useNavigate } from 'react-router-dom'
import { AdSidebar } from '../components/AdSidebar'
import { Footer } from '../components/Footer'
import { useQuery } from '@tanstack/react-query'
import { useAuth } from '../context/AuthContext'
import { messagesApi } from '../api/messages'
import { NotificationBell } from '../components/NotificationBell'

function SidebarDivider() {
  return <div style={{ height: '1px', background: 'linear-gradient(90deg, transparent, rgba(124,58,237,0.15), transparent)', margin: '8px 16px' }} />
}

export function ParentLayout() {
  const { user, logout } = useAuth()
  const navigate = useNavigate()

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

      {/* ── Floating Glass Sidebar (rose tint for parent) ── */}
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
        boxShadow: '0 8px 32px rgba(219,39,119,0.1), 0 2px 8px rgba(0,0,0,0.04)',
        position: 'relative',
        zIndex: 20,
      }}>

        <div style={{ padding: '20px 16px 14px', display: 'flex', alignItems: 'center', justifyContent: 'space-between', borderBottom: '1px solid rgba(124,58,237,0.1)' }}>
          <Link to="/parent/students" className="aurora-brand">
            TeacherDiary
          </Link>
          <NotificationBell />
        </div>

        <nav style={{ flex: 1, paddingTop: '10px', paddingBottom: '8px', overflowY: 'auto' }}>
          <NavLink to="/parent/students" className={({ isActive }) => `aurora-nav-item ${isActive ? 'active' : ''}`}>
            <span>👨‍👩‍👧</span> Моите деца
          </NavLink>

          <SidebarDivider />

          <NavLink to="/parent/messages" className={({ isActive }) => `aurora-nav-item ${isActive ? 'active' : ''}`} style={{ justifyContent: 'space-between' }}>
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
            <div style={{ width: '34px', height: '34px', borderRadius: '10px', flexShrink: 0, display: 'flex', alignItems: 'center', justifyContent: 'center', fontSize: '0.72rem', fontWeight: 700, fontFamily: "'Plus Jakarta Sans', sans-serif", background: 'linear-gradient(135deg, #db2777, #f97316)', color: 'white' }}>
              {initials}
            </div>
            <div style={{ minWidth: 0 }}>
              <p style={{ margin: 0, fontSize: '0.82rem', fontWeight: 600, fontFamily: "'Plus Jakarta Sans', sans-serif", color: '#1e1b4b', lineHeight: 1.25, overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap' }}>
                {user?.fullName}
              </p>
              <p style={{ margin: 0, fontSize: '0.72rem', color: '#a78bfa', fontFamily: "'Plus Jakarta Sans', sans-serif" }}>Родител</p>
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

      <main style={{ flex: 1, overflowY: 'auto', display: 'flex', flexDirection: 'column', minWidth: 0, paddingTop: '12px' }}>
        <div style={{ flex: 1 }}><Outlet /></div>
        <Footer />
      </main>

      <AdSidebar />
    </div>
  )
}
