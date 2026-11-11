import { useRef, type KeyboardEvent } from 'react';
import { Check, MessageCircleQuestion, ShieldAlert, X } from 'lucide-react';
import { useObj, useDraft, obj, run, can, put, idOf, type Obj } from '@overworld/kit';
import { Button } from './ui/button';
import { Card, CardContent, CardDescription, CardFooter, CardHeader, CardTitle } from './ui/card';
import { cn } from './ui/cn';

export function Settled({ text }: { text?: string | null }) {
  const denied = /denied|declined|skipped|withdrawn|kept/i.test(text ?? '');
  const Icon = denied ? X : Check;
  return (
    <div className="flex min-w-0 items-center gap-2 px-2 text-xs text-muted-foreground">
      <Icon className="size-3.5 shrink-0" />
      <span className="truncate">{text}</span>
    </div>
  );
}

const Pre = 'max-h-72 overflow-auto rounded border bg-background/60 p-2 font-mono text-[11px] leading-relaxed whitespace-pre-wrap break-all';

export function PermissionCard({ entry }: { entry: Obj }) {
  const p = entry.p;
  if (!p.isOpen) return <Settled text={`${p.answer}: ${p.subject}`} />;
  return (
    <Card size="sm" className="ring-warning/40">
      <CardHeader>
        <CardTitle className="flex items-center gap-2">
          <ShieldAlert className="size-4 text-warning" />
          {p.asks}
        </CardTitle>
        {p.hasReason && <CardDescription>{p.reason}</CardDescription>}
      </CardHeader>
      <CardContent>
        <pre className={Pre}>{p.subject}</pre>
      </CardContent>
      <CardFooter className="flex flex-wrap gap-2">
        <Button size="lg" className="flex-1" onClick={() => run(entry, 'AllowOnceCommand')}>Allow</Button>
        <Button size="lg" variant="outline" className="flex-1" onClick={() => run(entry, 'DenyCommand')}>Deny</Button>
        {p.canRemember && (
          <Button size="lg" variant="ghost" className="w-full" onClick={() => run(entry, 'AllowAlwaysCommand')}>
            Always allow <span className="font-mono text-xs text-muted-foreground">{p.remembers}</span>
          </Button>
        )}
      </CardFooter>
    </Card>
  );
}

const Key = 'flex size-5 shrink-0 items-center justify-center border border-border bg-muted font-mono text-[10px] text-muted-foreground';

const Picked = 'border-primary bg-primary text-primary-foreground';

function Option({ id, n, several }: { id: number; n: number; several: boolean }) {
  const option = useObj(id);
  if (!option) return null;
  const p = option.p;
  return (
    <button type="button" role="listitem" onClick={() => put(option, 'IsPicked', several ? !p.isPicked : true)} className={cn('flex w-full cursor-pointer items-start gap-2.5 rounded-md border border-transparent px-2.5 py-2 text-left hover:bg-muted', p.isPicked && 'border-[var(--accent-deep)] bg-[var(--selected)] hover:bg-[var(--selected)]')}>
      <span className={cn(Key, 'mt-px', several ? 'rounded' : 'rounded-full', p.isPicked && Picked)}>{n}</span>
      <span className="min-w-0 flex-1">
        <span className="text-sm font-medium">{p.label}</span>
        {p.isRecommended && <span className="ml-2 rounded-full border border-[var(--accent-deep)] bg-[var(--selected)] px-1.5 py-px align-[1px] text-[10px] font-medium text-[var(--accent-text)]">Recommended</span>}
        {p.hasDescription && <span className="mt-0.5 block text-xs leading-snug text-muted-foreground">{p.description}</span>}
      </span>
    </button>
  );
}

function Own({ question, n, field }: { question: Obj; n: number; field: { current: HTMLInputElement | null } }) {
  const p = question.p;
  const written = useDraft(p.written, (v) => put(question, 'Written', v));
  const open = p.isWriting || (p.written ?? '').length > 0;
  if (!open) {
    return (
      <button type="button" onClick={() => put(question, 'IsWriting', true)} className="flex w-full cursor-pointer items-center gap-2.5 rounded-md px-2.5 py-2 text-left text-sm text-muted-foreground hover:bg-muted">
        <span className={cn(Key, 'border-dashed', p.several ? 'rounded' : 'rounded-full')}>{n}</span>
        Something else…
      </button>
    );
  }
  return (
    <label className="flex items-center gap-2.5 rounded-md border border-[var(--accent-deep)] bg-muted px-2.5 py-1.5">
      <span className={cn(Key, p.several ? 'rounded' : 'rounded-full', Picked)}>{n}</span>
      <input ref={(e) => { field.current = e; }} autoFocus style={{ border: 0, padding: 0, background: 'transparent', borderRadius: 0 }} className="min-w-0 flex-1 text-sm outline-none placeholder:text-muted-foreground" placeholder="Write your own answer" value={written.local} onChange={(e) => written.write(e.target.value)} onBlur={(e) => { if (!e.target.value.trim()) put(question, 'IsWriting', false); }} />
    </label>
  );
}

