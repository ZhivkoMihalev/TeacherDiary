import { Page, expect } from '@playwright/test'
import { readFileSync } from 'node:fs'
import { dirname, join } from 'node:path'
import { fileURLToPath } from 'node:url'
import type { SeedData } from './global-setup'

const HERE = dirname(fileURLToPath(import.meta.url))

export function seed(): SeedData {
  return JSON.parse(readFileSync(join(HERE, '.seed.json'), 'utf-8'))
}

type Role = 'Teacher' | 'Student' | 'Parent'

const ROLE_LABEL: Record<Role, string> = {
  Teacher: 'Учител',
  Student: 'Ученик',
  Parent: 'Родител с ученик',
}

/**
 * Logs in through the real login UI: click the role panel, fill the form,
 * submit, and wait for the post-login route.
 */
export async function loginAs(page: Page, role: Role, email: string, password: string) {
  await page.goto('/login')
  // The role label buttons are the most stable click target.
  await page.getByRole('button', { name: ROLE_LABEL[role], exact: false }).first().click()

  await page.getByLabel('Имейл').fill(email)
  await page.getByLabel('Парола').fill(password)
  await page.getByRole('button', { name: 'Вход', exact: true }).click()

  const expectedPath =
    role === 'Teacher' ? /\/teacher\/classes/ :
    role === 'Student' ? /\/student\/dashboard/ :
    /\/parent\/students/
  await expect(page).toHaveURL(expectedPath, { timeout: 15_000 })
}

/**
 * Console-error noise that is environmental rather than a real app defect.
 * The SignalR negotiation is aborted whenever the layout unmounts / the test
 * navigates before the handshake finishes (the useEffect cleanup calls
 * connection.stop() mid-negotiate). It is logged on essentially every page and
 * is not a functional break, so we filter it out of the strict assertions while
 * still surfacing it in the report.
 */
const IGNORED_CONSOLE_PATTERNS = [
  /Failed to start the connection/i,
  /connection was stopped during negotiation/i,
  /Failed to complete negotiation/i,
]

export function isIgnoredConsoleError(text: string): boolean {
  return IGNORED_CONSOLE_PATTERNS.some((re) => re.test(text))
}

/** Captures any console errors emitted during the test for later assertion/report. */
export function trackConsoleErrors(page: Page): string[] {
  const errors: string[] = []
  page.on('console', (msg) => {
    if (msg.type() === 'error') errors.push(msg.text())
  })
  page.on('pageerror', (err) => errors.push(`pageerror: ${err.message}`))
  return errors
}

/** Console errors with the known environmental SignalR noise filtered out. */
export function significantErrors(errors: string[]): string[] {
  return errors.filter((e) => !isIgnoredConsoleError(e))
}
