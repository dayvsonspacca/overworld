import { html, boot, TitleBar } from '/kit/ui.js';
import { useObj } from '/kit/mirror.js';

function Painting({ root }) {
  const painting = useObj(root);
  if (!painting) return null;
  const { title, html: page, isEmpty, problem } = painting.p;
  return html`
    <${TitleBar} title="Painting" subtitle=${title} icon="/icon.png" />
    ${page != null && html`<iframe class="canvas" sandbox="allow-scripts allow-popups" srcdoc=${page}></iframe>`}
    ${isEmpty && html`<div class="empty">Nothing painted yet.</div>`}
    ${problem && html`<div class="empty"><span class="problem">${problem}</span></div>`}
  `;
}

boot(Painting);
