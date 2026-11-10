import { createElement, useEffect, useMemo, useRef, useState, type ReactNode } from 'react';
import { House, ListChecks, Scale, Sparkles, SquareTerminal } from 'lucide-react';
import { DndContext, PointerSensor, closestCenter, useSensor, useSensors, type DragEndEvent } from '@dnd-kit/core';
import { SortableContext, useSortable, verticalListSortingStrategy } from '@dnd-kit/sortable';
import { CSS } from '@dnd-kit/utilities';
import { boot, TitleBar, IconButton, Icon, Button, Modal, Prose, AllayIcon, SearchField, TextField, Progress, glyphs, useCommands, localFile, choose, useObj, useList, run, can, put, invoke, obj, idOf, type Obj } from '@overworld/kit';
import { Conversation, Viewer, Stage } from './chat/conversation';
import './app.css';

const focusComposer = () => requestAnimationFrame(() => document.querySelector<HTMLTextAreaElement>('.composer-box')?.focus());

function Sortable({ ids, onMove, disabled, children }: { ids: number[]; onMove: (from: number, to: number) => void; disabled?: boolean; children: ReactNode }) {
  const sensors = useSensors(useSensor(PointerSensor, { activationConstraint: { distance: 6 } }));
  const end = ({ active, over }: DragEndEvent) => {
    if (!over || active.id === over.id) return;
    onMove(ids.indexOf(Number(active.id)), ids.indexOf(Number(over.id)));
  };
  if (disabled) return <>{children}</>;
  return (
    <DndContext sensors={sensors} collisionDetection={closestCenter} onDragEnd={end}>
      <SortableContext items={ids} strategy={verticalListSortingStrategy}>{children}</SortableContext>
    </DndContext>
  );
}

function Lifted({ id, disabled, className, children }: { id: number; disabled?: boolean; className?: string; children: ReactNode }) {
  const { attributes, listeners, setNodeRef, transform, transition, isDragging } = useSortable({ id, disabled });
  return (
    <div ref={setNodeRef} className={`${className ?? ''} ${isDragging ? 'lifted' : ''}`} style={{ transform: CSS.Transform.toString(transform), transition }} {...attributes} {...listeners}>
      {children}
    </div>
  );
}

function RailProject({ id, workbench }: { id: number; workbench: Obj }) {
  const project = useObj(id);
  const version = useMemo(() => Date.now(), [project?.p.iconFile, project?.p.project]);
  if (!project) return null;
  const p = project.p;
  return (
    <Lifted id={id} className="rail-slot">
      {p.isCurrent && <i className="rail-mark" />}
      <button className={`rail-project ${p.isCurrent ? 'current' : ''}`} title={p.name} onClick={() => run(workbench, 'OpenCommand', { $ref: project.id })}>
        {p.hasIcon ? <img src={localFile(p.iconFile, version)} alt="" /> : <span>{p.letter}</span>}
      </button>
    </Lifted>
  );
}

function Rail({ workbench }: { workbench: Obj }) {
  const projects = useList(workbench.p.projects);
  const ids = projects.map((ref) => idOf(ref)!);
  return (
    <nav className="rail">
      <div className="rail-slot">
        {workbench.p.showsHome && <i className="rail-mark" />}
        <button className={`rail-project tool ${workbench.p.showsHome ? 'current' : ''}`} title="Home · Ctrl+H" onClick={() => run(workbench, 'GoHomeCommand')}><House className="size-4" /></button>
      </div>
      <i className="rail-rule" />
      <div className="rail-list">
        <Sortable ids={ids} onMove={(from, to) => invoke(workbench, 'MoveProjectAsync', from, to)}>
          {ids.map((id) => <RailProject key={id} id={id} workbench={workbench} />)}
        </Sortable>
        <button className="rail-project tool" title="Add a project · Ctrl+K Ctrl+O" onClick={() => run(workbench, 'AddProjectCommand')}><Icon d={glyphs.plus} /></button>
      </div>
      <button className={`rail-project tool ${workbench.p.showsPreferences ? 'current' : ''}`} title="Preferences · Ctrl+," onClick={() => run(workbench, 'TogglePreferencesCommand')}><Icon d={glyphs.gear} /></button>
    </nav>
  );
}

