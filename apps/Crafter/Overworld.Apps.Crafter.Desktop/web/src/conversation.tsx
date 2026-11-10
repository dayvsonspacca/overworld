import { useEffect, useRef, useState, type DragEvent, type KeyboardEvent, type ClipboardEvent, type ReactNode, type RefObject } from 'react';
import { Virtuoso, type VirtuosoHandle } from 'react-virtuoso';
import * as Menu from '@radix-ui/react-dropdown-menu';
import { Prose, ItemFrame, IconButton, Button, AllayIcon, glyphs, blob, hex, readFile, useObj, useList, run, can, put, invoke, ask, obj, idOf, on, type Obj } from '@overworld/kit';

function Node({ glyph, kind = '', last, children }: { glyph: string; kind?: string; last?: boolean; children: ReactNode }) {
  return (
    <div className={`node ${kind} ${last ? 'last' : ''}`}>
      <div className="node-gutter"><i className="node-line" /><span className="node-dot">{glyph}</span></div>
      <div className="node-body">{children}</div>
    </div>
  );
}

function Picture({ view, conversation, big }: { view: any; conversation: Obj; big?: boolean }) {
  if (!view) return null;
  return (
    <button className={`picture ${big ? 'big' : ''}`} title="Open the picture" onClick={() => run(conversation, 'OpenPictureCommand', view)}>
      <img src={blob(view.data)} alt={view.label} />
    </button>
  );
}

function Prompt({ entry, conversation }: { entry: Obj; conversation: Obj }) {
  const p = entry.p;
  return (
    <div className="prompt">
      {p.hasText && <div className="prompt-text">{p.text}</div>}
      {p.hasFiles && <div className="prompt-files">{p.files.map((f: string) => <span key={f}>@{f}</span>)}</div>}
      {p.hasSkills && <div className="prompt-skills">{p.skills.map((s: string) => <span key={s}>/{s}</span>)}</div>}
      {p.hasPictures && <div className="prompt-pictures">{p.pictures.map((v: any) => <Picture key={v.$id} view={v} conversation={conversation} />)}</div>}
    </div>
  );
}

function Act({ entry }: { entry: Obj }) {
  const p = entry.p;
  return (
    <Node glyph={p.glyph} kind={`${p.hasFailed ? 'failed' : ''} ${p.isRunning ? 'running' : ''}`}>
      <button className={`act ${p.hasFailed ? 'failed' : ''}`} disabled={!p.hasOutput} onClick={() => run(entry, 'ToggleCommand')}>
        <span className="act-subject">{p.subject}</span>
        {p.added && <span className="added">{p.added}</span>}
        {p.removed && <span className="danger">{p.removed}</span>}
        {p.verdict && <span className="verdict">{p.verdict}</span>}
      </button>
      {p.showsOutput && <pre className="act-output">{p.shown}</pre>}
    </Node>
  );
}

function Plan({ entry }: { entry: Obj }) {
  const p = entry.p;
  return (
    <Node glyph="☰">
      <div className="plan">
        <span className="group-label">Plan · {p.progress}</span>
        {(p.lines ?? []).map((line: any) => (
          <div key={line.$id} className={`plan-step ${line.isDone ? 'done' : ''} ${line.isDoing ? 'doing' : ''}`}>
            <span className="mono">{line.glyph}</span>
            <span>{line.text}</span>
          </div>
        ))}
      </div>
    </Node>
  );
}

const Answer = ({ text }: { text?: string | null }) => (text ? <span className="answer">{text}</span> : null);

function Permission({ entry }: { entry: Obj }) {
  const p = entry.p;
  return (
    <Node glyph={p.glyph}>
      <div className={`request ${p.isOpen ? 'open' : ''}`}>
        <strong>{p.asks}</strong>
        <div className="mono request-subject">{p.subject}</div>
        {p.hasReason && <div className="soft">{p.reason}</div>}
        {p.isOpen ? (
          <div className="buttons">
            <Button kind="primary" onClick={() => run(entry, 'AllowOnceCommand')}>Allow</Button>
            {p.canRemember && <Button onClick={() => run(entry, 'AllowAlwaysCommand')}>Always allow <span className="mono subtle">{p.remembers}</span></Button>}
            <Button onClick={() => run(entry, 'DenyCommand')}>Deny</Button>
          </div>
        ) : <Answer text={p.answer} />}
      </div>
    </Node>
  );
}

