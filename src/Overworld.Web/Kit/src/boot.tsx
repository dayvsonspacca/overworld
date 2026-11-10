import { Component, StrictMode, type ComponentType, type ReactNode } from 'react';
import { createRoot } from 'react-dom/client';
import * as Tooltip from '@radix-ui/react-tooltip';
import { connect, log, on, useRoot } from './mirror';
import { windowControls } from './window';
import { CommandBook } from './commands';
import { watchForDebugging } from './debug';

class Guard extends Component<{ children: ReactNode }, { broken: string | null }> {
  state = { broken: null as string | null };

  componentDidCatch(error: Error) {
    log(`render: ${error?.stack ?? error}`);
    this.setState({ broken: String(error?.message ?? error) });
  }

  render() {
    return this.state.broken ? <div className="empty danger">{this.state.broken}</div> : this.props.children;
  }
}

function reloadTheme() {
  const link = document.getElementById('theme') as HTMLLinkElement | null;
  if (link) link.href = `/theme.css?at=${Date.now()}`;
}

export function boot(App: ComponentType<{ root: any }>) {
  on('theme', reloadTheme);
  on('summon', windowControls.focus);
  connect();
  watchForDebugging();

  function Root() {
    const root = useRoot();
    return (
      <Tooltip.Provider>
        <CommandBook>{root ? <Guard><App root={root} /></Guard> : <div className="booting" />}</CommandBook>
      </Tooltip.Provider>
    );
  }

  createRoot(document.getElementById('app')!).render(<StrictMode><Root /></StrictMode>);
}
