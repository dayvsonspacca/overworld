import type { ComponentProps } from 'react';
import { Collapsible as Primitive } from 'radix-ui';

export const Collapsible = (props: ComponentProps<typeof Primitive.Root>) => <Primitive.Root data-slot="collapsible" {...props} />;

export const CollapsibleTrigger = (props: ComponentProps<typeof Primitive.CollapsibleTrigger>) => <Primitive.CollapsibleTrigger data-slot="collapsible-trigger" {...props} />;

export const CollapsibleContent = (props: ComponentProps<typeof Primitive.CollapsibleContent>) => <Primitive.CollapsibleContent data-slot="collapsible-content" {...props} />;
