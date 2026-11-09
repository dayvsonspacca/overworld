import { html, boot, TitleBar, IconButton, Icon, Button, Modal, Prose, AllayIcon, glyphs, useCommands, localFile, choose } from '/kit/ui.js';
import { useObj, useList, run, can, put, invoke, obj, idOf, ask } from '/kit/mirror.js';
import { useEffect, useRef, useState, useMemo } from 'preact/hooks';
import { Conversation, Viewer, Stage } from './conversation.js';

const listGlyph = 'M2.5 3.5h11v9h-11zM6.5 3.5v9';
const asideGlyph = 'M2.5 3.5h11v9h-11zM9.5 3.5v9';
const gearGlyph = 'M12.96 8.61l1.57.87-.86 2.09-1.73-.49-.86.86.49 1.73-2.09.86-.87-1.57H7.39l-.87 1.57-2.09-.86.49-1.73-.86-.86-1.73.49-.86-2.09 1.57-.87V7.39l-1.57-.87.86-2.09 1.73.49.86-.86-.49-1.73 2.09-.86.87 1.57h1.22l.87-1.57 2.09.86-.49 1.73.86.86 1.73-.49.86 2.09-1.57.87zM10.2 8a2.2 2.2 0 1 1-4.4 0 2.2 2.2 0 0 1 4.4 0z';

function focusComposer() {
  requestAnimationFrame(() => document.querySelector('.composer-box')?.focus());
}

function RailProject({ id, workbench }) {
  const project = useObj(id);
  const version = useMemo(() => Date.now(), [project?.p.iconFile, project?.p.project]);
  if (!project) return null;
  const p = project.p;
  return html`<div class="rail-slot">
    ${p.isCurrent && html`<i class="rail-mark"></i>`}
    <button class=${`rail-project ${p.isCurrent ? 'current' : ''}`} title=${p.name} onClick=${() => run(workbench, 'OpenCommand', { $ref: project.id })}>
      ${p.hasIcon ? html`<img src=${localFile(p.iconFile, version)} alt="" />` : html`<span>${p.letter}</span>`}
    </button>
  </div>`;
}

function Rail({ workbench }) {
  const projects = useList(workbench.p.projects);
  return html`<nav class="rail">
    <div class="rail-list">
      ${projects.map((ref) => html`<${RailProject} key=${idOf(ref)} id=${idOf(ref)} workbench=${workbench} />`)}
      <button class="rail-project tool" title="Add a project · Ctrl+K Ctrl+O" onClick=${() => run(workbench, 'AddProjectCommand')}><${Icon} d=${glyphs.plus} /></button>
    </div>
    <button class=${`rail-project tool ${workbench.p.showsPreferences ? 'current' : ''}`} title="Preferences · Ctrl+," onClick=${() => run(workbench, 'TogglePreferencesCommand')}><${Icon} d=${gearGlyph} /></button>
  </nav>`;
}

function ChatRow({ id, project, selected }) {
  const chat = useObj(id);
  if (!chat) return null;
  const p = chat.p;
  return html`<div class=${`chat-row ${selected ? 'selected' : ''}`} onClick=${() => put(project, 'Chat', { $ref: chat.id })}>
    <div class="chat-title"><i class=${`state ${p.isWorking ? 'working' : ''} ${p.isUnread ? 'unread' : ''}`}></i><span>${p.title}</span></div>
    ${p.lastLine && html`<div class="chat-line">${p.lastLine}</div>`}
    <button class="btn icon chat-delete" title="Delete the chat" onClick=${(e) => { e.stopPropagation(); run(project, 'AskToRemoveChatCommand', { $ref: chat.id }); }}><${Icon} d=${glyphs.trash} /></button>
  </div>`;
}

