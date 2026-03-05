#!/usr/bin/env node
'use strict';

const { chromium } = require('playwright');

const UI_URL = process.env.CHUMMER_BLAZOR_BASE_URL || 'http://127.0.0.1:8089';
const SAMPLE_CHARACTER_FILE = process.env.CHUMMER_UI_SAMPLE_FILE || '/work/testdata/BLUE.chum5';
const NAVIGATION_WAIT_UNTIL = process.env.CHUMMER_UI_NAV_WAIT_UNTIL || 'domcontentloaded';

async function run() {
  const browser = await chromium.launch({ headless: true });
  const page = await browser.newPage();

  try {
    await page.goto(`${UI_URL}/`, { waitUntil: NAVIGATION_WAIT_UNTIL, timeout: 30000 });
    await page.waitForSelector('text=Import Character File', { timeout: 15000 });

    const workspaceButtons = page.locator('#openCharactersTree .command-button');
    if (await workspaceButtons.count() === 0) {
      const waitForImportOutcome = async (timeout) => {
        const handle = await page.waitForFunction(() => {
          const workspaceButton = document.querySelector('#openCharactersTree .command-button');
          if (workspaceButton) {
            return 'workspace-ready';
          }

          const importError = document.querySelector('section.import .error');
          if (importError && importError.textContent && importError.textContent.trim().length > 0) {
            return 'import-error';
          }

          const resultError = document.querySelector('.results .error');
          if (resultError && resultError.textContent && resultError.textContent.trim().length > 0) {
            return 'result-error';
          }

          const serviceState = document.querySelector('#serviceState');
          if (serviceState && serviceState.textContent && serviceState.textContent.toLowerCase().includes('error')) {
            return 'service-error';
          }

          return '';
        }, { timeout });
        return handle.jsonValue();
      };

      const importInput = page.locator('section.import input[type="file"]').first();
      await importInput.setInputFiles(SAMPLE_CHARACTER_FILE);

      let importOutcome = '';
      try {
        importOutcome = await waitForImportOutcome(45000);
      } catch (error) {
        importOutcome = '';
      }

      if (importOutcome !== 'workspace-ready') {
        const readOptionalText = async (selector) => {
          const locator = page.locator(selector).first();
          if (await locator.count() === 0) {
            return '';
          }

          return (await locator.textContent()) || '';
        };

        const importError = await readOptionalText('section.import .error');
        const resultError = await readOptionalText('.results .error');
        const resultNote = await readOptionalText('.results .note');
        const importPanelHtml = await page.locator('section.import').first().innerHTML();
        const resultPanelHtml = await page.locator('.results').first().innerHTML();
        throw new Error(
          `Import did not produce workspace (${importOutcome || 'timeout'}). ` +
          `importError='${(importError || '').trim()}' resultError='${(resultError || '').trim()}' note='${(resultNote || '').trim()}'. ` +
          `importPanel='${importPanelHtml.replace(/\s+/g, ' ').trim()}' ` +
          `resultPanel='${resultPanelHtml.replace(/\s+/g, ' ').trim()}'`);
      }
    }

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

    const settingsButton = page.locator('.commands .command-button:has-text("global_settings")').first();
    await settingsButton.waitFor({ state: 'visible', timeout: 15000 });
    for (let attempt = 0; attempt < 40; attempt += 1) {
      if (!(await settingsButton.isDisabled())) {
        break;
      }

      await page.waitForTimeout(250);
    }

    if (await settingsButton.isDisabled()) {
      throw new Error('global_settings command stayed disabled for too long.');
    }

    await settingsButton.click();
    await page.waitForSelector('#dialogTitle', { timeout: 20000 });

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
