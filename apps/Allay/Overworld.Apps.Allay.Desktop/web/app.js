import { html, boot, TitleBar, Sidebar, Section, StatusBar, AllayIcon, IconButton, Button, glyphs, useCommands } from '/kit/ui.js';
import { useObj, useList, run, can, put, obj } from '/kit/mirror.js';

function AgentRow({ id, selected, onPick }) {
  const agent = useObj(id);
  if (!agent) return null;
  const p = agent.p;
  return html`<div class=${`row agent-row ${selected ? 'selected' : ''}`} onClick=${onPick}>
    <${AllayIcon} color=${p.color} size=${28} />
    <div class="text"><div class="name">${p.name}</div><div class="standing-line">${p.standing}</div></div>
    <span class=${`dot ${p.isReady ? 'ready' : ''} ${p.isBlocked ? 'blocked' : ''}`}></span>
  </div>`;
}

function Usage({ id }) {
  const usage = useObj(id);
  const limits = useList(usage?.p.limits);
  if (!usage) return null;
  const p = usage.p;
  return html`<div class="card">
    <div class="card-head">
      <span class="group-label">Usage</span>
      <span class="hint">${p.isMeasuring ? 'Checking…' : p.checked}</span>
      <${IconButton} d=${glyphs.refresh} title="Check the usage again" onClick=${() => run(usage, 'MeasureCommand')} />
    </div>
    ${limits.map((limit) => html`<div class="limit">
      <span class="label">${limit.name}</span>
      <span class=${`used ${limit.close ? 'close' : ''}`}>${limit.used}</span>
      <div class=${`meter ${limit.close ? 'close' : ''}`}><div style=${{ width: `${limit.fill}%` }}></div></div>
      ${limit.resets && html`<span class="resets">${limit.resets}</span>`}
    </div>`)}
    ${p.spending && html`<div class="limit">
      <span class="label">${p.spending.name}</span>
      <span><span class="used">${p.spending.spent}</span> <span class="subtle">${p.spending.cap}</span></span>
      ${p.spending.fill != null && html`<div class="meter"><div style=${{ width: `${p.spending.fill}%` }}></div></div>`}
    </div>`}
    ${p.problem && html`<span class="hint">${p.problem}</span>`}
    ${p.isBare && html`<span class="hint">It reports no limits and nothing spent.</span>`}
  </div>`;
}

function Agent({ id }) {
  const agent = useObj(id);
  const models = useList(agent?.p.models);
  if (!agent) return null;
  const p = agent.p;
  const state = `pill ${p.isReady ? 'ready' : ''} ${p.isBlocked ? 'blocked' : ''}`;
  return html`<div class="agent">
    <div class="agent-head">
      <${AllayIcon} color=${p.color} size=${48} />
      <div class="who"><h1>${p.name}</h1><div class="hint">${p.summary}</div></div>
      ${p.isHired
        ? html`<${Button} kind="danger-ghost" title="Apps stop offering it" disabled=${!can(agent, 'LetGoCommand')} onClick=${() => run(agent, 'LetGoCommand')}>Let go</${Button}>`
        : html`<${Button} kind="primary" title="It must be installed and signed in first" disabled=${!can(agent, 'HireCommand')} onClick=${() => run(agent, 'HireCommand')}>Hire</${Button}>`}
    </div>
    <div class="standing">
      <span class=${state}><span class=${`dot ${p.isReady ? 'ready' : ''} ${p.isBlocked ? 'blocked' : ''}`}></span>${p.state}</span>
      ${p.isReady && html`<span class="detail">${p.detail}</span>`}
    </div>
    ${p.isBlocked && html`<div class="remedy">
      <span>${p.detail}</span>
      ${p.remedy && html`<div class="command">${p.remedy}</div>`}
      <${Button} onClick=${() => run(agent, 'CheckCommand')}>Check again</${Button}>
    </div>`}
    ${p.showsUsage && html`<${Usage} id=${p.usage} />`}
    ${p.isHired && html`<div class="card">
      <span class="group-label">Starts with</span>
      <div class="setup">
        <span class="label">Mode</span>
        <div class="chips">
          ${['Auto', 'Edits', 'Plan'].map((mode) => html`<${Button} kind="chip" chosen=${p.mode === mode} onClick=${() => run(agent, 'PickModeCommand', mode)}>${mode}</${Button}>`)}
        </div>
        <span></span><span class="hint">${p.modeHint}</span>
        <span class="label">Model</span>
        <select value=${p.model?.id} onChange=${(e) => put(agent, 'Model', models.find((m) => m.id === e.target.value))}>
          ${models.map((m) => html`<option value=${m.id}>${m.label}</option>`)}
        </select>
        <span class="label">Effort</span>
        <div class="chips">
          <${Button} kind="chip" chosen=${!p.isMax} onClick=${() => run(agent, 'PickEffortCommand', 'Default')}>Default</${Button}>
          <${Button} kind="chip" chosen=${p.isMax} onClick=${() => run(agent, 'PickEffortCommand', 'Max')}>Max</${Button}>
        </div>
      </div>
      <span class="hint">Every app that runs ${p.name} starts its chats like this; a chat can still change them for itself.</span>
    </div>`}
    ${!p.isHired && html`<span class="hint">Hire it to choose the mode, model and effort it starts with.</span>`}
    ${p.problem && html`<span class="danger">${p.problem}</span>`}
  </div>`;
}

function Agents({ root }) {
  const agents = useObj(root);
  const list = useList(agents?.p.agents);
  useCommands([{ title: 'Check every agent again', group: 'Agents', keys: 'Ctrl+R', run: () => run(obj(root), 'CheckAllCommand') }], [root]);
  if (!agents) return null;
  const p = agents.p;
  const selected = p.selected?.$ref;
  return html`
    <${TitleBar} title="Allay" icon="/icon.png" trailing=${html`<${IconButton} d=${glyphs.refresh} title="Check every agent again · Ctrl+R" onClick=${() => run(agents, 'CheckAllCommand')} />`} />
    <div class="main">
      <${Sidebar}>
        <${Section} title="Agents" count=${list.length} fill>
          ${list.map((ref) => html`<${AgentRow} key=${ref.$ref} id=${ref.$ref} selected=${ref.$ref === selected} onPick=${() => put(agents, 'Selected', ref)} />`)}
        </${Section}>
      </${Sidebar}>
      <div class="page">
        ${p.problem && html`<div class="empty"><span class="danger">${p.problem}</span></div>`}
        ${selected && html`<${Agent} id=${selected} />`}
      </div>
    </div>
    <${StatusBar}><span>${p.headcount}</span><span style="flex:1"></span><span>Every app that runs agents offers the hired ones</span></${StatusBar}>
  `;
}

boot(Agents);
