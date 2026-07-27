import type { MessageInstance } from 'antd/es/message/interface';
import type { NotificationInstance } from 'antd/es/notification/interface';

// Ant Design's theme-aware message/notification requires the `<App>` component's
// `useApp()` hook, which only works inside a React render. `ToastHolder` mounts
// once near the root and stashes the instances here so non-React code (the axios
// interceptors) can still show a toast without needing to be a component itself.
let messageInstance: MessageInstance | null = null;
let notificationInstance: NotificationInstance | null = null;

/** Called once by `ToastHolder` to make `toast.*` calls work app-wide. */
export function registerToastInstances(
  message: MessageInstance,
  notification: NotificationInstance,
): void {
  messageInstance = message;
  notificationInstance = notification;
}

/** Fire-and-forget toast helpers, safe to call before `ToastHolder` has mounted. */
export const toast = {
  success(content: string): void {
    messageInstance?.success(content);
  },
  error(content: string): void {
    messageInstance?.error(content);
  },
  info(content: string): void {
    messageInstance?.info(content);
  },
  warning(content: string): void {
    messageInstance?.warning(content);
  },
  notifyError(message: string, description?: string): void {
    notificationInstance?.error({ message, description });
  },
};
