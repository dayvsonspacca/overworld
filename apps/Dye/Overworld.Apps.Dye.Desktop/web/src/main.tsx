import { useMemo } from 'react';
import { boot, TitleBar, Sidebar, Section, Button, TextField, useCommands, hex, localFile, pickFile, useObj, useList, run, can, put, obj, ask, type Obj } from '@overworld/kit';
import './app.css';

function Palettes({ id }: { id: any }) {
  const palettes = useObj(id);
  const all = useList(palettes?.p.palettes);
  const fresh = useObj(palettes?.p.new);
  if (!palettes || !fresh) return null;
  const p = palettes.p;
  const n = fresh.p;
  const save = () => run(fresh, 'SaveCommand');
  return (
    <div className="main">
      <Sidebar>
        <Section title="Palettes" count={all.length} fill>
          {all.map((palette) => (
            <div key={palette.$id} className={`row ${p.selected?.name === palette.name ? 'selected' : ''}`} onClick={() => put(palettes, 'Selected', palette)}>{palette.name}</div>
          ))}
          {all.length === 0 && <div className="row subtle">No palettes yet.</div>}
        </Section>
        <Section title="New palette">
          <div className="new">
            <TextField label="Name" value={n.name} placeholder="Sea" hint="Must not be taken by another palette." error={n.nameError} disabled={n.isSaving} onInput={(v) => put(fresh, 'Name', v)} onEnter={save} />
            <TextField label="Colors" value={n.colors} placeholder="#003F5C, #58508D" hint="Hex colors in order, separated by commas." error={n.colorsError} disabled={n.isSaving} onInput={(v) => put(fresh, 'Colors', v)} onEnter={save} />
            <div className="buttons">
              <Button kind="primary" disabled={n.isSaving} onClick={save}>{n.isSaving ? 'Saving…' : 'Save'}</Button>
              <Button onClick={() => run(fresh, 'ClearCommand')}>Clear</Button>
            </div>
            {n.problem && <span className="field-error">{n.problem}</span>}
          </div>
        </Section>
      </Sidebar>
      {all.length > 0 ? (
        <div className="swatches">
          {(p.swatches ?? []).map((s: any) => <div key={s.$id} className="swatch" style={{ background: hex(s.fill), color: s.ink }}><span>{s.label}</span></div>)}
        </div>
      ) : (
        <div className="nothing">
          {p.problem ? <span>{p.problem}</span> : (
            <>
              <strong>No palettes yet</strong>
              <span>Save one on the left, or from a terminal:</span>
              <code>overworld:dye save --name Sea --colors "#003F5C,#58508D"</code>
            </>
          )}
        </div>
      )}
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
              <span className="field-hint">An app and the shell in this theme's colors.</span>
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
            <div className="saving"><span className="field-hint">New themes come from a terminal or an agent:</span><code>{p.saving}</code></div>
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
            <span className="field-hint">{p.hint}</span>
            <div className="buttons">
              <Button kind="primary" onClick={choose}>Choose picture…</Button>
              {p.hasPicture && <Button kind="danger-ghost" onClick={() => run(profile, 'RemovePictureCommand')}>Remove</Button>}
            </div>
          </div>
        </div>
        {p.problem && <span className="danger">{p.problem}</span>}
        <div className="group">
          <div className="start-menu"><Avatar profile={profile} small /><span>{p.user}</span></div>
          <span className="field-hint">The start menu with this picture.</span>
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
    </>
  );
}

boot(Dye);