function Chats({ id, workbench }) {
  const project = useObj(id);
  const chats = useList(project?.p.chats);
  if (!project) return null;
  const p = project.p;
  const current = idOf(p.chat);
  return html`<section class="chats">
    <header class="chats-head">
      <button class="project-name" title="The project's page" onClick=${() => run(workbench, 'OpenOverviewCommand')}>${p.name}</button>
      <${IconButton} d=${glyphs.plus} title="New chat · Ctrl+T" onClick=${() => { run(workbench, 'NewChatCommand'); focusComposer(); }} />
    </header>
    <input id="search" class="chats-search" placeholder="Search chats" value=${p.query} onInput=${(e) => put(project, 'Query', e.target.value)} />
    <div class="chats-list">
      ${chats.map((ref) => html`<${ChatRow} key=${idOf(ref)} id=${idOf(ref)} project=${project} selected=${idOf(ref) === current} />`)}
      ${!p.hasChats && html`<div class="subtle note">No chats yet.</div>`}
      ${p.findsNothing && html`<div class="subtle note">No chat has those words.</div>`}
    </div>
  </section>`;
}

function Todo({ id }) {
  const todo = useObj(id);
  if (!todo) return null;
  const p = todo.p;
  return html`<div class=${`todo ${p.done ? 'done' : ''}`}>
    <input type="checkbox" checked=${p.done} onChange=${(e) => put(todo, 'Done', e.target.checked)} />
    <span class="mono subtle">${p.label}</span>
    <span class="todo-text">${p.text}</span>
    <button class="btn icon small" title="Remove" onClick=${() => run(todo, 'RemoveCommand')}><${Icon} d=${glyphs.close} /></button>
  </div>`;
}

function Adder({ value, placeholder, onInput, onAdd, disabled }) {
  return html`<input class="adder" value=${value} placeholder=${placeholder} onInput=${(e) => onInput(e.target.value)} onKeyDown=${(e) => e.key === 'Enter' && !disabled && onAdd()} />`;
}

function Todos({ id }) {
  const todos = useObj(id);
  const all = useList(todos?.p.todos);
  if (!todos) return null;
  const p = todos.p;
  return html`<div class="aside-pane">
    <${Adder} value=${p.newTodo} placeholder="Something left to do" onInput=${(v) => put(todos, 'NewTodo', v)} onAdd=${() => run(todos, 'AddCommand')} />
    ${all.map((ref) => html`<${Todo} key=${idOf(ref)} id=${idOf(ref)} />`)}
    ${!p.hasTodos && html`<div class="subtle note">Nothing left to do.</div>`}
    ${p.problem && html`<div class="field-error">${p.problem}</div>`}
  </div>`;
}

function Skills({ id }) {
  const skills = useObj(id);
  const all = useList(skills?.p.skills);
  if (!skills) return null;
  return html`<div class="aside-pane">
    ${all.map((skill) => html`<div class="skill"><strong>${skill.name}</strong><span class="subtle">${skill.description}</span></div>`)}
    ${!skills.p.hasSkills && html`<div class="subtle note">The project has no skills yet.</div>`}
    ${skills.p.problem && html`<div class="field-error">${skills.p.problem}</div>`}
  </div>`;
}

function Axioms({ id }) {
  const axioms = useObj(id);
  const all = useList(axioms?.p.axioms);
  if (!axioms) return null;
  const p = axioms.p;
  return html`<div class="aside-pane">
    <${Adder} value=${p.newAxiom} placeholder="A rule every agent follows" onInput=${(v) => put(axioms, 'NewAxiom', v)} onAdd=${() => run(axioms, 'AddCommand')} />
    ${all.map((axiom) => html`<div class="axiom-row"><span>${axiom}</span><button class="btn icon small" title="Remove" onClick=${() => run(axioms, 'RemoveCommand', axiom)}><${Icon} d=${glyphs.close} /></button></div>`)}
    ${!p.hasAxioms && html`<div class="subtle note">No axioms yet.</div>`}
    ${p.problem && html`<div class="field-error">${p.problem}</div>`}
  </div>`;
}

function Job({ id }) {
  const job = useObj(id);
  if (!job) return null;
  const p = job.p;
  return html`<div class="job">
    <div class="job-head"><span class="pill">${p.kind}</span><span class="job-description">${p.description}</span></div>
    <div class="buttons">
      ${p.prints && html`<${Button} kind="chip" chosen=${p.isOpen} onClick=${() => run(job, 'ToggleCommand')}>Output</${Button}>`}
      ${p.isOpen && html`<${Button} kind="chip" onClick=${() => run(job, 'RefreshCommand')}>Refresh</${Button}>`}
      <${Button} kind="chip" onClick=${() => run(job, 'StopCommand')}>Stop</${Button}>
    </div>
    ${p.isOpen && p.hasOutput && html`<pre class="act-output">${p.output}</pre>`}
  </div>`;
}

