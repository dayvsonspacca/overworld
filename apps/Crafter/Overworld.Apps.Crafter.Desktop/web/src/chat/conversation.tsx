import { useCallback, useEffect, useLayoutEffect, useRef, useState, type DragEvent } from 'react';
import { ChevronDown, Loader2, Lock, LockOpen, X } from 'lucide-react';
import { blob, obj, useObj, useList, run, put, invoke, ask, idOf, on, type Obj } from '@overworld/kit';
import { Button, ChatCards, LoanView, MessageList, cn } from '@overworld/chat';
import { Composer, attachPicture } from './composer';
import { ProposalCards } from './proposals';
import './chat.css';

const FollowKey = 'crafter.follow';
const NearBottom = 120;
const NearTop = 400;
const Pages = 3;

function useAutoScroll(deps: unknown[]) {
  const scrollRef = useRef<HTMLDivElement>(null);
  const contentRef = useRef<HTMLDivElement>(null);
  const [follow, setFollowState] = useState(() => localStorage.getItem(FollowKey) !== '0');
  const [atBottom, setAtBottom] = useState(true);
  const paused = useRef(false);

  const nearBottom = useCallback(() => {
    const el = scrollRef.current;
    return !el || el.scrollHeight - el.scrollTop - el.clientHeight < NearBottom;
  }, []);

  const scrollToBottom = useCallback((behavior: ScrollBehavior = 'auto') => {
    scrollRef.current?.scrollTo({ top: scrollRef.current.scrollHeight, behavior });
  }, []);

  const onScroll = useCallback(() => {
    const near = nearBottom();
    paused.current = !near;
    setAtBottom(near);
  }, [nearBottom]);

  useEffect(() => {
    const content = contentRef.current;
    if (!content) return;
    const observer = new ResizeObserver(() => {
      if (follow && !paused.current) scrollToBottom();
      setAtBottom(nearBottom());
    });
    observer.observe(content);
    return () => observer.disconnect();
  }, [follow, nearBottom, scrollToBottom]);

  useLayoutEffect(() => {
    const el = scrollRef.current;
    if (el && follow && !paused.current) el.scrollTop = el.scrollHeight;
  }, deps);

  const jumpToBottom = useCallback(() => {
    paused.current = false;
    scrollToBottom('smooth');
    setAtBottom(true);
  }, [scrollToBottom]);

  const setFollow = useCallback((next: boolean) => {
    localStorage.setItem(FollowKey, next ? '1' : '0');
    setFollowState(next);
    if (next) {
      paused.current = false;
      scrollToBottom('smooth');
      setAtBottom(true);
    }
  }, [scrollToBottom]);

  const release = useCallback(() => {
    paused.current = false;
  }, []);

  return { scrollRef, contentRef, onScroll, atBottom, follow, setFollow, jumpToBottom, release };
}

function useEarlier(conversation: Obj, scrollRef: React.RefObject<HTMLDivElement | null>) {
  const timeline = conversation.p.timeline;
  const [state, setState] = useState<'idle' | 'loading' | 'done'>('idle');
  const [brought, setBrought] = useState(false);
  const anchor = useRef<{ height: number; top: number } | null>(null);
  const busy = useRef(false);

  const load = useCallback(async () => {
    const el = scrollRef.current;
    if (busy.current || state === 'done' || !timeline?.$id || !el) return;
    busy.current = true;
    setState('loading');
    let ended = false;
    for (let page = 0; page < Pages; page++) {
      anchor.current = { height: el.scrollHeight, top: el.scrollTop };
      const more = await invoke<number | null>({ id: timeline.$id } as Obj, 'ShowEarlier');
      if (!more) {
        ended = more === 0;
        break;
      }
      setBrought(true);
    }
    busy.current = false;
    setState(ended ? 'done' : 'idle');
  }, [scrollRef, state, timeline?.$id]);

  const keep = useCallback(() => {
    const el = scrollRef.current;
    const was = anchor.current;
    if (!el || !was) return;
    el.scrollTop = was.top + (el.scrollHeight - was.height);
    anchor.current = null;
  }, [scrollRef]);

  return { state, load, keep, brought };
}

function Conversation({ id }: { id: any }) {
  const conversation = useObj(id);
  if (!conversation) return null;
  return <Chat key={conversation.id} conversation={conversation} />;
}

