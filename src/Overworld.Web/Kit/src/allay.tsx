import { useEffect, useState } from 'react';

const tinted = new Map<string, string>();
let allay: Promise<HTMLImageElement> | null = null;

function load() {
  allay ??= new Promise((resolve) => {
    const image = new Image();
    image.onload = () => resolve(image);
    image.src = '/kit/allay.png';
  });
  return allay;
}

function hsv(r: number, g: number, b: number): [number, number, number] {
  const max = Math.max(r, g, b);
  const spread = max - Math.min(r, g, b);
  let hue = spread === 0 ? 0 : max === r ? ((g - b) / spread) % 6 : max === g ? (b - r) / spread + 2 : (r - g) / spread + 4;
  hue = (((hue / 6) % 1) + 1) % 1;
  return [hue, max === 0 ? 0 : spread / max, max];
}

function rgb(h: number, s: number, v: number): [number, number, number] {
  const sector = Math.floor(h * 6);
  const part = h * 6 - sector;
  const low = v * (1 - s);
  const falling = v * (1 - part * s);
  const rising = v * (1 - (1 - part) * s);
  const sectors: [number, number, number][] = [[v, rising, low], [falling, v, low], [low, v, rising], [low, falling, v], [rising, low, v], [v, low, falling]];
  return sectors[((sector % 6) + 6) % 6];
}

async function tint(color: string) {
  const known = tinted.get(color);
  if (known) return known;
  const image = await load();
  const canvas = document.createElement('canvas');
  canvas.width = image.width;
  canvas.height = image.height;
  const context = canvas.getContext('2d')!;
  context.drawImage(image, 0, 0);
  const match = /^#?([0-9a-f]{6})$/i.exec(color);
  if (match) {
    const data = context.getImageData(0, 0, canvas.width, canvas.height);
    const px = data.data;
    let hue = 0, sat = 0, val = 0, n = 0;
    for (let i = 0; i < px.length; i += 4) {
      if (!px[i + 3]) continue;
      const [h, s, v] = hsv(px[i] / 255, px[i + 1] / 255, px[i + 2] / 255);
      if (s > 0.25) { hue += h; sat += s; val += v; n++; }
    }
    if (n) {
      hue /= n; sat /= n; val /= n;
      const target = parseInt(match[1], 16);
      const [th, ts, tv] = hsv(((target >> 16) & 255) / 255, ((target >> 8) & 255) / 255, (target & 255) / 255);
      for (let i = 0; i < px.length; i += 4) {
        if (!px[i + 3]) continue;
        const [h, s, v] = hsv(px[i] / 255, px[i + 1] / 255, px[i + 2] / 255);
        const [r, g, b] = rgb((((h - hue + th) % 1) + 1) % 1, Math.min(1, (s * ts) / sat), Math.min(1, (v * tv) / val));
        px[i] = Math.round(r * 255);
        px[i + 1] = Math.round(g * 255);
        px[i + 2] = Math.round(b * 255);
      }
      context.putImageData(data, 0, 0);
    }
  }
  const url = canvas.toDataURL();
  tinted.set(color, url);
  return url;
}

export function AllayIcon({ color, size = 28, className = '' }: { color?: string | null; size?: number; className?: string }) {
  const [src, setSrc] = useState(tinted.get(color ?? '') ?? null);
  useEffect(() => {
    let live = true;
    tint(color ?? '').then((url) => live && setSrc(url));
    return () => {
      live = false;
    };
  }, [color]);
  return <span className={`allay ${className}`} style={{ width: size, height: size }}>{src && <img src={src} alt="" width={size} height={size} />}</span>;
}