function Jobs({ id }) {
  const conversation = useObj(id);
  const jobs = useList(conversation?.p.jobs);
  return html`<div class="aside-pane">
    ${jobs.map((ref) => html`<${Job} key=${idOf(ref)} id=${idOf(ref)} />`)}
    ${jobs.length === 0 && html`<div class="subtle note">No background jobs.</div>`}
  </div>`;
}

function Aside({ id }) {
  const aside = useObj(id);
  const todos = useObj(aside?.p.todos);
  const conversation = useObj(aside?.p.conversation);
  const jobs = useList(conversation?.p.jobs);
  if (!aside) return null;
  const p = aside.p;
  return html`<aside class="aside">
    <div class="tabs">
      <${Button} kind="tab" chosen=${p.showsTodos} onClick=${() => run(aside, 'ShowTodosCommand')}>Todos ${todos?.p.hasLeft && html`<span class="mono">${todos.p.left}</span>`}</${Button}>
      <${Button} kind="tab" chosen=${p.showsSkills} onClick=${() => run(aside, 'ShowSkillsCommand')}>Skills</${Button}>
      <${Button} kind="tab" chosen=${p.showsAxioms} onClick=${() => run(aside, 'ShowAxiomsCommand')}>Axioms</${Button}>
      ${conversation?.p.hasJobs && html`<${Button} kind="tab" title="Ctrl+J" chosen=${p.showsJobs} onClick=${() => run(aside, 'ShowJobsCommand')}>Jobs <span class="mono">${jobs.length}</span></${Button}>`}
    </div>
    ${p.showsTodos && html`<${Todos} id=${p.todos} />`}
    ${p.showsSkills && html`<${Skills} id=${p.skills} />`}
    ${p.showsAxioms && html`<${Axioms} id=${p.axioms} />`}
    ${p.showsJobs && html`<${Jobs} id=${p.conversation} />`}
  </aside>`;
}

function SkillToggle({ id }) {
  const toggle = useObj(id);
  if (!toggle) return null;
  const p = toggle.p;
  return html`<label class="toggle-row">
    <input type="checkbox" checked=${p.gets || p.isDefault} disabled=${!p.canChange} onChange=${(e) => put(toggle, 'Gets', e.target.checked)} />
    <span><strong>${p.name}</strong><small class="subtle">${p.description}</small></span>
  </label>`;
}

