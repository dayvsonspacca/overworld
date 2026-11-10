import { useEffect, useRef, useState, type ReactNode, type MouseEvent } from 'react';
import * as Tooltip from '@radix-ui/react-tooltip';
import { windowControls } from './window';

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

type ButtonProps = {
  kind?: 'primary' | 'ghost' | 'chip' | 'tab' | 'danger-ghost';
  chosen?: boolean;
  disabled?: boolean;
  title?: string;
  className?: string;
  onClick?: (e: MouseEvent<HTMLButtonElement>) => void;
  children?: ReactNode;
};

export function Button({ kind = 'ghost', chosen, disabled, title, className = '', onClick, children }: ButtonProps) {
  return (
    <Tip text={title}>
      <button className={`btn ${kind} ${chosen ? 'chosen' : ''} ${className}`} disabled={disabled} onClick={onClick}>
        {children}
      </button>
    </Tip>
  );
}

export function IconButton({ d, title, onClick, disabled, chosen, className = '' }: { d: string; title?: string; onClick?: (e: MouseEvent<HTMLButtonElement>) => void; disabled?: boolean; chosen?: boolean; className?: string }) {
  return (
    <Tip text={title}>
      <button className={`btn icon ${chosen ? 'chosen' : ''} ${className}`} disabled={disabled} onClick={onClick} aria-label={title}>
        <Icon d={d} />
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
    <Tip text={copied ? 'Copied' : 'Copy'}>
      <button className={`btn icon copy ${copied ? 'copied' : ''} ${className}`} onClick={copy} aria-label="Copy">
        <Icon d={copied ? glyphs.check : glyphs.copy} />
      </button>
    </Tip>
  );
}

export function TitleBar({ title, subtitle, icon, leading, trailing }: { title: string; subtitle?: string | null; icon?: string; leading?: ReactNode; trailing?: ReactNode }) {
  const down = (e: MouseEvent) => {
    if (e.button !== 0 || (e.target as HTMLElement).closest('button, input, select, a')) return;
    if (e.detail === 2) windowControls.maximize();
    else windowControls.drag();
  };
  return (
    <header className="titlebar" onMouseDown={down}>
      <div className="titlebar-side">
        {icon && <img className="titlebar-icon" src={icon} alt="" />}
        {leading}
      </div>
      <div className="titlebar-title">
        <strong>{title}</strong>
        {subtitle && <span className="titlebar-subtitle">· {subtitle}</span>}
      </div>
      <div className="titlebar-side right">
        {trailing}
        <button className="winctl" aria-label="Minimize" onClick={windowControls.minimize}><Icon d={glyphs.minimize} /></button>
        <button className="winctl" aria-label="Maximize" onClick={windowControls.maximize}><Icon d={glyphs.maximize} /></button>
        <button className="winctl close" aria-label="Close" onClick={windowControls.close}><Icon d={glyphs.close} /></button>
      </div>
    </header>
  );
}

export function StatusBar({ children }: { children: ReactNode }) {
  return <footer className="statusbar">{children}</footer>;
}

export function Sidebar({ children, width = 260 }: { children: ReactNode; width?: number }) {
  return <aside className="sidebar" style={{ width }}>{children}</aside>;
}

export function Section({ title, count, fill, actions, children }: { title?: string; count?: number; fill?: boolean; actions?: ReactNode; children: ReactNode }) {
  return (
    <section className={`section ${fill ? 'fill' : ''}`}>
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

export function TextField({ label, value, placeholder, hint, error, onInput, onEnter, disabled }: { label?: string; value?: string; placeholder?: string; hint?: string; error?: string | null; onInput?: (value: string) => void; onEnter?: () => void; disabled?: boolean }) {
  return (
    <label className="field">
      {label && <span className="field-label">{label}</span>}
      <input className={error ? 'invalid' : ''} value={value ?? ''} placeholder={placeholder} disabled={disabled} onChange={(e) => onInput?.(e.target.value)} onKeyDown={(e) => e.key === 'Enter' && onEnter?.()} />
      {error ? <span className="field-error">{error}</span> : hint && <span className="field-hint">{hint}</span>}
    </label>
  );
}

export function ItemFrame({ source, title, onExpand, onSeen, height = 320, children }: { source?: string; title?: string; onExpand?: () => void; onSeen?: (seen: boolean) => void; height?: number; children: ReactNode }) {
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
  return (
    <div className="itemframe" ref={box}>
      <div className="itemframe-head">
        {source && <span className="itemframe-source">{source}</span>}
        <span className="itemframe-title">{title}</span>
        {onExpand && <IconButton d={glyphs.expand} title="Expand" onClick={onExpand} />}
      </div>
      <div className="itemframe-stage" style={{ height }}>{seen ? children : null}</div>
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
