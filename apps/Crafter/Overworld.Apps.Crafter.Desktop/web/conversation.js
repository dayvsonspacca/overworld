import { html, Prose, ItemFrame, IconButton, Icon, Button, CopyButton, AllayIcon, glyphs, blob, hex, useCommands } from '/kit/ui.js';
import { useObj, useList, run, can, put, invoke, ask, obj, idOf, on } from '/kit/mirror.js';
import { useEffect, useRef, useState, useLayoutEffect, useCallback } from 'preact/hooks';

function Node({ glyph, kind = '', last, children }) {
  return html`<div class=${`node ${kind} ${last ? 'last' : ''}`}>
    <div class="node-gutter"><i class="node-line"></i><span class="node-dot">${glyph}</span></div>
    <div class="node-body">${children}</div>
  </div>`;
}

function Picture({ view, conversation, big }) {
  if (!view) return null;
  return html`<button class=${`picture ${big ? 'big' : ''}`} title="Open the picture" onClick=${() => run(conversation, 'OpenPictureCommand', view)}>
    <img src=${blob(view.data)} alt=${view.label} />
  </button>`;
}

function Prompt({ entry, conversation }) {
  const p = entry.p;
  return html`<div class="prompt">
    ${p.hasText && html`<div class="prompt-text">${p.text}</div>`}
    ${p.hasFiles && html`<div class="prompt-files">${p.files.map((f) => html`<span>@${f}</span>`)}</div>`}
    ${p.hasSkills && html`<div class="prompt-skills">${p.skills.map((s) => html`<span>/${s}</span>`)}</div>`}
    ${p.hasPictures && html`<div class="prompt-pictures">${p.pictures.map((v) => html`<${Picture} view=${v} conversation=${conversation} />`)}</div>`}
  </div>`;
}

function Act({ entry }) {
  const p = entry.p;
  const kind = `${p.hasFailed ? 'failed' : ''} ${p.isRunning ? 'running' : ''}`;
  return html`<${Node} glyph=${p.glyph} kind=${kind}>
    <button class=${`act ${p.hasFailed ? 'failed' : ''}`} disabled=${!p.hasOutput} onClick=${() => run(entry, 'ToggleCommand')}>
      <span class="act-subject">${p.subject}</span>
      ${p.added && html`<span class="added">${p.added}</span>`}
      ${p.removed && html`<span class="danger">${p.removed}</span>`}
      ${p.verdict && html`<span class="verdict">${p.verdict}</span>`}
    </button>
    ${p.showsOutput && html`<pre class="act-output">${p.shown}</pre>`}
  </${Node}>`;
}

function Plan({ entry }) {
  const p = entry.p;
  return html`<${Node} glyph="☰">
    <div class="plan">
      <span class="group-label">Plan · ${p.progress}</span>
      ${(p.lines ?? []).map((line) => html`<div class=${`plan-step ${line.isDone ? 'done' : ''} ${line.isDoing ? 'doing' : ''}`}><span class="mono">${line.glyph}</span><span>${line.text}</span></div>`)}
    </div>
  </${Node}>`;
}

function Answer({ text }) {
  return text ? html`<span class="answer">${text}</span>` : null;
}

function Permission({ entry }) {
  const p = entry.p;
  return html`<${Node} glyph=${p.glyph}>
    <div class=${`request ${p.isOpen ? 'open' : ''}`}>
      <strong>${p.asks}</strong>
      <div class="mono request-subject">${p.subject}</div>
      ${p.hasReason && html`<div class="soft">${p.reason}</div>`}
      ${p.isOpen && html`<div class="buttons">
        <${Button} kind="primary" onClick=${() => run(entry, 'AllowOnceCommand')}>Allow</${Button}>
        ${p.canRemember && html`<${Button} onClick=${() => run(entry, 'AllowAlwaysCommand')}>Always allow <span class="mono subtle">${p.remembers}</span></${Button}>`}
        <${Button} onClick=${() => run(entry, 'DenyCommand')}>Deny</${Button}>
      </div>`}
      ${!p.isOpen && html`<${Answer} text=${p.answer} />`}
    </div>
  </${Node}>`;
}

