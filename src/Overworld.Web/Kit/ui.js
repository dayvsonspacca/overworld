import { h, render, createContext, Component } from 'preact';
import { useState, useEffect, useRef, useMemo, useContext, useCallback } from 'preact/hooks';
import htm from './vendor/htm.mjs';
import { marked } from './vendor/marked.mjs';
import hljs from './vendor/highlight.mjs';
import { connect, on, useRoot, log } from './mirror.js';

export const html = htm.bind(h);

const tauri = () => globalThis.__TAURI__?.window?.getCurrentWindow?.();

export const windowControls = {
  minimize: () => tauri()?.minimize(),
  maximize: () => tauri()?.toggleMaximize(),
  close: () => (tauri() ? tauri().close() : window.close()),
  drag: () => tauri()?.startDragging(),
  focus: () => tauri()?.unminimize().then(() => tauri()?.setFocus()),
};

export function Icon({ d, size = 16, className = '', fill = false }) {
  return html`<svg class=${`glyph ${className}`} width=${size} height=${size} viewBox="0 0 16 16" fill=${fill ? 'currentColor' : 'none'} stroke=${fill ? 'none' : 'currentColor'} stroke-width="1.1" stroke-linecap="round" stroke-linejoin="round"><path d=${d} /></svg>`;
}

export const glyphs = {
  minimize: 'M3.5 8.5h9',
  maximize: 'M4 4h8v8H4z',
  close: 'M4.5 4.5l7 7M11.5 4.5l-7 7',
  copy: 'M5.5 5.5h8v8h-8zM10.5 5.5V2.5h-8v8h3',
  check: 'M3 8.5l3.5 3.5L13 4.5',
  expand: 'M9 1h6v6M15 1L9 7M7 15H1V9M1 15l6-6',
  search: 'M7 12A5 5 0 1 0 7 2a5 5 0 0 0 0 10zM10.5 10.5L14 14',
  plus: 'M8 3v10M3 8h10',
  trash: 'M3 4.5h10M6.5 4.5V3h3v1.5M4.5 4.5l.6 8.5h5.8l.6-8.5',
  gear: 'M8 10.2a2.2 2.2 0 1 0 0-4.4 2.2 2.2 0 0 0 0 4.4zM8 1.5v2M8 12.5v2M1.5 8h2M12.5 8h2M3.4 3.4l1.4 1.4M11.2 11.2l1.4 1.4M3.4 12.6l1.4-1.4M11.2 4.8l1.4-1.4',
  chevron: 'M6 3.5L10.5 8 6 12.5',
  back: 'M10 3.5L5.5 8l4.5 4.5',
  refresh: 'M13 8a5 5 0 1 1-1.5-3.5M13 2.5v3h-3',
  stop: 'M4.5 4.5h7v7h-7z',
  send: 'M2.5 8h11M9 3.5L13.5 8 9 12.5',
  folder: 'M1.5 4h4.5l1.5 1.5h7v7.5h-13z',
  image: 'M2 3h12v10H2zM2 11l3.5-3.5 3 3 2-2L14 12M10.5 6.5h.01',
  file: 'M4 1.5h5l3 3v10H4zM9 1.5v3h3',
  dots: 'M3.5 8h.01M8 8h.01M12.5 8h.01',
};

export function Button({ kind = 'ghost', chosen, disabled, onClick, title, children, className = '' }) {
  return html`<button class=${`btn ${kind} ${chosen ? 'chosen' : ''} ${className}`} disabled=${disabled} title=${title} onClick=${onClick}>${children}</button>`;
}

export function IconButton({ d, title, onClick, disabled, chosen, className = '' }) {
  return html`<button class=${`btn icon ${chosen ? 'chosen' : ''} ${className}`} title=${title} disabled=${disabled} onClick=${onClick}><${Icon} d=${d} /></button>`;
}

export function CopyButton({ text, className = '' }) {
  const [copied, setCopied] = useState(false);
  useEffect(() => {
    if (!copied) return;
    const t = setTimeout(() => setCopied(false), 1500);
    return () => clearTimeout(t);
  }, [copied]);
  const copy = async (e) => {
    e.stopPropagation();
    if (!text) return;
    await navigator.clipboard.writeText(typeof text === 'function' ? text() : text);
    setCopied(true);
  };
  return html`<button class=${`btn icon copy ${copied ? 'copied' : ''} ${className}`} title=${copied ? 'Copied' : 'Copy'} onClick=${copy}><${Icon} d=${copied ? glyphs.check : glyphs.copy} /></button>`;
}