function ChatRow({ id, project, selected, arranging }: { id: number; project: Obj; selected: boolean; arranging: boolean }) {
  const chat = useObj(id);
  if (!chat) return null;
  const p = chat.p;
  return (
    <Lifted id={id} disabled={!arranging} className={`row chat-row ${selected ? 'selected' : ''} ${p.isWorking ? 'working' : ''} ${p.isUnread ? 'unread' : ''}`}>
      <div className="chat-text" onClick={() => put(project, 'Chat', { $ref: chat.id })}>
        <div className="chat-title"><span>{p.title}</span></div>
        {p.lastLine && <div className="chat-line">{p.lastLine}</div>}
      </div>
      <button className="btn icon chat-delete" title="Delete the chat" onPointerDown={(e) => e.stopPropagation()} onClick={(e) => { e.stopPropagation(); run(project, 'AskToRemoveChatCommand', { $ref: chat.id }); }}>
        <Icon d={glyphs.trash} />
      </button>
    </Lifted>
  );
}

function Chats({ id, workbench }: { id: number; workbench: Obj }) {
  const project = useObj(id);
  const chats = useList(project?.p.chats);
  if (!project) return null;
  const p = project.p;
  const current = idOf(p.chat);
  const ids = chats.map((ref) => idOf(ref)!);
  return (
    <section className="chats">
      <header className="chats-head">
        <button className="project-name" title="The project's page" onClick={() => run(workbench, 'OpenOverviewCommand')}>{p.name}</button>
        <IconButton d={glyphs.plus} title="New chat · Ctrl+T" onClick={() => { run(workbench, 'NewChatCommand'); focusComposer(); }} />
      </header>
      <div className="chats-search"><SearchField id="search" value={p.query ?? ''} placeholder="Search chats" keys="Ctrl+F" onInput={(v) => put(project, 'Query', v)} /></div>
      <div className="chats-list timeline">
        <Sortable ids={ids} disabled={!p.canArrange} onMove={(from, to) => invoke(project, 'MoveChatAsync', from, to)}>
          {ids.map((chat) => <ChatRow key={chat} id={chat} project={project} selected={chat === current} arranging={p.canArrange} />)}
        </Sortable>
        {!p.hasChats && <div className="subtle note">No chats yet.</div>}
        {p.findsNothing && <div className="subtle note">No chat has those words.</div>}
      </div>
    </section>
  );
}

function Adder({ value, placeholder, onInput, onAdd }: { value: string; placeholder: string; onInput: (value: string) => void; onAdd: () => void }) {
  return <div className="adder"><TextField label={<Icon d={glyphs.plus} size={14} />} value={value ?? ''} placeholder={placeholder} keys="Enter" onInput={onInput} onEnter={onAdd} /></div>;
}

function Todo({ id }: { id: number }) {
  const todo = useObj(id);
  if (!todo) return null;
  const p = todo.p;
  return (
    <div className={`todo ${p.done ? 'done' : ''}`}>
      <input type="checkbox" checked={!!p.done} onChange={(e) => put(todo, 'Done', e.target.checked)} />
      <span className="mono subtle">{p.label}</span>
      <span className="todo-text">{p.text}</span>
      <button className="btn icon small" title="Remove" onClick={() => run(todo, 'RemoveCommand')}><Icon d={glyphs.close} /></button>
    </div>
  );
}

function Todos({ id }: { id: any }) {
  const todos = useObj(id);
  const all = useList(todos?.p.todos);
  if (!todos) return null;
  const p = todos.p;
  return (
    <div className="aside-pane">
      {all.length > 0 && <div className="todos-progress"><span>{all.length - (p.left ?? 0)}/{all.length} done</span><Progress fill={((all.length - (p.left ?? 0)) / all.length) * 100} /></div>}
      <Adder value={p.newTodo} placeholder="Something left to do" onInput={(v) => put(todos, 'NewTodo', v)} onAdd={() => run(todos, 'AddCommand')} />
      {all.map((ref) => <Todo key={idOf(ref)} id={idOf(ref)!} />)}
      {!p.hasTodos && <div className="subtle note">Nothing left to do.</div>}
      {p.problem && <div className="legend-error">{p.problem}</div>}
    </div>
  );
}

function Skills({ id }: { id: any }) {
  const skills = useObj(id);
  const all = useList(skills?.p.skills);
  if (!skills) return null;
  return (
    <div className="aside-pane">
      {all.map((skill) => <div className="skill" key={skill.$id}><strong>{skill.name}</strong><span className="subtle">{skill.description}</span></div>)}
      {!skills.p.hasSkills && <div className="subtle note">The project has no skills yet.</div>}
      {skills.p.problem && <div className="legend-error">{skills.p.problem}</div>}
    </div>
  );
}

