import { useEffect } from 'react';
import { App } from 'antd';
import { registerToastInstances } from './toast';

/**
 * Mounts once inside the Ant `<App>` component-context provider and stashes its
 * `message`/`notification` instances so plain-module code (axios interceptors)
 * can call `toast.*` outside of React. Renders nothing.
 */
export function ToastHolder() {
  const { message, notification } = App.useApp();

  useEffect(() => {
    registerToastInstances(message, notification);
  }, [message, notification]);

  return null;
}