function Option({ id, question, several, open }: { id: number; question: number; several: boolean; open: boolean }) {
  const option = useObj(id);
  if (!option) return null;
  const p = option.p;
  return (
    <label className="option">
      <input type={several ? 'checkbox' : 'radio'} name={`q${question}`} checked={!!p.isPicked} disabled={!open} onChange={(e) => put(option, 'IsPicked', e.target.checked)} />
      <span><span>{p.label}</span>{p.hasDescription && <small className="subtle">{p.description}</small>}</span>
    </label>
  );
}

function Question({ id, open }: { id: number; open: boolean }) {
  const question = useObj(id);
  if (!question) return null;
  const p = question.p;
  return (
    <div className="question">
      <span className="group-label">{p.header}</span>
      <strong>{p.text}</strong>
      {(p.options ?? []).map((o: any) => <Option key={idOf(o)} id={idOf(o)!} question={id} several={p.several} open={open} />)}
      <input placeholder="Or write your own answer" value={p.written ?? ''} disabled={!open} onChange={(e) => put(question, 'Written', e.target.value)} />
    </div>
  );
}

function Questions({ entry }: { entry: Obj }) {
  const p = entry.p;
  return (
    <Node glyph="?">
      <div className={`request ${p.isOpen ? 'open' : ''}`}>
        {(p.asked ?? []).map((q: any) => <Question key={idOf(q)} id={idOf(q)!} open={p.isOpen} />)}
        {p.isOpen ? (
          <div className="buttons">
            <Button kind="primary" disabled={!can(entry, 'SendAnswersCommand')} onClick={() => run(entry, 'SendAnswersCommand')}>Answer</Button>
            <Button onClick={() => run(entry, 'SkipCommand')}>Skip</Button>
          </div>
        ) : <Answer text={p.answer} />}
      </div>
    </Node>
  );
}

function Proposal({ entry, title, children, approve = 'Approve', decline = 'Decline', glyph = '§' }: { entry: Obj; title: ReactNode; children: ReactNode; approve?: string; decline?: string; glyph?: string }) {
  const p = entry.p;
  return (
    <Node glyph={glyph}>
      <div className={`request ${p.isOpen ? 'open' : ''}`}>
        {title}
        {children}
        {p.isOpen ? (
          <div className="buttons">
            <Button kind="primary" onClick={() => run(entry, 'ApproveCommand')}>{approve}</Button>
            <Button onClick={() => run(entry, 'DeclineCommand')}>{decline}</Button>
          </div>
        ) : <Answer text={p.verdict} />}
      </div>
    </Node>
  );
}

function SummaryProposal({ entry }: { entry: Obj }) {
  const p = entry.p;
  const lines: any[] = p.lines ?? [];
  const proposed = lines.filter((line) => !line.isRemoved).map((line) => line.text).join('\n');
  const title = (
    <div className="request-head">
      <strong>{p.asking}</strong>
      <span className="chips">
        <Button kind="chip" chosen={p.showsPreview} onClick={() => run(entry, 'ShowPreviewCommand')}>Preview</Button>
        <Button kind="chip" chosen={p.showsChanges} onClick={() => run(entry, 'ShowChangesCommand')}>Changes</Button>
      </span>
    </div>
  );
  return (
    <Proposal entry={entry} glyph="¶" title={title}>
      {p.showsPreview
        ? <div className="prose-preview"><Prose text={proposed} /></div>
        : <pre className="prose-code">{lines.map((line) => <div key={line.$id} className={line.isAdded ? 'added' : line.isRemoved ? 'danger' : ''}>{line.marked}</div>)}</pre>}
    </Proposal>
  );
}

export function LoanView({ id }: { id: any }) {
  const loan = useObj(id);
  if (!loan) return null;
  const p = loan.p;
  switch (loan.type) {
    case 'PaintingLoan':
      return <iframe className="loan-painting" sandbox="allow-scripts allow-popups" srcDoc={p.html} />;
    case 'PaletteLoan':
      return <div className="loan-palette">{(p.swatches ?? []).map((s: any) => <div key={s.$id} style={{ background: hex(s.fill), color: s.ink }}><span>{s.label}</span></div>)}</div>;
    case 'ThemeLoan':
      return (
        <div className="loan-theme">
          {(p.groups ?? []).map((group: any) => (
            <div className="loan-group" key={group.$id}>
              <span className="group-label">{group.title}</span>
              <div className="loan-roles">
                {group.swatches.map((s: any) => <div className="loan-role" key={s.$id}><i style={{ background: hex(s.hex) }} /><span>{s.role}</span><code>{s.hex}</code></div>)}
              </div>
            </div>
          ))}
        </div>
      );
    default:
      return <div className="empty">{p.title}</div>;
  }
}