function Axioms({ id }: { id: any }) {
  const axioms = useObj(id);
  const all = useList<string>(axioms?.p.axioms);
  if (!axioms) return null;
  const p = axioms.p;
  return (
    <div className="aside-pane">
      <Adder value={p.newAxiom} placeholder="A rule every agent follows" onInput={(v) => put(axioms, 'NewAxiom', v)} onAdd={() => run(axioms, 'AddCommand')} />
      {all.map((axiom) => (
        <div className="axiom-row" key={axiom}>
          <span>{axiom}</span>
          <button className="btn icon small" title="Remove" onClick={() => run(axioms, 'RemoveCommand', axiom)}><Icon d={glyphs.close} /></button>
        </div>
      ))}
      {!p.hasAxioms && <div className="subtle note">No axioms yet.</div>}
      {p.problem && <div className="legend-error">{p.problem}</div>}
    </div>
  );
}

function Job({ id }: { id: number }) {
  const job = useObj(id);
  if (!job) return null;
  const p = job.p;
  return (
    <div className="job">
      <div className="job-head"><span className="pill">{p.kind}</span><span className="job-description">{p.description}</span></div>
      <div className="buttons">
        {p.prints && <Button kind="chip" chosen={p.isOpen} onClick={() => run(job, 'ToggleCommand')}>Output</Button>}
        {p.isOpen && <Button kind="chip" onClick={() => run(job, 'RefreshCommand')}>Refresh</Button>}
        <Button kind="chip" onClick={() => run(job, 'StopCommand')}>Stop</Button>
      </div>
      {p.isOpen && p.hasOutput && <pre className="act-output">{p.output}</pre>}
    </div>
  );
}

function Jobs({ id }: { id: any }) {
  const conversation = useObj(id);
  const jobs = useList(conversation?.p.jobs);
  return (
    <div className="aside-pane">
      {jobs.map((ref) => <Job key={idOf(ref)} id={idOf(ref)!} />)}
      {jobs.length === 0 && <div className="subtle note">No background jobs.</div>}
    </div>
  );
}

const Peeks = [
  { tab: 'Todos', title: 'Todos', icon: ListChecks },
  { tab: 'Axioms', title: 'Axioms', icon: Scale },
  { tab: 'Skills', title: 'Skills', icon: Sparkles },
  { tab: 'Jobs', title: 'Background jobs', icon: SquareTerminal },
] as const;

function Trail({ id, workbench }: { id: number; workbench: Obj }) {
  const aside = useObj(id);
  const todos = useObj(aside?.p.todos);
  const axioms = useObj(aside?.p.axioms);
  const skills = useObj(aside?.p.skills);
  const axiomList = useList<string>(axioms?.p.axioms);
  const skillList = useList(skills?.p.skills);
  const conversation = useObj(aside?.p.conversation);
  const jobs = useList(conversation?.p.jobs);
  const open = !!workbench.p.showsAside;
  useEffect(() => {
    if (!open) return;
    const key = (e: KeyboardEvent) => {
      const target = e.target as HTMLElement;
      if (e.key === 'Escape' && !e.defaultPrevented && (target === document.body || target.closest('.drawer'))) {
        e.preventDefault();
        run(workbench, 'ToggleAsideCommand');
      }
    };
    addEventListener('keydown', key);
    return () => removeEventListener('keydown', key);
  }, [open]);
  if (!aside) return null;
  const p = aside.p;
  const counts: Record<string, number | undefined> = {
    Todos: todos?.p.hasLeft ? todos.p.left : undefined,
    Axioms: axiomList.length || undefined,
    Skills: skillList.length || undefined,
    Jobs: jobs.length || undefined,
  };
  const shown = Peeks.filter((peek) => peek.tab !== 'Jobs' || conversation?.p.hasJobs);
  const current = Peeks.find((peek) => peek.tab === p.tab) ?? Peeks[0];
  return (
    <div className="trail-room">
      <aside className={`drawer ${open ? 'open' : ''}`} aria-hidden={!open}>
        <header className="drawer-head">
          <span className="drawer-title">{current.title}</span>
          {counts[current.tab] != null && <span className="drawer-count">{current.tab === 'Todos' ? `${counts.Todos} left` : counts[current.tab]}</span>}
          <span className="spacer" />
          <kbd>Esc</kbd>
          <button className="winctl" aria-label="Close" onClick={() => run(workbench, 'ToggleAsideCommand')}><Icon d={glyphs.close} size={14} /></button>
        </header>
        <div className="drawer-body">
          {p.tab === 'Todos' && <Todos id={p.todos} />}
          {p.tab === 'Skills' && <Skills id={p.skills} />}
          {p.tab === 'Axioms' && <Axioms id={p.axioms} />}
          {p.tab === 'Jobs' && <Jobs id={p.conversation} />}
        </div>
      </aside>
      <nav className="trail">
        {shown.map((peek) => (
          <button key={peek.tab} className={`trail-button ${open && p.tab === peek.tab ? 'chosen' : ''}`} title={peek.title} aria-label={peek.title} onClick={() => run(workbench, 'PeekCommand', peek.tab)}>
            {createElement(peek.icon, { className: 'size-4' })}
            {counts[peek.tab] != null && <span className={`trail-badge ${peek.tab === 'Jobs' ? 'live' : ''}`}>{counts[peek.tab]}</span>}
          </button>
        ))}
      </nav>
    </div>
  );
}