function Option({ id, question, several, open }) {
  const option = useObj(id);
  if (!option) return null;
  const p = option.p;
  return html`<label class="option">
    <input type=${several ? 'checkbox' : 'radio'} name=${`q${question}`} checked=${p.isPicked} disabled=${!open} onChange=${(e) => put(option, 'IsPicked', e.target.checked)} />
    <span><span>${p.label}</span>${p.hasDescription && html`<small class="subtle">${p.description}</small>`}</span>
  </label>`;
}

function Question({ id, open }) {
  const question = useObj(id);
  if (!question) return null;
  const p = question.p;
  return html`<div class="question">
    <span class="group-label">${p.header}</span>
    <strong>${p.text}</strong>
    ${(p.options ?? []).map((o) => html`<${Option} id=${idOf(o)} question=${id} several=${p.several} open=${open} />`)}
    <input placeholder="Or write your own answer" value=${p.written} disabled=${!open} onInput=${(e) => put(question, 'Written', e.target.value)} />
  </div>`;
}

function Questions({ entry }) {
  const p = entry.p;
  return html`<${Node} glyph="?">
    <div class=${`request ${p.isOpen ? 'open' : ''}`}>
      ${(p.asked ?? []).map((q) => html`<${Question} id=${idOf(q)} open=${p.isOpen} />`)}
      ${p.isOpen && html`<div class="buttons">
        <${Button} kind="primary" disabled=${!can(entry, 'SendAnswersCommand')} onClick=${() => run(entry, 'SendAnswersCommand')}>Answer</${Button}>
        <${Button} onClick=${() => run(entry, 'SkipCommand')}>Skip</${Button}>
      </div>`}
      ${!p.isOpen && html`<${Answer} text=${p.answer} />`}
    </div>
  </${Node}>`;
}

function Proposal({ entry, title, children, approve = 'Approve', decline = 'Decline', glyph = '§' }) {
  const p = entry.p;
  return html`<${Node} glyph=${glyph}>
    <div class=${`request ${p.isOpen ? 'open' : ''}`}>
      ${title}
      ${children}
      ${p.isOpen && html`<div class="buttons">
        <${Button} kind="primary" onClick=${() => run(entry, 'ApproveCommand')}>${approve}</${Button}>
        <${Button} onClick=${() => run(entry, 'DeclineCommand')}>${decline}</${Button}>
      </div>`}
      ${!p.isOpen && html`<${Answer} text=${p.verdict} />`}
    </div>
  </${Node}>`;
}

function SummaryProposal({ entry }) {
  const p = entry.p;
  const proposed = (p.lines ?? []).filter((line) => !line.isRemoved).map((line) => line.text).join('\n');
  const title = html`<div class="request-head"><strong>${p.asking}</strong><span class="chips">
    <${Button} kind="chip" chosen=${p.showsPreview} onClick=${() => run(entry, 'ShowPreviewCommand')}>Preview</${Button}>
    <${Button} kind="chip" chosen=${p.showsChanges} onClick=${() => run(entry, 'ShowChangesCommand')}>Changes</${Button}>
  </span></div>`;
  return html`<${Proposal} entry=${entry} glyph="¶" title=${title}>
    ${p.showsPreview
      ? html`<div class="prose-preview"><${Prose} text=${proposed} /></div>`
      : html`<pre class="prose-code">${(p.lines ?? []).map((line) => html`<div class=${line.isAdded ? 'added' : line.isRemoved ? 'danger' : ''}>${line.marked}</div>`)}</pre>`}
  </${Proposal}>`;
}

export function LoanView({ id }) {
  const loan = useObj(id);
  if (!loan) return null;
  const p = loan.p;
  if (loan.type === 'PaintingLoan') {
    return html`<iframe class="loan-painting" sandbox="allow-scripts allow-popups" srcdoc=${p.html}></iframe>`;
  }
  if (loan.type === 'PaletteLoan') {
    return html`<div class="loan-palette">${(p.swatches ?? []).map((s) => html`<div style=${{ background: hex(s.fill), color: s.ink }}><span>${s.label}</span></div>`)}</div>`;
  }
  if (loan.type === 'ThemeLoan') {
    return html`<div class="loan-theme">${(p.groups ?? []).map((group) => html`<div class="loan-group">
      <span class="group-label">${group.title}</span>
      <div class="loan-roles">${group.swatches.map((s) => html`<div class="loan-role"><i style=${{ background: hex(s.hex) }}></i><span>${s.role}</span><code>${s.hex}</code></div>`)}</div>
    </div>`)}</div>`;
  }
  return html`<div class="empty">${p.title}</div>`;
}