function Step({ id, n, on, onClick }: { id: number; n: number; on: boolean; onClick: () => void }) {
  const question = useObj(id);
  if (!question) return null;
  const done = !on && question.p.isAnswered;
  return (
    <button type="button" onClick={onClick} className={cn('flex h-6 cursor-pointer items-center gap-1.5 rounded-full border border-border px-2.5 text-[11px] text-muted-foreground', on && 'border-[var(--accent-deep)] bg-[var(--selected)] text-[var(--accent-text)]', done && 'text-foreground')}>
      {done ? <Check className="size-3 text-[var(--added)]" strokeWidth={3} /> : <span className="text-[10px]">{n}</span>}
      {question.p.header || `Question ${n}`}
    </button>
  );
}

function Question({ id, field }: { id: number; field: { current: HTMLInputElement | null } }) {
  const question = useObj(id);
  if (!question) return null;
  const p = question.p;
  const options = p.options ?? [];
  return (
    <div className="space-y-2.5">
      <p className="text-sm leading-snug font-medium">
        {p.text}
        <span className="ml-1.5 text-[11px] font-normal text-[var(--faint)]">{p.several ? 'pick any' : 'pick one'}</span>
      </p>
      <div role="list" className="space-y-0.5">
        {options.map((o: any, i: number) => <Option key={idOf(o)} id={idOf(o)!} n={i + 1} several={p.several} />)}
        <Own question={question} n={options.length + 1} field={field} />
      </div>
    </div>
  );
}

function Hint({ keys, children }: { keys: string; children: string }) {
  return <span><kbd className="rounded bg-muted px-1.5 py-px font-mono text-[10px] text-muted-foreground">{keys}</kbd> {children}</span>;
}

export function QuestionsCard({ entry }: { entry: Obj }) {
  const p = entry.p;
  const asked: any[] = p.asked ?? [];
  const current = useObj(idOf(asked[p.step ?? 0]) ?? -1);
  const field = useRef<HTMLInputElement | null>(null);
  if (!p.isOpen) return <Settled text={p.answer} />;
  const last = p.isLast ?? true;
  const go = () => run(entry, last ? 'SendAnswersCommand' : 'NextCommand');
  const able = can(entry, last ? 'SendAnswersCommand' : 'NextCommand');
  const keys = (e: KeyboardEvent<HTMLDivElement>) => {
    const typing = e.target instanceof HTMLInputElement;
    if (e.key === 'Enter') {
      if (e.target instanceof HTMLButtonElement && e.target.getAttribute('role') !== 'listitem') return;
      e.preventDefault();
      go();
    } else if (e.key === 'Tab' && p.hasSteps) {
      e.preventDefault();
      run(entry, e.shiftKey ? 'BackCommand' : 'NextCommand');
    } else if (e.key === 'Escape' && typing) {
      (e.target as HTMLInputElement).blur();
      (e.currentTarget as HTMLDivElement).focus();
    } else if (!typing && current && /^[1-9]$/.test(e.key)) {
      const n = Number(e.key);
      const options = current.p.options ?? [];
      if (n <= options.length) {
        const option = obj(options[n - 1]);
        if (option) put(option, 'IsPicked', current.p.several ? !option.p.isPicked : true);
      } else if (n === options.length + 1) {
        put(current, 'IsWriting', true);
        field.current?.focus();
      }
    }
  };
  return (
    <div tabIndex={-1} onKeyDown={keys} className="overflow-hidden rounded-xl border border-l-2 border-border border-l-warning bg-card text-sm outline-none">
      <div className="flex items-center gap-2.5 px-3.5 pt-2.5">
        <span className="flex items-center gap-2 text-xs text-muted-foreground">
          <MessageCircleQuestion className="size-4 text-warning" />
          The agent asks
        </span>
        <div className="ml-auto flex flex-wrap justify-end gap-1">
          {p.hasSteps
            ? asked.map((q: any, i: number) => <Step key={idOf(q)} id={idOf(q)!} n={i + 1} on={i === p.step} onClick={() => run(entry, 'GoCommand', i)} />)
            : current?.p.header && <span className="flex h-6 items-center rounded-full border border-[var(--accent-deep)] bg-[var(--selected)] px-2.5 text-[11px] text-[var(--accent-text)]">{current.p.header}</span>}
        </div>
      </div>
      <div className="px-3.5 pt-3 pb-1">
        {current && <Question key={current.id} id={current.id} field={field} />}
      </div>
      <div className="mt-2 flex items-center gap-2 border-t border-[var(--divider)] px-3.5 py-2.5">
        <div className="flex gap-3 text-[11px] text-[var(--faint)]">
          {current && <Hint keys={`1–${(current.p.options ?? []).length + 1}`}>pick</Hint>}
          {p.hasSteps && <Hint keys={p.step > 0 ? '⇧ Tab' : 'Tab'}>{p.step > 0 ? 'back' : 'next question'}</Hint>}
        </div>
        <div className="flex-1" />
        <Button variant="ghost" onClick={() => run(entry, 'SkipCommand')}>{p.hasSteps ? 'Skip all' : 'Skip'}</Button>
        {p.step > 0 && <Button variant="outline" onClick={() => run(entry, 'BackCommand')}>Back</Button>}
        <Button disabled={!able} onClick={go}>
          {last ? 'Answer' : 'Next'}
          <kbd className="rounded bg-black/20 px-1.5 py-px font-mono text-[10px]">⏎</kbd>
        </Button>
      </div>
    </div>
  );
}
