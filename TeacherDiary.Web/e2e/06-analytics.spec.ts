import { test, expect } from '@playwright/test'
import { loginAs, seed, trackConsoleErrors, significantErrors } from './helpers'

const SHOTS = 'e2e/screenshots'

test.beforeEach(async ({ context }) => {
  await context.clearCookies()
})

test.describe('Teacher analytics page', () => {
  test('renders the four analytics sections', async ({ page }) => {
    const s = seed()
    await loginAs(page, 'Teacher', s.teacher.email, s.password)
    await page.goto(`/teacher/classes/${s.teacher.classId}/analytics`)

    // The four card headers that make up the analytics dashboard.
    await expect(page.getByText(/Активност — последните/)).toBeVisible()
    await expect(page.getByText('Активност на учениците (последните 7 дни)')).toBeVisible()
    await expect(page.getByText('Изпълнение по предмети')).toBeVisible()
    await expect(page.getByText('Статистика за четене')).toBeVisible()

    await page.screenshot({ path: `${SHOTS}/teacher-analytics.png`, fullPage: true })
  })

  // KNOWN BUG (documented, expected to fail): ActivityBarChart computes
  // yTicks = [0, round(maxVal/2), maxVal]. When maxVal === 1 (low-activity
  // class) this is [0, 1, 1], producing a duplicate React key warning
  // ("Encountered two children with the same key, `1`"). Fix in
  // ClassAnalyticsPage.tsx: de-duplicate yTicks before mapping.
  test('analytics renders with no React duplicate-key warning', async ({ page }) => {
    test.fail(true, 'Known bug: ActivityBarChart emits duplicate key `1` when maxVal === 1')
    const errors = trackConsoleErrors(page)
    const s = seed()
    await loginAs(page, 'Teacher', s.teacher.email, s.password)
    await page.goto(`/teacher/classes/${s.teacher.classId}/analytics`)
    await expect(page.getByText('Статистика за четене')).toBeVisible()

    const sig = significantErrors(errors)
    expect(sig, `unexpected console errors:\n${sig.join('\n')}`).toEqual([])
  })

  test('engagement table lists the enrolled student and opens the detail drawer', async ({ page }) => {
    const s = seed()
    await loginAs(page, 'Teacher', s.teacher.email, s.password)
    await page.goto(`/teacher/classes/${s.teacher.classId}/analytics`)

    const studentCell = page.getByText('Георги', { exact: false }).first()
    await expect(studentCell).toBeVisible()
    await studentCell.click()

    // Drawer should open with a close affordance.
    await expect(page.getByRole('button', { name: 'Затвори' })).toBeVisible()
    await page.screenshot({ path: `${SHOTS}/teacher-analytics-drawer.png`, fullPage: true })
  })
})
