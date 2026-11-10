import { useMemo, useSyncExternalStore } from 'react';

export type Ref = { $ref: number };

export type Plain = { $type: string; $id: number; [key: string]: any };

export type Obj = { id: number; type: string; types: string[]; p: Record<string, any>; c: Record<string, boolean> };

type Message =
  | { t: 'obj'; id: number; type: string; types: string[]; props: Record<string, any>; cmds: Record<string, boolean> }
  | { t: 'list'; id: number; items: any[] }
  | { t: 'set'; id: number; prop: string; value: any }
  | { t: 'can'; id: number; cmd: string; value: boolean }
  | { t: 'root'; value: any }
  | { t: 'reply'; n: number; value: any }
  | { t: 'event'; name: string; data: any };

const objects = new Map<number, Obj>();
const lists = new Map<number, any[]>();
const watchers = new Map<number, Set<() => void>>();
const rootWatchers = new Set<() => void>();
const pending = new Map<number, (value: any) => void>();
const handlers = new Map<string, Set<(data: any) => void>>();
const queue: string[] = [];
const empty: any[] = [];
let root: any = null;
let socket: WebSocket | null = null;
let asked = 0;
let dirty = new Set<number>();
let scheduled = false;

export const key = new URLSearchParams(location.search).get('key') ?? '';

export function connect() {
  socket = new WebSocket(`ws://${location.host}/ws?key=${encodeURIComponent(key)}`);
  socket.onmessage = (message) => apply(JSON.parse(message.data));
  socket.onopen = () => {
    for (const each of queue.splice(0)) socket!.send(each);
    send({ t: 'focus', value: document.hasFocus() });
  };
  socket.onclose = () => {
    for (const [n, resolve] of pending) {
      resolve(null);
      pending.delete(n);
    }
    setTimeout(connect, 500);
  };
}

function send(message: object) {
  const text = JSON.stringify(message);
  if (socket?.readyState === WebSocket.OPEN) socket.send(text);
  else queue.push(text);
}

function apply(batch: Message[]) {
  for (const m of batch) {
    switch (m.t) {
      case 'obj':
        objects.set(m.id, { id: m.id, type: m.type, types: m.types, p: m.props, c: m.cmds });
        dirty.add(m.id);
        break;
      case 'list':
        lists.set(m.id, m.items);
        dirty.add(m.id);
        break;
      case 'set': {
        const o = objects.get(m.id);
        if (o) {
          objects.set(m.id, { ...o, p: { ...o.p, [m.prop]: m.value } });
          dirty.add(m.id);
        }
        break;
      }
      case 'can': {
        const o = objects.get(m.id);
        if (o) {
          objects.set(m.id, { ...o, c: { ...o.c, [m.cmd]: m.value } });
          dirty.add(m.id);
        }
        break;
      }
      case 'root':
        root = m.value;
        for (const w of rootWatchers) w();
        break;
      case 'reply':
        pending.get(m.n)?.(m.value);
        pending.delete(m.n);
        break;
      case 'event':
        for (const h of handlers.get(m.name) ?? []) h(m.data);
        break;
    }
  }
  schedule();
}

function schedule() {
  if (scheduled) return;
  scheduled = true;
  requestAnimationFrame(() => {
    scheduled = false;
    const changed = dirty;
    dirty = new Set();
    for (const id of changed) for (const w of watchers.get(id) ?? []) w();
  });
}

export const idOf = (ref: any): number | null =>
  ref && typeof ref === 'object' && '$ref' in ref ? ref.$ref : typeof ref === 'number' ? ref : null;

export const obj = (ref: any): Obj | null => objects.get(idOf(ref) ?? -1) ?? null;

export const list = (ref: any): any[] => lists.get(idOf(ref) ?? -1) ?? empty;

function watch(id: number | null) {
  return (fn: () => void) => {
    if (id == null) return () => {};
    if (!watchers.has(id)) watchers.set(id, new Set());
    watchers.get(id)!.add(fn);
    return () => {
      watchers.get(id)?.delete(fn);
    };
  };
}

export function useObj(ref: any): Obj | null {
  const id = idOf(ref);
  const subscribe = useMemo(() => watch(id), [id]);
  return useSyncExternalStore(subscribe, () => (id == null ? null : objects.get(id) ?? null));
}

export function useList<T = any>(ref: any): T[] {
  const id = idOf(ref);
  const subscribe = useMemo(() => watch(id), [id]);
  return useSyncExternalStore(subscribe, () => (id == null ? empty : lists.get(id) ?? empty));
}

function watchRoot(fn: () => void) {
  rootWatchers.add(fn);
  return () => {
    rootWatchers.delete(fn);
  };
}

export function useRoot(): any {
  return useSyncExternalStore(watchRoot, () => root);
}

const wire = (value: any) => (value && typeof value === 'object' && '$id' in value ? { $ref: value.$id } : value);

export const ref = (o: Obj | null): Ref | null => (o ? { $ref: o.id } : null);

export const can = (o: Obj | null, cmd: string) => !!o?.c?.[cmd];

export function run(o: Obj | null, cmd: string, arg?: any) {
  if (!o) return;
  send({ t: 'call', id: o.id, cmd, arg: arg === undefined ? null : wire(arg) });
}

export function put(o: Obj | null, prop: string, value: any) {
  if (!o) return;
  const current = objects.get(o.id);
  if (current) {
    const name = prop[0].toLowerCase() + prop.slice(1);
    objects.set(o.id, { ...current, p: { ...current.p, [name]: value } });
    dirty.add(o.id);
    schedule();
  }
  send({ t: 'set', id: o.id, prop, value: wire(value) });
}

export function invoke<T = any>(o: Obj | null, method: string, ...args: any[]): Promise<T> {
  return new Promise((resolve) => {
    if (!o) return resolve(null as T);
    const n = ++asked;
    pending.set(n, resolve);
    send({ t: 'invoke', n, id: o.id, method, args: args.map(wire) });
  });
}

export function ask<T = any>(name: string, arg?: any): Promise<T> {
  return new Promise((resolve) => {
    const n = ++asked;
    pending.set(n, resolve);
    send({ t: 'ask', n, name, arg: arg === undefined ? null : arg });
  });
}

export function on(name: string, handler: (data: any) => void) {
  if (!handlers.has(name)) handlers.set(name, new Set());
  handlers.get(name)!.add(handler);
  return () => {
    handlers.get(name)?.delete(handler);
  };
}

export function log(text: string) {
  send({ t: 'log', text: String(text) });
}

addEventListener('focus', () => send({ t: 'focus', value: true }));
addEventListener('blur', () => send({ t: 'focus', value: false }));
addEventListener('error', (e) => log(`error: ${e.message} at ${e.filename}:${e.lineno}`));
addEventListener('unhandledrejection', (e) => log(`rejection: ${e.reason?.stack ?? e.reason}`));