function Overview({ id, project }) {
  const overview = useObj(id);
  const skills = useList(overview?.p.skills);
  const nameBox = useRef(null);
  useEffect(() => {
    if (overview?.p.isRenaming) requestAnimationFrame(() => { nameBox.current?.focus(); nameBox.current?.select(); });
  }, [overview?.p.isRenaming]);
  if (!overview || !project) return null;
  const p = overview.p;
  const pick = async () => {
    const file = await choose({ title: "The project's icon", filters: [{ name: 'Pictures', extensions: ['png', 'jpg', 'jpeg', 'gif', 'webp'] }] });
    if (file) invoke(overview, 'SetIconAsync', file);
  };
  return html`<div class="overview">
    <div class="overview-inner">
      <div class="overview-head">
        <div class="overview-icon">${project.p.hasIcon ? html`<img src=${localFile(project.p.iconFile, Date.now())} alt="" />` : html`<span>${project.p.letter}</span>`}</div>
        <div class="overview-name">
          ${p.isRenaming
            ? html`<input ref=${nameBox} class="rename" value=${p.newName} onInput=${(e) => put(overview, 'NewName', e.target.value)} onKeyDown=${(e) => { if (e.key === 'Enter') run(overview, 'SaveNameCommand'); if (e.key === 'Escape') run(overview, 'CancelRenameCommand'); }} />`
            : html`<h1>${project.p.name}</h1>`}
          <span class="mono subtle footnote">${p.place}${p.branch && ` · ${p.branch}`}</span>
        </div>
        <div class="buttons">
          ${p.isRenaming
            ? html`<${Button} kind="primary" disabled=${!can(overview, 'SaveNameCommand')} onClick=${() => run(overview, 'SaveNameCommand')}>Save</${Button}><${Button} onClick=${() => run(overview, 'CancelRenameCommand')}>Cancel</${Button}>`
            : html`<${Button} title="F2" onClick=${() => invoke(overview, 'StartRename')}>Rename</${Button}>`}
          <${Button} title="Pick a picture to be the project's icon" onClick=${pick}>Icon</${Button}>
          ${project.p.hasIcon && html`<${Button} onClick=${() => run(overview, 'RemoveIconCommand')}>No icon</${Button}>`}
        </div>
      </div>
      ${p.problem && html`<div class="field-error">${p.problem}</div>`}
      <section class="overview-section">
        <div class="overview-section-head"><span class="group-label">Summary · .crafter/summary.md</span>
          ${!p.isEditing && html`<${Button} kind="chip" onClick=${() => run(overview, 'EditCommand')}>Edit</${Button}>`}
        </div>
        ${p.isEditing
          ? html`<textarea class="summary-editor" value=${p.draft} onInput=${(e) => put(overview, 'Draft', e.target.value)}></textarea>
              <div class="buttons"><${Button} kind="primary" onClick=${() => run(overview, 'SaveSummaryCommand')}>Save</${Button}><${Button} onClick=${() => run(overview, 'CancelEditCommand')}>Cancel</${Button}></div>`
          : p.hasSummary
            ? html`<div class="prose-preview"><${Prose} text=${p.summary} /></div>`
            : html`<span class="subtle">No summary yet. Write what the project is, its stack, its layout and its commands; its agents read it before every chat.</span>`}
      </section>
      <section class="overview-section">
        <span class="group-label">Skills</span>
        ${skills.map((ref) => html`<${SkillToggle} key=${idOf(ref)} id=${idOf(ref)} />`)}
        ${!p.hasSkills && html`<span class="subtle">The skills library has no skills yet; link one in Preferences.</span>`}
      </section>
      <section class="overview-section">
        <${Button} kind="danger-ghost" onClick=${() => run(overview, 'AskToRemoveCommand')}>Remove project</${Button}>
      </section>
    </div>
    ${p.isRemoving && html`<${Modal} title="Remove the project?" onClose=${() => run(overview, 'KeepProjectCommand')}
      actions=${html`<${Button} onClick=${() => run(overview, 'KeepProjectCommand')}>Keep it</${Button}><${Button} kind="primary" onClick=${() => run(overview, 'RemoveCommand')}>Remove</${Button}>`}>
      <span>${project.p.name} leaves Crafter. Its folder, its .crafter settings and its chats stay where they are.</span>
    </${Modal}>`}
  </div>`;
}

function DefaultSkill({ id }) {
  const skill = useObj(id);
  if (!skill) return null;
  const p = skill.p;
  return html`<label class="toggle-row">
    <input type="checkbox" checked=${p.isDefault} onChange=${(e) => put(skill, 'IsDefault', e.target.checked)} />
    <span><strong>${p.name}</strong><small class="subtle">${p.description}</small></span>
  </label>`;
}

