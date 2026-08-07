import { useEffect, useState } from 'react';
import { Card, Empty, Pagination, Space, Tag, Typography } from 'antd';
import dayjs from 'dayjs';
import { Link } from 'react-router-dom';
import { listMyOrders, type OrderSummaryResponse } from '../../../services/ordering/orderingApi';
import { ListSkeleton } from '../../../components/common/skeletons/ListSkeleton';
import { PageHeader } from '../../../components/common/layout/PageHeader';
import { LoadError } from '../../../components/common/errors/LoadError';
import { formatMoney } from '../../../utils/money';

const PAGE_SIZE = 10;

const STATUS_COLOR: Record<OrderSummaryResponse['status'], string> = {
  Pending: 'default',
  AwaitingPayment: 'processing',
  Confirmed: 'success',
  Failed: 'error',
  Refunded: 'default',
};

/** The buyer's own order history. */
export function MyOrdersPage() {
  const [orders, setOrders] = useState<OrderSummaryResponse[]>([]);
  const [totalCount, setTotalCount] = useState(0);
  const [page, setPage] = useState(1);
  const [loading, setLoading] = useState(true);
  const [loadError, setLoadError] = useState(false);
  const [reloadToken, setReloadToken] = useState(0);

  useEffect(() => {
    let cancelled = false;

    listMyOrders({ page, pageSize: PAGE_SIZE })
      .then((result) => {
        if (cancelled) {
          return;
        }
        setOrders(result.orders);
        setTotalCount(result.totalCount);
        setLoadError(false);
      })
      .catch(() => {
        if (!cancelled) {
          setLoadError(true);
        }
      })
      .finally(() => {
        if (!cancelled) {
          setLoading(false);
        }
      });

    return () => {
      cancelled = true;
    };
  }, [page, reloadToken]);

  if (loading) {
    return (
      <>
        <PageHeader title="My orders" />
        <ListSkeleton />
      </>
    );
  }

  return (
    <>
      <PageHeader title="My orders" />
      {loadError ? (
        <LoadError
          description="Could not load your orders."
          onRetry={() => {
            setLoading(true);
            setReloadToken((token) => token + 1);
          }}
        />
      ) : orders.length === 0 ? (
        <Empty description="No orders yet" style={{ margin: '64px 0' }} />
      ) : (
        <>
          <Space direction="vertical" size={12} style={{ width: '100%' }}>
            {orders.map((order) => (
              <Link key={order.id} to={`/orders/${order.id}`}>
                <Card
                  hoverable
                  styles={{ body: { padding: '16px 20px' } }}
                  style={{ width: '100%' }}
                >
                  <div
                    style={{
                      display: 'flex',
                      justifyContent: 'space-between',
                      alignItems: 'center',
                      flexWrap: 'wrap',
                      gap: 12,
                    }}
                  >
                    <Space direction="vertical" size={2}>
                      <Space>
                        <Tag color={STATUS_COLOR[order.status]}>{order.status}</Tag>
                        <Typography.Text type="secondary" style={{ fontSize: 12 }}>
                          #{order.id.slice(0, 8)}
                        </Typography.Text>
                      </Space>
                      <Typography.Text type="secondary" style={{ fontSize: 13 }}>
                        {dayjs(order.createdAt).format('MMM D, YYYY · h:mm A')}
                      </Typography.Text>
                    </Space>
                    <Typography.Text strong style={{ fontSize: 16 }}>
                      {formatMoney(order.totalMinor, order.currency)}
                    </Typography.Text>
                  </div>
                </Card>
              </Link>
            ))}
          </Space>
          <Pagination
            style={{ marginTop: 24, textAlign: 'center' }}
            current={page}
            pageSize={PAGE_SIZE}
            total={totalCount}
            onChange={setPage}
            showSizeChanger={false}
          />
        </>
      )}
    </>
  );
}
