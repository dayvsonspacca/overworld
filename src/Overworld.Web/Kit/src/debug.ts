import { log } from './mirror';

export function watchForDebugging() {
  if (!new URLSearchParams(location.search).has('debug')) return;
  let last = '';
  setInterval(() => {
    const all = document.body.innerText.replace(/\s+/g, ' ');
    const text = all.length > 1800 ? `${all.slice(0, 900)} … ${all.slice(-900)}` : all;
    if (text !== last) {
      last = text;
      log(`dom: ${text}`);
    }
  }, 1000);
  setTimeout(async () => {
    const tauri = (globalThis as any).__TAURI__;
    try {
      log(`tauri: window=${typeof tauri?.window} dialog=${typeof tauri?.dialog} focused=${tauri ? await tauri.window.getCurrentWindow().isFocused() : 'none'}`);
    } catch (problem) {
      log(`tauri: ${problem}`);
    }
  }, 2500);
  setTimeout(() => {
    const counts: Record<string, number> = {};
    for (const element of document.querySelectorAll('[class]')) {
      for (const name of element.classList) counts[name] = (counts[name] ?? 0) + 1;
    }
    log(`classes: ${Object.entries(counts).sort().map(([k, v]) => `${k}=${v}`).join(' ')}`);
  }, 4000);
}
