import { Skeleton, Space } from 'antd';

/** Placeholder for a vertical list of items (e.g. the public events browse page). */
export function ListSkeleton({ rows = 4 }: { rows?: number }) {
  return (
    <Space direction="vertical" style={{ width: '100%' }} size="large">
      {Array.from({ length: rows }, (_, index) => (
        <Skeleton key={index} active avatar paragraph={{ rows: 2 }} />
      ))}
    </Space>
  );
}
