import { test, expect } from '@playwright/test'
import { loginAs, seed, trackConsoleErrors, significantErrors } from './helpers'

test.beforeEach(async ({ context }) => {
  await context.clearCookies()
})

test.describe('Login page', () => {
  test('renders brand, role selector and three role buttons', async ({ page }) => {
    await page.goto('/login')
    await expect(page.getByText('Учителски дневник')).toBeVisible()
    await expect(page.getByRole('button', { name: 'Учител', exact: false }).first()).toBeVisible()
    await expect(page.getByRole('button', { name: 'Ученик', exact: false }).first()).toBeVisible()
    await expect(page.getByRole('button', { name: 'Родител с ученик', exact: false }).first()).toBeVisible()
    // Login form should NOT be visible until a role is chosen.
    await expect(page.getByLabel('Имейл')).toHaveCount(0)
  })

  test('selecting a role reveals the login form', async ({ page }) => {
    await page.goto('/login')
    await page.getByRole('button', { name: 'Учител', exact: false }).first().click()
    await expect(page.getByLabel('Имейл')).toBeVisible()
    await expect(page.getByLabel('Парола')).toBeVisible()
    await expect(page.getByRole('button', { name: 'Вход', exact: true })).toBeVisible()
  })

  test('invalid credentials show an error message', async ({ page }) => {
    await page.goto('/login')
    await page.getByRole('button', { name: 'Учител', exact: false }).first().click()
    await page.getByLabel('Имейл').fill('nobody@example.com')
    await page.getByLabel('Парола').fill('wrongpass')
    await page.getByRole('button', { name: 'Вход', exact: true }).click()
    await expect(page.getByText('Невалиден имейл или парола.')).toBeVisible()
    await expect(page).toHaveURL(/\/login/)
  })

  test('logging in with the wrong role type is rejected', async ({ page }) => {
    const s = seed()
    await page.goto('/login')
    // Pick "Ученик" but use the teacher's credentials.
    await page.getByRole('button', { name: 'Ученик', exact: false }).first().click()
    await page.getByLabel('Имейл').fill(s.teacher.email)
    await page.getByLabel('Парола').fill(s.password)
    await page.getByRole('button', { name: 'Вход', exact: true }).click()
    await expect(page.getByText(/Тези данни не са за профил/)).toBeVisible()
  })

  test('teacher can log in and lands on classes', async ({ page }) => {
    const errors = trackConsoleErrors(page)
    const s = seed()
    await loginAs(page, 'Teacher', s.teacher.email, s.password)
    await expect(page.getByRole('link', { name: 'TeacherDiary' }).first()).toBeVisible()
    const sig = significantErrors(errors)
    expect(sig, `console errors: ${sig.join('\n')}`).toEqual([])
  })

  test('student can log in and lands on dashboard', async ({ page }) => {
    const s = seed()
    await loginAs(page, 'Student', s.student.email, s.password)
    await expect(page).toHaveURL(/\/student\/dashboard/)
  })

  test('parent can log in and lands on students', async ({ page }) => {
    const s = seed()
    await loginAs(page, 'Parent', s.parent.email, s.password)
    await expect(page).toHaveURL(/\/parent\/students/)
  })
})