function Loan({ entry, conversation }: { entry: Obj; conversation: Obj }) {
  const loan = useObj(entry.p.loan);
  if (!loan) return null;
  return (
    <Node glyph="▧">
      <ItemFrame source={loan.p.source} title={loan.p.title} onSeen={(seen) => put(loan, 'IsLive', seen)} onExpand={() => run(conversation, 'ExpandLoanCommand', { $ref: entry.id })}>
        <LoanView id={loan.id} />
      </ItemFrame>
    </Node>
  );
}

function Entry({ id, conversation }: { id: number; conversation: Obj }) {
  const entry = useObj(id);
  if (!entry) return null;
  const p = entry.p;
  switch (entry.type) {
    case 'PromptEntry': return <Prompt entry={entry} conversation={conversation} />;
    case 'SpeechEntry': return <Node glyph="✦" kind="speech"><Prose text={p.text} /></Node>;
    case 'ThoughtEntry': return <Node glyph="…"><div className="thought"><span className="thought-head">{p.heading}</span><div className="thought-text">{p.text}</div></div></Node>;
    case 'ActEntry': return <Act entry={entry} />;
    case 'PlanEntry': return <Plan entry={entry} />;
    case 'PermissionEntry': return <Permission entry={entry} />;
    case 'QuestionsEntry': return <Questions entry={entry} />;
    case 'AxiomProposalEntry': return <Proposal entry={entry} title={<strong>A new axiom for the project</strong>}><div className="axiom">{p.axiom}</div></Proposal>;
    case 'AxiomRemovalProposalEntry': return <Proposal entry={entry} approve="Drop it" decline="Keep it" title={<strong>Drop an axiom of the project</strong>}><div className="axiom struck">{p.axiom}</div>{p.hasWhy && <div className="subtle">{p.why}</div>}</Proposal>;
    case 'SummaryProposalEntry': return <SummaryProposal entry={entry} />;
    case 'NoticeEntry': return <Node glyph="↺"><span className="footnote">{p.text}</span></Node>;
    case 'OutcomeEntry': return <Node glyph="■" last kind={p.wentWrong ? 'failed' : ''}><span className="footnote">{p.text}</span></Node>;
    case 'PictureEntry': return <Node glyph="◫"><div className="shown-picture"><Picture view={p.picture} conversation={conversation} big />{p.hasCaption && <span className="caption">{p.caption}</span>}</div></Node>;
    case 'LoanEntry': return <Loan entry={entry} conversation={conversation} />;
    default: return null;
  }
}

function Pulse({ id, color }: { id: any; color?: string }) {
  const pulse = useObj(id);
  if (!pulse?.p.isShown) return <div className="pulse-room" />;
  return (
    <div className="pulse">
      <AllayIcon color={color} size={26} className="pulse-allay" />
      <span className="pulse-word">{pulse.p.word}</span>
      <span className="pulse-time">· {pulse.p.elapsed}</span>
    </div>
  );
}

const Start = 1_000_000;

const Pages = 3;

function Top({ context }: { context?: { state: 'idle' | 'loading' | 'done' } }) {
  if (context?.state === 'loading') return <div className="timeline-top subtle footnote">Loading earlier…</div>;
  if (context?.state === 'done') return <div className="timeline-top subtle footnote">The start of the chat</div>;
  return <div className="timeline-top" />;
}

function Bottom({ context }: { context?: { pulse: any; color?: string } }) {
  return <div className="timeline-row"><Pulse id={context?.pulse} color={context?.color} /></div>;
}

