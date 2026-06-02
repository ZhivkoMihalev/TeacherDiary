import { request } from '@playwright/test'
import { writeFileSync } from 'node:fs'
import { dirname, join } from 'node:path'
import { fileURLToPath } from 'node:url'

const HERE = dirname(fileURLToPath(import.meta.url))

const API = 'http://localhost:5000/api'
const PASSWORD = 'Test123!'

export interface SeedData {
  password: string
  teacher: { email: string; classId: string; bookTitle: string }
  parent: { email: string; childId: string }
  student: { email: string }
}

/**
 * Seeds a full slice of data through the live API so the UI tests have
 * something real to render: a teacher with a class + book + assignment +
 * challenge, a self-registered student enrolled in that class, and a parent
 * with a child also enrolled.
 */
async function globalSetup() {
  const stamp = Date.now()
  // Note: Playwright resolves a leading-slash path against the origin only,
  // dropping the "/api" path segment of baseURL. Use a trailing slash + no
  // leading slash so relative resolution keeps "/api/".
  const ctx = await request.newContext({ baseURL: API + '/' })

  const teacherEmail = `qa.teacher.${stamp}@example.com`
  const studentEmail = `qa.student.${stamp}@example.com`
  const parentEmail = `qa.parent.${stamp}@example.com`

  // --- Teacher ---
  const teacherRes = await post(ctx, '/auth/register-teacher', {
    firstName: 'Мария', lastName: 'Иванова', email: teacherEmail, password: PASSWORD,
  })
  const teacherToken = teacherRes.token
  const tAuth = { Authorization: `Bearer ${teacherToken}` }

  const cls = await post(ctx, '/classes', { name: '3А', grade: 3, schoolYear: '2025/2026' }, tAuth)
  const classId: string = cls.id

  // Book + assign to class
  const bookTitle = `Под игото ${stamp}`
  const book = await post(ctx, '/books', {
    title: bookTitle, author: 'Иван Вазов', gradeLevel: 3, totalPages: 200,
  }, tAuth)
  const bookId: string = book.bookId ?? book.id
  const nowIso = new Date().toISOString()
  const inTwoWeeks = new Date(Date.now() + 14 * 864e5).toISOString()
  await post(ctx, `/reading/${classId}/assigned-books`, {
    bookId, startDateUtc: nowIso, endDateUtc: inTwoWeeks, points: 50,
  }, tAuth)

  // Assignment
  await post(ctx, `/classes/${classId}/assignments`, {
    title: 'Домашно по математика', subject: 'Математика',
    description: 'Реши задачи 1-10 от страница 25', dueDate: inTwoWeeks, points: 20,
  }, tAuth)

  // Challenge (TargetType: 0 = Pages assumed)
  await post(ctx, `/classes/${classId}/challenges`, {
    title: 'Прочети 100 страници', description: 'Прочети 100 страници до края на месеца',
    targetDescription: '100 страници', targetType: 0, targetValue: 100,
    startDate: nowIso, endDate: inTwoWeeks, points: 30,
  }, tAuth)

  // --- Student (self-register; creates own profile) ---
  await post(ctx, '/auth/register-student', {
    firstName: 'Георги', lastName: 'Петров', email: studentEmail, password: PASSWORD,
  })

  // Teacher finds the student by name and enrolls them in the class
  const search = await get(ctx, `/students/search?name=${encodeURIComponent('Петров')}&page=1&pageSize=20`, tAuth)
  const studentItems = search.items ?? search.data ?? []
  const found = studentItems.find((s: any) => `${s.firstName} ${s.lastName}`.includes('Георги'))
  if (found) {
    await post(ctx, `/classes/${classId}/students/${found.id}`, {}, tAuth)
  } else {
    console.warn('[seed] could not find self-registered student in search results')
  }

  // --- Parent + child ---
  const parentRes = await post(ctx, '/auth/register-parent', {
    firstName: 'Стоян', lastName: 'Колев', email: parentEmail, password: PASSWORD,
  })
  const pAuth = { Authorization: `Bearer ${parentRes.token}` }
  const child = await post(ctx, '/parent/students', { firstName: 'Ани', lastName: 'Колева' }, pAuth)
  const childId: string = child.id ?? child.studentId ?? child

  // Teacher enrolls the parent's child too
  if (typeof childId === 'string') {
    await post(ctx, `/classes/${classId}/students/${childId}`, {}, tAuth).catch(() => {})
  }

  const seed: SeedData = {
    password: PASSWORD,
    teacher: { email: teacherEmail, classId, bookTitle },
    parent: { email: parentEmail, childId: typeof childId === 'string' ? childId : '' },
    student: { email: studentEmail },
  }

  writeFileSync(join(HERE, '.seed.json'), JSON.stringify(seed, null, 2))
  console.log('[seed] done', seed)
  await ctx.dispose()
}

async function post(ctx: any, path: string, body: any, headers: Record<string, string> = {}) {
  const res = await ctx.post(path.replace(/^\//, ''), { data: body, headers })
  if (!res.ok()) {
    throw new Error(`POST ${path} -> ${res.status()} ${await res.text()}`)
  }
  const text = await res.text()
  return text ? JSON.parse(text) : {}
}

async function get(ctx: any, path: string, headers: Record<string, string> = {}) {
  const res = await ctx.get(path.replace(/^\//, ''), { headers })
  if (!res.ok()) {
    throw new Error(`GET ${path} -> ${res.status()} ${await res.text()}`)
  }
  const text = await res.text()
  return text ? JSON.parse(text) : {}
}

export default globalSetup
