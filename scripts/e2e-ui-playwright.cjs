#!/usr/bin/env node
'use strict';

const { chromium } = require('playwright');

const UI_URL = process.env.CHUMMER_BLAZOR_BASE_URL || 'http://127.0.0.1:8089';

async function run() {
  const browser = await chromium.launch({ headless: true });
  const page = await browser.newPage();

  try {
    await page.goto(`${UI_URL}/`, { waitUntil: 'networkidle', timeout: 30000 });
    await page.waitForSelector('text=Import Character File', { timeout: 15000 });

    const rawImportDetails = page.locator('section.import details').first();
    const isOpen = await rawImportDetails.evaluate(element => element.hasAttribute('open'));
    if (!isOpen) {
      await page.locator('section.import summary').click();
    }

    await page.getByRole('button', { name: 'Import Raw XML' }).click();
    await page.waitForSelector('#openCharactersTree .command-button', { timeout: 15000 });

    await page.locator('#openCharactersTree .command-button').first().click();

    await page.locator('#tab-skills').click();
    await page.waitForFunction(() => {
      const title = document.querySelector('.section-preview h2');
      return title && title.textContent && title.textContent.toLowerCase().includes('skills');
    }, { timeout: 15000 });

    const nameInput = page.locator('section.metadata label:has-text("Name") input').first();
    const aliasInput = page.locator('section.metadata label:has-text("Alias") input').first();
    await nameInput.fill('Playwright Runner');
    await aliasInput.fill('PW');

    await page.getByRole('button', { name: 'Update Metadata' }).click();
    await page.waitForFunction(() => {
      const summaryName = document.querySelector('#summaryName');
      const summaryAlias = document.querySelector('#summaryAlias');
      return summaryName instanceof HTMLInputElement
        && summaryAlias instanceof HTMLInputElement
        && summaryName.value === 'Playwright Runner'
        && summaryAlias.value === 'PW';
    }, { timeout: 15000 });

    await page.locator('.commands .command-button:has-text("global_settings")').first().click();
    await page.waitForSelector('#dialogTitle', { timeout: 10000 });

    const dialogTitle = (await page.locator('#dialogTitle').textContent()) || '';
    if (!dialogTitle.toLowerCase().includes('global settings')) {
      throw new Error(`Expected Global Settings dialog, got '${dialogTitle}'.`);
    }

    await page.locator('#dialogClose').click();

    await page.getByRole('button', { name: 'Save Workspace' }).click();
    await page.waitForFunction(() => {
      const note = document.querySelector('.results .note');
      return note && note.textContent && note.textContent.toLowerCase().includes('workspace saved');
    }, { timeout: 15000 });

    console.log('playwright UI flow completed');
  } finally {
    await browser.close();
  }
}

run().catch(error => {
  console.error('playwright UI flow failed:', error instanceof Error ? error.stack || error.message : error);
  process.exitCode = 1;
});
