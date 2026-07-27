import { Skeleton } from 'antd';

/** Placeholder for a single-record detail view (e.g. an event or ticket page). */
export function DetailSkeleton() {
  return <Skeleton active avatar paragraph={{ rows: 6 }} />;
}