function Chat({ conversation }: { conversation: Obj }) {
  const refs = useList(conversation.p.timeline?.shown);
  const box = useRef<HTMLTextAreaElement>(null);
  const [dropping, setDropping] = useState(false);
  const scroll = useAutoScroll([refs]);
  const earlier = useEarlier(conversation, scroll.scrollRef);
  const newest = useRef<number | null>(null);

  useLayoutEffect(() => earlier.keep(), [refs]);

  useEffect(() => on('focus-composer', () => box.current?.focus()), []);
  useEffect(() => {
    requestAnimationFrame(() => box.current?.focus());
  }, []);

  useEffect(() => {
    const prompts = refs.filter((ref) => obj(ref)?.type === 'PromptEntry');
    const last = prompts.length ? idOf(prompts[prompts.length - 1]) : null;
    if (newest.current !== null && last !== newest.current) {
      scroll.release();
      scroll.jumpToBottom();
    }
    newest.current = last;
  }, [refs]);

  useEffect(() => {
    const el = scroll.scrollRef.current;
    if (el && el.scrollHeight <= el.clientHeight + NearTop) earlier.load();
  }, [refs, earlier.state]);

  const scrolled = () => {
    scroll.onScroll();
    const el = scroll.scrollRef.current;
    if (el && el.scrollTop < NearTop) earlier.load();
  };

  const drop = async (e: DragEvent) => {
    e.preventDefault();
    setDropping(false);
    const target = { $ref: conversation.id };
    const uris = e.dataTransfer.getData('text/uri-list');
    const paths = uris.split(/\r?\n/).map((l) => l.trim()).filter((l) => l.startsWith('file://')).map((l) => decodeURIComponent(new URL(l).pathname));
    if (paths.length > 0) return void ask('attach', { conversation: target, paths });
    const files = [...e.dataTransfer.files].filter((f) => f.type.startsWith('image/'));
    for (const file of files) await attachPicture(conversation, file);
    const text = e.dataTransfer.getData('text/plain');
    if (files.length === 0 && text) put(conversation, 'Draft', `${conversation.p.draft ?? ''}${text}`);
  };

  return (
    <div className="chat relative flex min-h-0 flex-1 flex-col" onDragOver={(e) => { e.preventDefault(); setDropping(true); }} onDragLeave={() => setDropping(false)} onDrop={drop}>
      <div className="relative min-h-0 flex-1">
        <div ref={scroll.scrollRef} onScroll={scrolled} className="scroller absolute inset-0 overflow-y-auto">
          <div ref={scroll.contentRef} className="mx-auto w-full max-w-3xl">
            {earlier.state === 'loading' && (
              <div className="flex items-center justify-center gap-2 pt-4 text-xs text-muted-foreground">
                <Loader2 className="size-3.5 animate-spin" /> Loading earlier messages…
              </div>
            )}
            {earlier.state === 'done' && earlier.brought && <p className="pt-4 text-center text-xs text-muted-foreground">The start of the chat</p>}
            {refs.length === 0 && !conversation.p.isWorking && <p className="p-4 text-center text-sm text-muted-foreground">No messages yet.</p>}
            <ChatCards.Provider value={ProposalCards}><MessageList conversation={conversation} refs={refs} /></ChatCards.Provider>
          </div>
        </div>
        <div className="pointer-events-none absolute right-3 bottom-3 z-10 flex flex-col items-end gap-2">
          {!scroll.atBottom && (
            <Button variant="secondary" size="icon" onClick={scroll.jumpToBottom} aria-label="Go to the last message" className="pointer-events-auto size-10 rounded-full border shadow-lg">
              <ChevronDown />
            </Button>
          )}
          <Button
            variant="secondary"
            size="icon"
            onClick={() => scroll.setFollow(!scroll.follow)}
            aria-pressed={scroll.follow}
            title={scroll.follow ? 'Following the end: click to let go' : 'Let go: click to follow the end'}
            className={cn('pointer-events-auto size-9 rounded-full border shadow-md backdrop-blur', scroll.follow ? 'bg-background/70 text-muted-foreground' : 'bg-background text-foreground')}
          >
            {scroll.follow ? <Lock /> : <LockOpen />}
          </Button>
        </div>
      </div>
      <Composer conversation={conversation} box={box} />
      {dropping && (
        <div className="pointer-events-none absolute inset-2 z-20 grid place-items-center rounded-2xl border-2 border-dashed border-brand bg-background/80 text-base">
          Drop to attach
        </div>
      )}
    </div>
  );
}

