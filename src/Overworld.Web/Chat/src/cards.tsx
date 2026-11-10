import { Check, MessageCircleQuestion, PenLine, ShieldAlert, X } from 'lucide-react';
import { useObj, run, can, put, idOf, type Obj } from '@overworld/kit';
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

function Option({ id, several }: { id: number; several: boolean }) {
  const option = useObj(id);
  if (!option) return null;
  const p = option.p;
  return (
    <button type="button" role="listitem" onClick={() => put(option, 'IsPicked', several ? !p.isPicked : true)} className={cn('flex w-full cursor-pointer items-start gap-3 px-3 py-2 text-left', p.isPicked && 'bg-primary/[0.07]')}>
      <span className={cn('mt-0.5 flex size-4 shrink-0 items-center justify-center border', several ? 'rounded' : 'rounded-full', p.isPicked && 'border-primary bg-primary text-primary-foreground')}>
        {p.isPicked && (several ? <Check className="size-3" strokeWidth={3} /> : <span className="size-1.5 rounded-full bg-primary-foreground" />)}
      </span>
      <span className="min-w-0 flex-1">
        <span className={cn('text-sm', p.isPicked ? 'font-semibold' : 'font-medium')}>{p.label}</span>
        {p.hasDescription && <span className="mt-0.5 block text-xs leading-snug text-muted-foreground">{p.description}</span>}
      </span>
    </button>
  );
}

function Question({ id }: { id: number }) {
  const question = useObj(id);
  if (!question) return null;
  const p = question.p;
  return (
    <div className="space-y-1.5">
      <div className="space-y-0.5">
        {p.header && <div className="text-[11px] font-medium tracking-wide text-muted-foreground uppercase">{p.header}</div>}
        <p className="text-sm font-medium">{p.text}</p>
      </div>
      <div role="list" className="divide-y overflow-hidden rounded-lg border bg-background/40">
        {(p.options ?? []).map((o: any) => <Option key={idOf(o)} id={idOf(o)!} several={p.several} />)}
        <label className="flex items-start gap-3 px-3 py-2">
          <PenLine className="mt-0.5 size-4 shrink-0 text-muted-foreground" />
          <input className="min-w-0 flex-1 bg-transparent text-sm outline-none placeholder:text-muted-foreground" placeholder="Or write your own answer" value={p.written ?? ''} onChange={(e) => put(question, 'Written', e.target.value)} />
        </label>
      </div>
    </div>
  );
}

export function QuestionsCard({ entry }: { entry: Obj }) {
  const p = entry.p;
  if (!p.isOpen) return <Settled text={p.answer} />;
  return (
    <Card size="sm" className="ring-warning/40">
      <CardHeader>
        <CardTitle className="flex items-center gap-2">
          <MessageCircleQuestion className="size-4 text-warning" />
          The agent asks
        </CardTitle>
      </CardHeader>
      <CardContent className="space-y-2.5">
        {(p.asked ?? []).map((q: any) => <Question key={idOf(q)} id={idOf(q)!} />)}
      </CardContent>
      <CardFooter className="flex gap-2">
        <Button size="lg" className="flex-1" disabled={!can(entry, 'SendAnswersCommand')} onClick={() => run(entry, 'SendAnswersCommand')}>Answer</Button>
        <Button size="lg" variant="outline" className="flex-1" onClick={() => run(entry, 'SkipCommand')}>Skip</Button>
      </CardFooter>
    </Card>
  );
}
