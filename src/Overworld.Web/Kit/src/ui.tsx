import { useEffect, useMemo, useRef, useState, type ReactNode, type MouseEvent, type KeyboardEvent } from 'react';
import * as Tooltip from '@radix-ui/react-tooltip';
import { windowControls } from './window';
import { useDraft } from './mirror';

export const glyphs = {
  minimize: 'M3.5 8.5h9',
  maximize: 'M4 4h8v8H4z',
  close: 'M4.5 4.5l7 7M11.5 4.5l-7 7',
  copy: 'M5.5 5.5h8v8h-8zM10.5 5.5V2.5h-8v8h3',
  check: 'M3 8.5l3.5 3.5L13 4.5',
  expand: 'M9 1h6v6M15 1L9 7M7 15H1V9M1 15l6-6',
  plus: 'M8 3v10M3 8h10',
  trash: 'M3 4.5h10M6.5 4.5V3h3v1.5M4.5 4.5l.6 8.5h5.8l.6-8.5',
  refresh: 'M13 8a5 5 0 1 1-1.5-3.5M13 2.5v3h-3',
  gear: 'M12.96 8.61l1.57.87-.86 2.09-1.73-.49-.86.86.49 1.73-2.09.86-.87-1.57H7.39l-.87 1.57-2.09-.86.49-1.73-.86-.86-1.73.49-.86-2.09 1.57-.87V7.39l-1.57-.87.86-2.09 1.73.49.86-.86-.49-1.73 2.09-.86.87 1.57h1.22l.87-1.57 2.09.86-.49 1.73.86.86 1.73-.49.86 2.09-1.57.87zM10.2 8a2.2 2.2 0 1 1-4.4 0 2.2 2.2 0 0 1 4.4 0z',
  list: 'M2.5 3.5h11v9h-11zM6.5 3.5v9',
  aside: 'M2.5 3.5h11v9h-11zM9.5 3.5v9',
  chevron: 'M6 3.5l4.5 4.5L6 12.5',
  down: 'M3.5 6l4.5 4.5L12.5 6',
  search: 'M7 12A5 5 0 1 0 7 2a5 5 0 0 0 0 10zM14 14l-3.5-3.5',
  open: 'M9 2.5h4.5V7M13.5 2.5L7.5 8.5M12 9.5v3.5a.5.5 0 0 1-.5.5h-8a.5.5 0 0 1-.5-.5v-8a.5.5 0 0 1 .5-.5H7',
  moon: 'M13.5 9.5A6 6 0 1 1 6.5 2.5a4.5 4.5 0 0 0 7 7z',
};

export function Icon({ d, size = 16, className = '' }: { d: string; size?: number; className?: string }) {
  return (
    <svg className={`glyph ${className}`} width={size} height={size} viewBox="0 0 16 16" fill="none" stroke="currentColor" strokeWidth={1.1} strokeLinecap="round" strokeLinejoin="round">
      <path d={d} />
    </svg>
  );
}

export function Tip({ text, children }: { text?: string; children: ReactNode }) {
  if (!text) return <>{children}</>;
  return (
    <Tooltip.Root delayDuration={500}>
      <Tooltip.Trigger asChild>{children}</Tooltip.Trigger>
      <Tooltip.Portal>
        <Tooltip.Content className="tip" sideOffset={6}>{text}</Tooltip.Content>
      </Tooltip.Portal>
    </Tooltip.Root>
  );
}

function readable(keys: string) {
  return keys.replace(/Ctrl\+/g, 'Ctrl ').replace(/Shift\+/g, '⇧ ').replace(/Alt\+/g, 'Alt ').replace(/Enter/g, '⏎');
}

function named(title?: string) {
  const [name, ...rest] = (title ?? '').split(' · ');
  return { name, keys: rest.join(' · ') || undefined };
}

type ButtonProps = {
  kind?: 'primary' | 'ghost' | 'chip' | 'tab' | 'danger-ghost';
  chosen?: boolean;
  disabled?: boolean;
  title?: string;
  keys?: string;
  count?: number;
  className?: string;
  onClick?: (e: MouseEvent<HTMLButtonElement>) => void;
  children?: ReactNode;
};

export function Button({ kind = 'ghost', chosen, disabled, title, keys, count, className = '', onClick, children }: ButtonProps) {
  return (
    <Tip text={title}>
      <button className={`btn ${kind} ${chosen ? 'chosen' : ''} ${className}`} disabled={disabled} onClick={onClick}>
        {children}
        {count != null && <span className="count">{count}</span>}
        {keys && <kbd>{readable(keys)}</kbd>}
      </button>
    </Tip>
  );
}