function Loan({ entry, conversation }) {
  const loan = useObj(entry.p.loan);
  if (!loan) return null;
  return html`<${Node} glyph="▧">
    <${ItemFrame} source=${loan.p.source} title=${loan.p.title} onSeen=${(seen) => put(loan, 'IsLive', seen)} onExpand=${() => run(conversation, 'ExpandLoanCommand', { $ref: entry.id })}>
      <${LoanView} id=${loan.id} />
    </${ItemFrame}>
  </${Node}>`;
}

function Entry({ id, conversation }) {
  const entry = useObj(id);
  if (!entry) return null;
  const p = entry.p;
  switch (entry.type) {
    case 'PromptEntry': return html`<${Prompt} entry=${entry} conversation=${conversation} />`;
    case 'SpeechEntry': return html`<${Node} glyph="✦" kind="speech"><${Prose} text=${p.text} /></${Node}>`;
    case 'ThoughtEntry': return html`<${Node} glyph="…"><div class="thought"><span class="thought-head">${p.heading}</span><div class="thought-text">${p.text}</div></div></${Node}>`;
    case 'ActEntry': return html`<${Act} entry=${entry} />`;
    case 'PlanEntry': return html`<${Plan} entry=${entry} />`;
    case 'PermissionEntry': return html`<${Permission} entry=${entry} />`;
    case 'QuestionsEntry': return html`<${Questions} entry=${entry} />`;
    case 'AxiomProposalEntry': return html`<${Proposal} entry=${entry} title=${html`<strong>A new axiom for the project</strong>`}><div class="axiom">${p.axiom}</div></${Proposal}>`;
    case 'AxiomRemovalProposalEntry': return html`<${Proposal} entry=${entry} approve="Drop it" decline="Keep it" title=${html`<strong>Drop an axiom of the project</strong>`}><div class="axiom struck">${p.axiom}</div>${p.hasWhy && html`<div class="subtle">${p.why}</div>`}</${Proposal}>`;
    case 'SummaryProposalEntry': return html`<${SummaryProposal} entry=${entry} />`;
    case 'NoticeEntry': return html`<${Node} glyph="↺"><span class="footnote">${p.text}</span></${Node}>`;
    case 'OutcomeEntry': return html`<${Node} glyph="■" last kind=${p.wentWrong ? 'failed' : ''}><span class="footnote">${p.text}</span></${Node}>`;
    case 'PictureEntry': return html`<${Node} glyph="◫"><div class="shown-picture"><${Picture} view=${p.picture} conversation=${conversation} big />${p.hasCaption && html`<span class="caption">${p.caption}</span>`}</div></${Node}>`;
    case 'LoanEntry': return html`<${Loan} entry=${entry} conversation=${conversation} />`;
    default: return null;
  }
}

function Pulse({ id, color }) {
  const pulse = useObj(id);
  if (!pulse?.p.isShown) return null;
  return html`<div class="pulse"><${AllayIcon} color=${color} size=${26} className="pulse-allay" /><span class="pulse-word">${pulse.p.word}</span><span class="pulse-time">· ${pulse.p.elapsed}</span></div>`;
}

