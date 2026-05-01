// Auth
export interface AuthResponse {
  token: string
  userId: string
  email: string
  fullName: string
  role: 'Teacher' | 'Parent' | 'Student'
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

export interface RegisterStudentRequest {
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

export interface StudentActivityEntryDto {
  date: string
  description: string
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

export interface StudentChallengeDto {
  challengeId: string
  title: string
  description?: string
  targetDescription?: string
  targetValue: number
  currentValue: number
  started: boolean
  completed: boolean
  endDate: string
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
  activityLast7Days: StudentActivityEntryDto[]
  learningActivities: StudentLearningActivityDto[]
  challenges: StudentChallengeDto[]
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
export interface ChallengeStudentProgressDto {
  studentId: string
  studentName: string
  started: boolean
  completed: boolean
  currentValue: number
}

export interface ChallengeDto {
  id: string
  title: string
  description: string
  targetDescription?: string
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
  targetDescription?: string
  targetType: TargetType
  targetValue: number
  startDate: string
  endDate: string
  points: number
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

// Messages
export interface ConversationDto {
  otherUserId: string
  otherUserName: string
  studentName?: string
  lastMessage: string
  lastMessageAt: string
  unreadCount: number
  lastMessageIsFromMe: boolean
}

export interface MessageDto {
  id: string
  content: string | null
  imageUrl: string | null
  isFromMe: boolean
  isRead: boolean
  sentAt: string
}

export interface MessageContactDto {
  userId: string
  fullName: string
  studentName?: string
}

// Banners
export interface BannerDto {
  id: string
  imageUrl: string
  linkUrl: string | null
  isActive: boolean
  displayOrder: number
}

export interface BannerCreateRequest {
  imageUrl: string
  linkUrl: string
  displayOrder: number
}

// Notifications
export type NotificationType =
  | 'AssignmentCreated'
  | 'AssignmentCompleted'
  | 'AssignmentOverdue'
  | 'BookAssigned'
  | 'BookCompleted'
  | 'BookOverdue'
  | 'ChallengeCreated'
  | 'ChallengeCompleted'
  | 'BadgeEarned'
  | 'StreakReminder'
  | 'StreakBroken'
  | 'StudentJoinedClass'

export interface NotificationDto {
  id: string
  message: string
  type: NotificationType
  isRead: boolean
  navigationUrl: string | null
  createdAt: string
}

// Enums
export type ProgressStatus = 'NotStarted' | 'InProgress' | 'Completed'
export type TargetType = 'None' | 'Pages' | 'Books' | 'Assignments'
export type LearningActivityType = 'Reading' | 'Assignment' | 'Challenge'
