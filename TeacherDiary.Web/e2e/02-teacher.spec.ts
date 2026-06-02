import { test, expect } from '@playwright/test'
import { loginAs, seed, trackConsoleErrors, significantErrors } from './helpers'

const SHOTS = 'e2e/screenshots'

test.beforeEach(async ({ context }) => {
  await context.clearCookies()
})

test.describe('Teacher flows', () => {
  test('classes list shows the seeded class', async ({ page }) => {
    const s = seed()
    await loginAs(page, 'Teacher', s.teacher.email, s.password)
    await expect(page.getByText('3А').first()).toBeVisible()
    await page.screenshot({ path: `${SHOTS}/teacher-classes.png`, fullPage: true })
  })

  test('class dashboard (Дневник) renders stat cards', async ({ page }) => {
    const errors = trackConsoleErrors(page)
    const s = seed()
    await loginAs(page, 'Teacher', s.teacher.email, s.password)
    await page.goto(`/teacher/classes/${s.teacher.classId}/dashboard`)
    await expect(page.getByText('Активни днес')).toBeVisible()
    await expect(page.getByText(/Прочетени страници/)).toBeVisible()
    await page.screenshot({ path: `${SHOTS}/teacher-dashboard.png`, fullPage: true })
    const sig = significantErrors(errors)
    expect(sig, sig.join('\n')).toEqual([])
  })

  test('navigate through all class tabs', async ({ page }) => {
    const s = seed()
    await loginAs(page, 'Teacher', s.teacher.email, s.password)
    const base = `/teacher/classes/${s.teacher.classId}`

    for (const [tab, path, marker] of [
      ['Ученици', `${base}/students`, /Георги|Ани|Ученик|ученик/],
      ['Четене', `${base}/reading`, /Под игото|Книг|книг/],
      ['Задачи', `${base}/assignments`, /Домашно|задач|Задач/],
      ['Предизвикателства', `${base}/challenges`, /страници|предизвик|Предизвик/],
      ['Аналитики', `${base}/analytics`, /./],
    ] as const) {
      await page.goto(path)
      await expect(page).toHaveURL(new RegExp(path.replace(/[/]/g, '\\/')))
      // Page should render some content, not crash to a blank screen.
      await expect(page.locator('body')).toContainText(marker)
      await page.screenshot({ path: `${SHOTS}/teacher-${tab}.png`, fullPage: true })
    }
  })

  test('students tab lists enrolled students', async ({ page }) => {
    const s = seed()
    await loginAs(page, 'Teacher', s.teacher.email, s.password)
    await page.goto(`/teacher/classes/${s.teacher.classId}/students`)
    await expect(page.getByText('Георги').first()).toBeVisible()
  })

  test('reading tab shows the assigned book', async ({ page }) => {
    const s = seed()
    await loginAs(page, 'Teacher', s.teacher.email, s.password)
    await page.goto(`/teacher/classes/${s.teacher.classId}/reading`)
    await expect(page.getByText(/Под игото/).first()).toBeVisible()
  })

  test('assignments tab shows the created assignment', async ({ page }) => {
    const s = seed()
    await loginAs(page, 'Teacher', s.teacher.email, s.password)
    await page.goto(`/teacher/classes/${s.teacher.classId}/assignments`)
    await expect(page.getByText('Домашно по математика').first()).toBeVisible()
  })

  test('books library page renders', async ({ page }) => {
    const s = seed()
    await loginAs(page, 'Teacher', s.teacher.email, s.password)
    await page.getByRole('link', { name: /Книги/ }).click()
    await expect(page).toHaveURL(/\/teacher\/books/)
    await expect(page.getByText(/Под игото/).first()).toBeVisible()
    await page.screenshot({ path: `${SHOTS}/teacher-books.png`, fullPage: true })
  })

  test('sidebar navigation: notifications and messages', async ({ page }) => {
    const s = seed()
    await loginAs(page, 'Teacher', s.teacher.email, s.password)
    await page.getByRole('link', { name: /Известия/ }).click()
    await expect(page).toHaveURL(/\/teacher\/notifications/)
    await page.getByRole('link', { name: /Съобщения/ }).click()
    await expect(page).toHaveURL(/\/teacher\/messages/)
  })
})
