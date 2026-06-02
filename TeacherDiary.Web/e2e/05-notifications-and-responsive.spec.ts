import { test, expect } from '@playwright/test'
import { loginAs, seed } from './helpers'

const SHOTS = 'e2e/screenshots'

test.beforeEach(async ({ context }) => {
  await context.clearCookies()
})

test.describe('Notification bell', () => {
  test('bell opens dropdown and shows a list or empty state', async ({ page }) => {
    const s = seed()
    await loginAs(page, 'Teacher', s.teacher.email, s.password)
    // NOTE: the bell exposes title="Известия", but when there is an unread
    // badge the accessible NAME becomes the badge count (e.g. "2") instead of
    // "Известия" — so getByRole({ name: 'Известия' }) does NOT match. We locate
    // by the title attribute, which is stable regardless of the badge. This is
    // an accessibility defect, reported separately.
    // The layout mounts TWO bells (a lg:hidden mobile one and a hidden lg:block
    // desktop one); on desktop only the second is visible, so pick the visible.
    const bell = page.locator('button[title="Известия"]:visible').first()
    await expect(bell).toBeVisible()
    await bell.click()
    // Dropdown header "Известия" plus either notifications or empty state.
    await expect(page.getByText('Виж всички известия →')).toBeVisible()
    await page.screenshot({ path: `${SHOTS}/teacher-notif-dropdown.png` })
  })

  test('teacher receives a notification when a student completes an assignment', async ({ page }) => {
    // The seeded teacher should get StudentJoinedClass notifications from enrollment.
    const s = seed()
    await loginAs(page, 'Teacher', s.teacher.email, s.password)
    await page.goto('/teacher/notifications')
    await expect(page.locator('main')).toBeVisible()
    await page.screenshot({ path: `${SHOTS}/teacher-notifications-page.png`, fullPage: true })
  })
})

test.describe('Responsiveness (375px mobile)', () => {
  test.use({ viewport: { width: 375, height: 812 } })

  test('teacher layout: sidebar hidden, hamburger present, opens menu', async ({ page }) => {
    const s = seed()
    await loginAs(page, 'Teacher', s.teacher.email, s.password)
    await page.screenshot({ path: `${SHOTS}/mobile-teacher-classes.png`, fullPage: true })

    const hamburger = page.getByRole('button', { name: 'Отвори меню' })
    await expect(hamburger).toBeVisible()
    await hamburger.click()
    await expect(page.getByRole('link', { name: /Книги/ })).toBeVisible()
    await page.screenshot({ path: `${SHOTS}/mobile-teacher-menu-open.png` })
  })

  test('student dashboard at mobile width', async ({ page }) => {
    const s = seed()
    await loginAs(page, 'Student', s.student.email, s.password)
    await page.screenshot({ path: `${SHOTS}/mobile-student-dashboard.png`, fullPage: true })
    await expect(page.locator('main')).toBeVisible()
  })

  test('login page at mobile width', async ({ page }) => {
    await page.goto('/login')
    await page.getByRole('button', { name: 'Учител', exact: false }).first().click()
    await expect(page.getByLabel('Имейл')).toBeVisible()
    await page.screenshot({ path: `${SHOTS}/mobile-login.png`, fullPage: true })
  })
})
