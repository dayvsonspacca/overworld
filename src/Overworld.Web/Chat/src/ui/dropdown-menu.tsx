import type { ComponentProps } from 'react';
import { DropdownMenu as Primitive } from 'radix-ui';
import { CheckIcon } from 'lucide-react';
import { cn } from './cn';

export const DropdownMenu = (props: ComponentProps<typeof Primitive.Root>) => <Primitive.Root data-slot="dropdown-menu" {...props} />;

export const DropdownMenuTrigger = (props: ComponentProps<typeof Primitive.Trigger>) => <Primitive.Trigger data-slot="dropdown-menu-trigger" {...props} />;

export function DropdownMenuContent({ className, align = 'start', sideOffset = 4, ...props }: ComponentProps<typeof Primitive.Content>) {
  return (
    <Primitive.Portal>
      <Primitive.Content
        data-slot="dropdown-menu-content"
        sideOffset={sideOffset}
        align={align}
        className={cn(
          'z-50 max-h-(--radix-dropdown-menu-content-available-height) min-w-32 origin-(--radix-dropdown-menu-content-transform-origin) overflow-x-hidden overflow-y-auto rounded-xl bg-popover p-1 text-popover-foreground shadow-md ring-1 ring-foreground/10 duration-100 data-open:animate-in data-open:fade-in-0 data-open:zoom-in-95 data-closed:animate-out data-closed:fade-out-0 data-closed:zoom-out-95',
          className,
        )}
        {...props}
      />
    </Primitive.Portal>
  );
}

export function DropdownMenuItem({ className, ...props }: ComponentProps<typeof Primitive.Item>) {
  return (
    <Primitive.Item
      data-slot="dropdown-menu-item"
      className={cn(
        "relative flex cursor-pointer items-center gap-1.5 rounded-lg px-2 py-1.5 text-sm outline-hidden select-none focus:bg-accent focus:text-accent-foreground data-disabled:pointer-events-none data-disabled:opacity-50 [&_svg]:pointer-events-none [&_svg]:shrink-0 [&_svg:not([class*='size-'])]:size-4",
        className,
      )}
      {...props}
    />
  );
}

export const DropdownMenuLabel = ({ className, ...props }: ComponentProps<typeof Primitive.Label>) => <Primitive.Label data-slot="dropdown-menu-label" className={cn('px-2 py-1.5 text-xs font-medium text-muted-foreground', className)} {...props} />;

export const DropdownMenuSeparator = ({ className, ...props }: ComponentProps<typeof Primitive.Separator>) => <Primitive.Separator data-slot="dropdown-menu-separator" className={cn('-mx-1 my-1 h-px bg-border', className)} {...props} />;

export const DropdownMenuRadioGroup = (props: ComponentProps<typeof Primitive.RadioGroup>) => <Primitive.RadioGroup data-slot="dropdown-menu-radio-group" {...props} />;

export function DropdownMenuRadioItem({ className, children, ...props }: ComponentProps<typeof Primitive.RadioItem>) {
  return (
    <Primitive.RadioItem
      data-slot="dropdown-menu-radio-item"
      className={cn('relative flex cursor-pointer items-center gap-1.5 rounded-lg py-1.5 pr-8 pl-2 text-sm outline-hidden select-none focus:bg-accent focus:text-accent-foreground data-disabled:pointer-events-none data-disabled:opacity-50', className)}
      {...props}
    >
      <span className="pointer-events-none absolute right-2 flex items-center justify-center">
        <Primitive.ItemIndicator><CheckIcon className="size-4" /></Primitive.ItemIndicator>
      </span>
      {children}
    </Primitive.RadioItem>
  );
}
