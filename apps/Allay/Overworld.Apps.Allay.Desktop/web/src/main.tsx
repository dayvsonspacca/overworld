import { boot, TitleBar, Sidebar, Section, StatusBar, Ticker, RingGauge, Picker, AllayIcon, IconButton, Button, glyphs, useCommands, useObj, useList, run, can, put, obj, idOf } from '@overworld/kit';
import './app.css';

function AgentRow({ id, selected, onPick }: { id: number; selected: boolean; onPick: () => void }) {
  const agent = useObj(id);
  if (!agent) return null;
  const p = agent.p;
  return (
    <div className={`row agent-row ${selected ? 'selected' : ''}`} onClick={onPick}>
      <AllayIcon color={p.color} size={28} state={p.isChecking ? 'thinking' : 'idle'} />
      <div className="text">
        <div className="name">{p.name}</div>
        <div className="standing-line">{p.standing}</div>
      </div>
      <span className={`dot ${p.isReady ? 'ready' : ''} ${p.isBlocked ? 'blocked' : ''}`} />
    </div>
  );
}

function Usage({ id }: { id: any }) {
  const usage = useObj(id);
  const limits = useList(usage?.p.limits);
  if (!usage) return null;
  const p = usage.p;
  return (
    <div className="card">
      <div className="card-head">
        <span className="group-label">Usage</span>
        <span className="hint">{p.isMeasuring ? 'Checking…' : p.checked}</span>
        <IconButton d={glyphs.refresh} title="Check the usage again" onClick={() => run(usage, 'MeasureCommand')} />
      </div>
      {limits.map((limit) => (
        <div className="limit" key={limit.$id}>
          <span className="label">{limit.name}</span>
          <span className={`used ${limit.close ? 'close' : ''}`}>{limit.used}</span>
          <div className={`meter ${limit.close ? 'close' : ''}`}><div style={{ width: `${limit.fill}%` }} /></div>
          {limit.resets && <span className="resets">{limit.resets}</span>}
        </div>
      ))}
      {p.spending && (
        <div className="limit">
          <span className="label">{p.spending.name}</span>
          <span><span className="used">{p.spending.spent}</span> <span className="subtle">{p.spending.cap}</span></span>
          {p.spending.fill != null && <div className="meter"><div style={{ width: `${p.spending.fill}%` }} /></div>}
        </div>
      )}
      {p.problem && <span className="hint">{p.problem}</span>}
      {p.isBare && <span className="hint">It reports no limits and nothing spent.</span>}
    </div>
  );
}

function Agent({ id }: { id: number }) {
  const agent = useObj(id);
  const models = useList(agent?.p.models);
  if (!agent) return null;
  const p = agent.p;
  const state = `${p.isReady ? 'ready' : ''} ${p.isBlocked ? 'blocked' : ''}`;
  return (
    <div className="agent">
      <div className="agent-head">
        <AllayIcon color={p.color} size={48} state={p.isChecking ? 'thinking' : 'idle'} />
        <div className="who">
          <h1>{p.name}</h1>
          <div className="hint">{p.summary}</div>
        </div>
        {p.isHired ? (
          <Button kind="danger-ghost" title="Apps stop offering it" disabled={!can(agent, 'LetGoCommand')} onClick={() => run(agent, 'LetGoCommand')}>Let go</Button>
        ) : (
          <Button kind="primary" title="It must be installed and signed in first" disabled={!can(agent, 'HireCommand')} onClick={() => run(agent, 'HireCommand')}>Hire</Button>
        )}
      </div>
      <div className="standing">
        <span className={`pill ${state}`}><span className={`dot ${state}`} />{p.state}</span>
        {p.isReady && <span className="detail">{p.detail}</span>}
      </div>
      {p.isBlocked && (
        <div className="remedy">
          <span>{p.detail}</span>
          {p.remedy && <div className="command">{p.remedy}</div>}
          <Button onClick={() => run(agent, 'CheckCommand')}>Check again</Button>
        </div>
      )}
      {p.showsUsage && <Usage id={p.usage} />}
      {p.isHired && (
        <div className="card">
          <span className="group-label">Starts with</span>
          <div className="setup">
            <span className="label">Mode</span>
            <div className="chips">
              {['Auto', 'Edits', 'Plan'].map((mode) => (
                <Button key={mode} kind="chip" chosen={p.mode === mode} onClick={() => run(agent, 'PickModeCommand', mode)}>{mode}</Button>
              ))}
            </div>
            <span />
            <span className="hint">{p.modeHint}</span>
            <span className="label">Model</span>
            <Picker label={p.name} value={p.model} choices={models.map((m) => ({ value: m, label: m.label, description: m.id !== m.label ? m.id : undefined }))} same={(a: any, b: any) => a?.id === b?.id} onPick={(m) => put(agent, 'Model', m)} />
            <span className="label">Effort</span>
            <div className="chips">
              <Button kind="chip" chosen={!p.isMax} onClick={() => run(agent, 'PickEffortCommand', 'Default')}>Default</Button>
              <Button kind="chip" chosen={p.isMax} onClick={() => run(agent, 'PickEffortCommand', 'Max')}>Max</Button>
            </div>
          </div>
          <span className="hint">Every app that runs {p.name} starts its chats like this; a chat can still change them for itself.</span>
        </div>
      )}
      {!p.isHired && <span className="hint">Hire it to choose the mode, model and effort it starts with.</span>}
      {p.problem && <span className="danger">{p.problem}</span>}
    </div>
  );
}

function Status({ id, headcount }: { id: number | null | undefined; headcount: string }) {
  const agent = useObj(id);
  const usage = useObj(agent?.p.showsUsage ? agent.p.usage : null);
  const limits = useList(usage?.p.limits);
  const p = agent?.p;
  const busy = !!p?.isChecking || !!usage?.p.isMeasuring;
  return (
    <StatusBar busy={busy} off={!p?.isReady} state={p ? `${p.name} ${p.state.toLowerCase()}` : headcount}>
      <Ticker text={usage?.p.isMeasuring ? 'Checking the usage…' : headcount} />
      {limits.map((limit) => <span key={limit.$id} title={`${limit.name} · ${limit.resets ?? ''}`}><RingGauge fill={limit.fill} close={limit.close} label={limit.used} note={limit.name} /></span>)}
    </StatusBar>
  );
}

function Agents({ root }: { root: any }) {
  const agents = useObj(root);
  const all = useList(agents?.p.agents);
  useCommands([{ title: 'Check every agent again', group: 'Agents', keys: 'Ctrl+R', run: () => run(obj(root), 'CheckAllCommand') }], [root]);
  if (!agents) return null;
  const p = agents.p;
  const selected = idOf(p.selected);
  return (
    <>
      <TitleBar title="Allay" icon="/icon.png" trailing={<IconButton d={glyphs.refresh} title="Check every agent again · Ctrl+R" onClick={() => run(agents, 'CheckAllCommand')} />} />
      <div className="main">
        <Sidebar>
          <Section title="Agents" count={all.length} fill>
            {all.map((ref) => <AgentRow key={idOf(ref)} id={idOf(ref)!} selected={idOf(ref) === selected} onPick={() => put(agents, 'Selected', ref)} />)}
          </Section>
        </Sidebar>
        <div className="page">
          {p.problem && <div className="empty"><span className="danger">{p.problem}</span></div>}
          {selected != null && <Agent id={selected} />}
        </div>
      </div>
      <Status id={selected} headcount={p.headcount} />
    </>
  );
}

boot(Agents);
