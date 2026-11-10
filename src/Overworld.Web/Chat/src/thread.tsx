import { createElement, useState, type ReactNode } from 'react';
import { Brain, Loader2, XCircle } from 'lucide-react';
import { useObjs, idOf, type Obj } from '@overworld/kit';
import { cn } from './ui/cn';
import { LineCounts, Step, iconOf } from './activity';

const number = (text?: string | null) => Number((text ?? '').replace(/[^\d]/g, '')) || 0;

const fileOf = (subject: string) => subject.replace(/\/$/, '').split('/').pop() || subject;

const plural = (n: number, one: string, many: string) => `${n} ${n === 1 ? one : many}`;

const names = (subjects: string[]) => {
  const unique = [...new Set(subjects.map(fileOf))];
  return unique.length <= 2 ? unique.join(', ') : `${unique.slice(0, 2).join(', ')} +${unique.length - 2}`;
};

type Phrase = { kind: string; text: string; added?: number; removed?: number };

function phrasesOf(tools: Obj[]): Phrase[] {
  const by = (kind: string) => tools.filter((t) => t.p.kind === kind);
  const counts = (list: Obj[]) => ({ added: list.reduce((n, t) => n + number(t.p.added), 0), removed: list.reduce((n, t) => n + number(t.p.removed), 0) });
  const out: Phrase[] = [];
  const commands = by('Command');
  if (commands.length) out.push({ kind: 'Command', text: `Ran ${plural(commands.length, 'command', 'commands')}` });
  const reads = by('Read');
  if (reads.length) out.push({ kind: 'Read', text: `Read ${names(reads.map((t) => t.p.subject ?? ''))}` });
  const searches = by('Search');
  if (searches.length) out.push({ kind: 'Search', text: searches.length === 1 ? `Searched for ${searches[0].p.subject}` : `Searched ${plural(searches.length, 'time', 'times')}` });
  const created = by('Create');
  if (created.length) out.push({ kind: 'Create', text: `Wrote ${names(created.map((t) => t.p.subject ?? ''))}`, ...counts(created) });
  const changed = by('Change');
  if (changed.length) out.push({ kind: 'Change', text: `Edited ${names(changed.map((t) => t.p.subject ?? ''))}`, ...counts(changed) });
  const browsed = by('Browse');
  if (browsed.length) out.push({ kind: 'Browse', text: `Looked up ${plural(browsed.length, 'page', 'pages')}` });
  const delegated = by('Delegate');
  if (delegated.length) out.push({ kind: 'Delegate', text: `Handed off ${plural(delegated.length, 'task', 'tasks')}` });
  const served = by('Tool');
  if (served.length) {
    const actions = [...new Set(served.map((t) => (t.p.subject ?? '').split(' · ')[0]))];
    out.push({ kind: 'Tool', text: actions.length <= 2 ? `Used ${actions.join(', ')}` : `Used ${plural(served.length, 'tool', 'tools')}` });
  }
  const other = by('Other');
  if (other.length) out.push({ kind: 'Other', text: `Used ${plural(other.length, 'tool', 'tools')}` });
  return out;
}

const Doing: Record<string, string> = {
  Command: 'Running',
  Read: 'Reading',
  Change: 'Editing',
  Create: 'Writing',
  Search: 'Searching for',
  Browse: 'Looking up',
  Delegate: 'Handing off',
  Tool: 'Using',
  Other: 'Using',
};

function doing(act: Obj) {
  const subject: string = act.p.subject ?? '';
  const kind: string = act.p.kind;
  const what = kind === 'Read' || kind === 'Change' || kind === 'Create' ? fileOf(subject) : subject.split(' · ')[0];
  return `${Doing[kind] ?? 'Using'} ${what}`;
}

export function Node({ kind = 'plain', live, className, children }: { kind?: 'say' | 'acts' | 'plain'; live?: boolean; className?: string; children: ReactNode }) {
  return <div className={cn('fio-node', `fio-${kind}`, live && 'fio-live', className)}>{children}</div>;
}

export function Acts({ refs }: { refs: any[] }) {
  const steps = useObjs(refs).filter((s): s is Obj => s !== null);
  const [open, setOpen] = useState(false);
  if (steps.length === 0) return null;
  const tools = steps.filter((s) => s.type === 'ActEntry');
  const plans = steps.filter((s) => s.type === 'PlanEntry');
  const thoughts = steps.filter((s) => s.type === 'ThoughtEntry');
  const running = tools.find((t) => t.p.isRunning);
  const thinking = thoughts.find((t) => !t.p.isDone);
  const failed = tools.filter((t) => t.p.hasFailed).length;
  const phrases = phrasesOf(tools);
  const details = steps.filter((s) => s.type !== 'PlanEntry');
  return (
    <Node kind="acts" live={!!running || !!thinking}>
      {details.length > 0 && (
        <button type="button" className="fio-sentence" aria-expanded={open} onClick={() => setOpen(!open)}>
          {running || thinking ? (
            <span className="fio-phrase">
              <Loader2 className="size-3.5 animate-spin" />
              <span className="truncate">{running ? doing(running) : 'Thinking'}…</span>
            </span>
          ) : (
            <>
              {thoughts.length > 0 && <span className="fio-phrase"><Brain className="size-3.5" />Thought</span>}
              {phrases.map((phrase, i) => (
                <span key={phrase.kind} className="fio-phrase">
                  {(i > 0 || thoughts.length > 0) && <span className="fio-dot">·</span>}
                  {createElement(iconOf(phrase.kind), { className: 'size-3.5' })}
                  <span className="truncate">{phrase.text}</span>
                  <LineCounts added={phrase.added ?? 0} removed={phrase.removed ?? 0} />
                </span>
              ))}
              {failed > 0 && <span className="fio-phrase text-removed"><XCircle className="size-3.5" />{failed} failed</span>}
            </>
          )}
        </button>
      )}
      {open && <div className="fio-detail">{details.map((step) => <Step key={step.id} id={idOf(step.id)!} />)}</div>}
      {plans.map((plan) => <div key={plan.id} className="mt-2"><Step id={plan.id} /></div>)}
    </Node>
  );
}
