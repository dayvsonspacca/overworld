import { useEffect, useRef, useState } from 'react';
import { boot, TitleBar, Button, useObj, run, can, invoke, useCommands, obj, type Obj } from '@overworld/kit';
import { ChatBubble } from '@overworld/chat';
import './chat.css';
import './app.css';

const previews = new Map<string, string>();

function Preview({ painting, id, className = '' }: { painting: Obj; id: string; className?: string }) {
  const [html, setHtml] = useState(previews.get(id) ?? null);
  const box = useRef<HTMLDivElement>(null);
  useEffect(() => {
    if (html != null) return;
    const watcher = new IntersectionObserver(([entry]) => {
      if (!entry.isIntersecting) return;
      watcher.disconnect();
      invoke<string | null>(painting, 'PreviewAsync', id).then((page) => {
        if (page == null) return;
        previews.set(id, page);
        setHtml(page);
      });
    }, { rootMargin: '200px' });
    watcher.observe(box.current!);
    return () => watcher.disconnect();
  }, [id, html]);
  return (
    <div ref={box} className={`preview ${className}`}>
      {html != null && <iframe tabIndex={-1} sandbox="allow-scripts" scrolling="no" srcDoc={html} loading="lazy" />}
    </div>
  );
}

function when(painted: string) {
  const at = new Date(painted);
  const today = new Date().toDateString() === at.toDateString();
  return today ? at.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' }) : at.toLocaleDateString([], { day: 'numeric', month: 'short' });
}

function Home({ painting }: { painting: Obj }) {
  const shelves: any[] = painting.p.shelves ?? [];
  return (
    <div className="level">
      <div className="shelves">
        {shelves.map((shelf) => (
          <button key={shelf.key} className={`shelf ${shelf.key === 'none' ? 'loose' : ''}`} onClick={() => run(painting, 'EnterCommand', shelf.key)}>
            <span className="mosaic">{shelf.previews.map((id: string) => <Preview key={id} painting={painting} id={id} />)}</span>
            <span className="shelf-meta"><b>{shelf.name}</b><span>{shelf.count} {shelf.count === 1 ? 'design' : 'designs'}</span></span>
          </button>
        ))}
      </div>
    </div>
  );
}

function Group({ painting }: { painting: Obj }) {
  const p = painting.p;
  const designs: any[] = p.designs ?? [];
  return (
    <div className="level">
      <h1>{p.groupName}<span>{designs.length} {designs.length === 1 ? 'design' : 'designs'} · newest first</span></h1>
      <div className="designs">
        {designs.map((canvas) => (
          <button key={canvas.id} className="design" onClick={() => run(painting, 'OpenCommand', canvas.id)}>
            {p.fresh === canvas.id && <span className="fresh">New v{canvas.version}</span>}
            <span className="stack">
              {canvas.versions > 2 && <i className="behind two" />}
              {canvas.versions > 1 && <i className="behind" />}
              <Preview painting={painting} id={canvas.id} key={`${canvas.id}-${canvas.versions}`} />
            </span>
            <span className="design-meta">
              <b>{canvas.title}</b>
              <span>{canvas.versions > 1 ? `${canvas.versions} versions · ` : ''}{when(canvas.painted)}</span>
            </span>
          </button>
        ))}
      </div>
    </div>
  );
}

function Opened({ painting }: { painting: Obj }) {
  const p = painting.p;
  const open = p.open;
  return (
    <div className="opened">
      <iframe className="canvas" sandbox="allow-scripts allow-popups" srcDoc={p.html ?? ''} />
      {open.versions > 1 && (
        <div className="versions">
          <Button disabled={!can(painting, 'PreviousCommand')} onClick={() => run(painting, 'PreviousCommand')}>‹</Button>
          <span className="numbers">
            {Array.from({ length: open.versions }, (_, i) => i + 1).map((n) => (
              <button key={n} className={n === open.version ? 'on' : ''} onClick={() => run(painting, 'VersionCommand', n)}>v{n}</button>
            ))}
          </span>
          <Button disabled={!can(painting, 'NextCommand')} onClick={() => run(painting, 'NextCommand')}>›</Button>
          <span className="painted">{when(open.painted)}</span>
          {!p.isNewest && <Button kind="primary" onClick={() => run(painting, 'NewestCommand')}>Latest</Button>}
        </div>
      )}
    </div>
  );
}

function Painting({ root }: { root: any }) {
  const painting = useObj(root);
  const me = () => obj(root);
  useCommands([
    { title: 'Back', group: 'Painting', keys: 'Esc', run: () => run(me(), 'BackCommand'), when: () => !me()?.p.isHome },
    { title: 'Previous version', group: 'Painting', keys: 'ArrowLeft', run: () => run(me(), 'PreviousCommand'), when: () => can(me(), 'PreviousCommand') },
    { title: 'Next version', group: 'Painting', keys: 'ArrowRight', run: () => run(me(), 'NextCommand'), when: () => can(me(), 'NextCommand') },
  ], [root]);
  if (!painting) return null;
  const p = painting.p;
  const steps = [
    { label: 'Painting', onClick: p.isHome ? undefined : () => run(painting, 'HomeCommand') },
    ...(p.groupName ? [{ label: p.groupName, onClick: p.isOpen ? () => run(painting, 'BackCommand') : undefined }] : []),
    ...(p.isOpen ? [{ label: <>{p.title}{p.open.versions > 1 && <span className="of"> v{p.open.version}/{p.open.versions}</span>}</> }] : []),
  ];
  return (
    <>
      <TitleBar title="Painting" steps={steps} icon="/icon.png" trailing={!p.isHome && <Button keys="Esc" onClick={() => run(painting, 'BackCommand')}>Back</Button>} />
      {p.problem && <div className="empty"><span className="problem">{p.problem}</span></div>}
      {!p.problem && p.isEmpty && <div className="empty">Nothing painted yet.</div>}
      {!p.problem && !p.isEmpty && p.isHome && <Home painting={painting} />}
      {!p.problem && p.isInGroup && <Group painting={painting} />}
      {!p.problem && p.isOpen && <Opened painting={painting} />}
      {p.hasChat && <ChatBubble id={p.chat} />}
    </>
  );
}

boot(Painting);
