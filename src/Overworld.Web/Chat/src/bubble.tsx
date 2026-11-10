import { useEffect, useLayoutEffect, useRef, useState, type KeyboardEvent } from 'react';
import { ArrowUp, ChevronDown, RotateCcw, Square, X } from 'lucide-react';
import { AllayIcon, useCommands, useObj, useList, run, can, put, obj, type Obj } from '@overworld/kit';
import { LoanView, MessageList } from './messages';
import { Button } from './ui/button';
import { cn } from './ui/cn';

const NearBottom = 120;

function useFollow(deps: unknown[]) {
  const scroller = useRef<HTMLDivElement>(null);
  const content = useRef<HTMLDivElement>(null);
  const [atBottom, setAtBottom] = useState(true);
  const paused = useRef(false);
  const near = () => {
    const el = scroller.current;
    return !el || el.scrollHeight - el.scrollTop - el.clientHeight < NearBottom;
  };
  const toBottom = (behavior: ScrollBehavior = 'auto') => scroller.current?.scrollTo({ top: scroller.current.scrollHeight, behavior });
  useEffect(() => {
    const el = content.current;
    if (!el) return;
    const observer = new ResizeObserver(() => {
      if (!paused.current) toBottom();
      setAtBottom(near());
    });
    observer.observe(el);
    return () => observer.disconnect();
  }, []);
  useLayoutEffect(() => {
    if (!paused.current) toBottom();
  }, deps);
  const scrolled = () => {
    paused.current = !near();
    setAtBottom(!paused.current);
  };
  const jump = () => {
    paused.current = false;
    toBottom('smooth');
    setAtBottom(true);
  };
  return { scroller, content, atBottom, scrolled, jump };
}

function Composer({ chat, box }: { chat: Obj; box: React.RefObject<HTMLTextAreaElement | null> }) {
  const p = chat.p;
  const sent = useRef<{ value: string; at: number }[]>([]);
  const [local, setLocal] = useState<string>(p.draft ?? '');

  useEffect(() => {
    const value = p.draft ?? '';
    const now = Date.now();
    sent.current = sent.current.filter((s) => now - s.at < 1500);
    if (sent.current.some((s) => s.value === value)) return;
    setLocal(value);
  }, [p.draft]);

  useEffect(() => {
    const area = box.current;
    if (!area) return;
    area.style.height = 'auto';
    area.style.height = `${Math.min(area.scrollHeight, 160)}px`;
  }, [local]);

  const write = (value: string) => {
    setLocal(value);
    sent.current.push({ value, at: Date.now() });
    put(chat, 'Draft', value);
  };

  const send = () => {
    if (!can(chat, 'SendCommand')) return;
    sent.current = [];
    run(chat, 'SendCommand');
  };

  const key = (e: KeyboardEvent<HTMLTextAreaElement>) => {
    if (e.ctrlKey || e.altKey || e.metaKey) return;
    if (e.key === 'Enter' && !e.shiftKey) {
      e.preventDefault();
      send();
    }
  };

  return (
    <div className="bubble-composer">
      <textarea
        ref={box}
        rows={1}
        value={local}
        placeholder={p.placeholder}
        disabled={!p.agent}
        onChange={(e) => write(e.target.value)}
        onKeyDown={key}
      />
      {p.isWorking ? (
        <Button variant="secondary" size="icon-sm" className="rounded-full border" aria-label="Stop" title="Stop" onClick={() => run(chat, 'StopCommand')}><Square className="fill-current" /></Button>
      ) : (
        <Button size="icon-sm" className="rounded-full" aria-label="Send" title="Send · Enter" disabled={!can(chat, 'SendCommand')} onClick={send}><ArrowUp /></Button>
      )}
    </div>
  );
}

function Hello({ chat }: { chat: Obj }) {
  const p = chat.p;
  const suggestions: string[] = p.suggestions ?? [];
  return (
    <div className="bubble-hello">
      <AllayIcon color={p.color} size={40} />
      <b>{p.agent ? `Talk with ${p.agent}` : 'No agent to talk to'}</b>
      {p.problem && <span className="text-xs text-removed">{p.problem}</span>}
      {p.agent && suggestions.length > 0 && (
        <div className="bubble-suggestions">
          {suggestions.map((s) => <button key={s} type="button" onClick={() => run(chat, 'SuggestCommand', s)}>{s}</button>)}
        </div>
      )}
    </div>
  );
}

