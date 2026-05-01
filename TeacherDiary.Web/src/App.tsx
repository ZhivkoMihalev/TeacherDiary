import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { AuthProvider } from './context/AuthContext'
import { ProtectedRoute } from './components/ProtectedRoute'

import { TeacherLayout } from './layouts/TeacherLayout'
import { ParentLayout } from './layouts/ParentLayout'
import { StudentLayout } from './layouts/StudentLayout'

import { LoginPage } from './pages/auth/LoginPage'
import { RegisterTeacherPage } from './pages/auth/RegisterTeacherPage'
import { RegisterParentPage } from './pages/auth/RegisterParentPage'
import { RegisterStudentPage } from './pages/auth/RegisterStudentPage'

import { StudentDashboardPage } from './pages/student/StudentDashboardPage'
import { StudentBadgesPage } from './pages/student/StudentBadgesPage'

import { ClassesPage } from './pages/teacher/ClassesPage'
import { BooksPage } from './pages/teacher/BooksPage'
import { ClassDashboardPage, ClassOverview } from './pages/teacher/ClassDashboardPage'
import { ClassStudentsPage } from './pages/teacher/ClassStudentsPage'
import { ClassReadingPage } from './pages/teacher/ClassReadingPage'
import { ClassAssignmentsPage } from './pages/teacher/ClassAssignmentsPage'
import { ClassChallengesPage } from './pages/teacher/ClassChallengesPage'
import { TeacherStudentPage } from './pages/teacher/TeacherStudentPage'

import { MyStudentsPage } from './pages/parent/MyStudentsPage'
import { StudentProgressPage } from './pages/parent/StudentProgressPage'
import { MessagesPage } from './pages/shared/MessagesPage'

const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      retry: 1,
      staleTime: 30_000,
    },
  },
})

export default function App() {
  return (
    <QueryClientProvider client={queryClient}>
      <AuthProvider>
        <BrowserRouter>
          <Routes>
            {/* Public */}
            <Route path="/login" element={<LoginPage />} />
            <Route path="/register/teacher" element={<RegisterTeacherPage />} />
            <Route path="/register/parent" element={<RegisterParentPage />} />
            <Route path="/register/student" element={<RegisterStudentPage />} />

            {/* Teacher */}
            <Route
              path="/teacher"
              element={
                <ProtectedRoute role="Teacher">
                  <TeacherLayout />
                </ProtectedRoute>
              }
            >
              <Route index element={<Navigate to="classes" replace />} />
              <Route path="classes" element={<ClassesPage />} />
              <Route path="books" element={<BooksPage />} />
              <Route path="messages" element={<MessagesPage />} />
              <Route path="students/:studentId" element={<TeacherStudentPage />} />
              <Route path="classes/:classId" element={<ClassDashboardPage />}>
                <Route index element={<Navigate to="dashboard" replace />} />
                <Route path="dashboard" element={<ClassOverview />} />
                <Route path="students" element={<ClassStudentsPage />} />
                <Route path="reading" element={<ClassReadingPage />} />
                <Route path="assignments" element={<ClassAssignmentsPage />} />
                <Route path="challenges" element={<ClassChallengesPage />} />
              </Route>
            </Route>

            {/* Parent */}
            <Route
              path="/parent"
              element={
                <ProtectedRoute role="Parent">
                  <ParentLayout />
                </ProtectedRoute>
              }
            >
              <Route index element={<Navigate to="students" replace />} />
              <Route path="students" element={<MyStudentsPage />} />
              <Route path="students/:studentId" element={<StudentProgressPage />} />
              <Route path="messages" element={<MessagesPage />} />
            </Route>

            {/* Student */}
            <Route
              path="/student"
              element={
                <ProtectedRoute role="Student">
                  <StudentLayout />
                </ProtectedRoute>
              }
            >
              <Route index element={<Navigate to="dashboard" replace />} />
              <Route path="dashboard" element={<StudentDashboardPage />} />
              <Route path="badges" element={<StudentBadgesPage />} />
              <Route path="messages" element={<MessagesPage />} />
            </Route>

            {/* Root redirect */}
            <Route path="/" element={<Navigate to="/login" replace />} />
            <Route path="*" element={<Navigate to="/login" replace />} />
          </Routes>
        </BrowserRouter>
      </AuthProvider>
    </QueryClientProvider>
  )
}