export function IconButton({ d, title, onClick, disabled, chosen, className = '' }: { d: string; title?: string; onClick?: (e: MouseEvent<HTMLButtonElement>) => void; disabled?: boolean; chosen?: boolean; className?: string }) {
  const { name, keys } = named(title);
  return (
    <Tip text={keys ? readable(keys) : undefined}>
      <button className={`btn icon ${chosen ? 'chosen' : ''} ${className}`} disabled={disabled} onClick={onClick} aria-label={name || undefined}>
        <Icon d={d} />
        {name && <span className="btn-label"><span>{name}</span></span>}
      </button>
    </Tip>
  );
}

export function CopyButton({ text, className = '' }: { text: string | (() => string); className?: string }) {
  const [copied, setCopied] = useState(false);
  useEffect(() => {
    if (!copied) return;
    const t = setTimeout(() => setCopied(false), 1500);
    return () => clearTimeout(t);
  }, [copied]);
  const copy = async (e: MouseEvent) => {
    e.stopPropagation();
    await navigator.clipboard.writeText(typeof text === 'function' ? text() : text);
    setCopied(true);
  };
  return (
    <button className={`btn icon copy ${copied ? 'copied' : ''} ${className}`} onClick={copy} aria-label="Copy">
      <Icon d={copied ? glyphs.check : glyphs.copy} />
      <span className="btn-label"><span>{copied ? 'Copied' : 'Copy'}</span></span>
    </button>
  );
}

const tiles = new URLSearchParams(location.search).has('tiles');

export type TrailStep = { label: ReactNode; onClick?: () => void };

function Crumb({ title, subtitle, steps }: { title: string; subtitle?: string | null; steps?: TrailStep[] }) {
  const all: TrailStep[] = steps ?? [{ label: title }, ...(subtitle ? [{ label: subtitle }] : [])];
  return (
    <div className="titlebar-crumb">
      {all.map((step, i) => {
        const last = i === all.length - 1;
        return (
          <span key={i} className="titlebar-step">
            {i > 0 && <Icon d={glyphs.chevron} size={12} />}
            {last ? <b>{step.label}</b> : step.onClick ? <button className="titlebar-link" onClick={step.onClick}>{step.label}</button> : <span>{step.label}</span>}
          </span>
        );
      })}
    </div>
  );
}

export function TitleBar({ title, subtitle, steps, icon, leading, trailing }: { title: string; subtitle?: string | null; steps?: TrailStep[]; icon?: string; leading?: ReactNode; trailing?: ReactNode }) {
  const down = (e: MouseEvent) => {
    if (e.button !== 0 || (e.target as HTMLElement).closest('button, input, select, a, [role="button"]')) return;
    if (e.detail === 2) {
      if (!tiles) windowControls.maximize();
    }
    else windowControls.drag();
  };
  return (
    <header className="titlebar" onMouseDown={down}>
      {icon && <img className="titlebar-icon" src={icon} alt="" />}
      {leading && <div className="titlebar-side">{leading}</div>}
      <Crumb title={title} subtitle={subtitle} steps={steps} />
      <div className="titlebar-fill" />
      <div className="titlebar-side">
        {trailing}
        {!tiles && <button className="winctl" aria-label="Minimize" onClick={windowControls.minimize}><Icon d={glyphs.minimize} size={14} /></button>}
        {!tiles && <button className="winctl" aria-label="Maximize" onClick={windowControls.maximize}><Icon d={glyphs.maximize} size={14} /></button>}
        <button className="winctl close" aria-label="Close" onClick={windowControls.close}><Icon d={glyphs.close} size={14} /></button>
      </div>
    </header>
  );
}

export function StatusBar({ busy, state, off, children }: { busy?: boolean; state?: ReactNode; off?: boolean; children?: ReactNode }) {
  return (
    <footer className="statusbar">
      {busy && <span className="statusbar-sweep" />}
      {state != null && <span className="statusbar-state"><span className={`statusbar-dot ${busy ? 'busy' : off ? 'off' : ''}`} />{state}</span>}
      {children}
    </footer>
  );
}

export function Ticker({ text }: { text?: string | null }) {
  return <span className="ticker">{text && <span key={text}>{text}</span>}</span>;
}

export function RingGauge({ fill, label, note, close }: { fill: number; label: ReactNode; note?: ReactNode; close?: boolean }) {
  return (
    <span className={`ring-gauge ${close ? 'close' : ''}`}>
      <i style={{ ['--p' as string]: Math.max(0, Math.min(100, fill)) }} />
      {label}
      {note && <em>{note}</em>}
    </span>
  );
}

export function Sidebar({ children, width = 260 }: { children: ReactNode; width?: number }) {
  return <aside className="sidebar" style={{ width }}>{children}</aside>;
}

