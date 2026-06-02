import { test, expect } from '@playwright/test'
import { loginAs, seed, trackConsoleErrors, significantErrors } from './helpers'

const SHOTS = 'e2e/screenshots'

test.beforeEach(async ({ context }) => {
  await context.clearCookies()
})

test.describe('Parent flows', () => {
  test('my children page lists the seeded child', async ({ page }) => {
    const errors = trackConsoleErrors(page)
    const s = seed()
    await loginAs(page, 'Parent', s.parent.email, s.password)
    await expect(page.getByText('Ани').first()).toBeVisible()
    await page.screenshot({ path: `${SHOTS}/parent-children.png`, fullPage: true })
    const sig = significantErrors(errors)
    expect(sig, sig.join('\n')).toEqual([])
  })

  test('open child progress view', async ({ page }) => {
    const s = seed()
    await loginAs(page, 'Parent', s.parent.email, s.password)
    if (s.parent.childId) {
      await page.goto(`/parent/students/${s.parent.childId}`)
      await expect(page.locator('main')).toBeVisible()
      await page.screenshot({ path: `${SHOTS}/parent-child-progress.png`, fullPage: true })
    }
  })

  test('navigation: notifications and messages', async ({ page }) => {
    const s = seed()
    await loginAs(page, 'Parent', s.parent.email, s.password)
    await page.getByRole('link', { name: /Известия/ }).click()
    await expect(page).toHaveURL(/\/parent\/notifications/)
    await page.getByRole('link', { name: /Съобщения/ }).click()
    await expect(page).toHaveURL(/\/parent\/messages/)
  })
})
