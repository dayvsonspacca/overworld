import { createContext, memo, useContext, useEffect, useRef, useState, type ComponentType, type ReactNode } from 'react';
import { Check, ChevronLeft, ChevronRight, Copy, FileText, Folder, Info, Sparkles } from 'lucide-react';
import { AllayIcon, ItemFrame, blob, can, hex, obj, useObj, run, put, idOf, type Obj } from '@overworld/kit';
import { Button } from './ui/button';
import { cn } from './ui/cn';
import { Acts, Node } from './thread';
import { PermissionCard, QuestionsCard } from './cards';
import { Markdown, StreamingMarkdown } from './markdown';

export type Cards = Record<string, ComponentType<{ entry: Obj }>>;

export const ChatCards = createContext<Cards>({});

const Steps = new Set(['ActEntry', 'ThoughtEntry', 'PlanEntry']);

type Item = { key: number; kind: 'steps'; refs: any[] } | { key: number; kind: 'entry'; ref: any };

export function itemsOf(refs: any[]): Item[] {
  const items: Item[] = [];
  for (const ref of refs) {
    const id = idOf(ref)!;
    const type = obj(ref)?.type ?? '';
    const last = items.at(-1);
    if (Steps.has(type)) {
      if (last?.kind === 'steps') last.refs.push(ref);
      else items.push({ key: id, kind: 'steps', refs: [ref] });
    } else {
      items.push({ key: id, kind: 'entry', ref });
    }
  }
  return items;
}

function useArrivals(scope: number, keys: number[]): (key: number) => boolean {
  const seen = useRef(new Set<string>());
  const opened = useRef(new Set<number>());
  useEffect(() => {
    if (!keys.length) return;
    opened.current.add(scope);
    for (const k of keys) seen.current.add(`${scope}:${k}`);
  });
  return (key) => opened.current.has(scope) && !seen.current.has(`${scope}:${key}`);
}

function Arrival({ fresh, delay, className, children }: { fresh: boolean; delay?: number; className?: string; children: ReactNode }) {
  const [animate] = useState(fresh);
  return <div className={cn(animate && 'chat-enter', className)} style={animate && delay ? { animationDelay: `${delay}ms` } : undefined}>{children}</div>;
}

const Mention = /(^|\s)(@[^\s]+)/g;

function Chip({ name, title, dir, skill }: { name: string; title: string; dir?: boolean; skill?: boolean }) {
  const Icon = skill ? Sparkles : dir ? Folder : FileText;
  return (
    <span title={title} className="mx-0.5 inline-flex max-w-full items-center gap-1 rounded bg-primary-foreground/15 px-1.5 align-bottom font-mono text-[0.85em]">
      <Icon className="size-3 shrink-0" />
      <span className="truncate">{name}</span>
    </span>
  );
}

function withMentions(text: string): ReactNode[] {
  const out: ReactNode[] = [];
  let last = 0;
  for (const m of text.matchAll(Mention)) {
    const at = m.index! + m[1].length;
    out.push(text.slice(last, at));
    const path = m[2].slice(1);
    out.push(<Chip key={at} name={path.replace(/\/$/, '').split('/').pop()!} title={path} dir={path.endsWith('/')} />);
    last = at + m[2].length;
  }
  out.push(text.slice(last));
  return out;
}

function Thumb({ view, conversation, className }: { view: any; conversation: Obj; className?: string }) {
  if (!view) return null;
  return (
    <button type="button" title="Open the picture" onClick={() => run(conversation, 'OpenPictureCommand', view)} className={cn('cursor-zoom-in overflow-hidden rounded-lg border bg-muted', className)}>
      <img src={blob(view.data)} alt={view.label} className="h-full w-full object-cover" />
    </button>
  );
}

function UserBubble({ entry, conversation }: { entry: Obj; conversation: Obj }) {
  const p = entry.p;
  const files: string[] = p.files ?? [];
  const skills: string[] = p.skills ?? [];
  return (
    <div className="group flex flex-col items-end gap-1.5">
      {p.hasPictures && (
        <div className="flex max-w-[85%] flex-wrap justify-end gap-1.5">
          {p.pictures.map((v: any) => <Thumb key={v.$id} view={v} conversation={conversation} className="h-28 max-w-40" />)}
        </div>
      )}
      {(p.hasText || files.length > 0 || skills.length > 0) && (
        <div className="min-w-0 max-w-[85%] rounded-2xl rounded-br-md bg-primary px-3.5 py-2 text-sm whitespace-pre-wrap break-words text-primary-foreground">
          {withMentions(p.text ?? '')}
          {p.hasText && (files.length > 0 || skills.length > 0) ? '\n' : null}
          {skills.map((s) => <Chip key={`s${s}`} name={s} title={`/${s}`} skill />)}
          {files.map((f) => <Chip key={f} name={f.replace(/\/$/, '').split('/').pop()!} title={f} dir={f.endsWith('/')} />)}
        </div>
      )}
    </div>
  );
}

