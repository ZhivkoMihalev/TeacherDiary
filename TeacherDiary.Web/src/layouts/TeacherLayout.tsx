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
      className={`w-4 h-4 shrink-0 transition-transform duration-200 ${open ? 'rotate-90' : ''}`}
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
      <div className="flex items-center gap-0.5">
        <NavLink
          to={`/teacher/classes/${cls.id}`}
          className={({ isActive }) =>
            `flex-1 min-w-0 px-2 py-1.5 rounded-md text-sm transition-colors truncate ${
              isActive
                ? 'bg-white/15 text-white font-medium'
                : 'text-slate-300 hover:bg-white/10 hover:text-white'
            }`
          }
        >
          {cls.name}
        </NavLink>
        <button
          onClick={() => setStudentsOpen((v) => !v)}
          title={studentsOpen ? 'Скрий ученици' : 'Покажи ученици'}
          className="p-1 rounded text-slate-400 hover:bg-white/10 hover:text-white transition-colors shrink-0"
        >
          <ChevronIcon open={studentsOpen} />
        </button>
      </div>

      {studentsOpen && (
        <div className="ml-3 mt-0.5 space-y-0.5 border-l border-white/10 pl-2">
          {sorted.length === 0 ? (
            <p className="px-2 py-1 text-xs text-slate-400">Няма ученици</p>
          ) : (
            sorted.map((s) => (
              <NavLink
                key={s.id}
                to={`/teacher/students/${s.id}`}
                className={({ isActive }) =>
                  `block px-2 py-1 rounded text-xs truncate transition-colors ${
                    isActive
                      ? 'bg-white/15 text-white font-medium'
                      : 'text-slate-400 hover:bg-white/10 hover:text-white'
                  }`
                }
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

  return (
    <div className="flex h-screen bg-gray-50">
      {/* Sidebar */}
      <aside className="w-56 bg-[#1e2640] flex flex-col">
        <div className="px-5 py-4 border-b border-white/10 flex items-center justify-between">
          <Link to="/teacher/classes" className="text-lg font-semibold text-white hover:opacity-80 transition-opacity">
            TeacherDiary
          </Link>
          <NotificationBell />
        </div>

        <nav className="flex-1 px-3 py-4 space-y-1 overflow-y-auto">
          {/* Класове — expandable */}
          <div>
            <button
              onClick={() => {
                navigate('/teacher/classes')
                setClassesOpen((v) => !v)
              }}
              className={`w-full flex items-center justify-between px-3 py-2 rounded-lg text-sm font-medium transition-colors ${
                onClassesRoute
                  ? 'bg-white/15 text-white'
                  : 'text-slate-300 hover:bg-white/10 hover:text-white'
              }`}
            >
              <span>Класове</span>
              <ChevronIcon open={classesOpen} />
            </button>

            {classesOpen && classes.length > 0 && (
              <div className="mt-1 ml-2 space-y-0.5 border-l border-white/10 pl-2">
                {classes.map((c) => (
                  <ClassNavItem key={c.id} cls={c} />
                ))}
              </div>
            )}
          </div>

          {/* Книги */}
          <NavLink
            to="/teacher/books"
            className={({ isActive }) =>
              `flex items-center px-3 py-2 rounded-lg text-sm font-medium transition-colors ${
                isActive
                  ? 'bg-white/15 text-white'
                  : 'text-slate-300 hover:bg-white/10 hover:text-white'
              }`
            }
          >
            Книги
          </NavLink>

          {/* Съобщения */}
          <NavLink
            to="/teacher/messages"
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

      {/* Main */}
      <main className="flex-1 overflow-y-auto flex flex-col">
        <div className="flex-1">
          <Outlet />
        </div>
        <Footer />
      </main>

      {/* Ad sidebar */}
      <AdSidebar />
    </div>
  )
}