function Preferences({ id }) {
  const preferences = useObj(id);
  const skills = useList(preferences?.p.skills);
  const problems = useList(preferences?.p.problems);
  if (!preferences) return null;
  const p = preferences.p;
  const pickRoot = async () => {
    const folder = await choose({ directory: true, title: 'The projects folder' });
    if (folder) invoke(preferences, 'SetProjectsRootAsync', folder);
  };
  return html`<div class="preferences"><div class="overview-inner">
    <h1>Preferences</h1>
    <section class="overview-section">
      <span class="group-label">Projects folder</span>
      <div class="row-line"><span class="mono">${p.projectsRoot}</span><${Button} onClick=${pickRoot}>Choose</${Button}></div>
      <span class="subtle footnote">Adding a project lists the git repositories found under this folder.</span>
    </section>
    <section class="overview-section">
      <span class="group-label">Skills library</span>
      ${p.isLinked
        ? html`<div class="row-line"><span class="mono">${p.repository}</span><${Button} disabled=${!can(preferences, 'SyncCommand')} onClick=${() => run(preferences, 'SyncCommand')}>Sync</${Button}><${Button} disabled=${!can(preferences, 'UnlinkCommand')} onClick=${() => run(preferences, 'UnlinkCommand')}>Unlink</${Button}></div>`
        : html`<div class="row-line"><input class="mono grow" placeholder="git@github.com:you/skills.git" value=${p.draft} onInput=${(e) => put(preferences, 'Draft', e.target.value)} /><${Button} kind="primary" disabled=${!can(preferences, 'LinkCommand')} onClick=${() => run(preferences, 'LinkCommand')}>Link</${Button}></div>`}
      ${p.isBusy && html`<span class="subtle">Working with the repository…</span>`}
      <span class="subtle footnote">Linking clones the repository; Sync brings it to what the repository holds now.</span>
      ${p.problem && html`<div class="field-error">${p.problem}</div>`}
    </section>
    <section class="overview-section">
      <span class="group-label">Skills every project gets</span>
      ${skills.map((ref) => html`<${DefaultSkill} key=${idOf(ref)} id=${idOf(ref)} />`)}
      ${!p.hasSkills && html`<span class="subtle">Link a library to choose its skills.</span>`}
      ${problems.map((problem) => html`<div class="field-error">${problem}</div>`)}
    </section>
    <span class="subtle footnote">Agents, and the mode, model and effort they start with, are set in Allay, for every app.</span>
  </div></div>`;
}

function Adding({ id }) {
  const adding = useObj(id);
  const repositories = useList(adding?.p.repositories);
  const search = useRef(null);
  useEffect(() => {
    if (adding?.p.isOpen) requestAnimationFrame(() => search.current?.focus());
  }, [adding?.p.isOpen]);
  if (!adding?.p.isOpen) return null;
  const p = adding.p;
  const selected = p.selected?.$id;
  const steer = (e) => {
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
    if (folder) invoke(adding, 'AddAsync', folder);
  };
  return html`<${Modal} title="Add a project" onClose=${() => run(adding, 'CloseCommand')}
    actions=${html`<${Button} className="push-left" onClick=${other}>Another folder…</${Button}><${Button} onClick=${() => run(adding, 'CloseCommand')}>Cancel</${Button}><${Button} kind="primary" disabled=${!can(adding, 'AddCommand')} onClick=${() => run(adding, 'AddCommand')}>Add</${Button}>`}>
    <input ref=${search} placeholder="Search the repositories" value=${p.query} onInput=${(e) => put(adding, 'Query', e.target.value)} onKeyDown=${steer} />
    <span class="subtle footnote">Git repositories under ${p.root}</span>
    <div class="repositories">
      ${repositories.map((r) => html`<div class=${`repository ${r.$id === selected ? 'selected' : ''}`} onClick=${() => put(adding, 'Selected', r)} onDblClick=${() => run(adding, 'AddCommand')}>
        <span class="mono">${r.name}</span><span class="mono subtle footnote">${r.place}</span>
      </div>`)}
      ${p.isSearching && html`<div class="subtle note">Looking for repositories…</div>`}
      ${p.foundNone && html`<div class="subtle note">No repository here that is not open yet.</div>`}
    </div>
    ${p.problem && html`<div class="field-error">${p.problem}</div>`}
  </${Modal}>`;
}

function RemovingChat({ id }) {
  const project = useObj(id);
  const removing = useObj(project?.p.removing);
  if (!project?.p.isRemovingChat || !removing) return null;
  return html`<${Modal} title="Delete the chat?" onClose=${() => run(project, 'KeepChatCommand')}
    actions=${html`<${Button} onClick=${() => run(project, 'KeepChatCommand')}>Keep it</${Button}><${Button} kind="primary" onClick=${() => run(project, 'RemoveChatCommand')}>Delete</${Button}>`}>
    <span>"${removing.p.title}" and everything said in it leave Crafter for good.</span>
  </${Modal}>`;
}