export function TitleBar({ title, subtitle, icon, leading, trailing }) {
  const down = (e) => {
    if (e.button !== 0 || e.target.closest('button, input, select, a')) return;
    if (e.detail === 2) windowControls.maximize();
    else windowControls.drag();
  };
  return html`<header class="titlebar" onMouseDown=${down}>
    <div class="titlebar-side">
      ${icon && html`<img class="titlebar-icon" src=${icon} alt="" />`}
      ${leading}
    </div>
    <div class="titlebar-title"><strong>${title}</strong>${subtitle && html`<span class="titlebar-subtitle">· ${subtitle}</span>`}</div>
    <div class="titlebar-side right">
      ${trailing}
      <button class="winctl" title="Minimize" onClick=${windowControls.minimize}><${Icon} d=${glyphs.minimize} /></button>
      <button class="winctl" title="Maximize" onClick=${windowControls.maximize}><${Icon} d=${glyphs.maximize} /></button>
      <button class="winctl close" title="Close" onClick=${windowControls.close}><${Icon} d=${glyphs.close} /></button>
    </div>
  </header>`;
}

const CommandsContext = createContext(null);

function parseKeys(keys) {
  return keys.split(' ').map((step) => {
    const parts = step.split('+');
    const key = parts.pop() || '+';
    return { ctrl: parts.includes('Ctrl'), shift: parts.includes('Shift'), alt: parts.includes('Alt'), key: key.length === 1 ? key.toLowerCase() : key };
  });
}

function matches(step, e) {
  const key = e.key.length === 1 ? e.key.toLowerCase() : e.key;
  const wanted = step.key === 'Esc' ? 'Escape' : step.key;
  return step.ctrl === (e.ctrlKey || e.metaKey) && step.shift === e.shiftKey && step.alt === e.altKey && (key === wanted || (step.key === '/' && e.code === 'Slash'));
}

export function useCommands(commands, deps = []) {
  const book = useContext(CommandsContext);
  useEffect(() => book?.add(commands), deps);
}

function CommandBook({ children }) {
  const [entries, setEntries] = useState([]);
  const [palette, setPalette] = useState(null);
  const pendingStep = useRef(null);
  const book = useMemo(
    () => ({
      add(commands) {
        setEntries((all) => [...all, ...commands]);
        return () => setEntries((all) => all.filter((c) => !commands.includes(c)));
      },
    }),
    [],
  );

  useEffect(() => {
    const down = (e) => {
      if (e.defaultPrevented) return;
      if ((e.ctrlKey && e.shiftKey && e.key.toLowerCase() === 'p')) {
        e.preventDefault();
        setPalette('all');
        return;
      }
      if (e.ctrlKey && !e.shiftKey && (e.code === 'Slash' || e.key === '/')) {
        e.preventDefault();
        setPalette('keys');
        return;
      }
      for (const command of entries) {
        if (!command.keys || !command.run) continue;
        if (command.when && !command.when()) continue;
        const steps = parseKeys(command.keys);
        if (steps.length === 2 && pendingStep.current === command && matches(steps[1], e)) {
          e.preventDefault();
          pendingStep.current = null;
          command.run();
          return;
        }
        if (matches(steps[0], e)) {
          e.preventDefault();
          if (steps.length === 1) command.run();
          else pendingStep.current = command;
          return;
        }
      }
      pendingStep.current = null;
    };
    addEventListener('keydown', down);
    return () => removeEventListener('keydown', down);
  }, [entries]);

  return html`<${CommandsContext.Provider} value=${book}>
    ${children}
    ${palette && html`<${Palette} commands=${entries} keysOnly=${palette === 'keys'} onClose=${() => setPalette(null)} />`}
  </${CommandsContext.Provider}>`;
}

export function Keys({ keys }) {
  return html`<span class="keycaps">${keys.split(' ').map((step, i) => html`${i > 0 && html`<span class="then">then</span>`}${step.split('+').map((k) => html`<kbd>${k}</kbd>`)}`)}</span>`;
}