function Timeline({ conversation }: { conversation: Obj }) {
  const timeline = conversation.p.timeline;
  const entries = useList(timeline?.shown);
  const list = useRef<VirtuosoHandle>(null);
  const newest = useRef<number | null>(null);
  const top = useRef<number | null>(null);
  const first = useRef(Start);
  const loading = useRef(false);
  const [state, setState] = useState<'idle' | 'loading' | 'done'>(timeline?.hasEarlier === false ? 'done' : 'idle');

  const ids = entries.map((ref) => idOf(ref)!);
  if (top.current !== null) {
    const moved = ids.indexOf(top.current);
    if (moved > 0) first.current -= moved;
  }
  top.current = ids[0] ?? null;

  useEffect(() => {
    const prompts = entries.filter((ref) => obj(ref)?.type === 'PromptEntry');
    const last = prompts.length ? idOf(prompts[prompts.length - 1]) : null;
    if (newest.current !== null && last !== newest.current) {
      list.current?.scrollToIndex({ index: 'LAST', align: 'end' });
    }
    newest.current = last;
  }, [entries]);

  const earlier = async () => {
    if (loading.current || state === 'done' || !timeline?.$id) return;
    loading.current = true;
    setState('loading');
    let brought = 0;
    for (let page = 0; page < Pages; page++) {
      const more = await invoke<number | null>({ id: timeline.$id } as Obj, 'ShowEarlier');
      if (!more) {
        if (more === 0) brought = -1;
        break;
      }
      brought += more;
    }
    loading.current = false;
    setState(brought < 0 ? 'done' : 'idle');
  };

  return (
    <Virtuoso
      ref={list}
      className="timeline-page"
      data={entries}
      context={{ state, pulse: conversation.p.pulse, color: conversation.p.color }}
      firstItemIndex={first.current}
      initialTopMostItemIndex={{ index: 'LAST', align: 'end' }}
      startReached={earlier}
      followOutput={(atBottom) => (atBottom ? 'auto' : false)}
      atBottomThreshold={48}
      increaseViewportBy={{ top: 800, bottom: 400 }}
      computeItemKey={(_, ref) => idOf(ref)!}
      itemContent={(_, ref) => <div className="timeline-row"><Entry id={idOf(ref)!} conversation={conversation} /></div>}
      components={{ Header: Top, Footer: Bottom }}
    />
  );
}

function Mention({ id, onChoose }: { id: number; onChoose: (mention: Obj) => void }) {
  const mention = useObj(id);
  if (!mention) return null;
  const p = mention.p;
  return (
    <div className={`mention ${p.isSelected ? 'selected' : ''}`} onMouseDown={(e) => { e.preventDefault(); onChoose(mention); }}>
      <span className="mono accent">{p.glyph}</span>
      <span className="mono mention-name">{(p.name ?? []).map((part: any, i: number) => <span key={i} className={part.isHit ? 'hit' : ''}>{part.text}</span>)}</span>
      {p.isSkill && <span className="subtle mention-description">{p.description}</span>}
      <span className="spacer" />
      {p.hasFolder && <span className="mono subtle footnote">{p.folder}</span>}
    </div>
  );
}

function Mentions({ id, onChoose }: { id: any; onChoose: (mention: Obj) => void }) {
  const mentions = useObj(id);
  const choices = useList(mentions?.p.choices);
  if (!mentions?.p.isOpen || choices.length === 0) return null;
  return <div className="mentions">{choices.map((ref) => <Mention key={idOf(ref)} id={idOf(ref)!} onChoose={onChoose} />)}</div>;
}

function ModelChip({ conversation }: { conversation: Obj }) {
  const models = useList(conversation.p.models);
  const p = conversation.p;
  return (
    <Menu.Root>
      <Menu.Trigger asChild>
        <button className="btn chip" title="The model the agent answers with">{p.modelLabel}</button>
      </Menu.Trigger>
      <Menu.Portal>
        <Menu.Content className="menu" side="top" align="start" sideOffset={6}>
          {models.map((m) => (
            <Menu.Item key={m.$id} className="menu-item" onSelect={() => run(conversation, 'PickModelCommand', m)}>
              <span className="check">{p.modelLabel === m.label ? '✓' : ''}</span>{m.label}
            </Menu.Item>
          ))}
        </Menu.Content>
      </Menu.Portal>
    </Menu.Root>
  );
}

async function attachPicture(conversation: Obj, file: File) {
  await ask('attach-picture', { conversation: { $ref: conversation.id }, type: file.type || 'image/png', data: await readFile(file) });
}

