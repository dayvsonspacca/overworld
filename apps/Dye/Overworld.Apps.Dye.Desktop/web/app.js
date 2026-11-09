import { html, boot, TitleBar, Sidebar, Section, Button, TextField, useCommands, hex, localFile, pickFile } from '/kit/ui.js';
import { useObj, useList, run, can, put, obj, ask } from '/kit/mirror.js';
import { useMemo } from 'preact/hooks';

function Palettes({ id }) {
  const palettes = useObj(id);
  const all = useList(palettes?.p.palettes);
  const fresh = useObj(palettes?.p.new);
  if (!palettes || !fresh) return null;
  const p = palettes.p;
  const n = fresh.p;
  const save = () => run(fresh, 'SaveCommand');
  return html`<div class="main">
    <${Sidebar}>
      <${Section} title="Palettes" count=${all.length} fill>
        ${all.map((palette) => html`<div class=${`row ${p.selected?.name === palette.name ? 'selected' : ''}`} onClick=${() => put(palettes, 'Selected', palette)}>${palette.name}</div>`)}
        ${all.length === 0 && html`<div class="row subtle">No palettes yet.</div>`}
      </${Section}>
      <${Section} title="New palette">
        <div class="new">
          <${TextField} label="Name" value=${n.name} placeholder="Sea" hint="Must not be taken by another palette." error=${n.nameError} disabled=${n.isSaving} onInput=${(v) => put(fresh, 'Name', v)} onEnter=${save} />
          <${TextField} label="Colors" value=${n.colors} placeholder="#003F5C, #58508D" hint="Hex colors in order, separated by commas." error=${n.colorsError} disabled=${n.isSaving} onInput=${(v) => put(fresh, 'Colors', v)} onEnter=${save} />
          <div class="buttons">
            <${Button} kind="primary" disabled=${n.isSaving} onClick=${save}>${n.isSaving ? 'Saving…' : 'Save'}</${Button}>
            <${Button} onClick=${() => run(fresh, 'ClearCommand')}>Clear</${Button}>
          </div>
          ${n.problem && html`<span class="field-error">${n.problem}</span>`}
        </div>
      </${Section}>
    </${Sidebar}>
    ${all.length > 0
      ? html`<div class="swatches">${(p.swatches ?? []).map((s) => html`<div class="swatch" style=${{ background: hex(s.fill), color: s.ink }}><span>${s.label}</span></div>`)}</div>`
      : html`<div class="nothing">
          ${p.problem ? html`<span>${p.problem}</span>` : html`<strong>No palettes yet</strong><span>Save one on the left, or from a terminal:</span><code>overworld:dye save --name Sea --colors "#003F5C,#58508D"</code>`}
        </div>`}
  </div>`;
}

function Preview({ look }) {
  const c = (name) => hex(look[name]);
  return html`<div class="preview">
    <div class="mock-app" style=${{ background: c('background') }}>
      <div class="mock-bar" style=${{ background: c('raised'), borderColor: c('divider'), color: c('text') }}>Crafter</div>
      <div class="mock-body">
        <div class="mock-rail" style=${{ background: c('raised'), borderColor: c('divider') }}>
          <span class="label" style=${{ color: c('subtle') }}>Projects</span>
          <span class="item" style=${{ background: c('selected'), color: c('text') }}>overworld</span>
          <span class="item" style=${{ color: c('textSoft') }}>notes</span>
          <span class="item" style=${{ color: c('textSoft') }}>site</span>
        </div>
        <div class="mock-content">
          <h3 style=${{ color: c('text') }}>Summary</h3>
          <span style=${{ color: c('subtle') }}>What the project is, its stack and layout.</span>
          <span class="mock-input" style=${{ background: c('background'), borderColor: c('accent'), color: c('text') }}>Search the project…</span>
          <span style=${{ color: c('danger') }}>The folder was moved.</span>
          <div class="mock-buttons">
            <span style=${{ background: c('accent'), color: c('onAccent') }}>Save</span>
            <span style=${{ borderColor: c('divider'), color: c('text') }}>Cancel</span>
          </div>
        </div>
      </div>
    </div>
    <div class="mock-shell" style=${{ background: c('background') }}>
      <div class="mock-toast" style=${{ background: c('raised'), borderColor: c('divider'), color: c('text') }}>Theme applied<small style=${{ color: c('subtle') }}>The shell and the apps wear ${look.name}.</small></div>
      <div class="mock-taskbar" style=${{ borderColor: c('divider'), '--dot': c('accent') }}>
        ${[0, 1, 2, 3].map((i) => html`<i class=${i === 0 ? 'on' : ''} style=${{ background: c('surface') }}></i>`)}
      </div>
    </div>
  </div>`;
}

