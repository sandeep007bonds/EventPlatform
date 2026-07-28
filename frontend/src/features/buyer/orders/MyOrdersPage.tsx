import { useEffect, useState } from 'react';
import { Empty, List, Pagination, Tag, Typography } from 'antd';
import dayjs from 'dayjs';
import { Link } from 'react-router-dom';
import { listMyOrders, type OrderSummaryResponse } from '../../../services/ordering/orderingApi';
import { ListSkeleton } from '../../../components/common/skeletons/ListSkeleton';
import { toast } from '../../../components/common/feedback/toast';
import { formatMoney } from '../../../utils/money';

const PAGE_SIZE = 10;

/** The buyer's own order history. */
export function MyOrdersPage() {
  const [orders, setOrders] = useState<OrderSummaryResponse[]>([]);
  const [totalCount, setTotalCount] = useState(0);
  const [page, setPage] = useState(1);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    let cancelled = false;

    listMyOrders({ page, pageSize: PAGE_SIZE })
      .then((result) => {
        if (cancelled) {
          return;
        }
        setOrders(result.orders);
        setTotalCount(result.totalCount);
      })
      .catch(() => toast.error('Could not load your orders.'))
      .finally(() => {
        if (!cancelled) {
          setLoading(false);
        }
      });

    return () => {
      cancelled = true;
    };
  }, [page]);

  if (loading) {
    return <ListSkeleton />;
  }

  if (orders.length === 0) {
    return <Empty description="No orders yet" />;
  }

  return (
    <>
      <List
        dataSource={orders}
        renderItem={(order) => (
          <List.Item>
            <Link
              to={`/orders/${order.id}`}
              style={{ width: '100%', display: 'flex', justifyContent: 'space-between' }}
            >
              <span>
                <Tag color={order.status === 'Confirmed' ? 'green' : 'default'}>{order.status}</Tag>
                {dayjs(order.createdAt).format('MMM D, YYYY')}
              </span>
              <Typography.Text strong>
                {formatMoney(order.totalMinor, order.currency)}
              </Typography.Text>
            </Link>
          </List.Item>
        )}
      />
      <Pagination
        style={{ marginTop: 16, textAlign: 'center' }}
        current={page}
        pageSize={PAGE_SIZE}
        total={totalCount}
        onChange={setPage}
        showSizeChanger={false}
      />
    </>
  );
}
