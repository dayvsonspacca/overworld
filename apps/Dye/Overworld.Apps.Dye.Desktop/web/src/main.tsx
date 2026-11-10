import { useEffect, useMemo, useState } from 'react';
import { boot, TitleBar, Sidebar, Section, Button, TextField, useCommands, hex, localFile, pickFile, useObj, useList, run, can, put, obj, ask, type Obj } from '@overworld/kit';
import { ChatBubble } from '@overworld/chat';
import './chat.css';
import './app.css';

const Hex = /^#?[0-9a-f]{6}$/i;

const typed = (colors: string) => (colors ?? '').split(/[\s,]+/).filter((c) => Hex.test(c)).map((c) => (c.startsWith('#') ? c : `#${c}`));

function useCopied(): [string | null, (text: string, shown: string) => void] {
  const [copied, setCopied] = useState<string | null>(null);
  useEffect(() => {
    if (!copied) return;
    const t = setTimeout(() => setCopied(null), 1400);
    return () => clearTimeout(t);
  }, [copied]);
  return [copied, (text, shown) => navigator.clipboard.writeText(text).then(() => setCopied(shown), () => undefined)];
}

function NewCard({ id }: { id: any }) {
  const fresh = useObj(id);
  if (!fresh) return null;
  const n = fresh.p;
  const save = () => run(fresh, 'SaveCommand');
  const shown = typed(n.colors);
  return (
    <div className="new-card">
      <span className="new-head">New palette</span>
      <TextField label="Name" value={n.name} placeholder="Sea" error={n.nameError} disabled={n.isSaving} onInput={(v) => put(fresh, 'Name', v)} onEnter={save} />
      <TextField label="Colors" value={n.colors} placeholder="#003F5C, #58508D" error={n.colorsError} disabled={n.isSaving} onInput={(v) => put(fresh, 'Colors', v)} onEnter={save} />
      <div className="typed">{shown.map((c, i) => <i key={`${c}-${i}`} style={{ background: c }} />)}</div>
      {n.problem && <span className="legend-error">{n.problem}</span>}
      <div className="buttons">
        <Button kind="primary" keys="Enter" disabled={n.isSaving} onClick={save}>{n.isSaving ? 'Saving…' : 'Save'}</Button>
        {(n.name || n.colors) && <Button onClick={() => run(fresh, 'ClearCommand')}>Clear</Button>}
      </div>
    </div>
  );
}

function Opened({ palettes }: { palettes: Obj }) {
  const p = palettes.p;
  const [copied, copy] = useCopied();
  useEffect(() => {
    const key = (e: KeyboardEvent) => {
      if (e.key === 'Escape' && !e.defaultPrevented && !(e.target as HTMLElement).closest('input, textarea')) {
        e.preventDefault();
        run(palettes, 'CloseCommand');
      }
    };
    addEventListener('keydown', key);
    return () => removeEventListener('keydown', key);
  }, []);
  const colors: any[] = p.selected.colors ?? [];
  return (
    <div className="opened">
      <div className="opened-head">
        <Button keys="Esc" onClick={() => run(palettes, 'CloseCommand')}>All palettes</Button>
        <h1>{p.selected.name}</h1>
        <span className="count">{colors.length} colors</span>
        <span className="spacer" />
        <Button onClick={() => copy(colors.map((c) => c.hex).join(', '), 'every color')}>Copy all</Button>
      </div>
      <div className="bands">
        {colors.map((c, i) => (
          <button key={i} className="band" title={`Copy ${c.hex}`} style={{ background: c.hex, color: c.isLight ? '#000' : '#fff' }} onClick={() => copy(c.hex, c.hex)}>
            <b>{c.hex}</b>
            <small>rgb {c.r} {c.g} {c.b}</small>
          </button>
        ))}
      </div>
      <div className={`copied ${copied ? 'on' : ''}`}>{copied && <>Copied {copied}</>}</div>
    </div>
  );
}

