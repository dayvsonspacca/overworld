import { useEffect, useRef, useState, type ClipboardEvent, type KeyboardEvent, type RefObject } from 'react';
import { ArrowUp, Check, ChevronDown, ClipboardList, FilePen, FileText, Paperclip, Plus, Square, Wand2, X, type LucideIcon } from 'lucide-react';
import { blob, choose, readFile, obj, useObj, useList, useDraft, run, can, put, invoke, ask, idOf, type Obj } from '@overworld/kit';
import { DropdownMenu, DropdownMenuContent, DropdownMenuItem, DropdownMenuLabel, DropdownMenuRadioGroup, DropdownMenuRadioItem, DropdownMenuSeparator, DropdownMenuTrigger, cn } from '@overworld/chat';

export const footerButton =
  'inline-flex h-8 min-w-8 shrink-0 cursor-pointer items-center justify-center gap-1 rounded-full px-2 text-xs text-muted-foreground transition-colors hover:bg-muted hover:text-foreground active:bg-muted disabled:pointer-events-none disabled:opacity-40';

const Modes: Record<string, { label: string; description: string; icon: LucideIcon }> = {
  Auto: { label: 'Auto', description: 'Does what it needs.', icon: Wand2 },
  Edits: { label: 'Edits', description: 'Changes files, but asks before anything else.', icon: FilePen },
  Plan: { label: 'Plan', description: 'Explores and plans before changing anything.', icon: ClipboardList },
};

export async function attachPicture(conversation: Obj, file: File) {
  await ask('attach-picture', { conversation: { $ref: conversation.id }, type: file.type || 'image/png', data: await readFile(file) });
}

function ModelPill({ conversation }: { conversation: Obj }) {
  const models = useList(conversation.p.models);
  const p = conversation.p;
  return (
    <DropdownMenu>
      <DropdownMenuTrigger asChild>
        <button type="button" className={cn(footerButton, 'min-w-0 max-w-44 shrink')}>
          <span className="truncate text-foreground/80">{p.modelLabel}</span>
          {p.isMax && <span className="shrink-0">Max</span>}
          <ChevronDown className="size-3 shrink-0" />
        </button>
      </DropdownMenuTrigger>
      <DropdownMenuContent side="top" align="start" className="w-72" onCloseAutoFocus={(e) => e.preventDefault()}>
        <DropdownMenuLabel>Effort</DropdownMenuLabel>
        <DropdownMenuRadioGroup value={p.isMax ? 'max' : 'default'} onValueChange={(value) => (value === 'max') !== !!p.isMax && run(conversation, 'ToggleEffortCommand')}>
          <DropdownMenuRadioItem value="default">Default</DropdownMenuRadioItem>
          <DropdownMenuRadioItem value="max">Max</DropdownMenuRadioItem>
        </DropdownMenuRadioGroup>
        <DropdownMenuSeparator />
        <DropdownMenuLabel>Model</DropdownMenuLabel>
        <div className="max-h-64 overflow-y-auto">
          <DropdownMenuRadioGroup value={p.modelLabel} onValueChange={(label) => run(conversation, 'PickModelCommand', models.find((m) => m.label === label))}>
            {models.map((m) => <DropdownMenuRadioItem key={m.$id} value={m.label}>{m.label}</DropdownMenuRadioItem>)}
          </DropdownMenuRadioGroup>
        </div>
      </DropdownMenuContent>
    </DropdownMenu>
  );
}

function ModeMenu({ conversation }: { conversation: Obj }) {
  const mode: string = conversation.p.mode ?? 'Auto';
  const info = Modes[mode] ?? Modes.Auto;
  const Icon = info.icon;
  return (
    <DropdownMenu>
      <DropdownMenuTrigger asChild>
        <button type="button" className={cn(footerButton, 'min-w-0 max-w-32 shrink')} title={info.description}>
          <Icon className="size-4 shrink-0" />
          <span className="truncate">{info.label}</span>
        </button>
      </DropdownMenuTrigger>
      <DropdownMenuContent side="top" align="end" className="w-72" onCloseAutoFocus={(e) => e.preventDefault()}>
        <DropdownMenuLabel>Modes</DropdownMenuLabel>
        {Object.entries(Modes).map(([key, m]) => {
          const ItemIcon = m.icon;
          return (
            <DropdownMenuItem key={key} className="items-start" onSelect={() => put(conversation, 'Mode', key)}>
              <ItemIcon className="mt-0.5" />
              <div className="min-w-0 flex-1">
                <div>{m.label}</div>
                <div className="text-xs text-muted-foreground">{m.description}</div>
              </div>
              {key === mode && <Check className="mt-0.5" />}
            </DropdownMenuItem>
          );
        })}
      </DropdownMenuContent>
    </DropdownMenu>
  );
}