function paths(uris: string) {
  return uris
    .split(/\r?\n/)
    .map((line) => line.trim())
    .filter((line) => line.startsWith('file://'))
    .map((line) => decodeURIComponent(new URL(line).pathname));
}

function Composer({ conversation, box }: { conversation: Obj; box: RefObject<HTMLTextAreaElement | null> }) {
  const p = conversation.p;
  const attachments = useList(p.attachments);
  const mentions = obj(p.mentions);
  const escaped = useRef(0);
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
    area.style.height = `${Math.min(area.scrollHeight, innerHeight * 0.4)}px`;
  }, [local]);

  const look = (value: string, caret: number) => mentions && invoke(mentions, 'Look', value, caret);

  const write = (value: string, caret?: number) => {
    setLocal(value);
    sent.current.push({ value, at: Date.now() });
    put(conversation, 'Draft', value);
    look(value, caret ?? value.length);
  };

  const accept = async (chosen: Obj | null) => {
    const result = await invoke(mentions, 'Accept', box.current!.value, chosen ? { $ref: chosen.id } : null);
    if (!Array.isArray(result)) return;
    const [draft, caret] = result;
    write(draft, caret);
    requestAnimationFrame(() => {
      box.current?.focus();
      box.current?.setSelectionRange(caret, caret);
    });
  };

  const key = (e: KeyboardEvent<HTMLTextAreaElement>) => {
    if (e.ctrlKey || e.altKey || e.metaKey) return;
    const m = obj(p.mentions);
    if (m?.p.isOpen) {
      if (e.key === 'ArrowDown' || e.key === 'ArrowUp') {
        e.preventDefault();
        invoke(m, 'Move', e.key === 'ArrowDown' ? 1 : -1);
        return;
      }
      if (e.key === 'Enter' || e.key === 'Tab') {
        e.preventDefault();
        accept(null);
        return;
      }
      if (e.key === 'Escape') {
        e.preventDefault();
        invoke(m, 'Close');
        return;
      }
    }
    if (e.key === 'Enter' && !e.shiftKey) {
      e.preventDefault();
      if (can(conversation, 'SendCommand')) {
        sent.current = [];
        run(conversation, 'SendCommand');
      }
      return;
    }
    if (e.key === 'Escape') {
      e.preventDefault();
      const now = Date.now();
      if (obj(conversation)?.p.isWorking) {
        run(conversation, 'StopCommand');
        escaped.current = 0;
      } else if (now - escaped.current <= 500) {
        sent.current = [];
        run(conversation, 'ClearCommand');
        escaped.current = 0;
      } else {
        escaped.current = now;
      }
    }
  };

  const paste = async (e: ClipboardEvent) => {
    const files = [...(e.clipboardData?.files ?? [])].filter((f) => f.type.startsWith('image/'));
    if (files.length === 0) return;
    e.preventDefault();
    for (const file of files) await attachPicture(conversation, file);
  };

  return (
    <>
      <Mentions id={p.mentions} onChoose={accept} />
      <div className="composer">
        {attachments.length > 0 && (
          <div className="attachments">
            {attachments.map((a) => (
              <span className="attachment" title={a.tip} key={a.$id}>
                {a.isPicture ? <img src={blob(a.data)} alt="" /> : <span className="mono">{a.label}</span>}
                <IconButton d={glyphs.close} title="Remove" onClick={() => run(conversation, 'DetachCommand', a)} />
              </span>
            ))}
          </div>
        )}
        {p.problem && <span className="field-error">{p.problem}</span>}
        <textarea
          ref={box}
          className="composer-box"
          rows={1}
          placeholder={p.placeholder}
          value={local}
          onChange={(e) => write(e.target.value, e.target.selectionStart)}
          onKeyDown={key}
          onKeyUp={(e) => ['ArrowLeft', 'ArrowRight', 'Home', 'End'].includes(e.key) && look(e.currentTarget.value, e.currentTarget.selectionStart)}
          onClick={(e) => look(e.currentTarget.value, e.currentTarget.selectionStart)}
          onPaste={paste}
          onBlur={() => mentions?.p.isOpen && setTimeout(() => invoke(mentions, 'Close'), 150)}
        />
        <div className="composer-bar">
          <Button kind="chip" title="How the agent works: Auto, Edits or Plan" onClick={() => run(conversation, 'CycleModeCommand')}>{p.modeLabel}</Button>
          <ModelChip conversation={conversation} />
          <Button kind="chip" chosen={p.isMax} title="How hard the agent thinks" onClick={() => run(conversation, 'ToggleEffortCommand')}>{p.effortLabel}</Button>
          {p.hasQueued && <span className="footnote subtle">{p.queuedLabel}</span>}
          <span className="spacer" />
          {p.isWorking
            ? <Button title="Stop the agent, and bring back what was sent if it has not answered yet · Esc twice clears the message" onClick={() => run(conversation, 'StopCommand')}>Stop <span className="mono subtle">Esc</span></Button>
            : <Button kind="primary" title="Enter · Shift+Enter for a new line" disabled={!can(conversation, 'SendCommand')} onClick={() => run(conversation, 'SendCommand')}>Send</Button>}
        </div>
      </div>
    </>
  );
}

