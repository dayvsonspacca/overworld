import { createElement } from 'react';
import { Bot, Brain, ChevronRight, FilePen, FilePlus, FileText, Globe, Loader2, Plug, Search, SquareTerminal, Wrench, XCircle, type LucideIcon } from 'lucide-react';
import { useObj, run, type Obj } from '@overworld/kit';
import { Collapsible, CollapsibleContent, CollapsibleTrigger } from './ui/collapsible';
import { cn } from './ui/cn';
import { CopyButton, Markdown } from './markdown';

const Icons: Record<string, LucideIcon> = {
  Command: SquareTerminal,
  Read: FileText,
  Change: FilePen,
  Create: FilePlus,
  Search,
  Browse: Globe,
  Delegate: Bot,
  Tool: Plug,
  Other: Wrench,
};

export const iconOf = (kind?: string): LucideIcon => Icons[kind ?? ''] ?? Wrench;

const Pre = 'max-h-72 overflow-auto rounded border bg-background/60 p-2 font-mono text-[11px] leading-relaxed whitespace-pre-wrap break-all';

const number = (text?: string | null) => Number((text ?? '').replace(/[^\d]/g, '')) || 0;

export function LineCounts({ added, removed, className }: { added: number; removed: number; className?: string }) {
  if (!added && !removed) return null;
  return (
    <span className={cn('shrink-0 font-mono text-[11px] tabular-nums', className)}>
      {added > 0 && <span className="text-added">+{added}</span>}
      {added > 0 && removed > 0 && ' '}
      {removed > 0 && <span className="text-removed">−{removed}</span>}
    </span>
  );
}

function split(subject: string): { name: string; summary: string } {
  const at = subject.indexOf(': ');
  return at > 0 && at < 40 ? { name: subject.slice(0, at), summary: subject.slice(at + 2) } : { name: '', summary: subject };
}

const Labels: Record<string, string> = {
  Command: 'Bash',
  Read: 'Read',
  Change: 'Edit',
  Create: 'Write',
  Search: 'Search',
  Browse: 'Fetch',
  Delegate: 'Agent',
  Tool: 'Tool',
  Other: 'Tool',
};

function ToolRow({ act }: { act: Obj }) {
  const p = act.p;
  const { name, summary } = split(p.subject ?? '');
  const label = name || Labels[p.kind] || 'Tool';
  const failed = p.hasFailed;
  return (
    <Collapsible open={p.isOpen} onOpenChange={(open) => open !== p.isOpen && run(act, 'ToggleCommand')}>
      <CollapsibleTrigger disabled={!p.hasOutput} className="group flex w-full min-w-0 cursor-pointer items-center gap-2 rounded-md px-2 py-1.5 text-left text-sm hover:bg-muted/60 disabled:cursor-default">
        {createElement(iconOf(p.kind), { className: cn('size-4 shrink-0 text-muted-foreground', failed && 'text-removed') })}
        <span className="shrink-0 font-medium">{label}</span>
        <span className="min-w-0 flex-1 truncate font-mono text-xs text-muted-foreground">{summary}</span>
        <LineCounts added={number(p.added)} removed={number(p.removed)} />
        {p.isRunning && <Loader2 className="size-3.5 shrink-0 animate-spin text-muted-foreground" />}
        {failed && <XCircle className="size-3.5 shrink-0 text-removed" />}
        {p.hasOutput && <ChevronRight className="size-3.5 shrink-0 text-muted-foreground transition-transform group-data-[state=open]:rotate-90" />}
      </CollapsibleTrigger>
      <CollapsibleContent className="space-y-1.5 px-2 pt-1 pb-2">
        {p.kind === 'Command' && (
          <div className="relative">
            <pre className={cn(Pre, 'pr-8')}><span className="select-none text-muted-foreground">$ </span>{summary}</pre>
            <CopyButton text={summary} label="Copy the command" className="absolute top-1 right-1 text-muted-foreground" />
          </div>
        )}
        {p.hasOutput && <pre className={cn(Pre, failed && 'border-removed/30 text-removed')}>{p.shown}</pre>}
      </CollapsibleContent>
    </Collapsible>
  );
}

function ThinkingRow({ thought }: { thought: Obj }) {
  const p = thought.p;
  return (
    <Collapsible>
      <CollapsibleTrigger className="group flex w-full cursor-pointer items-center gap-2 rounded-md px-2 py-1.5 text-left text-sm text-muted-foreground hover:bg-muted/60">
        {p.isDone ? <Brain className="size-4 shrink-0" /> : <Loader2 className="size-4 shrink-0 animate-spin" />}
        <span className="flex-1">{p.heading}</span>
        <ChevronRight className="size-3.5 transition-transform group-data-[state=open]:rotate-90" />
      </CollapsibleTrigger>
      <CollapsibleContent className="px-2 pt-1 pb-2">
        <p className="text-xs whitespace-pre-wrap text-muted-foreground">{p.text}</p>
      </CollapsibleContent>
    </Collapsible>
  );
}

function PlanRow({ plan }: { plan: Obj }) {
  const p = plan.p;
  return (
    <div className="space-y-1 rounded-lg border bg-muted/30 px-3 py-2">
      <div className="text-[11px] font-medium tracking-wide text-muted-foreground uppercase">Plan · {p.progress}</div>
      {(p.lines ?? []).map((line: any) => (
        <div key={line.$id} className={cn('flex gap-2 text-sm', line.isDone && 'text-muted-foreground line-through', line.isDoing && 'text-brand')}>
          <span className="w-3 font-mono">{line.glyph}</span>
          <span>{line.text}</span>
        </div>
      ))}
    </div>
  );
}

export function Step({ id }: { id: number }) {
  const entry = useObj(id);
  if (!entry) return null;
  if (entry.type === 'ThoughtEntry') return <ThinkingRow thought={entry} />;
  if (entry.type === 'PlanEntry') return <PlanRow plan={entry} />;
  return <ToolRow act={entry} />;
}

export { Markdown };
