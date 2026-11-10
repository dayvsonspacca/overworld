import * as Dialog from '@radix-ui/react-dialog';
import type { ReactNode } from 'react';

export function Modal({ title, onClose, actions, wide, children }: { title?: string; onClose?: () => void; actions?: ReactNode; wide?: boolean; children: ReactNode }) {
  return (
    <Dialog.Root open onOpenChange={(open) => !open && onClose?.()}>
      <Dialog.Portal>
        <Dialog.Overlay className="scrim" />
        <Dialog.Content className={`modal ${wide ? 'wide' : ''}`} aria-describedby={undefined}>
          {title ? <Dialog.Title className="modal-title">{title}</Dialog.Title> : <Dialog.Title className="hidden">Dialog</Dialog.Title>}
          <div className="modal-body">{children}</div>
          {actions && <div className="modal-actions">{actions}</div>}
        </Dialog.Content>
      </Dialog.Portal>
    </Dialog.Root>
  );
}
