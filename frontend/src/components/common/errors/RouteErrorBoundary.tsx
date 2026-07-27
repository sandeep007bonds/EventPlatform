import { Component } from 'react';
import type { ErrorInfo, ReactNode } from 'react';
import { ServerErrorPage } from './ServerErrorPage';

interface Props {
  children: ReactNode;
}

interface State {
  hasError: boolean;
}

/**
 * Catches uncaught render errors anywhere in the route tree and shows the full-page 500 —
 * reserved for genuine route-level failures, not the toast shown for a failed API call.
 */
export class RouteErrorBoundary extends Component<Props, State> {
  public state: State = { hasError: false };

  public static getDerivedStateFromError(): State {
    return { hasError: true };
  }

  public componentDidCatch(error: unknown, info: ErrorInfo): void {
    console.error('Unhandled render error', error, info.componentStack);
  }

  public render(): ReactNode {
    if (this.state.hasError) {
      return <ServerErrorPage />;
    }

    return this.props.children;
  }
}
