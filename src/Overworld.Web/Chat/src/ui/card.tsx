import type { ComponentProps } from 'react';
import { cn } from './cn';

export function Card({ className, size = 'default', ...props }: ComponentProps<'div'> & { size?: 'default' | 'sm' }) {
  return (
    <div
      data-slot="card"
      data-size={size}
      className={cn(
        'group/card flex flex-col gap-(--card-spacing) overflow-hidden rounded-xl bg-card py-(--card-spacing) text-sm text-card-foreground ring-1 ring-foreground/10 [--card-spacing:--spacing(4)] has-data-[slot=card-footer]:pb-(--card-spacing) data-[size=sm]:[--card-spacing:--spacing(3)]',
        className,
      )}
      {...props}
    />
  );
}

export const CardHeader = ({ className, ...props }: ComponentProps<'div'>) => <div data-slot="card-header" className={cn('grid auto-rows-min items-start gap-1 px-(--card-spacing)', className)} {...props} />;

export const CardTitle = ({ className, ...props }: ComponentProps<'div'>) => <div data-slot="card-title" className={cn('text-base leading-snug font-medium group-data-[size=sm]/card:text-sm', className)} {...props} />;

export const CardDescription = ({ className, ...props }: ComponentProps<'div'>) => <div data-slot="card-description" className={cn('text-sm text-muted-foreground', className)} {...props} />;

export const CardContent = ({ className, ...props }: ComponentProps<'div'>) => <div data-slot="card-content" className={cn('px-(--card-spacing)', className)} {...props} />;

export const CardFooter = ({ className, ...props }: ComponentProps<'div'>) => <div data-slot="card-footer" className={cn('flex items-center px-(--card-spacing)', className)} {...props} />;
