// Auth
export interface AuthResponse {
  token: string
  userId: string
  email: string
  fullName: string
  role: 'Teacher' | 'Parent'
}

export interface RegisterTeacherRequest {
  firstName: string
  lastName: string
  email: string
  password: string
}

export interface RegisterParentRequest {
  firstName: string
  lastName: string
  email: string
  password: string
}

export interface LoginRequest {
  email: string
  password: string
}

// Classes
export interface ClassDto {
  id: string
  name: string
  grade: number
  schoolYear: string
  studentsCount?: number
}

export interface ClassCreateRequest {
  name: string
  grade: number
  schoolYear: string
}

// Students
export interface StudentDto {
  id: string
  firstName: string
  lastName: string
  classId: string | null
  isActive?: boolean
  topMedalCode?: string
  topPointsMedalCode?: string
}

export interface StudentSearchDto {
  id: string
  firstName: string
  lastName: string
  classId: string | null
}

export interface PagedResult<T> {
  page: number
  pageSize: number
  totalCount: number
  items: T[]
}

export interface StudentReadingDto {
  assignedBookId: string
  bookTitle: string
  currentPage: number
  totalPages: number
  status: ProgressStatus
  endDateUtc: string | null
  isExpired: boolean
}

export interface StudentAssignmentDto {
  assignmentId: string
  title: string
  subject: string
  status: ProgressStatus
  dueDate: string
  isExpired: boolean
}

export interface StudentActivityDayDto {
  date: string
  pagesRead: number
  assignmentsCompleted: number
  pointsEarned: number
}

export interface StudentLearningActivityDto {
  learningActivityId: string
  title: string
  type: LearningActivityType
  status: ProgressStatus
  currentValue: number
  targetValue: number | null
  score: number | null
  dueDateUtc: string | null
  isExpired: boolean
}

export interface StudentDetailsDto {
  studentId: string
  studentName: string
  isActive: boolean
  lastActivityAt: string | null
  totalPagesRead: number
  completedAssignments: number
  totalPoints: number
  topMedalCode?: string
  topPointsMedalCode?: string
  reading: StudentReadingDto[]
  assignments: StudentAssignmentDto[]
  activityLast7Days: StudentActivityDayDto[]
  learningActivities: StudentLearningActivityDto[]
}

export interface StudentBadgeDto {
  code: string
  name: string
  description: string
  icon: string
  awardedAt: string
}

export interface StudentActivityDto {
  studentId: string
  studentName: string
  pagesReadToday: number
  assignmentsCompletedToday: number
  lastActivityAt: string | null
  isActiveToday: boolean
}

// Books
export interface BookDto {
  id: string
  title: string
  author: string
  gradeLevel: number
  totalPages: number
}

export interface BookCreateRequest {
  title: string
  author: string
  gradeLevel: number
  totalPages: number
}

export interface BookUpdateRequest {
  title: string
  author: string
  gradeLevel: number
  totalPages: number
}

export interface AssignedBookDto {
  id: string
  bookId: string
  title: string
  author: string
  totalPages: number
  startDateUtc: string
  endDateUtc: string
  points: number
  notStartedCount: number
  inProgressCount: number
  completedCount: number
  isExpired: boolean
}

export interface AssignBookRequest {
  bookId: string
  startDateUtc: string
  endDateUtc: string
  points: number
}

export interface AssignedBookStudentProgressDto {
  studentId: string
  studentName: string
  currentPage: number
  totalPages: number
  status: ProgressStatus
}

export interface AssignmentStudentProgressDto {
  studentId: string
  studentName: string
  status: ProgressStatus
}

// Assignments
export interface AssignmentDto {
  id: string
  title: string
  subject: string
  description: string
  dueDate: string
  points: number
  completedCount: number
  totalStudents: number
  isExpired: boolean
}

export interface AssignmentCreateRequest {
  title: string
  subject: string
  description: string
  dueDate: string
  points: number
}

// Challenges
export interface ChallengeDto {
  id: string
  title: string
  description: string
  targetType: TargetType
  targetValue: number
  startDate: string
  endDate: string
  points: number
  completedCount: number
  totalStudents: number
  isExpired: boolean
}

export interface ChallengeCreateRequest {
  title: string
  description: string
  targetType: TargetType
  targetValue: number
  startDate: string
  endDate: string
}

// Dashboard
export interface LeaderboardItemDto {
  studentId: string
  studentName: string
  points: number
  topMedalCode?: string
  topPointsMedalCode?: string
}

export interface TopReaderDto {
  studentId: string
  studentName: string
  pagesReadLast7Days: number
}

export interface StudentStreakDto {
  studentId: string
  studentName: string
  currentStreak: number
  bestStreak: number
}

export interface RecentBadgeDto {
  studentId: string
  studentName: string
  badgeCode: string
  badgeName: string
  badgeIcon: string
  awardedAt: string
}

export interface DashboardDto {
  classId: string
  className: string
  studentsCount: number
  activeTodayCount: number
  inactiveTodayCount: number
  totalPagesReadLast7Days: number
  completedAssignmentsLast7Days: number
  activeLearningActivitiesCount: number
  completedLearningActivitiesLast7Days: number
  leaderboard: LeaderboardItemDto[]
  topReaders: TopReaderDto[]
  bestStreaks: StudentStreakDto[]
  recentBadges: RecentBadgeDto[]
}

// Enums
export type ProgressStatus = 'NotStarted' | 'InProgress' | 'Completed'
export type TargetType = 'None' | 'Pages' | 'Books' | 'Assignments'
export type LearningActivityType = 'Reading' | 'Assignment' | 'Challenge'
