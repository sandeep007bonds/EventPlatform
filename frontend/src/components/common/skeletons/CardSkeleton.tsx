import { Card, Skeleton } from 'antd';

/** Placeholder for a single card (e.g. an event card on the browse grid). */
export function CardSkeleton() {
  return (
    <Card>
      <Skeleton active avatar paragraph={{ rows: 2 }} />
    </Card>
  );
}
