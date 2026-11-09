import { useEffect, useReducer, useRef } from 'preact/hooks';

const objects = new Map();
const lists = new Map();
const watchers = new Map();
const pending = new Map();
const handlers = new Map();
const rootWatchers = new Set();
let root = null;
let socket = null;
let asked = 0;
let dirty = new Set();
let scheduled = false;
const queue = [];

const key = new URLSearchParams(location.search).get('key') ?? '';

export function connect() {
  socket = new WebSocket(`ws://${location.host}/ws?key=${encodeURIComponent(key)}`);
  socket.onmessage = (message) => apply(JSON.parse(message.data));
  socket.onopen = () => {
    for (const each of queue.splice(0)) socket.send(each);
    send({ t: 'focus', value: document.hasFocus() });
  };
  socket.onclose = () => setTimeout(connect, 500);
}

function send(message) {
  const text = JSON.stringify(message);
  if (socket?.readyState === WebSocket.OPEN) socket.send(text);
  else queue.push(text);
}

function apply(batch) {
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

export const idOf = (ref) => (ref && typeof ref === 'object' && '$ref' in ref ? ref.$ref : ref ?? null);

export const obj = (ref) => objects.get(idOf(ref)) ?? null;

export const list = (ref) => lists.get(idOf(ref)) ?? [];

export const is = (o, type) => !!o && o.types.includes(type);

function watch(id, fn) {
  if (id == null) return () => {};
  if (!watchers.has(id)) watchers.set(id, new Set());
  watchers.get(id).add(fn);
  return () => watchers.get(id)?.delete(fn);
}

function useWatched(id, read) {
  const [, bump] = useReducer((n) => n + 1, 0);
  const seen = useRef(null);
  const value = read(id);
  seen.current = value;
  useEffect(() => {
    const stop = watch(id, bump);
    if (read(id) !== seen.current) bump();
    return stop;
  }, [id]);
  return value;
}

const readObj = (id) => objects.get(id) ?? null;

const readList = (id) => lists.get(id) ?? empty;

const empty = Object.freeze([]);

export function useObj(ref) {
  return useWatched(idOf(ref), readObj);
}

export function useList(ref) {
  return useWatched(idOf(ref), readList);
}

export function useRoot() {
  const [, bump] = useReducer((n) => n + 1, 0);
  const seen = root;
  useEffect(() => {
    rootWatchers.add(bump);
    if (root !== seen) bump();
    return () => rootWatchers.delete(bump);
  }, []);
  return root;
}

export function run(o, cmd, arg) {
  if (!o) return;
  send({ t: 'call', id: o.id, cmd, arg: arg === undefined ? null : wire(arg) });
}

export const can = (o, cmd) => !!o?.c?.[cmd];

const wire = (value) => (value && typeof value === 'object' && '$id' in value ? { $ref: value.$id } : value);

export function put(o, prop, value) {
  if (!o) return;
  const name = prop[0].toLowerCase() + prop.slice(1);
  const fresh = { ...objects.get(o.id), p: { ...objects.get(o.id).p, [name]: value } };
  objects.set(o.id, fresh);
  dirty.add(o.id);
  schedule();
  send({ t: 'set', id: o.id, prop, value: wire(value) });
}

export function invoke(o, method, ...args) {
  return new Promise((resolve) => {
    const n = ++asked;
    pending.set(n, resolve);
    send({ t: 'invoke', n, id: o.id, method, args: args.map(wire) });
  });
}

export function ask(name, arg) {
  return new Promise((resolve) => {
    const n = ++asked;
    pending.set(n, resolve);
    send({ t: 'ask', n, name, arg: arg === undefined ? null : arg });
  });
}

export function on(name, handler) {
  if (!handlers.has(name)) handlers.set(name, new Set());
  handlers.get(name).add(handler);
  return () => handlers.get(name).delete(handler);
}

export const ref = (o) => (o ? { $ref: o.id } : null);

addEventListener('focus', () => send({ t: 'focus', value: true }));
addEventListener('blur', () => send({ t: 'focus', value: false }));

export function log(text) {
  send({ t: 'log', text: String(text) });
}

addEventListener('error', (e) => log(`error: ${e.message} at ${e.filename}:${e.lineno}`));
addEventListener('unhandledrejection', (e) => log(`rejection: ${e.reason?.stack ?? e.reason}`));

if (new URLSearchParams(location.search).has('debug')) {
  let last = '';
  setInterval(() => {
    const all = document.body.innerText.replace(/\s+/g, ' ');
    const text = all.length > 1800 ? `${all.slice(0, 900)} … ${all.slice(-900)}` : all;
    if (text !== last) {
      last = text;
      log(`dom: ${text}`);
    }
  }, 1000);
  setTimeout(async () => {
    try {
      const tauri = globalThis.__TAURI__;
      log(`tauri: window=${typeof tauri?.window} dialog=${typeof tauri?.dialog} focused=${tauri ? await tauri.window.getCurrentWindow().isFocused() : 'none'}`);
    } catch (problem) {
      log(`tauri: ${problem}`);
    }
  }, 2500);
  setTimeout(() => {
    const counts = {};
    for (const element of document.querySelectorAll('[class]')) {
      for (const name of element.classList) counts[name] = (counts[name] ?? 0) + 1;
    }
    log(`classes: ${Object.entries(counts).sort().map(([k, v]) => `${k}=${v}`).join(' ')}`);
  }, 4000);
}