export function Section({ title, count, fill, foot, actions, children }: { title?: string; count?: ReactNode; fill?: boolean; foot?: boolean; actions?: ReactNode; children: ReactNode }) {
  return (
    <section className={`section ${fill ? 'fill' : ''} ${foot ? 'foot' : ''}`}>
      {title && (
        <div className="section-head">
          <span className="section-title">{title}</span>
          {actions}
          {count != null && <span className="section-count">{count}</span>}
        </div>
      )}
      <div className="section-body">{children}</div>
    </section>
  );
}

export function Timeline({ children }: { children: ReactNode }) {
  return <div className="timeline">{children}</div>;
}

export function TimelineDay({ title }: { title: string }) {
  return <div className="timeline-day">{title}</div>;
}

export function Progress({ fill }: { fill: number }) {
  return <div className="progress"><i style={{ width: `${Math.max(0, Math.min(100, fill))}%` }} /></div>;
}

type FieldProps = {
  id?: string;
  label?: ReactNode;
  value?: string;
  placeholder?: string;
  hint?: string;
  error?: string | null;
  keys?: string;
  multiline?: boolean;
  disabled?: boolean;
  autoFocus?: boolean;
  onInput?: (value: string) => void;
  onEnter?: () => void;
  onKeyDown?: (e: KeyboardEvent<HTMLInputElement | HTMLTextAreaElement>) => void;
};

export function TextField({ id, label, value, placeholder, hint, error, keys, multiline, disabled, autoFocus, onInput, onEnter, onKeyDown }: FieldProps) {
  const [shaking, setShaking] = useState(false);
  const draft = useDraft(value, (v) => onInput?.(v));
  const key = (e: KeyboardEvent<HTMLInputElement | HTMLTextAreaElement>) => {
    onKeyDown?.(e);
    if (e.defaultPrevented || e.key !== 'Enter' || (multiline && !e.ctrlKey)) return;
    if (error) {
      setShaking(false);
      requestAnimationFrame(() => setShaking(true));
      return;
    }
    draft.forget();
    onEnter?.();
  };
  const common = { id, value: draft.local, placeholder, disabled, autoFocus, onChange: (e: { target: { value: string } }) => draft.write(e.target.value), onKeyDown: key };
  return (
    <label className="legend">
      <span className={`legend-key ${multiline ? 'stacked' : ''} ${error ? 'invalid' : ''} ${disabled ? 'off' : ''} ${shaking ? 'shake' : ''}`} onAnimationEnd={() => setShaking(false)}>
        {label != null && <b>{label}</b>}
        {multiline ? <textarea {...common} /> : <input {...common} />}
        {keys && <kbd>{readable(keys)}</kbd>}
      </span>
      {error ? <span className="legend-error">{error}</span> : hint && <span className="legend-hint">{hint}</span>}
    </label>
  );
}

export function SearchField({ id, value, placeholder, keys, onInput, onKeyDown, autoFocus }: { id?: string; value: string; placeholder?: string; keys?: string; onInput: (value: string) => void; onKeyDown?: (e: KeyboardEvent<HTMLInputElement | HTMLTextAreaElement>) => void; autoFocus?: boolean }) {
  return <TextField id={id} label={<Icon d={glyphs.search} size={14} />} value={value} placeholder={placeholder} keys={keys} onInput={onInput} onKeyDown={onKeyDown} autoFocus={autoFocus} />;
}

export type Choice<T> = { value: T; label: string; description?: string; group?: string };