function SkillToggle({ id }: { id: number }) {
  const toggle = useObj(id);
  if (!toggle) return null;
  const p = toggle.p;
  return (
    <label className="toggle-row">
      <input type="checkbox" checked={!!(p.gets || p.isDefault)} disabled={!p.canChange} onChange={(e) => put(toggle, 'Gets', e.target.checked)} />
      <span><strong>{p.name}</strong><small className="subtle">{p.description}</small></span>
    </label>
  );
}

function Overview({ id, project }: { id: any; project: Obj }) {
  const overview = useObj(id);
  const skills = useList(overview?.p.skills);
  const name = useRef<HTMLInputElement>(null);
  const version = useMemo(() => Date.now(), [project.p.iconFile, project.p.project]);
  useEffect(() => {
    if (overview?.p.isRenaming) requestAnimationFrame(() => { name.current?.focus(); name.current?.select(); });
  }, [overview?.p.isRenaming]);
  if (!overview) return null;
  const p = overview.p;
  const pick = async () => {
    const file = await choose({ title: "The project's icon", filters: [{ name: 'Pictures', extensions: ['png', 'jpg', 'jpeg', 'gif', 'webp'] }] });
    if (typeof file === 'string') invoke(overview, 'SetIconAsync', file);
  };
  return (
    <div className="overview">
      <div className="overview-inner">
        <div className="overview-head">
          <div className="overview-icon">{project.p.hasIcon ? <img src={localFile(project.p.iconFile, version)} alt="" /> : <span>{project.p.letter}</span>}</div>
          <div className="overview-name">
            {p.isRenaming
              ? <input ref={name} className="rename" value={p.newName ?? ''} onChange={(e) => put(overview, 'NewName', e.target.value)} onKeyDown={(e) => { if (e.key === 'Enter') run(overview, 'SaveNameCommand'); if (e.key === 'Escape') run(overview, 'CancelRenameCommand'); }} />
              : <h1>{project.p.name}</h1>}
            <span className="mono subtle footnote">{project.p.place}{project.p.branch && ` · ${project.p.branch}`}</span>
          </div>
          <div className="buttons">
            {p.isRenaming ? (
              <>
                <Button kind="primary" disabled={!can(overview, 'SaveNameCommand')} onClick={() => run(overview, 'SaveNameCommand')}>Save</Button>
                <Button onClick={() => run(overview, 'CancelRenameCommand')}>Cancel</Button>
              </>
            ) : <Button title="F2" onClick={() => invoke(overview, 'StartRename')}>Rename</Button>}
            <Button title="Pick a picture to be the project's icon" onClick={pick}>Icon</Button>
            {project.p.hasIcon && <Button onClick={() => run(overview, 'RemoveIconCommand')}>No icon</Button>}
          </div>
        </div>
        {p.problem && <div className="legend-error">{p.problem}</div>}
        <section className="overview-section">
          <div className="overview-section-head">
            <span className="group-label">Summary · .crafter/summary.md</span>
            {!p.isEditing && <Button kind="chip" onClick={() => run(overview, 'EditCommand')}>Edit</Button>}
          </div>
          {p.isEditing ? (
            <>
              <textarea className="summary-editor" value={p.draft ?? ''} onChange={(e) => put(overview, 'Draft', e.target.value)} />
              <div className="buttons">
                <Button kind="primary" onClick={() => run(overview, 'SaveSummaryCommand')}>Save</Button>
                <Button onClick={() => run(overview, 'CancelEditCommand')}>Cancel</Button>
              </div>
            </>
          ) : p.hasSummary
            ? <div className="prose-preview"><Prose text={p.summary} /></div>
            : <span className="subtle">No summary yet. Write what the project is, its stack, its layout and its commands; its agents read it before every chat.</span>}
        </section>
        <section className="overview-section">
          <span className="group-label">Skills</span>
          {skills.map((ref) => <SkillToggle key={idOf(ref)} id={idOf(ref)!} />)}
          {!p.hasSkills && <span className="subtle">The skills library has no skills yet; link one in Preferences.</span>}
        </section>
        <section className="overview-section">
          <Button kind="danger-ghost" onClick={() => run(overview, 'AskToRemoveCommand')}>Remove project</Button>
        </section>
      </div>
      {p.isRemoving && (
        <Modal title="Remove the project?" onClose={() => run(overview, 'KeepProjectCommand')} actions={<><Button onClick={() => run(overview, 'KeepProjectCommand')}>Keep it</Button><Button kind="primary" onClick={() => run(overview, 'RemoveCommand')}>Remove</Button></>}>
          <span>{project.p.name} leaves Crafter. Its folder, its .crafter settings and its chats stay where they are.</span>
        </Modal>
      )}
    </div>
  );
}

