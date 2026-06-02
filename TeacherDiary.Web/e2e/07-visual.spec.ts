import { test, expect } from '@playwright/test'
import { loginAs, seed } from './helpers'

// Visual regression baselines for key screens. First run creates the baseline
// PNGs under e2e/07-visual.spec.ts-snapshots/; subsequent runs diff against them.
// Masks cover the rotating AdSidebar banner (changes every 10s / navigation),
// which would otherwise produce false positives.

test.beforeEach(async ({ context }) => {
  await context.clearCookies()
})

const ad = (page: import('@playwright/test').Page) =>
  page.locator('aside:has-text("Реклами")')

test.describe('Visual regression', () => {
  test('login page (role not yet chosen)', async ({ page }) => {
    await page.goto('/login')
    await expect(page.getByRole('button', { name: 'Учител', exact: false }).first()).toBeVisible()
    await expect(page).toHaveScreenshot('login.png', { maxDiffPixelRatio: 0.02 })
  })

  test('teacher class dashboard', async ({ page }) => {
    const s = seed()
    await loginAs(page, 'Teacher', s.teacher.email, s.password)
    await page.goto(`/teacher/classes/${s.teacher.classId}/dashboard`)
    await expect(page.getByText('Активни днес')).toBeVisible()
    await expect(page).toHaveScreenshot('teacher-dashboard.png', {
      fullPage: true,
      mask: [ad(page)],
      maxDiffPixelRatio: 0.02,
    })
  })

  test('student dashboard', async ({ page }) => {
    const s = seed()
    await loginAs(page, 'Student', s.student.email, s.password)
    await expect(page.locator('main')).toBeVisible()
    await expect(page).toHaveScreenshot('student-dashboard.png', {
      fullPage: true,
      mask: [ad(page)],
      maxDiffPixelRatio: 0.02,
    })
  })

  test('parent children page', async ({ page }) => {
    const s = seed()
    await loginAs(page, 'Parent', s.parent.email, s.password)
    await expect(page.getByText('Ани').first()).toBeVisible()
    await expect(page).toHaveScreenshot('parent-children.png', {
      fullPage: true,
      mask: [ad(page)],
      maxDiffPixelRatio: 0.02,
    })
  })
})