function CopyMarkdown({ text }: { text: string }) {
  const [copied, setCopied] = useState(false);
  useEffect(() => {
    if (!copied) return;
    const timer = setTimeout(() => setCopied(false), 1500);
    return () => clearTimeout(timer);
  }, [copied]);
  return (
    <Button variant="ghost" size="xs" className="-ml-2 text-muted-foreground" title="Copy the answer as Markdown" onClick={() => navigator.clipboard.writeText(text).then(() => setCopied(true))}>
      {copied ? <Check /> : <Copy />}
      {copied ? 'Copied' : 'Copy'}
    </Button>
  );
}

function AssistantMessage({ entry, final, streaming }: { entry: Obj; final: boolean; streaming: boolean }) {
  const text: string = entry.p.text ?? '';
  return (
    <div className="group/answer">
      {streaming ? <StreamingMarkdown text={text} className="opacity-90" /> : <Markdown text={text} />}
      {final && !streaming && (
        <div className="mt-1.5 flex items-center gap-1.5 opacity-0 transition-opacity duration-150 group-hover/answer:opacity-100 focus-within:opacity-100">
          <CopyMarkdown text={text} />
        </div>
      )}
    </div>
  );
}

function LoanView({ id }: { id: any }) {
  const loan = useObj(id);
  if (!loan) return null;
  const p = loan.p;
  if (loan.type === 'PaintingLoan') return <iframe className="h-full w-full border-0 bg-white" sandbox="allow-scripts allow-popups" srcDoc={p.html} />;
  if (loan.type === 'PaletteLoan') {
    return (
      <div className="flex h-full">
        {(p.swatches ?? []).map((s: any) => (
          <div key={s.$id} className="flex flex-1 items-end justify-center pb-6 font-mono text-xs" style={{ background: hex(s.fill), color: s.ink }}>{s.label}</div>
        ))}
      </div>
    );
  }
  return (
    <div className="flex h-full flex-col gap-3 overflow-y-auto p-3">
      {(p.groups ?? []).map((group: any) => (
        <div key={group.$id} className="space-y-1.5">
          <div className="text-[11px] font-medium tracking-wide text-muted-foreground uppercase">{group.title}</div>
          <div className="flex flex-wrap gap-1.5">
            {group.swatches.map((s: any) => (
              <span key={s.$id} className="flex items-center gap-1.5 rounded-md bg-card px-2 py-1 text-[11px]">
                <i className="size-4 rounded border" style={{ background: hex(s.hex) }} />
                {s.role}
                <code className="text-muted-foreground">{s.hex}</code>
              </span>
            ))}
          </div>
        </div>
      ))}
    </div>
  );
}

export { LoanView };

function PaintingVersions({ loan }: { loan: Obj }) {
  const p = loan.p;
  if (p.versions < 2) return null;
  return (
    <span className="painting-versions">
      <span className={cn('painting-version', !p.isNewest && 'older')}>v{p.shown}{!p.isNewest && ` · v${p.versions} is newer`}{p.isNewest && !p.isPainted && ' · newest'}</span>
      <Button variant="ghost" size="icon-xs" aria-label="Previous version" disabled={!can(loan, 'PreviousCommand')} onClick={() => run(loan, 'PreviousCommand')}><ChevronLeft /></Button>
      <Button variant="ghost" size="icon-xs" aria-label="Next version" disabled={!can(loan, 'NextCommand')} onClick={() => run(loan, 'NextCommand')}><ChevronRight /></Button>
    </span>
  );
}

function Loan({ entry, conversation }: { entry: Obj; conversation: Obj }) {
  const loan = useObj(entry.p.loan);
  if (!loan) return null;
  const painting = loan.type === 'PaintingLoan';
  return (
    <ItemFrame
      source={loan.p.source}
      title={loan.p.title}
      actions={painting ? <PaintingVersions loan={loan} /> : undefined}
      ready={!painting || !!loan.p.html}
      onExpand={() => run(conversation, 'ExpandLoanCommand', { $ref: entry.id })}
      onSeen={(seen) => put(loan, 'IsLive', seen)}
    >
      <LoanView id={loan.id} />
    </ItemFrame>
  );
}