function DefaultSkill({ id }: { id: number }) {
  const skill = useObj(id);
  if (!skill) return null;
  const p = skill.p;
  return (
    <label className="toggle-row">
      <input type="checkbox" checked={!!p.isDefault} onChange={(e) => put(skill, 'IsDefault', e.target.checked)} />
      <span><strong>{p.name}</strong><small className="subtle">{p.description}</small></span>
    </label>
  );
}

function Preferences({ id }: { id: any }) {
  const preferences = useObj(id);
  const skills = useList(preferences?.p.skills);
  const problems = useList<string>(preferences?.p.problems);
  if (!preferences) return null;
  const p = preferences.p;
  const pickRoot = async () => {
    const folder = await choose({ directory: true, title: 'The projects folder' });
    if (typeof folder === 'string') invoke(preferences, 'SetProjectsRootAsync', folder);
  };
  return (
    <div className="preferences">
      <div className="overview-inner">
        <h1>Preferences</h1>
        <section className="overview-section">
          <span className="group-label">Projects folder</span>
          <div className="row-line"><span className="mono">{p.projectsRoot}</span><Button onClick={pickRoot}>Choose</Button></div>
          <span className="subtle footnote">Adding a project lists the git repositories found under this folder.</span>
        </section>
        <section className="overview-section">
          <span className="group-label">Skills library</span>
          {p.isLinked ? (
            <div className="row-line">
              <span className="mono">{p.repository}</span>
              <Button disabled={!can(preferences, 'SyncCommand')} onClick={() => run(preferences, 'SyncCommand')}>Sync</Button>
              <Button disabled={!can(preferences, 'UnlinkCommand')} onClick={() => run(preferences, 'UnlinkCommand')}>Unlink</Button>
            </div>
          ) : (
            <div className="row-line">
              <div className="grow"><TextField label="Repo" placeholder="git@github.com:you/skills.git" value={p.draft ?? ''} onInput={(v) => put(preferences, 'Draft', v)} onEnter={() => can(preferences, 'LinkCommand') && run(preferences, 'LinkCommand')} /></div>
              <Button kind="primary" disabled={!can(preferences, 'LinkCommand')} onClick={() => run(preferences, 'LinkCommand')}>Link</Button>
            </div>
          )}
          {p.isBusy && <span className="subtle">Working with the repository…</span>}
          <span className="subtle footnote">Linking clones the repository; Sync brings it to what the repository holds now.</span>
          {p.problem && <div className="legend-error">{p.problem}</div>}
        </section>
        <section className="overview-section">
          <span className="group-label">Skills every project gets</span>
          {skills.map((ref) => <DefaultSkill key={idOf(ref)} id={idOf(ref)!} />)}
          {!p.hasSkills && <span className="subtle">Link a library to choose its skills.</span>}
          {problems.map((problem) => <div className="legend-error" key={problem}>{problem}</div>)}
        </section>
        <span className="subtle footnote">Agents, and the mode, model and effort they start with, are set in Allay, for every app.</span>
      </div>
    </div>
  );
}

