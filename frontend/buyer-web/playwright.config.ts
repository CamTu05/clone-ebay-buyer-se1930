import {defineConfig} from '@playwright/test'
export default defineConfig({testDir:'./e2e',workers:1,timeout:60000,use:{baseURL:process.env.E2E_BASE_URL??'http://127.0.0.1:5174',headless:true,launchOptions:process.env.E2E_BROWSER_PATH?{executablePath:process.env.E2E_BROWSER_PATH}:{}},reporter:'list'})