function Spread({ chat }: { chat: Obj }) {
  const spread = useObj(chat.p.spread);
  const loan = useObj(spread?.p.loan);
  useEffect(() => {
    if (loan) put(loan, 'IsLive', true);
  }, [loan?.id]);
  if (!spread || !loan) return null;
  return (
    <div className="chat bubble-spread">
      <div className="flex h-10 shrink-0 items-center gap-2 border-b bg-card pr-2 pl-4">
        <span className="rounded-full bg-muted px-2 py-px text-[11px] text-muted-foreground">{loan.p.source}</span>
        <span className="min-w-0 flex-1 truncate text-sm font-medium">{loan.p.title}</span>
        <span className="text-xs text-muted-foreground">Esc closes</span>
        <Button variant="ghost" size="icon-sm" aria-label="Close" onClick={() => run(chat, 'CloseSpreadCommand')}><X /></Button>
      </div>
      <div className="min-h-0 flex-1"><LoanView id={loan.id} /></div>
    </div>
  );
}

function Panel({ chat }: { chat: Obj }) {
  const p = chat.p;
  const refs = useList(p.timeline?.shown);
  const box = useRef<HTMLTextAreaElement>(null);
  const follow = useFollow([refs]);
  const empty = refs.length === 0 && !p.isWorking;

  useEffect(() => {
    requestAnimationFrame(() => box.current?.focus());
  }, []);

  return (
    <div className="chat bubble-panel chat-pop" role="dialog" aria-label="Chat">
      <div className="bubble-head">
        <AllayIcon color={p.color} size={24} state={p.isWorking ? 'working' : 'idle'} />
        <b className="min-w-0 flex-1 truncate">{p.agent ?? 'Agent'}</b>
        <Button variant="ghost" size="icon-sm" aria-label="New chat" title="New chat" disabled={!can(chat, 'RestartCommand') || empty} onClick={() => run(chat, 'RestartCommand')}><RotateCcw /></Button>
        <Button variant="ghost" size="icon-sm" aria-label="Close" title="Close · Esc" onClick={() => run(chat, 'CloseCommand')}><X /></Button>
      </div>
      {empty ? (
        <Hello chat={chat} />
      ) : (
        <div className="relative min-h-0 flex-1">
          <div ref={follow.scroller} onScroll={follow.scrolled} className="scroller absolute inset-0 overflow-y-auto">
            <div ref={follow.content}>
              <MessageList conversation={chat} refs={refs} />
              {p.problem && <p className="px-4 pb-3 text-xs text-removed">{p.problem}</p>}
            </div>
          </div>
          {!follow.atBottom && (
            <Button variant="secondary" size="icon" onClick={follow.jump} aria-label="Go to the last message" className="absolute right-3 bottom-3 size-9 rounded-full border shadow-lg"><ChevronDown /></Button>
          )}
        </div>
      )}
      <Composer chat={chat} box={box} />
    </div>
  );
}

export function ChatBubble({ id }: { id: any }) {
  const chat = useObj(id);
  const open = !!chat?.p.isOpen;
  useCommands([
    { title: 'Chat with the agent', group: 'Window', keys: 'Ctrl+J', run: () => run(obj(id), 'ToggleCommand') },
  ], [id]);
  useEffect(() => {
    if (!open) return;
    const key = (e: globalThis.KeyboardEvent) => {
      if (e.key !== 'Escape' || e.defaultPrevented) return;
      const now = obj(id);
      if (!now) return;
      e.preventDefault();
      run(now, now.p.spread ? 'CloseSpreadCommand' : 'CloseCommand');
    };
    addEventListener('keydown', key, true);
    return () => removeEventListener('keydown', key, true);
  }, [open, id]);
  if (!chat) return null;
  const p = chat.p;
  return (
    <>
      {open ? (
        <Panel key={p.timeline?.$id} chat={chat} />
      ) : (
        <button type="button" className={cn('bubble-fab', p.isWorking && 'working')} style={{ ['--agent' as string]: p.color ?? 'var(--accent)' }} title="Chat · Ctrl+J" aria-label="Open the chat" onClick={() => run(chat, 'ToggleCommand')}>
          <AllayIcon color={p.color} size={28} state={p.isWorking ? 'working' : 'idle'} />
        </button>
      )}
      {open && <Spread chat={chat} />}
    </>
  );
}