function Adding({ id }: { id: any }) {
  const adding = useObj(id);
  const repositories = useList(adding?.p.repositories);
  if (!adding?.p.isOpen) return null;
  const p = adding.p;
  const selected = p.selected?.$id;
  const steer = (e: React.KeyboardEvent) => {
    if (repositories.length === 0) return;
    const at = repositories.findIndex((r) => r.$id === selected);
    if (e.key === 'ArrowDown' || e.key === 'ArrowUp') {
      e.preventDefault();
      const step = e.key === 'ArrowDown' ? 1 : -1;
      put(adding, 'Selected', repositories[(at + step + repositories.length) % repositories.length]);
    } else if (e.key === 'Enter' && can(adding, 'AddCommand')) {
      e.preventDefault();
      run(adding, 'AddCommand');
    }
  };
  const other = async () => {
    const folder = await choose({ directory: true, title: 'A folder to open as a project' });
    if (typeof folder === 'string') invoke(adding, 'AddAsync', folder);
  };
  return (
    <Modal title="Add a project" onClose={() => run(adding, 'CloseCommand')} actions={
      <>
        <Button className="push-left" onClick={other}>Another folder…</Button>
        <Button onClick={() => run(adding, 'CloseCommand')}>Cancel</Button>
        <Button kind="primary" disabled={!can(adding, 'AddCommand')} onClick={() => run(adding, 'AddCommand')}>Add</Button>
      </>
    }>
      <SearchField autoFocus placeholder="Search the repositories" value={p.query ?? ''} onInput={(v) => put(adding, 'Query', v)} onKeyDown={steer} />
      <span className="subtle footnote">Git repositories under {p.root}</span>
      <div className="repositories">
        {repositories.map((r) => (
          <div key={r.$id} className={`repository ${r.$id === selected ? 'selected' : ''}`} onClick={() => put(adding, 'Selected', r)} onDoubleClick={() => run(adding, 'AddCommand')}>
            <span className="mono">{r.name}</span><span className="mono subtle footnote">{r.place}</span>
          </div>
        ))}
        {p.isSearching && <div className="subtle note">Looking for repositories…</div>}
        {p.foundNone && <div className="subtle note">No repository here that is not open yet.</div>}
      </div>
      {p.problem && <div className="legend-error">{p.problem}</div>}
    </Modal>
  );
}

function RemovingChat({ id }: { id: number }) {
  const project = useObj(id);
  const removing = useObj(project?.p.removing);
  if (!project?.p.isRemovingChat || !removing) return null;
  return (
    <Modal title="Delete the chat?" onClose={() => run(project, 'KeepChatCommand')} actions={<><Button onClick={() => run(project, 'KeepChatCommand')}>Keep it</Button><Button kind="primary" onClick={() => run(project, 'RemoveChatCommand')}>Delete</Button></>}>
      <span>"{removing.p.title}" and everything said in it leave Crafter for good.</span>
    </Modal>
  );
}

function Gate({ id }: { id: any }) {
  const gate = useObj(id);
  if (!gate?.p.isShut) return null;
  const p = gate.p;
  return (
    <div className="gate">
      <div className="gate-card">
        <AllayIcon color={p.color} size={96} />
        <h2>{p.heading}</h2>
        <p className="soft">{p.message}</p>
        {p.agent && <div className="gate-agent"><AllayIcon color={p.color} size={20} /><span>{p.agent}</span></div>}
        {p.trouble && <pre className="act-output">{p.trouble}</pre>}
        {p.isChecking && <span className="subtle">Waiting for an agent…</span>}
        <div className="buttons">
          <Button kind="primary" onClick={() => run(gate, 'OpenAllayCommand')}>Open Allay</Button>
          <Button onClick={() => run(gate, 'LoadCommand')}>Check again</Button>
        </div>
        {p.problem && <span className="legend-error">{p.problem}</span>}
      </div>
    </div>
  );
}

function ago(touched: string) {
  const minutes = Math.floor((Date.now() - new Date(touched).getTime()) / 60000);
  if (minutes < 1) return 'now';
  if (minutes < 60) return `${minutes} min`;
  const hours = Math.floor(minutes / 60);
  if (hours < 24) return `${hours} h`;
  const days = Math.floor(hours / 24);
  return days === 1 ? 'yesterday' : `${days} d`;
}

function useMinutes() {
  const [, tick] = useState(0);
  useEffect(() => {
    const every = setInterval(() => tick((n) => n + 1), 60000);
    return () => clearInterval(every);
  }, []);
}

function Badge({ project, size }: { project: Obj; size: 'badge' | 'mini' }) {
  const version = useMemo(() => Date.now(), [project.p.iconFile, project.p.project]);
  return <div className={`home-${size}`}>{project.p.hasIcon ? <img src={localFile(project.p.iconFile, version)} alt="" /> : <span>{project.p.letter}</span>}</div>;
}

function count(n: number, one: string, many: string) {
  return `${n} ${n === 1 ? one : many}`;
}

