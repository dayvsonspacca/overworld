import { boot, TitleBar, useObj } from '@overworld/kit';
import './app.css';

function Painting({ root }: { root: any }) {
  const painting = useObj(root);
  if (!painting) return null;
  const { title, html, isEmpty, problem } = painting.p;
  return (
    <>
      <TitleBar title="Painting" subtitle={title} icon="/icon.png" />
      {html != null && <iframe className="canvas" sandbox="allow-scripts allow-popups" srcDoc={html} />}
      {isEmpty && <div className="empty">Nothing painted yet.</div>}
      {problem && <div className="empty"><span className="problem">{problem}</span></div>}
    </>
  );
}

boot(Painting);
