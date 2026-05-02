import { NavLink, Link, Outlet, useNavigate } from 'react-router-dom'
import { AdSidebar } from '../components/AdSidebar'
import { Footer } from '../components/Footer'
import { useQuery } from '@tanstack/react-query'
import { useAuth } from '../context/AuthContext'
import { messagesApi } from '../api/messages'
import { NotificationBell } from '../components/NotificationBell'

export function StudentLayout() {
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

  return (
    <div className="flex h-screen bg-gray-50">
      <aside className="w-56 bg-[#1e2640] flex flex-col">
        <div className="px-5 py-4 border-b border-white/10 flex items-center justify-between">
          <Link to="/student/dashboard" className="text-lg font-semibold text-white hover:opacity-80 transition-opacity">
            TeacherDiary
          </Link>
          <NotificationBell />
        </div>

        <nav className="flex-1 px-3 py-4 space-y-1">
          <NavLink
            to="/student/dashboard"
            className={({ isActive }) =>
              `flex items-center px-3 py-2 rounded-lg text-sm font-medium transition-colors ${
                isActive
                  ? 'bg-white/15 text-white'
                  : 'text-slate-300 hover:bg-white/10 hover:text-white'
              }`
            }
          >
            Моят напредък
          </NavLink>

          <NavLink
            to="/student/badges"
            className={({ isActive }) =>
              `flex items-center px-3 py-2 rounded-lg text-sm font-medium transition-colors ${
                isActive
                  ? 'bg-white/15 text-white'
                  : 'text-slate-300 hover:bg-white/10 hover:text-white'
              }`
            }
          >
            Значки
          </NavLink>

          <NavLink
            to="/student/messages"
            className={({ isActive }) =>
              `flex items-center justify-between px-3 py-2 rounded-lg text-sm font-medium transition-colors ${
                isActive
                  ? 'bg-white/15 text-white'
                  : 'text-slate-300 hover:bg-white/10 hover:text-white'
              }`
            }
          >
            <span>Съобщения</span>
            {unreadCount > 0 && (
              <span className="ml-1 bg-red-500 text-white text-xs font-bold rounded-full min-w-[18px] h-[18px] flex items-center justify-center px-1">
                {unreadCount}
              </span>
            )}
          </NavLink>
        </nav>

        <div className="px-4 py-4 border-t border-white/10">
          <p className="text-xs text-slate-400 mb-2 truncate">{user?.fullName}</p>
          <button
            onClick={handleLogout}
            className="w-full text-left text-sm text-slate-400 hover:text-red-400 transition-colors"
          >
            Изход
          </button>
        </div>
      </aside>

      <main className="flex-1 overflow-y-auto flex flex-col">
        <div className="flex-1">
          <Outlet />
        </div>
        <Footer />
      </main>

      <AdSidebar />
    </div>
  )
}