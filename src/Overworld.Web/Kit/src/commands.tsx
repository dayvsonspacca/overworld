import { createContext, useContext, useEffect, useMemo, useRef, useState, type ReactNode, type KeyboardEvent as KeyEvent } from 'react';
import * as Dialog from '@radix-ui/react-dialog';
import { Keys, SearchField, Icon, glyphs } from './ui';

export type Scope = '@' | '/' | '#';

export type Shortcut = { title: string; group?: string; keys?: string; scope?: Scope; choices?: { title: string; run: () => void }[]; run?: () => void; when?: () => boolean };

const scopes: { key: '>' | Scope; name: string }[] = [{ key: '>', name: 'Commands' }, { key: '@', name: 'Chats' }, { key: '/', name: 'Skills' }, { key: '#', name: 'Files' }];

type Book = { add(commands: Shortcut[]): () => void };

const BookContext = createContext<Book | null>(null);

type Step = { ctrl: boolean; shift: boolean; alt: boolean; key: string };

function parse(keys: string): Step[] {
  return keys.split(' ').map((step) => {
    const parts = step.split('+');
    const key = parts.pop() || '+';
    return { ctrl: parts.includes('Ctrl'), shift: parts.includes('Shift'), alt: parts.includes('Alt'), key: key.length === 1 ? key.toLowerCase() : key };
  });
}

function matches(step: Step, e: KeyboardEvent) {
  const pressed = e.key.length === 1 ? e.key.toLowerCase() : e.key;
  const wanted = step.key === 'Esc' ? 'Escape' : step.key;
  const sameKey = pressed === wanted || (step.key === '/' && e.code === 'Slash') || (/^\d$/.test(step.key) && e.code === `Digit${step.key}`);
  return step.ctrl === (e.ctrlKey || e.metaKey) && step.shift === e.shiftKey && step.alt === e.altKey && sameKey;
}

export function useCommands(commands: Shortcut[], deps: unknown[] = []) {
  const book = useContext(BookContext);
  useEffect(() => book?.add(commands), deps);
}

export function CommandBook({ children }: { children: ReactNode }) {
  const [entries, setEntries] = useState<Shortcut[]>([]);
  const [palette, setPalette] = useState<'all' | 'keys' | null>(null);
  const waiting = useRef<Shortcut | null>(null);
  const book = useMemo<Book>(() => ({
    add(commands) {
      setEntries((all) => [...all, ...commands]);
      return () => setEntries((all) => all.filter((c) => !commands.includes(c)));
    },
  }), []);

  useEffect(() => {
    const down = (e: KeyboardEvent) => {
      if (e.defaultPrevented || palette) return;
      if (e.ctrlKey && e.shiftKey && e.key.toLowerCase() === 'p') {
        e.preventDefault();
        setPalette('all');
        return;
      }
      if (e.ctrlKey && !e.shiftKey && (e.code === 'Slash' || e.key === '/')) {
        e.preventDefault();
        setPalette('keys');
        return;
      }
      const typing = e.target instanceof HTMLElement && (e.target.isContentEditable || /^(INPUT|TEXTAREA|SELECT)$/.test(e.target.tagName));
      for (const command of entries) {
        if (!command.keys || !command.run || (command.when && !command.when())) continue;
        const steps = parse(command.keys);
        if (typing && !steps[0].ctrl && !steps[0].alt && steps[0].key !== 'Esc') continue;
        if (steps.length === 2 && waiting.current === command && matches(steps[1], e)) {
          e.preventDefault();
          waiting.current = null;
          command.run();
          return;
        }
        if (matches(steps[0], e)) {
          e.preventDefault();
          if (steps.length === 1) command.run();
          else waiting.current = command;
          return;
        }
      }
      waiting.current = null;
    };
    addEventListener('keydown', down);
    return () => removeEventListener('keydown', down);
  }, [entries, palette]);

  return (
    <BookContext.Provider value={book}>
      {children}
      {palette && <Palette entries={entries} keysOnly={palette === 'keys'} onClose={() => setPalette(null)} />}
    </BookContext.Provider>
  );
}

