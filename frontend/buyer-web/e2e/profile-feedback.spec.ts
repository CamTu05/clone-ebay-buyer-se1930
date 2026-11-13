import { expect, test } from '@playwright/test'

const password = process.env.BUYER_TEST_PASSWORD ?? 'Demo@12345'

async function login(page: import('@playwright/test').Page) {
  await page.goto('/login')
  await page.getByLabel('Email', { exact: true }).fill('seed.buyer01@example.test')
  await page.getByLabel('Password', { exact: true }).fill(password)
  await page.getByRole('button', { name: 'Sign in', exact: true }).click()
  await expect(page).toHaveURL('/')
}

const profile = { success: true, message: 'Success', errors: null, data: { id: 1, username: 'Demo Buyer 1', email: 'seed.buyer01@example.test', phoneNumber: null, avatarUrl: null, emailVerified: true, phoneVerified: false } }

async function openProfile(page: import('@playwright/test').Page) {
  await page.route('**/api/me', route => route.fulfill({ json: profile }))
  await page.getByRole('link', { name: 'Profile', exact: true }).first().click()
}

test('buyer profile renders received positive feedback and pagination', async ({ page }) => {
  await login(page)
  await page.route('**/api/me/feedback?**', route => {
    const pageNumber = new URL(route.request().url()).searchParams.get('page')
    return route.fulfill({ json: { success: true, message: 'Success', errors: null, data: { positiveFeedbackCount: 3, items: [{ id: Number(pageNumber), orderId: 101, seller: { id: 20, username: pageNumber === '2' ? 'fashion_shop' : 'camera_store', avatarUrl: null }, purchaseItems: [{ orderItemId: 201, listingId: 301, title: 'Canon EOS camera', imageUrl: null, unitPrice: 500000 }], currency: 'VND', verifiedPurchase: true, comment: pageNumber === '2' ? 'Excellent buyer. Smooth transaction.' : 'Great buyer, prompt payment. Thank you!', createdAt: '2026-11-01T10:00:00Z' }], page: Number(pageNumber), pageSize: 10, totalItems: 11, totalPages: 2 } } })
  })
  await openProfile(page)
  await expect(page.getByText('Positive feedback: 3')).toBeVisible()
  await expect(page.getByText('camera_store')).toBeVisible()
  await expect(page.getByText(/Great buyer, prompt payment/)).toBeVisible()
  await expect(page.getByText('Canon EOS camera')).toBeVisible()
  await expect(page.getByText('Verified purchase')).toBeVisible()
  await expect(page.getByRole('link', { name: 'Order #101' })).toHaveAttribute('href', '/orders?orderId=101')
  await page.getByRole('button', { name: 'Next' }).click()
  await expect(page.getByText('fashion_shop')).toBeVisible()
})

test('buyer feedback supports loading, empty and error retry states', async ({ page }) => {
  await login(page)
  let attempt = 0
  await page.route('**/api/me/feedback?**', async route => {
    attempt += 1
    if (attempt === 1) return route.fulfill({ status: 503, json: { success: false, message: 'Unavailable', data: null, errors: [] } })
    return route.fulfill({ json: { success: true, message: 'Success', errors: null, data: { positiveFeedbackCount: 0, items: [], page: 1, pageSize: 10, totalItems: 0, totalPages: 0 } } })
  })
  await openProfile(page)
  await expect(page.getByRole('alert')).toContainText('server is unavailable')
  await page.getByRole('button', { name: 'Try again' }).click()
  await expect(page.getByText('Positive feedback: 0')).toBeVisible()
  await expect(page.getByText('No feedback received yet.')).toBeVisible()
})