function Palettes({ id }: { id: any }) {
  const palettes = useObj(id);
  const all = useList(palettes?.p.palettes);
  if (!palettes) return null;
  const p = palettes.p;
  if (p.selected) return <div className="main"><Opened palettes={palettes} /></div>;
  return (
    <div className="main">
      <div className="gallery">
        {p.problem && <span className="danger">{p.problem}</span>}
        <div className="cards">
          {all.map((palette) => (
            <button key={palette.$id} className="card" onClick={() => put(palettes, 'Selected', palette)}>
              <span className="strip">{(palette.colors ?? []).map((c: any, i: number) => <i key={i} style={{ background: c.hex }} />)}</span>
              <span className="meta"><b>{palette.name}</b><span>{palette.colors?.length ?? 0} colors</span></span>
            </button>
          ))}
          <NewCard id={p.new} />
        </div>
        {all.length === 0 && !p.problem && <span className="legend-hint">Or from a terminal: <code>overworld:dye save --name Sea --colors "#003F5C,#58508D"</code></span>}
      </div>
    </div>
  );
}

function Preview({ look }: { look: any }) {
  const c = (name: string) => hex(look[name]);
  return (
    <div className="preview">
      <div className="mock-app" style={{ background: c('background') }}>
        <div className="mock-bar" style={{ background: c('raised'), borderColor: c('divider'), color: c('text') }}>Crafter</div>
        <div className="mock-body">
          <div className="mock-rail" style={{ background: c('raised'), borderColor: c('divider') }}>
            <span className="label" style={{ color: c('subtle') }}>Projects</span>
            <span className="item" style={{ background: c('selected'), color: c('text') }}>overworld</span>
            <span className="item" style={{ color: c('textSoft') }}>notes</span>
            <span className="item" style={{ color: c('textSoft') }}>site</span>
          </div>
          <div className="mock-content">
            <h3 style={{ color: c('text') }}>Summary</h3>
            <span style={{ color: c('subtle') }}>What the project is, its stack and layout.</span>
            <span className="mock-input" style={{ background: c('background'), borderColor: c('accent'), color: c('text') }}>Search the project…</span>
            <span style={{ color: c('danger') }}>The folder was moved.</span>
            <div className="mock-buttons">
              <span style={{ background: c('accent'), color: c('onAccent') }}>Save</span>
              <span style={{ borderColor: c('divider'), color: c('text') }}>Cancel</span>
            </div>
          </div>
        </div>
      </div>
      <div className="mock-shell" style={{ background: c('background') }}>
        <div className="mock-toast" style={{ background: c('raised'), borderColor: c('divider'), color: c('text') }}>
          Theme applied<small style={{ color: c('subtle') }}>The shell and the apps wear {look.name}.</small>
        </div>
        <div className="mock-taskbar" style={{ borderColor: c('divider'), ['--dot' as any]: c('accent') }}>
          {[0, 1, 2, 3].map((i) => <i key={i} className={i === 0 ? 'on' : ''} style={{ background: c('surface') }} />)}
        </div>
      </div>
    </div>
  );
}

function Themes({ id }: { id: any }) {
  const themes = useObj(id);
  const all = useList(themes?.p.themes);
  if (!themes) return null;
  const p = themes.p;
  const selected = p.selected;
  return (
    <div className="main">
      <Sidebar>
        <Section title="Themes" count={all.length} fill>
          {all.map((look) => (
            <div key={look.$id} className={`row theme-row ${selected?.name === look.name ? 'selected' : ''}`} onClick={() => put(themes, 'Selected', look)}>
              <span className="name">{look.name}</span>
              {look.active && <span className="worn">wearing {look.wearing}</span>}
              <span className="dots"><i style={{ background: hex(look.background) }} /><i style={{ background: hex(look.accent) }} /><i style={{ background: hex(look.text) }} /></span>
            </div>
          ))}
          {all.length === 0 && <div className="row subtle">No themes yet.</div>}
        </Section>
      </Sidebar>
      {!selected ? <div className="nothing">{p.problem}</div> : (
        <div className="page">
          <div className="theme-page">
            <div className="theme-head">
              <h1>{selected.name}</h1>
              {selected.active && <span className="wearing">● Wearing now · {selected.wearing}</span>}
              <span className="spacer" />
              <Button kind="chip" chosen={p.showsDark} onClick={() => run(themes, 'ShowCommand', 'Dark')}>Dark</Button>
              <Button kind="chip" chosen={p.showsLight} onClick={() => run(themes, 'ShowCommand', 'Light')}>Light</Button>
              <Button kind="primary" disabled={!can(themes, 'ApplyCommand')} onClick={() => run(themes, 'ApplyCommand')}>{p.applying}</Button>
            </div>
            {p.problem && <span className="danger">{p.problem}</span>}
            <div className="group">
              <Preview look={selected} />
              <span className="legend-hint">An app and the shell in this theme's colors.</span>
            </div>
            <div className="groups">
              {(p.groups ?? []).map((group: any) => (
                <div className="group" key={group.$id}>
                  <span className="section-title">{group.title}</span>
                  <div className="role-swatches">
                    {group.swatches.map((s: any) => (
                      <div className="role" key={s.$id}>
                        <div className={`fill ${s.isTint ? 'tint' : ''}`}><i style={{ background: hex(s.hex) }} /></div>
                        <div className="meta"><strong>{s.role}</strong><code>{s.hex}</code></div>
                      </div>
                    ))}
                  </div>
                </div>
              ))}
            </div>
            <div className="saving"><span className="legend-hint">New themes come from a terminal or an agent:</span><code>{p.saving}</code></div>
          </div>
        </div>
      )}
    </div>
  );
}