function Themes({ id }) {
  const themes = useObj(id);
  const all = useList(themes?.p.themes);
  if (!themes) return null;
  const p = themes.p;
  const selected = p.selected;
  return html`<div class="main">
    <${Sidebar}>
      <${Section} title="Themes" count=${all.length} fill>
        ${all.map((look) => html`<div class=${`row theme-row ${selected?.name === look.name ? 'selected' : ''}`} onClick=${() => put(themes, 'Selected', look)}>
          <span class="name">${look.name}</span>
          ${look.active && html`<span class="worn">wearing ${look.wearing}</span>`}
          <span class="dots"><i style=${{ background: hex(look.background) }}></i><i style=${{ background: hex(look.accent) }}></i><i style=${{ background: hex(look.text) }}></i></span>
        </div>`)}
        ${all.length === 0 && html`<div class="row subtle">No themes yet.</div>`}
      </${Section}>
    </${Sidebar}>
    ${!selected
      ? html`<div class="nothing">${p.problem}</div>`
      : html`<div class="page"><div class="theme-page">
          <div class="theme-head">
            <h1>${selected.name}</h1>
            ${selected.active && html`<span class="wearing">● Wearing now · ${selected.wearing}</span>`}
            <span class="spacer"></span>
            <${Button} kind="chip" chosen=${p.showsDark} onClick=${() => run(themes, 'ShowCommand', 'Dark')}>Dark</${Button}>
            <${Button} kind="chip" chosen=${p.showsLight} onClick=${() => run(themes, 'ShowCommand', 'Light')}>Light</${Button}>
            <${Button} kind="primary" disabled=${!can(themes, 'ApplyCommand')} onClick=${() => run(themes, 'ApplyCommand')}>${p.applying}</${Button}>
          </div>
          ${p.problem && html`<span class="danger">${p.problem}</span>`}
          <div class="group">
            <${Preview} look=${selected} />
            <span class="field-hint">An app and the shell in this theme's colors.</span>
          </div>
          <div class="groups">
            ${(p.groups ?? []).map((group) => html`<div class="group">
              <span class="section-title">${group.title}</span>
              <div class="role-swatches">
                ${group.swatches.map((s) => html`<div class="role">
                  <div class=${`fill ${s.isTint ? 'tint' : ''}`}><i style=${{ background: hex(s.hex) }}></i></div>
                  <div class="meta"><strong>${s.role}</strong><code>${s.hex}</code></div>
                </div>`)}
              </div>
            </div>`)}
          </div>
          <div class="saving"><span class="field-hint">New themes come from a terminal or an agent:</span><code>${p.saving}</code></div>
        </div></div>`}
  </div>`;
}

function Avatar({ profile, small }) {
  const version = useMemo(() => Date.now(), [profile.p.profile]);
  const p = profile.p;
  return html`<div class=${`avatar ${small ? 'small' : ''}`}>${p.hasPicture ? html`<img src=${localFile(p.picture, version)} alt="" />` : p.initial}</div>`;
}

function Profile({ id }) {
  const profile = useObj(id);
  if (!profile) return null;
  const p = profile.p;
  const choose = async () => {
    const picked = await pickFile('image/png,image/jpeg,image/webp');
    if (picked) await ask('picture', picked);
  };
  return html`<div class="page"><div class="profile">
    <h1>Profile</h1>
    <div class="picture-row">
      <${Avatar} profile=${profile} />
      <div class="picture-text">
        <h2>Picture</h2>
        <span class="field-hint">${p.hint}</span>
        <div class="buttons">
          <${Button} kind="primary" onClick=${choose}>Choose picture…</${Button}>
          ${p.hasPicture && html`<${Button} kind="danger-ghost" onClick=${() => run(profile, 'RemovePictureCommand')}>Remove</${Button}>`}
        </div>
      </div>
    </div>
    ${p.problem && html`<span class="danger">${p.problem}</span>`}
    <div class="group">
      <div class="start-menu"><${Avatar} profile=${profile} small /><span>${p.user}</span></div>
      <span class="field-hint">The start menu with this picture.</span>
    </div>
  </div></div>`;
}

function Dye({ root }) {
  const dye = useObj(root);
  useCommands([
    { title: 'Palettes', group: 'Window', keys: 'Ctrl+1', run: () => run(obj(root), 'ShowPalettesCommand') },
    { title: 'Themes', group: 'Window', keys: 'Ctrl+2', run: () => run(obj(root), 'ShowThemesCommand') },
    { title: 'Profile', group: 'Window', keys: 'Ctrl+3', run: () => run(obj(root), 'ShowProfileCommand') },
  ], [root]);
  if (!dye) return null;
  const p = dye.p;
  const chips = html`<div class="chips">
    <${Button} kind="chip" title="Palettes · Ctrl+1" chosen=${p.showsPalettes} onClick=${() => run(dye, 'ShowPalettesCommand')}>Palettes</${Button}>
    <${Button} kind="chip" title="Themes · Ctrl+2" chosen=${p.showsThemes} onClick=${() => run(dye, 'ShowThemesCommand')}>Themes</${Button}>
    <${Button} kind="chip" title="Profile · Ctrl+3" chosen=${p.showsProfile} onClick=${() => run(dye, 'ShowProfileCommand')}>Profile</${Button}>
  </div>`;
  return html`
    <${TitleBar} title="Dye" icon="/icon.png" trailing=${chips} />
    ${p.showsPalettes && html`<${Palettes} id=${p.palettes} />`}
    ${p.showsThemes && html`<${Themes} id=${p.themes} />`}
    ${p.showsProfile && html`<${Profile} id=${p.profile} />`}
  `;
}

boot(Dye);
