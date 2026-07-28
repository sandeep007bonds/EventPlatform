import { useEffect, useState } from 'react';
import { Card, List, Tag, Typography } from 'antd';
import { useParams } from 'react-router-dom';
import { getOrder, type OrderResponse } from '../../../services/ordering/orderingApi';
import { getOrderTickets, type TicketResponse } from '../../../services/ticketing/ticketingApi';
import { DetailSkeleton } from '../../../components/common/skeletons/DetailSkeleton';
import { NotFoundPage } from '../../../components/common/errors/NotFoundPage';
import { formatMoney } from '../../../utils/money';

const TICKET_POLL_INTERVAL_MS = 2000;
const TICKET_POLL_MAX_ATTEMPTS = 8;

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
    <>
      <Card title="Order confirmation">
        <Tag color={order.status === 'Confirmed' ? 'green' : 'default'}>{order.status}</Tag>
        <List
          style={{ marginTop: 16 }}
          dataSource={order.lines}
          renderItem={(line) => (
            <List.Item>
              <span>Seat {line.seatId}</span>
              <span>{formatMoney(line.priceMinor, order.currency)}</span>
            </List.Item>
          )}
        />
        <Typography.Title level={4} style={{ marginTop: 16 }}>
          Total: {formatMoney(order.totalMinor, order.currency)}
        </Typography.Title>
      </Card>

      <Card title="Tickets" style={{ marginTop: 24 }}>
        {tickets.length === 0 ? (
          <Typography.Text type="secondary">
            {ticketsPending
              ? 'Your tickets are being generated…'
              : 'Tickets are taking longer than expected — refresh shortly.'}
          </Typography.Text>
        ) : (
          <List
            dataSource={tickets}
            renderItem={(ticket) => (
              <List.Item>
                <Card size="small" style={{ width: '100%' }}>
                  <Typography.Text strong>Seat {ticket.seatId}</Typography.Text>
                  <Tag
                    style={{ marginLeft: 8 }}
                    color={ticket.status === 'Issued' ? 'green' : 'default'}
                  >
                    {ticket.status}
                  </Tag>
                  <Typography.Paragraph code copyable style={{ marginTop: 8, marginBottom: 0 }}>
                    {ticket.token}
                  </Typography.Paragraph>
                </Card>
              </List.Item>
            )}
          />
        )}
      </Card>
    </>
  );
}