export function Picker<T>({ label, value, choices, onPick, up, same = (a, b) => a === b }: { label: ReactNode; value: T | null | undefined; choices: Choice<T>[]; onPick: (value: T) => void; up?: boolean; same?: (a: T, b: T) => boolean }) {
  const [open, setOpen] = useState(false);
  const [query, setQuery] = useState('');
  const [lit, setLit] = useState(0);
  const box = useRef<HTMLDivElement>(null);
  const trigger = useRef<HTMLDivElement>(null);
  const current = choices.findIndex((c) => value != null && same(c.value, value));
  const shown = useMemo(() => choices.map((c, i) => ({ c, i })).filter(({ c }) => `${c.label} ${c.description ?? ''}`.toLowerCase().includes(query.toLowerCase())), [choices, query]);

  useEffect(() => {
    if (!open) return;
    const away = (e: globalThis.MouseEvent) => !box.current?.contains(e.target as Node) && setOpen(false);
    addEventListener('mousedown', away);
    return () => removeEventListener('mousedown', away);
  }, [open]);

  const show = () => {
    setQuery('');
    setLit(Math.max(0, shown.findIndex(({ i }) => i === current)));
    setOpen(true);
  };
  const close = () => {
    setOpen(false);
    trigger.current?.focus();
  };
  const pick = (i: number) => {
    close();
    onPick(choices[i].value);
  };
  const key = (e: KeyboardEvent) => {
    if (!open) {
      if (e.key === 'Enter' || e.key === ' ' || e.key === 'ArrowDown') {
        e.preventDefault();
        show();
      }
      return;
    }
    if (e.key === 'Escape') {
      e.preventDefault();
      e.stopPropagation();
      close();
    }
    else if (e.key === 'ArrowDown' || e.key === 'ArrowUp') {
      e.preventDefault();
      setLit((l) => (l + (e.key === 'ArrowDown' ? 1 : -1) + shown.length) % Math.max(1, shown.length));
    }
    else if (e.key === 'Enter' && shown[lit]) {
      e.preventDefault();
      pick(shown[lit].i);
    }
  };

  let group: string | undefined;
  return (
    <div className="picker" ref={box} onKeyDown={key}>
      <div ref={trigger} className={`legend-key ${open ? 'open' : ''}`} tabIndex={0} role="button" onClick={() => (open ? close() : show())}>
        <b>{label}</b>
        <span className="picker-value">{current >= 0 ? choices[current].label : ''}</span>
        <span className="picker-chevron"><Icon d={glyphs.down} size={14} /></span>
      </div>
      {open && (
        <div className={`picker-pop ${up ? 'up' : ''}`}>
          <label className="legend-key">
            <b><Icon d={glyphs.search} size={14} /></b>
            <input autoFocus value={query} placeholder="Filter" onChange={(e) => { setQuery(e.target.value); setLit(0); }} />
          </label>
          <div className="picker-list">
            {shown.map(({ c, i }, n) => {
              const head = c.group && c.group !== group ? (group = c.group) : null;
              return (
                <div key={i}>
                  {head && <div className="picker-group">{head}</div>}
                  <div className={`picker-option ${i === current ? 'chosen' : ''} ${n === lit ? 'lit' : ''}`} onMouseEnter={() => setLit(n)} onClick={() => pick(i)}>
                    <div><span>{c.label}</span>{c.description && <small>{c.description}</small>}</div>
                    {n < 9 && <kbd>{n + 1}</kbd>}
                  </div>
                </div>
              );
            })}
            {shown.length === 0 && <div className="picker-empty">Nothing matches.</div>}
          </div>
          <div className="picker-foot"><span><kbd>↑↓</kbd> move</span><span><kbd>⏎</kbd> pick</span><span><kbd>Esc</kbd> close</span></div>
        </div>
      )}
    </div>
  );
}

type Sight = 'asleep' | 'waking' | 'live';

export function ItemFrame({ source, title, actions, onExpand, onOpen, onSeen, ready = true, height = 320, children }: { source?: string; title?: string; actions?: ReactNode; onExpand?: () => void; onOpen?: () => void; onSeen?: (seen: boolean) => void; ready?: boolean; height?: number; children: ReactNode }) {
  const box = useRef<HTMLDivElement>(null);
  const [seen, setSeen] = useState(false);
  useEffect(() => {
    let leaving: ReturnType<typeof setTimeout> | undefined;
    let waking: ReturnType<typeof setTimeout> | undefined;
    const io = new IntersectionObserver(([entry]) => {
      if (entry.isIntersecting) {
        clearTimeout(leaving);
        waking = setTimeout(() => setSeen(true), 150);
      } else {
        clearTimeout(waking);
        leaving = setTimeout(() => setSeen(false), 1000);
      }
    }, { rootMargin: '240px 0px' });
    io.observe(box.current!);
    return () => {
      io.disconnect();
      clearTimeout(leaving);
      clearTimeout(waking);
    };
  }, []);
  useEffect(() => onSeen?.(seen), [seen]);
  const sight: Sight = !seen ? 'asleep' : ready ? 'live' : 'waking';
  return (
    <div className="itemframe" ref={box}>
      <div className="titlebar">
        <Crumb title={source ?? ''} subtitle={title} />
        <div className="titlebar-fill" />
        {actions}
        <span className="itemframe-state">{sight}</span>
        {onOpen && <IconButton d={glyphs.open} title={source ? `Open in ${source}` : 'Open'} onClick={onOpen} />}
        {onExpand && <IconButton d={glyphs.expand} title="Expand" onClick={onExpand} />}
      </div>
      <div className="itemframe-live">{sight === 'waking' && <i />}</div>
      <div className="itemframe-stage" style={{ height }}>
        {seen ? children : <div className="itemframe-sleep"><Icon d={glyphs.moon} /><span>Asleep · comes alive when seen</span></div>}
      </div>
    </div>
  );
}

export function Keys({ keys }: { keys: string }) {
  return (
    <span className="keycaps">
      {keys.split(' ').map((step, i) => (
        <span key={i} className="keycaps">
          {i > 0 && <span className="then">then</span>}
          {step.split('+').map((k) => <kbd key={k}>{k}</kbd>)}
        </span>
      ))}
    </span>
  );
}