function Palette({ entries, keysOnly, onClose }: { entries: Shortcut[]; keysOnly: boolean; onClose: () => void }) {
  const [query, setQuery] = useState('');
  const [lit, setLit] = useState(0);
  const [path, setPath] = useState<Shortcut | null>(null);
  const list = useRef<HTMLDivElement>(null);
  const prefix = !path && !keysOnly ? /^([>@/#])\s?/.exec(query) : null;
  const scope = (prefix?.[1] ?? null) as '>' | Scope | null;
  const text = (prefix ? query.slice(prefix[0].length) : query).toLowerCase();
  const usable = entries.filter((c) => (keysOnly ? c.keys : c.run || c.choices) && (!c.when || c.when()));
  const present = scopes.filter((s) => usable.some((c) => (c.scope ?? '>') === s.key));
  const shown = path
    ? path.choices!.map((c) => ({ title: c.title, group: path.title, run: c.run } as Shortcut)).filter((c) => c.title.toLowerCase().includes(text))
    : usable.filter((c) => (!scope || (c.scope ?? '>') === scope) && `${c.group ?? ''} ${c.title}`.toLowerCase().includes(text));

  useEffect(() => {
    const row = list.current?.querySelector<HTMLElement>('.palette-item.lit');
    if (!row || !list.current) return;
    const box = list.current;
    if (row.offsetTop < box.scrollTop) box.scrollTop = row.offsetTop - 24;
    else if (row.offsetTop + row.offsetHeight > box.scrollTop + box.clientHeight) box.scrollTop = row.offsetTop + row.offsetHeight - box.clientHeight;
  }, [lit, query, path]);

  const choose = (c: Shortcut | undefined) => {
    if (!c) return;
    if (c.choices && !path) {
      setPath(c);
      setQuery('');
      setLit(0);
      return;
    }
    onClose();
    if (c.run) setTimeout(c.run, 0);
  };
  const key = (e: KeyEvent) => {
    if (e.key === 'ArrowDown' || e.key === 'ArrowUp') {
      e.preventDefault();
      setLit((l) => (l + (e.key === 'ArrowDown' ? 1 : -1) + shown.length) % Math.max(1, shown.length));
    }
    else if (e.key === 'Enter') {
      e.preventDefault();
      choose(shown[lit]);
    }
    else if (e.key === 'Tab') {
      e.preventDefault();
      if (shown[lit]?.choices && !path) choose(shown[lit]);
    }
    else if (e.key === 'Backspace' && !query && path) {
      e.preventDefault();
      setPath(null);
      setLit(0);
    }
    else if (e.key === 'Escape' && path) {
      e.preventDefault();
      e.stopPropagation();
      setPath(null);
      setQuery('');
      setLit(0);
    }
  };

  let group: string | undefined;
  return (
    <Dialog.Root open onOpenChange={(open) => !open && onClose()}>
      <Dialog.Portal>
        <Dialog.Overlay className="scrim" />
        <Dialog.Content className="palette" aria-describedby={undefined}>
          <Dialog.Title className="hidden">Commands</Dialog.Title>
          <div className="palette-top">
            <SearchField
              autoFocus
              value={query}
              placeholder={keysOnly ? 'Search shortcuts' : path ? `${path.title} ›` : present.length > 1 ? 'Commands, ' + present.filter((s) => s.key !== '>').map((s) => `${s.key}${s.name.toLowerCase()}`).join(', ') : 'Type a command'}
              keys={keysOnly ? 'Ctrl+/' : 'Ctrl+Shift+P'}
              onInput={(v) => { setQuery(v); setLit(0); }}
              onKeyDown={key}
            />
            {!keysOnly && !path && present.length > 1 && (
              <div className="palette-scopes">
                <button className={`btn chip ${scope === null ? 'chosen' : ''}`} onClick={() => { setQuery(''); setLit(0); }}>All</button>
                {present.map((s) => (
                  <button key={s.key} className={`btn chip ${scope === s.key ? 'chosen' : ''}`} onClick={() => { setQuery(`${s.key} `); setLit(0); }}>
                    {s.name} <small>{s.key}</small>
                  </button>
                ))}
              </div>
            )}
          </div>
          <div className="palette-list" ref={list}>
            {shown.map((c, i) => {
              const head = (c.group ?? '') !== group ? (group = c.group ?? '') : null;
              return (
                <div key={`${c.group}-${c.title}-${i}`}>
                  {head && <div className="palette-group">{head}</div>}
                  <div className={`palette-item ${i === lit ? 'lit' : ''}`} onMouseMove={() => i !== lit && setLit(i)} onClick={() => choose(c)}>
                    <span className="palette-title">{c.title}</span>
                    {c.choices && !path && <span className="palette-more"><Icon d={glyphs.chevron} size={12} /></span>}
                    {c.keys && <Keys keys={c.keys} />}
                  </div>
                </div>
              );
            })}
            {shown.length === 0 && <div className="palette-empty">Nothing matches</div>}
          </div>
          <div className="palette-foot">
            {path ? <span><kbd>⌫</kbd> back</span> : <span><kbd>Tab</kbd> open ›</span>}
            <span><kbd>↑↓</kbd> move</span>
            <span><kbd>⏎</kbd> run</span>
            <span><kbd>Esc</kbd> close</span>
          </div>
        </Dialog.Content>
      </Dialog.Portal>
    </Dialog.Root>
  );
}
