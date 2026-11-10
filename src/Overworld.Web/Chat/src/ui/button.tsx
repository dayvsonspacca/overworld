import type { ComponentProps } from 'react';
import { cva, type VariantProps } from 'class-variance-authority';
import { Slot } from 'radix-ui';
import { cn } from './cn';

export const buttonVariants = cva(
  "group/button inline-flex shrink-0 cursor-pointer items-center justify-center rounded-lg border border-b-[3px] border-border bg-secondary bg-clip-padding text-sm font-medium whitespace-nowrap text-foreground transition-[transform,border-width,background-color,color] duration-100 outline-none select-none focus-visible:border-[var(--accent-deep)] focus-visible:ring-3 focus-visible:ring-ring/30 active:not-aria-[haspopup]:translate-y-[2px] active:not-aria-[haspopup]:border-b disabled:pointer-events-none disabled:opacity-40 [&_svg]:pointer-events-none [&_svg]:shrink-0 [&_svg:not([class*='size-'])]:size-4",
  {
    variants: {
      variant: {
        default: 'border-[var(--accent-deep)] bg-brand text-[var(--on-accent)] hover:bg-[var(--accent-hover)]',
        outline: 'hover:bg-border/40 aria-expanded:border-[var(--accent-deep)]',
        secondary: 'hover:bg-border/40 aria-expanded:border-[var(--accent-deep)]',
        ghost: 'border-transparent border-b bg-transparent text-muted-foreground hover:bg-muted hover:text-foreground active:not-aria-[haspopup]:translate-y-0 aria-expanded:bg-muted aria-expanded:text-foreground',
        destructive: 'border-[color-mix(in_srgb,var(--danger)_45%,var(--border))] text-destructive hover:bg-[color-mix(in_srgb,var(--danger)_12%,var(--surface))]',
        link: 'border-transparent bg-transparent text-primary underline-offset-4 hover:underline active:not-aria-[haspopup]:translate-y-0',
      },
      size: {
        default: 'h-8 gap-1.5 px-3',
        xs: "h-6 gap-1 border-b-2 px-2 text-xs [&_svg:not([class*='size-'])]:size-3",
        sm: "h-7 gap-1 px-2.5 text-[0.8rem] [&_svg:not([class*='size-'])]:size-3.5",
        lg: 'h-9 gap-1.5 px-4',
        icon: 'size-8',
        'icon-xs': "size-6 border-b-2 [&_svg:not([class*='size-'])]:size-3",
        'icon-sm': 'size-7',
        'icon-lg': 'size-9',
      },
    },
    defaultVariants: { variant: 'default', size: 'default' },
  },
);

export function Button({ className, variant = 'default', size = 'default', asChild = false, ...props }: ComponentProps<'button'> & VariantProps<typeof buttonVariants> & { asChild?: boolean }) {
  const Comp = asChild ? Slot.Root : 'button';
  return <Comp data-slot="button" className={cn(buttonVariants({ variant, size, className }))} {...props} />;
}