function Card({ id, workbench }: { id: number; workbench: Obj }) {
  const project = useObj(id);
  const aside = useObj(project?.p.aside);
  const todos = useObj(aside?.p.todos);
  if (!project) return null;
  const p = project.p;
  const left = todos?.p.left ?? 0;
  return (
    <div className="home-card" role="button" tabIndex={0} onClick={() => run(workbench, 'OpenCommand', { $ref: project.id })} onKeyDown={(e) => { if (e.key === 'Enter') run(workbench, 'OpenCommand', { $ref: project.id }); }}>
      <button className="btn icon home-quick" title="New chat in this project" onClick={(e) => { e.stopPropagation(); run(workbench, 'NewChatInCommand', { $ref: project.id }); focusComposer(); }}><Icon d={glyphs.plus} /></button>
      <div className="home-card-top">
        <Badge project={project} size="badge" />
        <div className="home-card-name">
          <strong>{p.name}</strong>
          <span className="mono subtle footnote">{p.place}{p.branch && ` · ${p.branch}`}</span>
        </div>
      </div>
      <div className="home-card-line">{p.lastLine ?? <span className="subtle">No chats yet.</span>}</div>
      <div className="home-pills">
        {p.working > 0 && <span className="pill home-working"><i className="home-pulse" />{p.working} working</span>}
        {p.waiting > 0 && <span className="pill home-waiting">waiting on you</span>}
        <span className="pill">{count(p.chatCount ?? 0, 'chat', 'chats')}</span>
        {left > 0 && <span className="pill">{count(left, 'todo', 'todos')}</span>}
      </div>
    </div>
  );
}

function Recent({ id, workbench }: { id: number; workbench: Obj }) {
  const recent = useObj(id);
  const project = useObj(recent?.p.project);
  const chat = useObj(recent?.p.chat);
  if (!recent || !project || !chat) return null;
  const c = chat.p;
  return (
    <button className={`home-row ${c.isWorking ? 'working' : ''} ${c.isUnread ? 'unread' : ''}`} onClick={() => run(workbench, 'OpenRecentCommand', { $ref: recent.id })}>
      <Badge project={project} size="mini" />
      <span className="home-row-title">{c.title}</span>
      <span className="home-row-line">{c.isWaiting ? <span className="home-waiting-text">Waiting on you</span> : c.lastLine}</span>
      <span className="home-row-when">{c.isWorking ? 'working' : c.touched && ago(c.touched)}</span>
    </button>
  );
}

function Home({ id, workbench }: { id: any; workbench: Obj }) {
  const home = useObj(id);
  const recent = useList(home?.p.recent);
  const projects = useList(workbench.p.projects);
  useMinutes();
  if (!home) return null;
  const h = home.p;
  const told = [
    count(projects.length, 'project', 'projects'),
    h.working > 0 && count(h.working, 'agent working', 'agents working'),
    h.waiting > 0 && `${h.waiting} waiting on you`,
  ].filter(Boolean).join(' · ');
  return (
    <div className="home">
      <div className="home-inner">
        <header className="home-head">
          <div><h1>Crafter</h1><span className="subtle footnote">{told}</span></div>
          <span className="spacer" />
          <Button title="Ctrl+," onClick={() => run(workbench, 'TogglePreferencesCommand')}>Preferences</Button>
          <Button kind="primary" title="Ctrl+K Ctrl+O" onClick={() => run(workbench, 'AddProjectCommand')}>Add a project</Button>
        </header>
        {workbench.p.problem && <div className="legend-error">{workbench.p.problem}</div>}
        <section className="home-section">
          <span className="group-label">Projects</span>
          <div className="home-grid">
            {projects.map((ref) => <Card key={idOf(ref)} id={idOf(ref)!} workbench={workbench} />)}
            <button className="home-add" onClick={() => run(workbench, 'AddProjectCommand')}><Icon d={glyphs.plus} size={20} /><span>Add a project</span></button>
          </div>
        </section>
        <section className="home-section">
          <span className="group-label">Recent chats</span>
          {h.hasRecent
            ? <div className="home-rows">{recent.map((ref) => <Recent key={idOf(ref)} id={idOf(ref)!} workbench={workbench} />)}</div>
            : <span className="subtle">The chats you talk in show up here.</span>}
        </section>
      </div>
    </div>
  );
}