function AddMenu({ conversation, onMention }: { conversation: Obj; onMention: () => void }) {
  const attach = async () => {
    const picked = await choose({ multiple: true, title: 'Attach files or pictures' });
    const paths = Array.isArray(picked) ? picked : picked ? [picked] : [];
    if (paths.length > 0) await ask('attach', { conversation: { $ref: conversation.id }, paths });
  };
  return (
    <DropdownMenu>
      <DropdownMenuTrigger asChild>
        <button type="button" className={cn(footerButton, 'px-0')} aria-label="Add">
          <Plus className="size-5" />
        </button>
      </DropdownMenuTrigger>
      <DropdownMenuContent side="top" align="start" className="w-64" onCloseAutoFocus={(e) => e.preventDefault()}>
        <DropdownMenuItem onSelect={attach}>
          <Paperclip />
          <div>
            <div>Attach</div>
            <div className="text-xs text-muted-foreground">Files or pictures from this machine</div>
          </div>
        </DropdownMenuItem>
        <DropdownMenuItem onSelect={() => setTimeout(onMention, 50)}>
          <FileText />
          <div>
            <div>Mention a file</div>
            <div className="text-xs text-muted-foreground">@ and its name</div>
          </div>
        </DropdownMenuItem>
      </DropdownMenuContent>
    </DropdownMenu>
  );
}

function Attachments({ conversation }: { conversation: Obj }) {
  const attachments = useList(conversation.p.attachments);
  if (attachments.length === 0) return null;
  return (
    <div className="border-b px-1.5">
      <div className="flex gap-1.5 overflow-x-auto py-1.5">
        {attachments.map((a) => (
          <span key={a.$id} title={a.tip} className="group relative flex h-14 shrink-0 items-center gap-1.5 overflow-hidden rounded-lg border bg-muted/40 pr-7 pl-1.5">
            {a.isPicture ? <img src={blob(a.data)} alt="" className="size-11 rounded object-cover" /> : <FileText className="size-4 text-muted-foreground" />}
            {!a.isPicture && <span className="max-w-40 truncate font-mono text-xs">{a.label}</span>}
            <button type="button" aria-label="Remove" onClick={() => run(conversation, 'DetachCommand', a)} className="absolute top-1 right-1 flex size-5 cursor-pointer items-center justify-center rounded-full bg-background/80 text-muted-foreground hover:text-foreground">
              <X className="size-3" />
            </button>
          </span>
        ))}
      </div>
    </div>
  );
}

function Mention({ id, onChoose }: { id: number; onChoose: (mention: Obj) => void }) {
  const mention = useObj(id);
  if (!mention) return null;
  const p = mention.p;
  return (
    <div className={cn('flex cursor-pointer items-center gap-2 rounded-lg px-2 py-1.5 text-sm', p.isSelected && 'bg-accent')} onMouseDown={(e) => { e.preventDefault(); onChoose(mention); }}>
      <span className="w-3 font-mono text-brand">{p.glyph}</span>
      <span className="font-mono text-xs whitespace-nowrap">{(p.name ?? []).map((part: any, i: number) => <span key={i} className={part.isHit ? 'font-semibold text-foreground' : 'text-foreground/80'}>{part.text}</span>)}</span>
      {p.isSkill && <span className="min-w-0 truncate text-xs text-muted-foreground">{p.description}</span>}
      <span className="flex-1" />
      {p.hasFolder && <span className="truncate font-mono text-[11px] text-muted-foreground">{p.folder}</span>}
    </div>
  );
}

function Mentions({ id, onChoose }: { id: any; onChoose: (mention: Obj) => void }) {
  const mentions = useObj(id);
  const choices = useList(mentions?.p.choices);
  if (!mentions?.p.isOpen || choices.length === 0) return null;
  return (
    <div className="absolute right-0 bottom-full left-0 mb-2 max-h-72 overflow-y-auto rounded-xl bg-popover p-1 text-popover-foreground shadow-md ring-1 ring-foreground/10">
      {choices.map((ref) => <Mention key={idOf(ref)} id={idOf(ref)!} onChoose={onChoose} />)}
    </div>
  );
}

