import { Skeleton } from 'antd';

/** Placeholder for a table view (e.g. the admin orders/inventory pages). */
export function TableSkeleton({ rows = 6 }: { rows?: number }) {
  return <Skeleton active title={false} paragraph={{ rows }} />;
}
