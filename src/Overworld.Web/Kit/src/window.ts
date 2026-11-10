import { key } from './mirror';

type Tauri = {
  window: { getCurrentWindow(): any };
  dialog?: { open(options: object): Promise<string | string[] | null> };
};

const tauri = () => (globalThis as any).__TAURI__ as Tauri | undefined;

const current = () => tauri()?.window.getCurrentWindow();

export const windowControls = {
  minimize: () => current()?.minimize(),
  maximize: () => current()?.toggleMaximize(),
  close: () => (current() ? current().close() : window.close()),
  drag: () => current()?.startDragging(),
  focus: async () => {
    await current()?.unminimize();
    await current()?.setFocus();
  },
};

export type Choosing = { directory?: boolean; multiple?: boolean; title?: string; filters?: { name: string; extensions: string[] }[] };

export async function choose(options: Choosing = {}): Promise<string | string[] | null> {
  const dialog = tauri()?.dialog;
  if (!dialog) {
    const typed = prompt(options.title ?? (options.directory ? 'Folder path' : 'File path'));
    return typed ? (options.multiple ? [typed] : typed) : null;
  }
  return dialog.open(options);
}

export const localFile = (path: string | null | undefined, version: string | number = '') =>
  path ? `/file?key=${encodeURIComponent(key)}&path=${encodeURIComponent(path)}&v=${version}` : undefined;

export const blob = (value: any) =>
  value && typeof value === 'object' && '$blob' in value ? `/blob?key=${encodeURIComponent(key)}&id=${value.$blob}` : undefined;

export function hex(color: string | null | undefined) {
  if (!color) return 'transparent';
  const digits = color.replace('#', '');
  return digits.length === 8 ? `#${digits.slice(2)}${digits.slice(0, 2)}` : `#${digits}`;
}

export function readFile(file: File): Promise<string> {
  return new Promise((resolve) => {
    const reader = new FileReader();
    reader.onload = () => resolve(String(reader.result).split(',')[1] ?? '');
    reader.readAsDataURL(file);
  });
}

export function pickFile(accept?: string): Promise<{ name: string; data: string } | null> {
  return new Promise((resolve) => {
    const input = document.createElement('input');
    input.type = 'file';
    input.accept = accept ?? '';
    input.onchange = async () => {
      const file = input.files?.[0];
      resolve(file ? { name: file.name, data: await readFile(file) } : null);
    };
    input.click();
  });
}
