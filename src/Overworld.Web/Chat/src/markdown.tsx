import { memo, useEffect, useState, type ComponentProps } from 'react';
import ReactMarkdown, { type Components } from 'react-markdown';
import remarkGfm from 'remark-gfm';
import { Check, Copy } from 'lucide-react';
import { ask } from '@overworld/kit';
import { Button } from './ui/button';
import { cn } from './ui/cn';

const CollapseAfter = 14;

export function CopyButton({ text, className, label = 'Copy' }: { text: string; className?: string; label?: string }) {
  const [copied, setCopied] = useState(false);
  useEffect(() => {
    if (!copied) return;
    const timer = setTimeout(() => setCopied(false), 1500);
    return () => clearTimeout(timer);
  }, [copied]);
  return (
    <Button
      variant="ghost"
      size="icon-xs"
      className={className}
      title={label}
      aria-label={label}
      onClick={async () => {
        await navigator.clipboard.writeText(text);
        setCopied(true);
      }}
    >
      {copied ? <Check className="text-added" /> : <Copy />}
    </Button>
  );
}

export function CodeBlock({ code, lang }: { code: string; lang?: string }) {
  const lines = code.split('\n').length;
  const long = lines > CollapseAfter;
  const [expanded, setExpanded] = useState(!long);
  return (
    <div className="my-2 overflow-hidden rounded-md border bg-muted/40">
      <div className="flex items-center justify-between border-b px-2 py-1 text-xs text-muted-foreground">
        <span className="font-mono">{lang || 'code'} · {lines} {lines === 1 ? 'line' : 'lines'}</span>
        <div className="flex items-center gap-1">
          {long && (
            <Button variant="ghost" size="xs" onClick={() => setExpanded((e) => !e)}>
              {expanded ? 'Collapse' : 'Expand'}
            </Button>
          )}
          <CopyButton text={code} />
        </div>
      </div>
      <pre className={cn('overflow-x-auto p-3 font-mono text-xs leading-relaxed', !expanded && 'max-h-48 overflow-y-hidden')}>
        <code>{code}</code>
      </pre>
    </div>
  );
}

const open = (href?: string) => href && ask('open-link', { url: href });

const components: Components = {
  pre: ({ children }) => <>{children}</>,
  code: ({ className, children }) => {
    const text = String(children).replace(/\n$/, '');
    const lang = /language-([\w+-]+)/.exec(className ?? '')?.[1];
    if (lang || text.includes('\n')) return <CodeBlock code={text} lang={lang} />;
    return <code className="rounded bg-muted px-1 py-0.5 font-mono text-[0.85em] break-words">{text}</code>;
  },
  p: ({ children }) => <p className="my-2 leading-relaxed first:mt-0 last:mb-0">{children}</p>,
  ul: ({ children }) => <ul className="my-2 list-disc space-y-1 pl-5">{children}</ul>,
  ol: ({ children }) => <ol className="my-2 list-decimal space-y-1 pl-5">{children}</ol>,
  h1: ({ children }) => <h3 className="mt-4 mb-2 text-base font-semibold">{children}</h3>,
  h2: ({ children }) => <h3 className="mt-4 mb-2 text-base font-semibold">{children}</h3>,
  h3: ({ children }) => <h4 className="mt-3 mb-1.5 font-semibold">{children}</h4>,
  a: ({ children, href }) => (
    <a href={href} onClick={(e) => { e.preventDefault(); open(href); }} className="cursor-pointer break-all text-brand underline underline-offset-2">
      {children}
    </a>
  ),
  blockquote: ({ children }) => <blockquote className="my-2 border-l-2 pl-3 text-muted-foreground">{children}</blockquote>,
  hr: () => <hr className="my-4" />,
  table: ({ children }) => (
    <div className="my-2 overflow-x-auto rounded-md border">
      <table className="w-full border-collapse text-xs">{children}</table>
    </div>
  ),
  th: ({ children }) => <th className="border-b bg-muted/50 px-2 py-1.5 text-left font-medium">{children}</th>,
  td: ({ children }) => <td className="border-b px-2 py-1.5 align-top">{children}</td>,
};

const remarkPlugins = [remarkGfm];

export const Markdown = memo(function Markdown({ text, className }: { text: string } & ComponentProps<'div'>) {
  return (
    <div className={cn('text-sm break-words', className)}>
      <ReactMarkdown remarkPlugins={remarkPlugins} components={components}>{text}</ReactMarkdown>
    </div>
  );
});

function blocksOf(text: string): string[] {
  const lines = text.split('\n');
  const out: string[] = [];
  let start = 0;
  let fence = '';
  for (let i = 0; i < lines.length; i++) {
    const line = lines[i];
    const mark = /^ {0,3}(`{3,}|~{3,})/.exec(line)?.[1];
    if (mark) {
      if (!fence) fence = mark;
      else if (mark[0] === fence[0] && mark.length >= fence.length) fence = '';
      continue;
    }
    if (fence || line.trim() || i === start) continue;
    const next = lines[i + 1];
    if (next === undefined || !/^\S/.test(next)) continue;
    out.push(lines.slice(start, i).join('\n'));
    start = i + 1;
  }
  out.push(lines.slice(start).join('\n'));
  return out;
}

export function StreamingMarkdown({ text, className }: { text: string; className?: string }) {
  return (
    <div className={cn('space-y-2', className)}>
      {blocksOf(text).map((block, i) => <Markdown key={i} text={block} />)}
    </div>
  );
}
