#!/usr/bin/env node
'use strict';

const checks = [
  {
    url: 'http://chummer-portal:8080/',
    assert: text => text.includes('Chummer Portal') && text.includes('/blazor/')
  },
  {
    url: 'http://chummer-portal:8080/blazor/health',
    assert: text => {
      const payload = JSON.parse(text);
      return payload?.pathBase === '/blazor' && payload?.ok === true;
    }
  },
  {
    url: 'http://chummer-portal:8080/blazor/',
    assert: text => /<base href="[^"]*\/blazor\/"/i.test(text)
  },
  {
    url: 'http://chummer-portal:8080/avalonia/',
    assert: text => text.includes('Avalonia Browser Host')
  },
  {
    url: 'http://chummer-portal:8080/avalonia/health',
    assert: text => {
      const payload = JSON.parse(text);
      return payload?.head === 'avalonia-browser' && payload?.pathBase === '/avalonia' && payload?.ok === true;
    }
  },
  {
    method: 'POST',
    url: 'http://chummer-portal:8080/blazor/_blazor/negotiate?negotiateVersion=1',
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
    url: 'http://chummer-portal:8080/api/health',
    assert: text => {
      const payload = JSON.parse(text);
      return payload?.ok === true;
    }
  },
  {
    url: 'http://chummer-portal:8080/docs/',
    assert: text => text.toLowerCase().includes('swagger-ui')
  },
  {
    url: 'http://chummer-portal:8080/downloads/releases.json',
    assert: text => {
      const payload = JSON.parse(text);
      return typeof payload?.version === 'string' && Array.isArray(payload?.downloads);
    }
  },
  {
    url: 'http://chummer-portal:8080/downloads/',
    assert: text => text.includes('Desktop Downloads') && text.includes('/downloads/releases.json')
  }
];

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