function Palette({ commands, keysOnly, onClose }) {
  const [query, setQuery] = useState('');
  const [at, setAt] = useState(0);
  const input = useRef(null);
  useEffect(() => input.current?.focus(), []);
  const shown = commands
    .filter((c) => (keysOnly ? c.keys : c.run))
    .filter((c) => `${c.group ?? ''} ${c.title}`.toLowerCase().includes(query.toLowerCase()));
  const choose = (c) => {
    onClose();
    if (c?.run) setTimeout(c.run, 0);
  };
  const key = (e) => {
    if (e.key === 'Escape') { e.preventDefault(); onClose(); }
    else if (e.key === 'ArrowDown') { e.preventDefault(); setAt((a) => Math.min(a + 1, shown.length - 1)); }
    else if (e.key === 'ArrowUp') { e.preventDefault(); setAt((a) => Math.max(a - 1, 0)); }
    else if (e.key === 'Enter') { e.preventDefault(); choose(shown[at]); }
  };
  return html`<div class="scrim" onMouseDown=${(e) => e.target === e.currentTarget && onClose()}>
    <div class="palette">
      <input ref=${input} class="palette-input" placeholder=${keysOnly ? 'Search shortcuts' : 'Type a command'} value=${query} onInput=${(e) => { setQuery(e.target.value); setAt(0); }} onKeyDown=${key} />
      <div class="palette-list">
        ${shown.map((c, i) => html`<div class=${`palette-item ${i === at ? 'selected' : ''}`} onMouseEnter=${() => setAt(i)} onClick=${() => choose(c)}>
          <span class="palette-group">${c.group}</span><span class="palette-title">${c.title}</span>${c.keys && html`<${Keys} keys=${c.keys} />`}
        </div>`)}
        ${shown.length === 0 && html`<div class="palette-empty">Nothing matches</div>`}
      </div>
      <div class="palette-hints"><span>↑↓ move</span><span>Enter run</span><span>Esc close</span></div>
    </div>
  </div>`;
}

export function Modal({ title, onClose, children, actions, wide }) {
  useEffect(() => {
    if (!onClose) return;
    const key = (e) => {
      if (e.key === 'Escape') { e.preventDefault(); onClose(); }
    };
    addEventListener('keydown', key);
    return () => removeEventListener('keydown', key);
  }, [onClose]);
  return html`<div class="scrim" onMouseDown=${(e) => onClose && e.target === e.currentTarget && onClose()}>
    <div class=${`modal ${wide ? 'wide' : ''}`}>
      ${title && html`<h2 class="modal-title">${title}</h2>`}
      <div class="modal-body">${children}</div>
      ${actions && html`<div class="modal-actions">${actions}</div>`}
    </div>
  </div>`;
}

export function StatusBar({ children }) {
  return html`<footer class="statusbar">${children}</footer>`;
}

export function ItemFrame({ source, title, onExpand, onSeen, height = 320, children }) {
  const box = useRef(null);
  const [seen, setSeen] = useState(false);
  useEffect(() => {
    let leaving = null;
    let waking = null;
    const io = new IntersectionObserver(([entry]) => {
      if (entry.isIntersecting) {
        clearTimeout(leaving);
        waking = setTimeout(() => setSeen(true), 150);
      } else {
        clearTimeout(waking);
        leaving = setTimeout(() => setSeen(false), 1000);
      }
    }, { rootMargin: '240px 0px' });
    io.observe(box.current);
    return () => { io.disconnect(); clearTimeout(leaving); clearTimeout(waking); };
  }, []);
  useEffect(() => onSeen?.(seen), [seen]);
  return html`<div class="itemframe" ref=${box}>
    <div class="itemframe-head">
      ${source && html`<span class="itemframe-source">${source}</span>`}
      <span class="itemframe-title">${title}</span>
      ${onExpand && html`<${IconButton} d=${glyphs.expand} title="Expand" onClick=${onExpand} />`}
    </div>
    <div class="itemframe-stage" style=${{ height: `${height}px` }}>${seen ? children : null}</div>
  </div>`;
}

marked.use({ gfm: true, breaks: false });