function Timeline({ conversation }) {
  const entries = useList(conversation.p.timeline?.entries);
  const page = useRef(null);
  const content = useRef(null);
  const following = useRef(true);
  const lastPrompt = useRef(null);

  useLayoutEffect(() => {
    const scroller = page.current;
    const follow = () => {
      if (following.current) scroller.scrollTop = scroller.scrollHeight;
    };
    const watcher = new ResizeObserver(follow);
    watcher.observe(content.current);
    watcher.observe(scroller);
    follow();
    return () => watcher.disconnect();
  }, [conversation.id]);

  useEffect(() => {
    following.current = true;
    page.current.scrollTop = page.current.scrollHeight;
  }, [conversation.id]);

  useEffect(() => {
    const prompts = entries.filter((ref) => obj(ref)?.type === 'PromptEntry');
    const newest = prompts.length ? idOf(prompts[prompts.length - 1]) : null;
    if (newest !== lastPrompt.current) {
      if (lastPrompt.current !== null) {
        following.current = true;
        page.current.scrollTop = page.current.scrollHeight;
      }
      lastPrompt.current = newest;
    }
  }, [entries]);

  const scrolled = () => {
    const s = page.current;
    following.current = s.scrollHeight - s.scrollTop - s.clientHeight < 8;
  };

  return html`<div class="timeline-page" ref=${page} onScroll=${scrolled}>
    <div class="timeline" ref=${content}>
      ${entries.map((ref) => html`<${Entry} key=${idOf(ref)} id=${idOf(ref)} conversation=${conversation} />`)}
      <${Pulse} id=${conversation.p.pulse} color=${conversation.p.color} />
    </div>
  </div>`;
}

function Mentions({ id, onChoose }) {
  const mentions = useObj(id);
  const choices = useList(mentions?.p.choices);
  if (!mentions?.p.isOpen || choices.length === 0) return null;
  return html`<div class="mentions">
    ${choices.map((ref) => html`<${Mention} key=${idOf(ref)} id=${idOf(ref)} onChoose=${onChoose} />`)}
  </div>`;
}

function Mention({ id, onChoose }) {
  const mention = useObj(id);
  if (!mention) return null;
  const p = mention.p;
  return html`<div class=${`mention ${p.isSelected ? 'selected' : ''}`} onMouseDown=${(e) => { e.preventDefault(); onChoose(mention); }}>
    <span class="mono accent">${p.glyph}</span>
    <span class="mono mention-name">${(p.name ?? []).map((part) => html`<span class=${part.isHit ? 'hit' : ''}>${part.text}</span>`)}</span>
    ${p.isSkill && html`<span class="subtle mention-description">${p.description}</span>`}
    <span class="spacer"></span>
    ${p.hasFolder && html`<span class="mono subtle footnote">${p.folder}</span>`}
  </div>`;
}

function ModelChip({ conversation }) {
  const [open, setOpen] = useState(false);
  const models = useList(conversation.p.models);
  const p = conversation.p;
  useEffect(() => {
    if (!open) return;
    const close = () => setOpen(false);
    setTimeout(() => addEventListener('click', close, { once: true }), 0);
    return () => removeEventListener('click', close);
  }, [open]);
  return html`<span class="menu-anchor">
    <${Button} kind="chip" title="The model the agent answers with" onClick=${() => setOpen(!open)}>${p.modelLabel}</${Button}>
    ${open && html`<div class="menu">${models.map((m) => html`<div class="menu-item" onClick=${() => run(conversation, 'PickModelCommand', m)}><span class="check">${p.modelLabel === m.label ? '✓' : ''}</span>${m.label}</div>`)}</div>`}
  </span>`;
}

