import { createContext, useContext, useEffect, useMemo, useRef, useState, type ReactNode } from 'react';
import { Command } from 'cmdk';
import * as Dialog from '@radix-ui/react-dialog';
import { Keys } from './ui';

export type Shortcut = { title: string; group?: string; keys?: string; run?: () => void; when?: () => boolean };

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
      for (const command of entries) {
        if (!command.keys || !command.run || (command.when && !command.when())) continue;
        const steps = parse(command.keys);
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

  const shown = entries.filter((c) => (palette === 'keys' ? c.keys : c.run));
  const choose = (c: Shortcut) => {
    setPalette(null);
    if (c.run) setTimeout(c.run, 0);
  };

  return (
    <BookContext.Provider value={book}>
      {children}
      <Dialog.Root open={palette !== null} onOpenChange={(open) => !open && setPalette(null)}>
        <Dialog.Portal>
          <Dialog.Overlay className="scrim" />
          <Dialog.Content className="palette" aria-describedby={undefined}>
            <Dialog.Title className="hidden">Commands</Dialog.Title>
            <Command loop>
              <Command.Input className="palette-input" placeholder={palette === 'keys' ? 'Search shortcuts' : 'Type a command'} autoFocus />
              <Command.List className="palette-list">
                <Command.Empty className="palette-empty">Nothing matches</Command.Empty>
                {shown.map((c, i) => (
                  <Command.Item key={`${c.group}-${c.title}-${i}`} className="palette-item" value={`${c.group ?? ''} ${c.title} ${i}`} onSelect={() => choose(c)}>
                    <span className="palette-group">{c.group}</span>
                    <span className="palette-title">{c.title}</span>
                    {c.keys && <Keys keys={c.keys} />}
                  </Command.Item>
                ))}
              </Command.List>
            </Command>
            <div className="palette-hints"><span>↑↓ move</span><span>Enter run</span><span>Esc close</span></div>
          </Dialog.Content>
        </Dialog.Portal>
      </Dialog.Root>
    </BookContext.Provider>
  );
}