function highlight(code, language) {
  try {
    if (language && hljs.getLanguage(language)) return hljs.highlight(code, { language, ignoreIllegals: true }).value;
    return hljs.highlightAuto(code).value;
  } catch {
    return code.replace(/[&<>]/g, (c) => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;' })[c]);
  }
}

function escape(text) {
  return text.replace(/[&<>"]/g, (c) => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;' })[c]);
}

const renderer = {
  code({ text, lang }) {
    const language = (lang ?? '').split(/\s/)[0];
    if (language === 'markdown' || language === 'md') {
      return `<div class="prose-drafted" data-source="${escape(text)}"><div class="prose-drafted-bar"><button class="btn chip chosen" data-show="preview">Preview</button><button class="btn chip" data-show="source">Source</button><button class="btn icon copy" data-copy title="Copy"></button></div><div class="prose-preview">${marked.parse(text)}</div><pre class="prose-code hidden"><code>${escape(text)}</code></pre></div>`;
    }
    return `<div class="prose-codeblock"><pre class="prose-code"><code class="hljs">${highlight(text, language)}</code></pre><button class="btn icon copy prose-copy" data-copy title="Copy"></button></div>`;
  },
  link({ href, text }) {
    return `<a href="${escape(href)}" target="_blank" rel="noreferrer">${text}</a>`;
  },
};
marked.use({ renderer });

const copyIcon = `<svg class="glyph" width="16" height="16" viewBox="0 0 16 16" fill="none" stroke="currentColor" stroke-width="1.1" stroke-linecap="round" stroke-linejoin="round"><path d="${glyphs.copy}"/></svg>`;
const checkIcon = `<svg class="glyph" width="16" height="16" viewBox="0 0 16 16" fill="none" stroke="currentColor" stroke-width="1.1" stroke-linecap="round" stroke-linejoin="round"><path d="${glyphs.check}"/></svg>`;

export function Prose({ text, className = '' }) {
  const box = useRef(null);
  const rendered = useMemo(() => marked.parse(text ?? ''), [text]);
  useEffect(() => {
    for (const button of box.current.querySelectorAll('[data-copy]')) if (!button.innerHTML) button.innerHTML = copyIcon;
  }, [rendered]);
  const click = useCallback(async (e) => {
    const copy = e.target.closest('[data-copy]');
    if (copy) {
      const drafted = copy.closest('.prose-drafted');
      const code = drafted ? drafted.dataset.source : copy.closest('.prose-codeblock')?.querySelector('code')?.innerText ?? '';
      await navigator.clipboard.writeText(code.replace(/\n$/, ''));
      copy.classList.add('copied');
      copy.innerHTML = checkIcon;
      copy.title = 'Copied';
      setTimeout(() => { copy.classList.remove('copied'); copy.innerHTML = copyIcon; copy.title = 'Copy'; }, 1500);
      return;
    }
    const show = e.target.closest('[data-show]');
    if (show) {
      const drafted = show.closest('.prose-drafted');
      const preview = show.dataset.show === 'preview';
      drafted.querySelector('.prose-preview').classList.toggle('hidden', !preview);
      drafted.querySelector('.prose-code').classList.toggle('hidden', preview);
      for (const b of drafted.querySelectorAll('[data-show]')) b.classList.toggle('chosen', b === show);
    }
  }, []);
  return html`<div ref=${box} class=${`prose ${className}`} onClick=${click} dangerouslySetInnerHTML=${{ __html: rendered }} />`;
}

function reloadTheme() {
  const link = document.getElementById('theme');
  if (link) link.href = `/theme.css?at=${Date.now()}`;
}

class Guard extends Component {
  componentDidCatch(error) {
    log(`render: ${error?.stack ?? error}`);
    this.setState({ broken: String(error?.message ?? error) });
  }

  render() {
    return this.state.broken ? html`<div class="empty danger">${this.state.broken}</div>` : this.props.children;
  }
}

export function boot(App) {
  on('theme', reloadTheme);
  on('summon', windowControls.focus);
  connect();
  function Root() {
    const root = useRoot();
    return html`<${CommandBook}>${root ? html`<${Guard}><${App} root=${root} /></${Guard}>` : html`<div class="booting"></div>`}</${CommandBook}>`;
  }
  render(html`<${Root} />`, document.getElementById('app'));
}

export function useEvent(name, handler, deps = []) {
  useEffect(() => on(name, handler), deps);
}

export function plural(n, one, many) {
  return `${n} ${n === 1 ? one : many}`;
}

export function hex(color) {
  if (!color) return 'transparent';
  const digits = color.replace('#', '');
  return digits.length === 8 ? `#${digits.slice(2)}${digits.slice(0, 2)}` : `#${digits}`;
}

const tinted = new Map();
let allay = null;

function loadAllay() {
  allay ??= new Promise((resolve) => {
    const image = new Image();
    image.onload = () => resolve(image);
    image.src = '/kit/allay.png';
  });
  return allay;
}

function hsv(r, g, b) {
  const max = Math.max(r, g, b);
  const spread = max - Math.min(r, g, b);
  let hue = spread === 0 ? 0 : max === r ? ((g - b) / spread) % 6 : max === g ? (b - r) / spread + 2 : (r - g) / spread + 4;
  hue = ((hue / 6) % 1 + 1) % 1;
  return [hue, max === 0 ? 0 : spread / max, max];
}

function rgb(h, s, v) {
  const sector = Math.floor(h * 6);
  const part = h * 6 - sector;
  const low = v * (1 - s), falling = v * (1 - part * s), rising = v * (1 - (1 - part) * s);
  return [[v, rising, low], [falling, v, low], [low, v, rising], [low, falling, v], [rising, low, v], [v, low, falling]][((sector % 6) + 6) % 6];
}

async function tint(color) {
  if (tinted.has(color)) return tinted.get(color);
  const image = await loadAllay();
  const canvas = document.createElement('canvas');
  canvas.width = image.width;
  canvas.height = image.height;
  const context = canvas.getContext('2d');
  context.drawImage(image, 0, 0);
  const match = /^#?([0-9a-f]{6})$/i.exec(color ?? '');
  if (match) {
    const data = context.getImageData(0, 0, canvas.width, canvas.height);
    const px = data.data;
    let hue = 0, sat = 0, val = 0, n = 0;
    for (let i = 0; i < px.length; i += 4) {
      if (!px[i + 3]) continue;
      const [h, s, v] = hsv(px[i] / 255, px[i + 1] / 255, px[i + 2] / 255);
      if (s > 0.25) { hue += h; sat += s; val += v; n++; }
    }
    if (n) {
      hue /= n; sat /= n; val /= n;
      const target = parseInt(match[1], 16);
      const [th, ts, tv] = hsv(((target >> 16) & 255) / 255, ((target >> 8) & 255) / 255, (target & 255) / 255);
      for (let i = 0; i < px.length; i += 4) {
        if (!px[i + 3]) continue;
        const [h, s, v] = hsv(px[i] / 255, px[i + 1] / 255, px[i + 2] / 255);
        const [r, g, b] = rgb((((h - hue + th) % 1) + 1) % 1, Math.min(1, (s * ts) / sat), Math.min(1, (v * tv) / val));
        px[i] = Math.round(r * 255); px[i + 1] = Math.round(g * 255); px[i + 2] = Math.round(b * 255);
      }
      context.putImageData(data, 0, 0);
    }
  }
  const url = canvas.toDataURL();
  tinted.set(color, url);
  return url;
}

export function AllayIcon({ color, size = 28, className = '' }) {
  const [src, setSrc] = useState(tinted.get(color) ?? null);
  useEffect(() => {
    let live = true;
    tint(color).then((url) => live && setSrc(url));
    return () => { live = false; };
  }, [color]);
  return html`<span class=${`allay ${className}`} style=${{ width: `${size}px`, height: `${size}px` }}>${src && html`<img src=${src} alt="" width=${size} height=${size} />`}</span>`;
}

export function Sidebar({ children, width = 260 }) {
  return html`<aside class="sidebar" style=${{ width: `${width}px` }}>${children}</aside>`;
}

export function Section({ title, count, fill, actions, children }) {
  return html`<section class=${`section ${fill ? 'fill' : ''}`}>
    ${title && html`<div class="section-head"><span class="section-title">${title}</span>${actions}${count != null && html`<span class="section-count">${count}</span>`}</div>`}
    <div class="section-body">${children}</div>
  </section>`;
}

const pageKey = new URLSearchParams(location.search).get('key') ?? '';

export const localFile = (path, version = '') => (path ? `/file?key=${encodeURIComponent(pageKey)}&path=${encodeURIComponent(path)}&v=${version}` : null);

export function TextField({ label, value, placeholder, hint, error, onInput, onEnter, disabled, mono }) {
  return html`<label class="field">
    ${label && html`<span class="field-label">${label}</span>`}
    <input class=${`${error ? 'invalid' : ''} ${mono ? 'mono' : ''}`} value=${value ?? ''} placeholder=${placeholder} disabled=${disabled}
      onInput=${(e) => onInput?.(e.target.value)} onKeyDown=${(e) => e.key === 'Enter' && onEnter?.()} />
    ${error ? html`<span class="field-error">${error}</span>` : hint && html`<span class="field-hint">${hint}</span>`}
  </label>`;
}

export function pickFile(accept) {
  return new Promise((resolve) => {
    const input = document.createElement('input');
    input.type = 'file';
    input.accept = accept ?? '';
    input.onchange = () => {
      const file = input.files?.[0];
      if (!file) return resolve(null);
      const reader = new FileReader();
      reader.onload = () => resolve({ name: file.name, data: String(reader.result).split(',')[1] ?? '' });
      reader.readAsDataURL(file);
    };
    input.click();
  });
}

export const blob = (value) => (value && typeof value === 'object' && '$blob' in value ? `/blob?key=${encodeURIComponent(pageKey)}&id=${value.$blob}` : null);

export async function choose({ directory = false, multiple = false, filters, title } = {}) {
  const dialog = globalThis.__TAURI__?.dialog;
  if (!dialog) {
    const typed = prompt(title ?? (directory ? 'Folder path' : 'File path'));
    return typed ? (multiple ? [typed] : typed) : null;
  }
  return dialog.open({ directory, multiple, filters, title });
}
