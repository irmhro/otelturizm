import { chromium } from 'playwright';
import fs from 'fs';
import path from 'path';

const base = process.env.OTEL_BASE_URL || 'http://127.0.0.1:5103';
const slug = process.env.OTEL_DEMO_SLUG || 'orkestra-bogaz-otel';
const root = path.resolve('Docs/frontend-screenshots/fe-otel-public/otel-detay');

const shots = [
  { dir: 'desktop', width: 1440, height: 900 },
  { dir: 'mobil', width: 390, height: 844 },
];

async function capture() {
  fs.mkdirSync(path.join(root, 'desktop'), { recursive: true });
  fs.mkdirSync(path.join(root, 'mobil'), { recursive: true });

  const browser = await chromium.launch({ headless: true });
  const url = `${base}/oteller/${slug}`;

  for (const shot of shots) {
    const context = await browser.newContext({
      viewport: { width: shot.width, height: shot.height },
      deviceScaleFactor: shot.dir === 'mobil' ? 3 : 1,
    });
    const page = await context.newPage();
    const response = await page.goto(url, { waitUntil: 'networkidle', timeout: 60000 });
    if (!response || response.status() >= 400) {
      console.warn(`WARN ${shot.dir}: HTTP ${response?.status() ?? 'n/a'} for ${url}`);
    }
    await page.waitForTimeout(1200);
    const out = path.join(root, shot.dir, 'step-01-full-page.png');
    await page.screenshot({ path: out, fullPage: true });
    console.log(`OK ${out}`);
    await context.close();
  }

  await browser.close();
}

capture().catch((err) => {
  console.error(err);
  process.exit(1);
});