export function Conversation({ id }: { id: any }) {
  const conversation = useObj(id);
  const box = useRef<HTMLTextAreaElement>(null);
  const [dropping, setDropping] = useState(false);
  useEffect(() => on('focus-composer', () => box.current?.focus()), []);
  useEffect(() => {
    requestAnimationFrame(() => box.current?.focus());
  }, [idOf(id)]);
  if (!conversation) return null;

  const drop = async (e: DragEvent) => {
    e.preventDefault();
    setDropping(false);
    const target = { $ref: conversation.id };
    const found = paths(e.dataTransfer.getData('text/uri-list'));
    if (found.length > 0) {
      await ask('attach', { conversation: target, paths: found });
      return;
    }
    const files = [...e.dataTransfer.files];
    if (files.length > 0) {
      for (const file of files.filter((f) => f.type.startsWith('image/'))) await attachPicture(conversation, file);
      return;
    }
    const text = e.dataTransfer.getData('text/plain');
    if (!text) return;
    const typed = paths(text.split(/\s+/).map((t) => (t.startsWith('/') ? `file://${t}` : t)).join('\n'));
    if (typed.length > 0) await ask('attach', { conversation: target, paths: typed });
    else put(conversation, 'Draft', `${conversation.p.draft ?? ''}${text}`);
  };

  return (
    <div className="conversation" onDragOver={(e) => { e.preventDefault(); setDropping(true); }} onDragLeave={() => setDropping(false)} onDrop={drop}>
      <Timeline key={conversation.id} conversation={conversation} />
      <div className="composer-dock"><Composer conversation={conversation} box={box} /></div>
      {dropping && <div className="drop">Drop to attach</div>}
    </div>
  );
}

export function Viewer({ id }: { id: any }) {
  const viewer = useObj(id);
  const open = !!viewer?.p.isOpen && !!viewer.p.current;
  const source = open ? blob(viewer!.p.current.data) : undefined;

  const copy = async () => {
    if (!source) return;
    const picture = await (await fetch(source)).blob();
    try {
      await navigator.clipboard.write([new ClipboardItem({ [picture.type]: picture })]);
    } catch {
      await navigator.clipboard.writeText(viewer!.p.current.label);
    }
  };

  useEffect(() => {
    if (!open) return;
    const key = (e: globalThis.KeyboardEvent) => {
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
    <div className="viewer" onMouseDown={(e) => e.target === e.currentTarget && run(viewer, 'CloseCommand')}>
      <img src={source} alt={p.current.label} />
      <div className="viewer-bar">
        <span>{p.current.label}</span>
        {p.hasOthers && <span className="subtle mono">{p.position}</span>}
        <span className="spacer" />
        <Button onClick={copy}>Copy <span className="mono subtle">Ctrl+C</span></Button>
        <Button onClick={outside}>Open in the system viewer</Button>
        <span className="footnote subtle">Esc closes · ← → move</span>
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
    const key = (e: globalThis.KeyboardEvent) => {
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
    <div className="stage">
      <div className="stage-head">
        <span className="itemframe-source">{loan.p.source}</span>
        <strong>{loan.p.title}</strong>
        <span className="spacer" />
        <span className="footnote subtle">Esc closes</span>
        <IconButton d={glyphs.close} title="Close · Esc" onClick={() => run(conversation, 'CloseSpreadCommand')} />
      </div>
      <div className="stage-body"><LoanView id={loan.id} /></div>
    </div>
  );
}
