import { useState, type ReactNode } from 'react';
import { BookMarked, FileText } from 'lucide-react';
import { run, type Obj } from '@overworld/kit';
import { Button, Card, CardContent, CardFooter, CardHeader, CardTitle, Markdown, Settled, cn, type Cards } from '@overworld/chat';

function ProposalCard({ entry, icon, title, approve = 'Approve', decline = 'Decline', extra, children }: { entry: Obj; icon: ReactNode; title: ReactNode; approve?: string; decline?: string; extra?: ReactNode; children: ReactNode }) {
  const p = entry.p;
  if (!p.isOpen) return <Settled text={p.verdict} />;
  return (
    <Card size="sm" className="ring-brand/40">
      <CardHeader className={extra ? 'grid-cols-[1fr_auto]' : undefined}>
        <CardTitle className="flex items-center gap-2">{icon}{title}</CardTitle>
        {extra}
      </CardHeader>
      <CardContent>{children}</CardContent>
      <CardFooter className="flex gap-2">
        <Button size="lg" className="flex-1" onClick={() => run(entry, 'ApproveCommand')}>{approve}</Button>
        <Button size="lg" variant="outline" className="flex-1" onClick={() => run(entry, 'DeclineCommand')}>{decline}</Button>
      </CardFooter>
    </Card>
  );
}

export function AxiomCard({ entry }: { entry: Obj }) {
  return (
    <ProposalCard entry={entry} icon={<BookMarked className="size-4 text-brand" />} title="A new axiom for the project">
      <p className="text-sm">{entry.p.axiom}</p>
    </ProposalCard>
  );
}

export function AxiomRemovalCard({ entry }: { entry: Obj }) {
  const p = entry.p;
  return (
    <ProposalCard entry={entry} icon={<BookMarked className="size-4 text-brand" />} title="Drop an axiom of the project" approve="Drop it" decline="Keep it">
      <p className="text-sm line-through">{p.axiom}</p>
      {p.hasWhy && <p className="mt-1 text-xs text-muted-foreground">{p.why}</p>}
    </ProposalCard>
  );
}

export function SummaryCard({ entry }: { entry: Obj }) {
  const p = entry.p;
  const lines: any[] = p.lines ?? [];
  const [changes, setChanges] = useState(false);
  const proposed = lines.filter((line) => !line.isRemoved).map((line) => line.text).join('\n');
  const tabs = (
    <div className="flex gap-1">
      <Button size="xs" variant={changes ? 'ghost' : 'secondary'} onClick={() => setChanges(false)}>Preview</Button>
      <Button size="xs" variant={changes ? 'secondary' : 'ghost'} onClick={() => setChanges(true)}>Changes</Button>
    </div>
  );
  return (
    <ProposalCard entry={entry} icon={<FileText className="size-4 text-brand" />} title={p.asking} extra={tabs}>
      {changes ? (
        <pre className="max-h-96 overflow-auto rounded border bg-background/60 p-2 font-mono text-[11px] leading-relaxed whitespace-pre-wrap">
          {lines.map((line) => <div key={line.$id} className={cn(line.isAdded && 'bg-added/10 text-added', line.isRemoved && 'bg-removed/10 text-removed')}>{line.marked}</div>)}
        </pre>
      ) : (
        <div className="max-h-96 overflow-auto rounded border bg-background/60 p-3"><Markdown text={proposed} /></div>
      )}
    </ProposalCard>
  );
}

export const ProposalCards: Cards = {
  AxiomProposalEntry: AxiomCard,
  AxiomRemovalProposalEntry: AxiomRemovalCard,
  SummaryProposalEntry: SummaryCard,
};
