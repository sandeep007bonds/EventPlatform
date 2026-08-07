import { useEffect, useState } from 'react';
import { Card, Table, Tag } from 'antd';
import dayjs from 'dayjs';
import {
  listTenantOrders,
  type OrderStatus,
  type OrderSummaryResponse,
} from '../../../services/ordering/orderingApi';
import { TableSkeleton } from '../../../components/common/skeletons/TableSkeleton';
import { PageHeader } from '../../../components/common/layout/PageHeader';
import { LoadError } from '../../../components/common/errors/LoadError';
import { formatMoney } from '../../../utils/money';

const PAGE_SIZE = 20;
const STATUS_OPTIONS: OrderStatus[] = [
  'Pending',
  'AwaitingPayment',
  'Confirmed',
  'Failed',
  'Refunded',
];

const STATUS_COLOR: Record<OrderStatus, string> = {
  Pending: 'default',
  AwaitingPayment: 'processing',
  Confirmed: 'success',
  Failed: 'error',
  Refunded: 'default',
};

/** All orders for the organizer's tenant. */
export function AdminOrdersPage() {
  const [orders, setOrders] = useState<OrderSummaryResponse[]>([]);
  const [totalCount, setTotalCount] = useState(0);
  const [page, setPage] = useState(1);
  const [loading, setLoading] = useState(true);
  const [loadError, setLoadError] = useState(false);
  const [reloadToken, setReloadToken] = useState(0);

  useEffect(() => {
    let cancelled = false;

    listTenantOrders({ page, pageSize: PAGE_SIZE })
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
        <PageHeader title="Orders" />
        <TableSkeleton />
      </>
    );
  }

  return (
    <>
      <PageHeader title="Orders" description="Every order placed against your events." />
      {loadError ? (
        <Card>
          <LoadError
            description="Could not load orders."
            onRetry={() => {
              setLoading(true);
              setReloadToken((token) => token + 1);
            }}
          />
        </Card>
      ) : (
        <Card styles={{ body: { padding: 0 } }}>
          <Table<OrderSummaryResponse>
            rowKey="id"
            dataSource={orders}
            pagination={{
              current: page,
              pageSize: PAGE_SIZE,
              total: totalCount,
              onChange: setPage,
              showSizeChanger: false,
            }}
            columns={[
              { title: 'Order', dataIndex: 'id', render: (id: string) => id.slice(0, 8) },
              {
                title: 'Status',
                dataIndex: 'status',
                // Client-side, current page only — Ordering has no server-side status filter yet.
                filters: STATUS_OPTIONS.map((option) => ({ text: option, value: option })),
                onFilter: (value, record) => record.status === value,
                render: (status: OrderSummaryResponse['status']) => (
                  <Tag color={STATUS_COLOR[status]}>{status}</Tag>
                ),
              },
              {
                title: 'Total',
                key: 'total',
                sorter: (a, b) => a.totalMinor - b.totalMinor,
                render: (_, order) => formatMoney(order.totalMinor, order.currency),
              },
              {
                title: 'Created',
                dataIndex: 'createdAt',
                sorter: (a, b) => dayjs(a.createdAt).valueOf() - dayjs(b.createdAt).valueOf(),
                defaultSortOrder: 'descend',
                render: (createdAt: string) => dayjs(createdAt).format('MMM D, YYYY · h:mm A'),
              },
            ]}
          />
        </Card>
      )}
    </>
  );
}
