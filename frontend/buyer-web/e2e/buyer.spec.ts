import {test,expect} from '@playwright/test'
const password=process.env.BUYER_TEST_PASSWORD??'Demo@12345'
test.beforeAll(()=>{if(process.env.E2E_ALLOW_MUTATIONS!=='isolated-database')throw Error('Set E2E_ALLOW_MUTATIONS=isolated-database against a dedicated test API.')})
test('guest cart, register, verify, merge, save, restore and PayPal checkout',async({page,request})=>{
 const suffix=Date.now().toString(36),email=`e2e.${suffix}@example.test`
 await page.goto('/products/1');await page.getByRole('button',{name:'Add to cart',exact:true}).click();await expect(page).toHaveURL(/\/cart/)
 await page.goto('/register');await page.getByLabel('Email',{exact:true}).fill(email);await page.getByLabel('Username',{exact:true}).fill('buyer_'+suffix);await page.getByLabel('Password',{exact:true}).fill(password);await page.getByRole('button',{name:'Continue',exact:true}).click();await expect(page.getByRole('status')).toContainText('Check your email')
 const mailbox=await request.get((process.env.E2E_API_URL??'http://localhost:5139')+'/api/auth/dev-mailbox');const mails=(await mailbox.json()).data;const body=mails.find((m:{recipient:string})=>m.recipient===email).body;const token=body.match(/token=([A-F0-9]{64})/)[1]
 await page.goto('/verify-email?token='+token);await page.getByRole('button',{name:'Continue',exact:true}).click();await expect(page.getByRole('status')).toContainText('Email verified')
 await page.goto('/login');await page.getByLabel('Email',{exact:true}).fill(email);await page.getByLabel('Password',{exact:true}).fill(password);await page.getByRole('button',{name:'Sign in',exact:true}).click();await expect(page).toHaveURL('/')
 // Navigate with client-side links to retain the intentionally in-memory login session.
 await page.getByRole('link',{name:'Shopping cart',exact:true}).first().click();await page.getByRole('button',{name:'Save for later',exact:true}).click();await expect(page.getByRole('button',{name:'Move to cart',exact:true})).toBeVisible();await page.getByRole('button',{name:'Move to cart',exact:true}).click();await expect(page.getByRole('button',{name:'Go to checkout',exact:true})).toBeEnabled()
 // Address creation is exercised via API then checkout is visited with client navigation.
 const addressResponse=page.waitForResponse(r=>r.url().endsWith('/api/addresses')&&r.request().method()==='POST')
 await page.getByRole('link',{name:'Shipping addresses',exact:true}).first().click();await page.getByRole('button',{name:/Add.*address/i}).first().click()
 await page.getByLabel('Full name',{exact:true}).fill('E2E Buyer');await page.getByLabel('Phone number',{exact:true}).fill('0901234567');await page.getByLabel('Street address',{exact:true}).fill('123 Test street');await page.getByLabel('City / town',{exact:true}).fill('Hanoi')
 await page.getByLabel('Country / region',{exact:true}).selectOption('VN');await page.getByRole('button',{name:/Save address/i}).click();await addressResponse
 await page.getByRole('link',{name:'Shopping cart',exact:true}).first().click();await page.getByRole('button',{name:'Go to checkout',exact:true}).click();await expect(page.getByRole('button',{name:'Confirm and pay',exact:true})).toBeEnabled();await page.getByRole('button',{name:'Confirm and pay',exact:true}).click();await expect(page.getByText(/PENDING ·/)).toBeVisible();await page.getByRole('button',{name:'Simulate successful payment',exact:true}).click();await expect(page.getByText(/CONFIRMED ·/)).toBeVisible()
})
test('Buy It Now guest PayPal failure and retry preserve unrelated cart',async({page})=>{
 await page.goto('/products/2');await page.getByRole('button',{name:'Add to cart',exact:true}).click();await page.goto('/products/3');await page.getByRole('button',{name:'Buy It Now',exact:true}).click();await expect(page).toHaveURL(/listingId=3/)
 for(const [label,value] of Object.entries({'Email':'guest.e2e@example.test','Full name':'Guest E2E','Phone':'0901234567','Street':'123 Test street','City':'Hanoi','Country code':'VN'}))await page.getByLabel(label,{exact:true}).fill(value)
 await page.getByRole('button',{name:'Confirm and pay',exact:true}).click();await page.getByRole('button',{name:'Simulate failed payment',exact:true}).click();await expect(page.getByText(/PENDING ·/)).toBeVisible();await page.getByRole('button',{name:'Simulate successful payment',exact:true}).click();await expect(page.getByText(/CONFIRMED ·/)).toBeVisible();await page.goto('/cart');await expect(page.getByRole('link',{name:/Samsung Galaxy phone Demo 2/})).toBeVisible()
})