function Gate({ id }) {
  const gate = useObj(id);
  if (!gate?.p.isShut) return null;
  const p = gate.p;
  return html`<div class="gate">
    <div class="gate-card">
      <${AllayIcon} color=${p.color} size=${96} />
      <h2>${p.heading}</h2>
      <p class="soft">${p.message}</p>
      ${p.agent && html`<div class="gate-agent"><${AllayIcon} color=${p.color} size=${20} /><span>${p.agent}</span></div>`}
      ${p.trouble && html`<pre class="act-output">${p.trouble}</pre>`}
      ${p.isChecking && html`<span class="subtle">Waiting for an agent…</span>`}
      <div class="buttons">
        <${Button} kind="primary" onClick=${() => run(gate, 'OpenAllayCommand')}>Open Allay</${Button}>
        <${Button} onClick=${() => run(gate, 'LoadCommand')}>Check again</${Button}>
      </div>
      ${p.problem && html`<span class="field-error">${p.problem}</span>`}
    </div>
  </div>`;
}

function Main({ workbench, project }) {
  const chat = useObj(project?.p.chat);
  const aside = useObj(project?.p.aside);
  if (!project) {
    return html`<main class="main">${workbench.p.problem ? html`<div class="empty danger">${workbench.p.problem}</div>` : html`<div class="empty">Add a project to start.</div>`}</main>`;
  }
  const p = project.p;
  return html`<main class="main">
    ${chat && aside?.p.conversation
      ? html`<${Conversation} id=${idOf(aside.p.conversation)} focus />`
      : html`<div class="empty column"><span class="soft">No chat open.</span><span class="subtle footnote">Ctrl+T starts a new one.</span></div>`}
    ${p.showsOverview && html`<${Overview} id=${p.overview} project=${project} />`}
    ${p.problem && html`<div class="main-problem danger">${p.problem}</div>`}
  </main>`;
}

function Crafter({ root }) {
  const workbench = useObj(root.workbench);
  const project = useObj(workbench?.p.project);
  const aside = useObj(project?.p.aside);
  const w = () => obj(root.workbench);
  const conversation = () => obj(obj(obj(w()?.p.project)?.p.aside)?.p.conversation);

  useCommands([
    { title: 'New chat', group: 'Chats', keys: 'Ctrl+T', run: () => { run(w(), 'NewChatCommand'); focusComposer(); } },
    { title: 'Write a message', group: 'Chats', keys: 'Ctrl+L', run: focusComposer },
    { title: 'Stop the agent', group: 'Chats', run: () => run(conversation(), 'StopCommand') },
    { title: 'Background jobs of the chat', group: 'Chats', keys: 'Ctrl+J', run: () => run(w(), 'ShowJobsCommand') },
    { title: 'Next chat', group: 'Chats', keys: 'Ctrl+Tab', run: () => run(w(), 'NextChatCommand') },
    { title: 'Previous chat', group: 'Chats', keys: 'Ctrl+Shift+Tab', run: () => run(w(), 'PreviousChatCommand') },
    { title: 'Search chats', group: 'Chats', keys: 'Ctrl+F', run: () => { const s = document.getElementById('search'); s?.focus(); s?.select(); } },
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
  const conversationId = aside?.p.conversation;

  return html`
    <${TitleBar} title=${p.title} subtitle=${p.subtitle} icon="/icon.png"
      leading=${html`<${IconButton} d=${listGlyph} chosen=${p.showsList} title="Show or hide the chats · Ctrl+B" onClick=${() => run(workbench, 'ToggleListCommand')} />`}
      trailing=${html`<${IconButton} d=${asideGlyph} chosen=${p.showsAside} title="Show or hide the aside · Ctrl+Alt+B" onClick=${() => run(workbench, 'ToggleAsideCommand')} />`} />
    <div class="workbench">
      <${Rail} workbench=${workbench} />
      ${p.showsPreferences
        ? html`<${Preferences} id=${p.preferences} />`
        : html`
          ${p.showsList && project && html`<div class="list-room"><${Chats} id=${project.id} workbench=${workbench} /></div>`}
          <${Main} workbench=${workbench} project=${project} />
          ${p.showsAside && aside && html`<div class="aside-room"><${Aside} id=${aside.id} /></div>`}
        `}
    </div>
    ${conversationId && html`<${Stage} id=${idOf(conversationId)} />`}
    ${conversationId && html`<${Viewer} id=${obj(conversationId)?.p.viewer} />`}
    ${project && html`<${RemovingChat} id=${project.id} />`}
    <${Adding} id=${p.adding} />
    <${Gate} id=${root.gate} />
  `;
}

boot(Crafter);
