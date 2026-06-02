import { test, expect } from '@playwright/test'
import { loginAs, seed, trackConsoleErrors, significantErrors } from './helpers'

const SHOTS = 'e2e/screenshots'

test.beforeEach(async ({ context }) => {
  await context.clearCookies()
})

test.describe('Student flows', () => {
  test('dashboard / gamification hub renders without console errors', async ({ page }) => {
    const errors = trackConsoleErrors(page)
    const s = seed()
    await loginAs(page, 'Student', s.student.email, s.password)
    await expect(page).toHaveURL(/\/student\/dashboard/)
    // Dashboard should render meaningful content (points / streak / activities area).
    await expect(page.locator('main')).toBeVisible()
    await page.screenshot({ path: `${SHOTS}/student-dashboard.png`, fullPage: true })
    const sig = significantErrors(errors)
    expect(sig, sig.join('\n')).toEqual([])
  })

  test('badges page renders', async ({ page }) => {
    const s = seed()
    await loginAs(page, 'Student', s.student.email, s.password)
    await page.getByRole('link', { name: /Значки/ }).click()
    await expect(page).toHaveURL(/\/student\/badges/)
    await expect(page.locator('main')).toBeVisible()
    await page.screenshot({ path: `${SHOTS}/student-badges.png`, fullPage: true })
  })

  test('navigation: notifications and messages', async ({ page }) => {
    const s = seed()
    await loginAs(page, 'Student', s.student.email, s.password)
    await page.getByRole('link', { name: /Известия/ }).click()
    await expect(page).toHaveURL(/\/student\/notifications/)
    await page.getByRole('link', { name: /Съобщения/ }).click()
    await expect(page).toHaveURL(/\/student\/messages/)
  })

  test('logout returns to login', async ({ page }) => {
    const s = seed()
    await loginAs(page, 'Student', s.student.email, s.password)
    await page.getByRole('button', { name: /Изход/ }).click()
    await expect(page).toHaveURL(/\/login/)
  })
})