export function Composer({ conversation, box }: { conversation: Obj; box: RefObject<HTMLTextAreaElement | null> }) {
  const p = conversation.p;
  const mentions = obj(p.mentions);
  const escaped = useRef(0);
  const draft = useDraft(p.draft, (value) => put(conversation, 'Draft', value));
  const local = draft.local;
  const [focused, setFocused] = useState(false);
  const color = 'var(--accent)';

  useEffect(() => {
    const area = box.current;
    if (!area) return;
    area.style.height = 'auto';
    area.style.height = `${Math.min(area.scrollHeight, 192)}px`;
  }, [local]);

  const look = (value: string, caret: number) => mentions && invoke(mentions, 'Look', value, caret);

  const write = (value: string, caret?: number) => {
    draft.write(value);
    look(value, caret ?? value.length);
  };

  const accept = async (chosen: Obj | null) => {
    const result = await invoke(obj(p.mentions), 'Accept', box.current!.value, chosen ? { $ref: chosen.id } : null);
    if (!Array.isArray(result)) return;
    const [draft, caret] = result;
    write(draft, caret);
    requestAnimationFrame(() => {
      box.current?.focus();
      box.current?.setSelectionRange(caret, caret);
    });
  };

  const mention = () => {
    const area = box.current;
    if (!area) return;
    const at = area.selectionStart ?? local.length;
    const before = local.slice(0, at);
    const value = `${before}${before && !/\s$/.test(before) ? ' ' : ''}@${local.slice(at)}`;
    const caret = value.length - local.slice(at).length;
    write(value, caret);
    requestAnimationFrame(() => {
      area.focus();
      area.setSelectionRange(caret, caret);
    });
  };

  const send = () => {
    if (!can(conversation, 'SendCommand')) return;
    draft.forget();
    run(conversation, 'SendCommand');
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
      send();
      return;
    }
    if (e.key === 'Escape') {
      e.preventDefault();
      const now = Date.now();
      if (obj(conversation)?.p.isWorking) {
        run(conversation, 'StopCommand');
        escaped.current = 0;
      } else if (now - escaped.current <= 500) {
        draft.forget();
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
    <div className="mx-auto w-full max-w-3xl px-4 pt-1.5 pb-4">
      {p.problem && <p className="mb-1.5 px-2 text-xs text-destructive">{p.problem}</p>}
      <div
        className="relative rounded-2xl border bg-card shadow-[0_12px_32px_-18px_rgb(0_0_0/0.35)] transition-[border-color,box-shadow] duration-200"
        style={focused ? { borderColor: `color-mix(in oklab, ${color} 70%, transparent)`, boxShadow: `0 0 0 3px color-mix(in oklab, ${color} 12%, transparent), 0 12px 40px -14px color-mix(in oklab, ${color} 50%, transparent)` } : undefined}
      >
        <Mentions id={p.mentions} onChoose={accept} />
        <Attachments conversation={conversation} />
        <textarea
          ref={box}
          rows={1}
          value={local}
          placeholder={p.placeholder}
          onFocus={() => setFocused(true)}
          onBlur={() => {
            setFocused(false);
            if (mentions?.p.isOpen) setTimeout(() => invoke(mentions, 'Close'), 150);
          }}
          onChange={(e) => write(e.target.value, e.target.selectionStart)}
          onKeyDown={key}
          onKeyUp={(e) => ['ArrowLeft', 'ArrowRight', 'Home', 'End'].includes(e.key) && look(e.currentTarget.value, e.currentTarget.selectionStart)}
          onClick={(e) => look(e.currentTarget.value, e.currentTarget.selectionStart)}
          onPaste={paste}
          className="block max-h-48 min-h-11 w-full resize-none border-0 bg-transparent px-3.5 py-2.5 text-sm text-foreground outline-none placeholder:text-muted-foreground"
        />
        <div className="flex items-center gap-0.5 px-1.5 pb-1.5">
          <AddMenu conversation={conversation} onMention={mention} />
          <ModelPill conversation={conversation} />
          {p.hasQueued && <span className="px-2 text-xs text-muted-foreground">{p.queuedLabel}</span>}
          <div className="flex-1" />
          <ModeMenu conversation={conversation} />
          {p.isWorking ? (
            <button type="button" onClick={() => run(conversation, 'StopCommand')} aria-label="Stop" title="Stop · Esc" className="ml-0.5 flex size-8 shrink-0 cursor-pointer items-center justify-center rounded-lg bg-brand text-[var(--on-accent)]">
              <Square className="size-3.5 fill-current" />
            </button>
          ) : (
            <button type="button" onClick={send} disabled={!can(conversation, 'SendCommand')} aria-label="Send" title="Send · Enter" className="ml-0.5 flex size-8 shrink-0 cursor-pointer items-center justify-center rounded-lg bg-brand text-[var(--on-accent)] transition-[filter] hover:brightness-110 disabled:cursor-default disabled:opacity-40">
              <ArrowUp className="size-4.5" />
            </button>
          )}
        </div>
      </div>
    </div>
  );
}
