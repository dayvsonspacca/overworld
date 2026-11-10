import { memo, useState, type ReactNode } from 'react';
import Markdown, { type Components } from 'react-markdown';
import remarkGfm from 'remark-gfm';
import rehypeHighlight from 'rehype-highlight';
import { Button, CopyButton } from './ui';
import { ask } from './mirror';

function textOf(node: any): string {
  if (!node) return '';
  if (node.type === 'text') return node.value;
  return (node.children ?? []).map(textOf).join('');
}

function Drafted({ source }: { source: string }) {
  const [preview, setPreview] = useState(true);
  return (
    <div className="prose-drafted">
      <div className="prose-drafted-bar">
        <CopyButton text={source} />
        <Button kind="chip" chosen={preview} onClick={() => setPreview(true)}>Preview</Button>
        <Button kind="chip" chosen={!preview} onClick={() => setPreview(false)}>Source</Button>
      </div>
      {preview ? <div className="prose-preview"><Prose text={source} /></div> : <pre className="prose-code"><code>{source}</code></pre>}
    </div>
  );
}

const components: Components = {
  pre({ node, children }) {
    const code = (node as any)?.children?.[0];
    const language = (code?.properties?.className ?? []).find((c: string) => c.startsWith('language-'))?.slice(9);
    const source = textOf(code).replace(/\n$/, '');
    if (language === 'markdown' || language === 'md') return <Drafted source={source} />;
    return (
      <div className="prose-codeblock">
        <pre className="prose-code">{children as ReactNode}</pre>
        <CopyButton text={source} className="prose-copy" />
      </div>
    );
  },
  a({ href, children }) {
    return (
      <a href={href} onClick={(e) => { e.preventDefault(); if (href) ask('open-link', { url: href }); }}>
        {children}
      </a>
    );
  },
};

export const Prose = memo(function Prose({ text, className = '' }: { text: string; className?: string }) {
  return (
    <div className={`prose ${className}`}>
      <Markdown remarkPlugins={[remarkGfm]} rehypePlugins={[[rehypeHighlight, { detect: true, ignoreMissing: true }]]} components={components}>
        {text ?? ''}
      </Markdown>
    </div>
  );
});
