import { useEffect, useState } from 'react';
import { Card, Col, Result, Row, Skeleton, Space, Tag, Typography } from 'antd';
import { CheckCircleFilled } from '@ant-design/icons';
import { useParams } from 'react-router-dom';
import { getOrder, type OrderResponse } from '../../../services/ordering/orderingApi';
import { getOrderTickets, type TicketResponse } from '../../../services/ticketing/ticketingApi';
import { DetailSkeleton } from '../../../components/common/skeletons/DetailSkeleton';
import { NotFoundPage } from '../../../components/common/errors/NotFoundPage';
import { formatMoney } from '../../../utils/money';

const TICKET_POLL_INTERVAL_MS = 2000;
const TICKET_POLL_MAX_ATTEMPTS = 8;

const ORDER_STATUS_COLOR: Record<OrderResponse['status'], string> = {
  Pending: 'default',
  AwaitingPayment: 'processing',
  Confirmed: 'success',
  Failed: 'error',
  Refunded: 'default',
};

/**
 * Order confirmation + tickets. Ticketing is populated asynchronously (pub/sub off
 * `OrderConfirmed`), so if the ticket list comes back empty this polls briefly before
 * giving up — the order itself is already confirmed by the time this page loads.
 */
export function OrderPage() {
  const { orderId } = useParams<{ orderId: string }>();

  const [order, setOrder] = useState<OrderResponse | null>(null);
  const [tickets, setTickets] = useState<TicketResponse[]>([]);
  const [loading, setLoading] = useState(true);
  const [ticketsPending, setTicketsPending] = useState(true);
  const [notFound, setNotFound] = useState(false);

  useEffect(() => {
    if (!orderId) {
      return;
    }

    let cancelled = false;
    getOrder(orderId)
      .then((result) => {
        if (!cancelled) {
          setOrder(result);
        }
      })
      .catch(() => setNotFound(true))
      .finally(() => {
        if (!cancelled) {
          setLoading(false);
        }
      });

    return () => {
      cancelled = true;
    };
  }, [orderId]);

  useEffect(() => {
    if (!orderId) {
      return;
    }

    let cancelled = false;
    let attempts = 0;

    const poll = () => {
      getOrderTickets(orderId)
        .then((result) => {
          if (cancelled) {
            return;
          }
          if (result.length > 0) {
            setTickets(result);
            setTicketsPending(false);
            return;
          }
          attempts += 1;
          if (attempts >= TICKET_POLL_MAX_ATTEMPTS) {
            setTicketsPending(false);
            return;
          }
          setTimeout(poll, TICKET_POLL_INTERVAL_MS);
        })
        .catch(() => setTicketsPending(false));
    };

    poll();

    return () => {
      cancelled = true;
    };
  }, [orderId]);

  if (loading) {
    return <DetailSkeleton />;
  }

  if (notFound || !order) {
    return <NotFoundPage />;
  }

  return (
    <div style={{ maxWidth: 760, margin: '0 auto' }}>
      {order.status === 'Confirmed' && (
        <Result
          icon={<CheckCircleFilled style={{ color: '#52c41a' }} />}
          title="You're all set!"
          subTitle={`Order confirmed — ${formatMoney(order.totalMinor, order.currency)} charged.`}
          style={{ paddingBottom: 8 }}
        />
      )}

      <Card
        title="Order summary"
        styles={{ body: { padding: 24 } }}
        extra={<Tag color={ORDER_STATUS_COLOR[order.status]}>{order.status}</Tag>}
      >
        <div>
          {order.lines.map((line, index) => (
            <div
              key={`${line.seatId ?? line.generalAdmissionAllocationId}-${index}`}
              style={{
                display: 'flex',
                justifyContent: 'space-between',
                padding: '10px 0',
                borderBottom: '1px solid rgba(0,0,0,0.06)',
              }}
            >
              <Typography.Text>
                {line.seatId ? `Seat ${line.seatId}` : `General admission × ${line.quantity}`}
              </Typography.Text>
              <Typography.Text>{formatMoney(line.priceMinor, order.currency)}</Typography.Text>
            </div>
          ))}
        </div>
        <div
          style={{
            display: 'flex',
            justifyContent: 'space-between',
            alignItems: 'baseline',
            marginTop: 16,
          }}
        >
          <Typography.Text strong style={{ fontSize: 16 }}>
            Total
          </Typography.Text>
          <Typography.Title level={4} style={{ margin: 0 }}>
            {formatMoney(order.totalMinor, order.currency)}
          </Typography.Title>
        </div>
      </Card>

      <Typography.Title level={5} style={{ marginTop: 32, marginBottom: 16 }}>
        Your tickets
      </Typography.Title>

      {tickets.length === 0 ? (
        <Card styles={{ body: { padding: 24 } }}>
          {ticketsPending ? (
            <Space direction="vertical" style={{ width: '100%' }}>
              <Skeleton active paragraph={{ rows: 1 }} />
              <Typography.Text type="secondary">Your tickets are being generated…</Typography.Text>
            </Space>
          ) : (
            <Typography.Text type="secondary">
              Tickets are taking longer than expected — refresh shortly.
            </Typography.Text>
          )}
        </Card>
      ) : (
        <Row gutter={[16, 16]}>
          {tickets.map((ticket) => (
            <Col key={ticket.id} xs={24} sm={12}>
              <Card
                styles={{ body: { padding: 20 } }}
                style={{
                  borderStyle: 'dashed',
                  borderColor: 'rgba(0,0,0,0.15)',
                }}
              >
                <div
                  style={{
                    display: 'flex',
                    justifyContent: 'space-between',
                    alignItems: 'center',
                    marginBottom: 12,
                  }}
                >
                  <Typography.Text strong>
                    {ticket.seatId ? `Seat ${ticket.seatId}` : 'General admission'}
                  </Typography.Text>
                  <Tag color={ticket.status === 'Issued' ? 'success' : 'default'}>
                    {ticket.status}
                  </Tag>
                </div>
                <Typography.Text type="secondary" style={{ fontSize: 12 }}>
                  Scan token
                </Typography.Text>
                <Typography.Paragraph
                  code
                  copyable
                  style={{ marginTop: 4, marginBottom: 0, wordBreak: 'break-all' }}
                >
                  {ticket.token}
                </Typography.Paragraph>
              </Card>
            </Col>
          ))}
        </Row>
      )}
    </div>
  );
}