function Main({ workbench, project }: { workbench: Obj; project: Obj | null }) {
  const chat = useObj(project?.p.chat);
  const aside = useObj(project?.p.aside);
  if (!project) {
    return <main className="main"><Home id={workbench.p.home} workbench={workbench} /></main>;
  }
  const p = project.p;
  return (
    <main className="main">
      {chat && aside?.p.conversation
        ? <Conversation id={aside.p.conversation} />
        : <div className="empty column"><span className="soft">No chat open.</span><span className="subtle footnote">Ctrl+T starts a new one.</span></div>}
      {p.showsOverview && <Overview id={p.overview} project={project} />}
      {p.problem && <div className="main-problem danger">{p.problem}</div>}
    </main>
  );
}

function Crafter({ root }: { root: any }) {
  const workbench = useObj(root.workbench);
  const project = useObj(workbench?.p.project);
  const aside = useObj(project?.p.aside);
  const conversation = useObj(aside?.p.conversation);
  const w = () => obj(root.workbench);
  const talking = () => obj(obj(obj(w()?.p.project)?.p.aside)?.p.conversation);

  useCommands([
    { title: 'New chat', group: 'Chats', keys: 'Ctrl+T', run: () => { run(w(), 'NewChatCommand'); focusComposer(); } },
    { title: 'Write a message', group: 'Chats', keys: 'Ctrl+L', run: focusComposer },
    { title: 'Stop the agent', group: 'Chats', run: () => run(talking(), 'StopCommand') },
    { title: 'Background jobs of the chat', group: 'Chats', keys: 'Ctrl+J', run: () => run(w(), 'ShowJobsCommand') },
    { title: 'Next chat', group: 'Chats', keys: 'Ctrl+Tab', run: () => run(w(), 'NextChatCommand') },
    { title: 'Previous chat', group: 'Chats', keys: 'Ctrl+Shift+Tab', run: () => run(w(), 'PreviousChatCommand') },
    { title: 'Search chats', group: 'Chats', keys: 'Ctrl+F', run: () => { const s = document.getElementById('search') as HTMLInputElement | null; s?.focus(); s?.select(); } },
    { title: 'Home', group: 'Projects', keys: 'Ctrl+H', run: () => run(w(), 'GoHomeCommand') },
    { title: 'Add a project', group: 'Projects', keys: 'Ctrl+K Ctrl+O', run: () => run(w(), 'AddProjectCommand') },
    { title: "Open the project's page", group: 'Projects', run: () => run(w(), 'OpenOverviewCommand') },
    { title: 'Rename the project', group: 'Projects', keys: 'F2', run: () => run(w(), 'RenameCommand') },
    ...Array.from({ length: 9 }, (_, slot) => ({ title: `Go to project ${slot + 1}`, group: 'Projects', keys: `Ctrl+${slot + 1}`, run: () => run(w(), 'GoCommand', slot) })),
    { title: 'Preferences', group: 'Window', keys: 'Ctrl+,', run: () => run(w(), 'TogglePreferencesCommand') },
    { title: 'Show or hide the chats', group: 'Window', keys: 'Ctrl+B', run: () => run(w(), 'ToggleListCommand') },
    { title: 'Show or hide the todos, skills and axioms', group: 'Window', keys: 'Ctrl+Alt+B', run: () => run(w(), 'ToggleAsideCommand') },
  ], [root.workbench]);

  if (!workbench) return null;
  const p = workbench.p;
  return (
    <>
      <TitleBar
        title={p.subtitle ?? p.title}
        subtitle={p.subtitle ? p.title : null}
        icon="/icon.png"
        leading={<IconButton d={glyphs.list} chosen={p.showsList} title="Show or hide the chats · Ctrl+B" onClick={() => run(workbench, 'ToggleListCommand')} />}
        trailing={<IconButton d={glyphs.aside} chosen={p.showsAside} title="Todos, axioms and skills · Ctrl+Alt+B" onClick={() => run(workbench, 'ToggleAsideCommand')} />}
      />
      <div className="workbench">
        <Rail workbench={workbench} />
        {p.showsPreferences ? <Preferences id={p.preferences} /> : (
          <>
            {p.showsList && project && <div className="list-room"><Chats id={project.id} workbench={workbench} /></div>}
            <Main workbench={workbench} project={project} />
            {aside && <Trail id={aside.id} workbench={workbench} />}
          </>
        )}
      </div>
      {conversation && <Stage id={conversation.id} />}
      {conversation && <Viewer id={conversation.p.viewer} />}
      {project && <RemovingChat id={project.id} />}
      <Adding id={p.adding} />
      <Gate id={root.gate} />
    </>
  );
}

boot(Crafter);