function Avatar({ profile, small }: { profile: Obj; small?: boolean }) {
  const version = useMemo(() => Date.now(), [profile.p.profile]);
  const p = profile.p;
  return <div className={`avatar ${small ? 'small' : ''}`}>{p.hasPicture ? <img src={localFile(p.picture, version)} alt="" /> : p.initial}</div>;
}

function Profile({ id }: { id: any }) {
  const profile = useObj(id);
  if (!profile) return null;
  const p = profile.p;
  const choose = async () => {
    const picked = await pickFile('image/png,image/jpeg,image/webp');
    if (picked) await ask('picture', picked);
  };
  return (
    <div className="page">
      <div className="profile">
        <h1>Profile</h1>
        <div className="picture-row">
          <Avatar profile={profile} />
          <div className="picture-text">
            <h2>Picture</h2>
            <span className="legend-hint">{p.hint}</span>
            <div className="buttons">
              <Button kind="primary" onClick={choose}>Choose picture…</Button>
              {p.hasPicture && <Button kind="danger-ghost" onClick={() => run(profile, 'RemovePictureCommand')}>Remove</Button>}
            </div>
          </div>
        </div>
        {p.problem && <span className="danger">{p.problem}</span>}
        <div className="group">
          <div className="start-menu"><Avatar profile={profile} small /><span>{p.user}</span></div>
          <span className="legend-hint">The start menu with this picture.</span>
        </div>
      </div>
    </div>
  );
}

function Dye({ root }: { root: any }) {
  const dye = useObj(root);
  useCommands([
    { title: 'Palettes', group: 'Window', keys: 'Ctrl+1', run: () => run(obj(root), 'ShowPalettesCommand') },
    { title: 'Themes', group: 'Window', keys: 'Ctrl+2', run: () => run(obj(root), 'ShowThemesCommand') },
    { title: 'Profile', group: 'Window', keys: 'Ctrl+3', run: () => run(obj(root), 'ShowProfileCommand') },
  ], [root]);
  if (!dye) return null;
  const p = dye.p;
  const chips = (
    <div className="chips">
      <Button kind="chip" title="Palettes · Ctrl+1" chosen={p.showsPalettes} onClick={() => run(dye, 'ShowPalettesCommand')}>Palettes</Button>
      <Button kind="chip" title="Themes · Ctrl+2" chosen={p.showsThemes} onClick={() => run(dye, 'ShowThemesCommand')}>Themes</Button>
      <Button kind="chip" title="Profile · Ctrl+3" chosen={p.showsProfile} onClick={() => run(dye, 'ShowProfileCommand')}>Profile</Button>
    </div>
  );
  return (
    <>
      <TitleBar title="Dye" icon="/icon.png" trailing={chips} />
      {p.showsPalettes && <Palettes id={p.palettes} />}
      {p.showsThemes && <Themes id={p.themes} />}
      {p.showsProfile && <Profile id={p.profile} />}
      {p.hasChat && <ChatBubble id={p.chat} />}
    </>
  );
}

boot(Dye);