function EntryView({ id, conversation, final, streaming }: { id: number; conversation: Obj; final: boolean; streaming: boolean }) {
  const entry = useObj(id);
  const cards = useContext(ChatCards);
  if (!entry) return null;
  const p = entry.p;
  switch (entry.type) {
    case 'PromptEntry':
      return <UserBubble entry={entry} conversation={conversation} />;
    case 'SpeechEntry':
      return <AssistantMessage entry={entry} final={final} streaming={streaming} />;
    case 'NoticeEntry':
      return (
        <div className="flex items-start gap-2 text-xs text-muted-foreground">
          <Info className="mt-0.5 size-3.5 shrink-0" />
          <span className="line-clamp-4 whitespace-pre-wrap">{p.text}</span>
        </div>
      );
    case 'OutcomeEntry':
      return <span className={cn('text-xs text-muted-foreground', p.wentWrong && 'text-warning')}>{p.text}</span>;
    case 'PermissionEntry':
      return <PermissionCard entry={entry} />;
    case 'QuestionsEntry':
      return <QuestionsCard entry={entry} />;
    case 'PictureEntry':
      return (
        <div className="flex flex-col items-start gap-1.5">
          <Thumb view={p.picture} conversation={conversation} className="max-h-96 max-w-full" />
          {p.hasCaption && <span className="text-xs text-muted-foreground">{p.caption}</span>}
        </div>
      );
    case 'LoanEntry':
      return <Loan entry={entry} conversation={conversation} />;
    default: {
      const Card = cards[entry.type];
      return Card ? <Card entry={entry} /> : null;
    }
  }
}

type Turn = { key: number; kind: 'prompt'; item: Item } | { key: number; kind: 'agent'; items: Item[] };

function turnsOf(items: Item[], working: boolean): Turn[] {
  const turns: Turn[] = [];
  for (const item of items) {
    const prompt = item.kind === 'entry' && obj(item.ref)?.type === 'PromptEntry';
    const last = turns.at(-1);
    if (prompt) turns.push({ key: item.key, kind: 'prompt', item });
    else if (last?.kind === 'agent') last.items.push(item);
    else turns.push({ key: item.key, kind: 'agent', items: [item] });
  }
  if (working && turns.at(-1)?.kind !== 'agent') turns.push({ key: -1, kind: 'agent', items: [] });
  return turns;
}

const typeOf = (item: Item) => (item.kind === 'entry' ? obj(item.ref)?.type ?? '' : 'steps');

function AgentTurn({ items, conversation, live, isFresh }: { items: Item[]; conversation: Obj; live: boolean; isFresh: (key: number) => boolean }) {
  const pulse = useObj(conversation.p.pulse);
  const p = conversation.p;
  const word: string = pulse?.p.word ?? 'Working…';
  const thinking = word.startsWith('Thinking');
  const speeches = items.filter((i) => typeOf(i) === 'SpeechEntry');
  const lastSpeech = speeches.at(-1)?.key;
  const tail = items.at(-1)?.key;
  return (
    <div className={cn('fio', live && 'fio-turn-live')} style={{ ['--agent' as string]: p.color ?? 'var(--accent)' }}>
      <span className="fio-head"><AllayIcon color={p.color} size={24} state={live ? (thinking ? 'thinking' : 'working') : 'idle'} /></span>
      <div className="fio-who">
        <b>{p.agent ?? 'Agent'}</b>
        {p.modelLabel && <span>· {p.modelLabel}</span>}
      </div>
      {items.map((item) => {
        const type = typeOf(item);
        const fresh = isFresh(item.key) && type !== 'SpeechEntry';
        return (
          <Arrival key={item.key} fresh={fresh} className="fio-item">
            {item.kind === 'steps' ? (
              <Acts refs={item.refs} />
            ) : (
              <Node kind={type === 'SpeechEntry' ? 'say' : 'plain'}>
                <EntryView id={item.key} conversation={conversation} final={!live && item.key === lastSpeech} streaming={live && item.key === lastSpeech && item.key === tail} />
              </Node>
            )}
          </Arrival>
        );
      })}
      {live && pulse?.p.isShown && (
        <div className="fio-item fio-end" role="status">
          <span>{word}</span>
          <span className="tabular-nums">{pulse.p.elapsed}</span>
        </div>
      )}
    </div>
  );
}

function MessageListView({ conversation, refs }: { conversation: Obj; refs: any[] }) {
  const items = itemsOf(refs);
  const isFresh = useArrivals(conversation.id, items.map((i) => i.key));
  const working = !!conversation.p.isWorking;
  const turns = turnsOf(items, working);
  return (
    <div className="flex flex-col gap-6 px-4 py-4">
      {turns.map((turn, i) =>
        turn.kind === 'prompt' ? (
          <EntryView key={turn.key} id={turn.key} conversation={conversation} final={false} streaming={false} />
        ) : (
          <AgentTurn key={turn.key} items={turn.items} conversation={conversation} live={working && i === turns.length - 1} isFresh={isFresh} />
        ),
      )}
    </div>
  );
}

const Shown = ['isWorking', 'color', 'agent', 'modelLabel'];

export const MessageList = memo(MessageListView, (before, after) =>
  before.refs === after.refs
  && before.conversation.id === after.conversation.id
  && idOf(before.conversation.p.pulse) === idOf(after.conversation.p.pulse)
  && Shown.every((name) => before.conversation.p[name] === after.conversation.p[name]));