function Composer({ conversation, box }) {
  const p = conversation.p;
  const attachments = useList(p.attachments);
  const mentions = obj(p.mentions);
  const escaped = useRef(0);
  const [local, setLocal] = useState(p.draft ?? '');
  const sent = useRef([]);

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

  const write = (value, caret) => {
    setLocal(value);
    sent.current.push({ value, at: Date.now() });
    put(conversation, 'Draft', value);
    if (mentions) invoke(mentions, 'Look', value, caret ?? value.length);
  };

  const accept = async (chosen) => {
    if (!mentions) return;
    const result = await invoke(mentions, 'Accept', box.current.value, chosen ? { $ref: chosen.id } : null);
    if (!Array.isArray(result)) return;
    const [draft, caret] = result;
    write(draft, caret);
    requestAnimationFrame(() => {
      box.current.focus();
      box.current.setSelectionRange(caret, caret);
    });
  };

  const key = (e) => {
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

  const paste = async (e) => {
    const files = [...(e.clipboardData?.files ?? [])].filter((f) => f.type.startsWith('image/'));
    if (files.length === 0) return;
    e.preventDefault();
    for (const file of files) await attachPicture(conversation, file);
  };

  return html`<div class="composer">
    ${attachments.length > 0 && html`<div class="attachments">${attachments.map((a) => html`<span class="attachment" title=${a.tip}>
      ${a.isPicture ? html`<img src=${blob(a.data)} alt="" />` : html`<span class="mono">${a.label}</span>`}
      <${IconButton} d=${glyphs.close} title="Remove" onClick=${() => run(conversation, 'DetachCommand', a)} />
    </span>`)}</div>`}
    ${p.problem && html`<span class="field-error">${p.problem}</span>`}
    <textarea ref=${box} class="composer-box" rows="1" placeholder=${p.placeholder} value=${local}
      onInput=${(e) => write(e.target.value, e.target.selectionStart)}
      onKeyDown=${key}
      onKeyUp=${(e) => mentions && ['ArrowLeft', 'ArrowRight', 'Home', 'End'].includes(e.key) && invoke(mentions, 'Look', e.target.value, e.target.selectionStart)}
      onClick=${(e) => mentions && invoke(mentions, 'Look', e.target.value, e.target.selectionStart)}
      onPaste=${paste}
      onBlur=${() => mentions?.p.isOpen && setTimeout(() => invoke(mentions, 'Close'), 150)}></textarea>
    <div class="composer-bar">
      <${Button} kind="chip" title="How the agent works: Auto, Edits or Plan" onClick=${() => run(conversation, 'CycleModeCommand')}>${p.modeLabel}</${Button}>
      <${ModelChip} conversation=${conversation} />
      <${Button} kind="chip" chosen=${p.isMax} title="How hard the agent thinks" onClick=${() => run(conversation, 'ToggleEffortCommand')}>${p.effortLabel}</${Button}>
      ${p.hasQueued && html`<span class="footnote subtle">${p.queuedLabel}</span>`}
      <span class="spacer"></span>
      ${p.isWorking
        ? html`<${Button} title="Stop the agent, and bring back what was sent if it has not answered yet · Esc twice clears the message" onClick=${() => run(conversation, 'StopCommand')}>Stop <span class="mono subtle">Esc</span></${Button}>`
        : html`<${Button} kind="primary" title="Enter · Shift+Enter for a new line" disabled=${!can(conversation, 'SendCommand')} onClick=${() => run(conversation, 'SendCommand')}>Send</${Button}>`}
    </div>
  </div>`;
}

async function attachPicture(conversation, file) {
  const data = await new Promise((resolve) => {
    const reader = new FileReader();
    reader.onload = () => resolve(String(reader.result).split(',')[1] ?? '');
    reader.readAsDataURL(file);
  });
  await ask('attach-picture', { conversation: { $ref: conversation.id }, type: file.type || 'image/png', data });
}

function paths(uris) {
  return uris
    .split(/\r?\n/)
    .map((line) => line.trim())
    .filter((line) => line.startsWith('file://'))
    .map((line) => decodeURIComponent(new URL(line).pathname));
}

export function Conversation({ id, focus }) {
  const conversation = useObj(id);
  const box = useRef(null);
  const [dropping, setDropping] = useState(false);
  useEffect(() => on('focus-composer', () => box.current?.focus()), []);
  useEffect(() => {
    if (focus) requestAnimationFrame(() => box.current?.focus());
  }, [id]);
  if (!conversation) return null;
  const mentions = conversation.p.mentions;

  const over = (e) => {
    e.preventDefault();
    setDropping(true);
  };
  const drop = async (e) => {
    e.preventDefault();
    setDropping(false);
    const uris = e.dataTransfer.getData('text/uri-list');
    const found = uris ? paths(uris) : [];
    if (found.length > 0) {
      await ask('attach', { conversation: { $ref: conversation.id }, paths: found });
      return;
    }
    const files = [...(e.dataTransfer.files ?? [])];
    if (files.length > 0) {
      for (const file of files.filter((f) => f.type.startsWith('image/'))) await attachPicture(conversation, file);
      return;
    }
    const text = e.dataTransfer.getData('text/plain');
    if (text) {
      const found = paths(text.split(/\s+/).map((t) => (t.startsWith('/') ? `file://${t}` : t)).join('\n'));
      if (found.length > 0) await ask('attach', { conversation: { $ref: conversation.id }, paths: found });
      else put(conversation, 'Draft', `${conversation.p.draft ?? ''}${text}`);
    }
  };

  const chooseMention = async (mention) => {
    const result = await invoke(obj(mentions), 'Accept', box.current.value, { $ref: mention.id });
    if (!Array.isArray(result)) return;
    const [draft, caret] = result;
    put(conversation, 'Draft', draft);
    requestAnimationFrame(() => {
      box.current.value = draft;
      box.current.focus();
      box.current.setSelectionRange(caret, caret);
      invoke(obj(mentions), 'Look', draft, caret);
    });
  };

  return html`<div class="conversation" onDragOver=${over} onDragLeave=${() => setDropping(false)} onDrop=${drop}>
    <${Timeline} conversation=${conversation} />
    <div class="composer-dock">
      <${Mentions} id=${mentions} onChoose=${chooseMention} />
      <${Composer} conversation=${conversation} box=${box} />
    </div>
    ${dropping && html`<div class="drop">Drop to attach</div>`}
  </div>`;
}

export function Viewer({ id }) {
  const viewer = useObj(id);
  useEffect(() => {
    if (!viewer?.p.isOpen) return;
    const key = (e) => {
      if (e.key === 'Escape') { e.preventDefault(); run(viewer, 'CloseCommand'); }
      else if (e.key === 'ArrowRight') run(viewer, 'NextCommand');
      else if (e.key === 'ArrowLeft') run(viewer, 'PreviousCommand');
      else if (e.ctrlKey && e.key.toLowerCase() === 'c' && !getSelection()?.toString()) { e.preventDefault(); copy(); }
    };
    addEventListener('keydown', key, true);
    return () => removeEventListener('keydown', key, true);
  }, [viewer?.p.isOpen]);
  if (!viewer?.p.isOpen || !viewer.p.current) return null;
  const p = viewer.p;
  const source = blob(p.current.data);
  async function copy() {
    const fetched = await fetch(source);
    const png = await fetched.blob();
    try {
      await navigator.clipboard.write([new ClipboardItem({ [png.type]: png })]);
    } catch {
      await navigator.clipboard.writeText(p.current.label);
    }
  }
  const outside = async () => {
    const file = await invoke(viewer, 'FileOfCurrent');
    if (file) ask('open-outside', { path: file });
  };
  return html`<div class="viewer" onMouseDown=${(e) => e.target === e.currentTarget && run(viewer, 'CloseCommand')}>
    <img src=${source} alt=${p.current.label} />
    <div class="viewer-bar">
      <span>${p.current.label}</span>
      ${p.hasOthers && html`<span class="subtle mono">${p.position}</span>`}
      <span class="spacer"></span>
      <${Button} onClick=${copy}>Copy <span class="mono subtle">Ctrl+C</span></${Button}>
      <${Button} onClick=${outside}>Open in the system viewer</${Button}>
      <span class="footnote subtle">Esc closes · ← → move</span>
    </div>
  </div>`;
}

export function Stage({ id }) {
  const conversation = useObj(id);
  const spread = useObj(conversation?.p.spread);
  const loan = useObj(spread?.p.loan);
  useEffect(() => {
    if (!spread) return;
    const key = (e) => {
      if (e.key === 'Escape') { e.preventDefault(); run(conversation, 'CloseSpreadCommand'); }
    };
    addEventListener('keydown', key, true);
    return () => removeEventListener('keydown', key, true);
  }, [spread?.id]);
  useEffect(() => {
    if (loan) put(loan, 'IsLive', true);
  }, [loan?.id]);
  if (!spread || !loan) return null;
  return html`<div class="stage">
    <div class="stage-head">
      <span class="itemframe-source">${loan.p.source}</span>
      <strong>${loan.p.title}</strong>
      <span class="spacer"></span>
      <span class="footnote subtle">Esc closes</span>
      <${IconButton} d=${glyphs.close} title="Close · Esc" onClick=${() => run(conversation, 'CloseSpreadCommand')} />
    </div>
    <div class="stage-body"><${LoanView} id=${loan.id} /></div>
  </div>`;
}
