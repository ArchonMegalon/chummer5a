#!/usr/bin/env node
'use strict';

const baseUrl = (process.env.PORTAL_BASE_URL || 'http://127.0.0.1:8091').replace(/\/+$/, '');
const verifyProxies = String(process.env.PORTAL_VERIFY_PROXIES || '').toLowerCase() === '1';

const requiredLandingLinks = [
  '/what-is-chummer',
  '/now',
  '/horizons',
  '/downloads',
  '/participate',
  '/status',
  '/artifacts',
  '/home'
];

const checks = [
  {
    url: `${baseUrl}/`,
    assert: text =>
      text.includes('Shadowrun rules truth, with receipts.') &&
      requiredLandingLinks.every(link => text.includes(link))
  },
  {
    url: `${baseUrl}/what-is-chummer`,
    assert: text =>
      text.includes('What Is Chummer?') &&
      text.includes('What the product is trying to become')
  },
  {
    url: `${baseUrl}/now`,
    assert: text =>
      text.includes('What Is Real Today') &&
      text.includes('Deterministic rules truth')
  },
  {
    url: `${baseUrl}/horizons`,
    assert: text =>
      text.includes('Coming Next') &&
      text.includes('KARMA FORGE') &&
      text.includes('RUNSITE')
  },
  {
    url: `${baseUrl}/participate`,
    assert: text =>
      text.includes('How participation works') &&
      text.includes('bounded booster path')
  },
  {
    url: `${baseUrl}/status`,
    assert: text =>
      text.includes('Public Status') &&
      text.includes('Available now')
  },
  {
    url: `${baseUrl}/artifacts`,
    assert: text =>
      text.includes('Featured Artifacts') &&
      text.includes('Runsite pack')
  },
  {
    url: `${baseUrl}/home`,
    assert: text =>
      text.includes('Sign in to unlock overlays') &&
      text.includes('Participate')
  },
  {
    url: `${baseUrl}/api/public/landing`,
    assert: text => {
      const payload = JSON.parse(text);
      return payload?.surface === 'chummer.run'
        && payload?.headline === 'Shadowrun rules truth, with receipts.'
        && Array.isArray(payload?.featureCards);
    }
  },
  {
    url: `${baseUrl}/downloads/releases.json`,
    assert: text => {
      const payload = JSON.parse(text);
      return typeof payload?.version === 'string'
        && typeof payload?.status === 'string'
        && typeof payload?.source === 'string'
        && Array.isArray(payload?.downloads);
    }
  },
  {
    url: `${baseUrl}/downloads/`,
    assert: text =>
      text.includes('Desktop Downloads') &&
      text.includes('/downloads/releases.json') &&
      text.includes('fallback-link')
  }
];

if (verifyProxies) {
  checks.push(
    {
      url: `${baseUrl}/blazor/health`,
      assert: text => {
        const payload = JSON.parse(text);
        return payload?.pathBase === '/blazor' && payload?.ok === true;
      }
    },
    {
      url: `${baseUrl}/blazor/`,
      assert: text => /<base href="[^"]*\/blazor\/"/i.test(text)
    },
    {
      url: `${baseUrl}/blazor/deep-link-check`,
      assert: text => /<base href="[^"]*\/blazor\/"/i.test(text)
    },
    {
      url: `${baseUrl}/hub/health`,
      assert: text => {
        const payload = JSON.parse(text);
        return payload?.head === 'hub-web' && payload?.pathBase === '/hub' && payload?.ok === true;
      }
    },
    {
      url: `${baseUrl}/hub/`,
      assert: text => /<base href="[^"]*\/hub\/"/i.test(text) && text.includes('ChummerHub Web')
    },
    {
      url: `${baseUrl}/session/health`,
      assert: text => {
        const payload = JSON.parse(text);
        return payload?.head === 'session-web' && payload?.pathBase === '/session' && payload?.ok === true;
      }
    },
    {
      url: `${baseUrl}/session/`,
      assert: text => /<base href="[^"]*\/session\/"/i.test(text) && text.includes('Chummer Session Web')
    },
    {
      url: `${baseUrl}/coach/health`,
      assert: text => {
        const payload = JSON.parse(text);
        return payload?.head === 'coach-web' && payload?.pathBase === '/coach' && payload?.ok === true;
      }
    },
    {
      url: `${baseUrl}/coach/`,
      assert: text => /<base href="[^"]*\/coach\/"/i.test(text) && text.includes('Chummer Coach')
    },
    {
      url: `${baseUrl}/avalonia/`,
      assert: text => text.includes('Avalonia Browser Host')
    },
    {
      url: `${baseUrl}/avalonia/health`,
      assert: text => {
        const payload = JSON.parse(text);
        return payload?.head === 'avalonia-browser' && payload?.pathBase === '/avalonia' && payload?.ok === true;
      }
    },
    {
      method: 'POST',
      url: `${baseUrl}/blazor/_blazor/negotiate?negotiateVersion=1`,
      headers: {
        'Content-Type': 'text/plain;charset=UTF-8'
      },
      body: '',
      assert: text => {
        const payload = JSON.parse(text);
        return typeof payload?.connectionId === 'string' && payload.connectionId.length > 0;
      }
    },
    {
      url: `${baseUrl}/api/health`,
      assert: text => {
        const payload = JSON.parse(text);
        return payload?.ok === true;
      }
    },
    {
      url: `${baseUrl}/api/tools/master-index`,
      assert: text => !text.includes('missing_or_invalid_api_key')
    },
    {
      url: `${baseUrl}/api/ai/status`,
      assert: text => {
        const payload = JSON.parse(text);
        return payload?.status === 'scaffolded'
          && Array.isArray(payload?.routes)
          && payload.routes.includes('coach')
          && Array.isArray(payload?.providers)
          && !text.includes('missing_or_invalid_api_key');
      }
    },
    {
      url: `${baseUrl}/openapi/v1.json`,
      assert: text => {
        const payload = JSON.parse(text);
        return typeof payload?.openapi === 'string' && payload.openapi.length > 0;
      }
    },
    {
      url: `${baseUrl}/docs/`,
      assert: text =>
        text.includes('Self-hosted OpenAPI explorer') &&
        text.includes('/docs/docs.js') &&
        !text.toLowerCase().includes('jsdelivr')
    }
  );
}

(async () => {
  for (const check of checks) {
    const response = await fetch(check.url, {
      method: check.method ?? 'GET',
      headers: check.headers,
      body: check.body
    });
    const body = await response.text();
    if (!response.ok) {
      throw new Error(`Portal check failed: ${check.url} -> HTTP ${response.status}`);
    }

    let passed = false;
    try {
      passed = Boolean(check.assert(body));
    } catch (error) {
      throw new Error(`Portal check failed: ${check.url} -> assertion threw: ${error.message}`);
    }

    if (!passed) {
      throw new Error(`Portal check failed: ${check.url} -> assertion returned false`);
    }

    console.log(`ok: ${check.url}`);
  }

  console.log('portal E2E completed');
})().catch(error => {
  console.error(error.message);
  process.exit(1);
});
