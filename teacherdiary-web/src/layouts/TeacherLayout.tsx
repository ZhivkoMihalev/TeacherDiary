import { useState, useEffect } from 'react'
import { NavLink, Link, Outlet, useNavigate, useLocation } from 'react-router-dom'
import { useQuery } from '@tanstack/react-query'
import { useAuth } from '../context/AuthContext'
import { classesApi } from '../api/classes'

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

  function handleLogout() {
    logout()
    navigate('/login')
  }

  return (
    <div className="flex h-screen bg-gray-50">
      {/* Sidebar */}
      <aside className="w-56 bg-white border-r border-gray-200 flex flex-col">
        <div className="px-5 py-4 border-b border-gray-200">
          <Link to="/teacher/classes" className="text-lg font-semibold text-indigo-600 hover:opacity-80 transition-opacity">
            TeacherDiary
          </Link>
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
                  ? 'bg-indigo-50 text-indigo-700'
                  : 'text-gray-600 hover:bg-gray-100 hover:text-gray-900'
              }`}
            >
              <span>Класове</span>
              <ChevronIcon open={classesOpen} />
            </button>

            {classesOpen && classes.length > 0 && (
              <div className="mt-1 ml-2 space-y-0.5 border-l border-gray-100 pl-2">
                {classes.map((c) => (
                  <NavLink
                    key={c.id}
                    to={`/teacher/classes/${c.id}`}
                    className={({ isActive }) =>
                      `flex items-center px-2 py-1.5 rounded-md text-sm transition-colors truncate ${
                        isActive
                          ? 'bg-indigo-50 text-indigo-700 font-medium'
                          : 'text-gray-500 hover:bg-gray-100 hover:text-gray-800'
                      }`
                    }
                  >
                    {c.name}
                  </NavLink>
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
                  ? 'bg-indigo-50 text-indigo-700'
                  : 'text-gray-600 hover:bg-gray-100 hover:text-gray-900'
              }`
            }
          >
            Книги
          </NavLink>
        </nav>

        <div className="px-4 py-4 border-t border-gray-200">
          <p className="text-xs text-gray-500 mb-2 truncate">{user?.fullName}</p>
          <button
            onClick={handleLogout}
            className="w-full text-left text-sm text-gray-500 hover:text-red-600 transition-colors"
          >
            Изход
          </button>
        </div>
      </aside>

      {/* Main */}
      <main className="flex-1 overflow-y-auto">
        <Outlet />
      </main>
    </div>
  )
}