export { Conversation };

export function Viewer({ id }: { id: any }) {
  const viewer = useObj(id);
  const open = !!viewer?.p.isOpen && !!viewer.p.current;
  const source = open ? blob(viewer!.p.current.data) : undefined;

  const copy = async () => {
    if (!source) return;
    const picture = await (await fetch(source)).blob();
    await navigator.clipboard.write([new ClipboardItem({ [picture.type]: picture })]).catch(() => undefined);
  };

  useEffect(() => {
    if (!open) return;
    const key = (e: KeyboardEvent) => {
      if (e.key === 'Escape') { e.preventDefault(); run(viewer, 'CloseCommand'); }
      else if (e.key === 'ArrowRight') run(viewer, 'NextCommand');
      else if (e.key === 'ArrowLeft') run(viewer, 'PreviousCommand');
      else if (e.ctrlKey && e.key.toLowerCase() === 'c' && !getSelection()?.toString()) { e.preventDefault(); copy(); }
    };
    addEventListener('keydown', key, true);
    return () => removeEventListener('keydown', key, true);
  }, [open, source]);

  if (!open) return null;
  const p = viewer!.p;
  const outside = async () => {
    const file = await invoke(viewer, 'FileOfCurrent');
    if (file) ask('open-outside', { path: file });
  };
  return (
    <div className="chat fixed inset-x-0 top-11 bottom-0 z-50 flex flex-col items-center justify-center gap-3 bg-[var(--scrim)] p-6 backdrop-blur-sm" onMouseDown={(e) => e.target === e.currentTarget && run(viewer, 'CloseCommand')}>
      <img src={source} alt={p.current.label} className="max-h-[calc(100%-4rem)] max-w-full rounded-xl object-contain shadow-2xl" />
      <div className="flex w-full max-w-3xl items-center gap-2 rounded-full bg-card px-3 py-1.5 text-sm ring-1 ring-foreground/10">
        <span className="truncate">{p.current.label}</span>
        {p.hasOthers && <span className="font-mono text-xs text-muted-foreground">{p.position}</span>}
        <span className="flex-1" />
        <Button variant="ghost" size="sm" onClick={copy}>Copy</Button>
        <Button variant="ghost" size="sm" onClick={outside}>Open outside</Button>
        <Button variant="ghost" size="icon-sm" aria-label="Close" onClick={() => run(viewer, 'CloseCommand')}><X /></Button>
      </div>
    </div>
  );
}

export function Stage({ id }: { id: any }) {
  const conversation = useObj(id);
  const spread = useObj(conversation?.p.spread);
  const loan = useObj(spread?.p.loan);
  useEffect(() => {
    if (!spread) return;
    const key = (e: KeyboardEvent) => {
      if (e.key === 'Escape') { e.preventDefault(); run(conversation, 'CloseSpreadCommand'); }
    };
    addEventListener('keydown', key, true);
    return () => removeEventListener('keydown', key, true);
  }, [spread?.id]);
  useEffect(() => {
    if (loan) put(loan, 'IsLive', true);
  }, [loan?.id]);
  if (!spread || !loan) return null;
  return (
    <div className="chat fixed inset-x-0 top-11 bottom-0 z-40 flex flex-col bg-background">
      <div className="flex h-10 shrink-0 items-center gap-2 border-b bg-card pr-2 pl-4">
        <span className="rounded-full bg-muted px-2 py-px text-[11px] text-muted-foreground">{loan.p.source}</span>
        <span className="min-w-0 flex-1 truncate text-sm font-medium">{loan.p.title}</span>
        <span className="text-xs text-muted-foreground">Esc closes</span>
        <Button variant="ghost" size="icon-sm" aria-label="Close" onClick={() => run(conversation, 'CloseSpreadCommand')}><X /></Button>
      </div>
      <div className="min-h-0 flex-1"><LoanView id={loan.id} /></div>
    </div>
  );
}
