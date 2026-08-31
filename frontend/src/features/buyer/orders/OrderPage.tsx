import { useEffect, useState } from 'react';
import {
  Alert,
  Button,
  Card,
  Col,
  Modal,
  Result,
  Row,
  Skeleton,
  Space,
  Tag,
  Typography,
} from 'antd';
import { CheckCircleFilled } from '@ant-design/icons';
import { useParams } from 'react-router-dom';
import type { AxiosError } from 'axios';
import { cancelOrder, getOrder, type OrderResponse } from '../../../services/ordering/orderingApi';
import {
  getOrderTickets,
  getTicketQrCodeUrl,
  type TicketResponse,
} from '../../../services/ticketing/ticketingApi';
import { DetailSkeleton } from '../../../components/common/skeletons/DetailSkeleton';
import { NotFoundPage } from '../../../components/common/errors/NotFoundPage';
import { ServerErrorPage } from '../../../components/common/errors/ServerErrorPage';
import { toast } from '../../../components/common/feedback/toast';
import { formatMoney } from '../../../utils/money';
import { PriceRow } from '../checkout/PriceRow';

interface ConflictBody {
  message?: string;
}

const TICKET_POLL_INTERVAL_MS = 2000;
const TICKET_POLL_MAX_ATTEMPTS = 8;

// No max-attempts cap here, unlike the ticket poll above — payment authentication (a 3-D Secure
// challenge, a UPI app-switch) can legitimately take a while, and the order's own extended hold
// already bounds how long this can run before the checkout saga itself times the buyer out.
const ORDER_STATUS_POLL_INTERVAL_MS = 1500;

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
  const [qrUrls, setQrUrls] = useState<Map<string, string>>(new Map());
  const [loading, setLoading] = useState(true);
  const [ticketsPending, setTicketsPending] = useState(true);
  const [notFound, setNotFound] = useState(false);
  const [loadError, setLoadError] = useState(false);
  const [cancelling, setCancelling] = useState(false);

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
      .catch((error: AxiosError) => {
        if (cancelled) {
          return;
        }
        if (error.response?.status === 404) {
          setNotFound(true);
        } else {
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
  }, [orderId]);

  // While the order is awaiting payment (the buyer is mid-authentication — a 3-D Secure challenge,
  // a UPI app-switch, or the webhook simply hasn't landed yet), poll for it to flip to a terminal
  // status so the page updates organically instead of looking stuck.
  useEffect(() => {
    if (!orderId || order?.status !== 'AwaitingPayment') {
      return;
    }

    let cancelled = false;

    const poll = () => {
      getOrder(orderId)
        .then((result) => {
          if (cancelled) {
            return;
          }
          setOrder(result);
          if (result.status === 'AwaitingPayment') {
            setTimeout(poll, ORDER_STATUS_POLL_INTERVAL_MS);
          }
        })
        .catch(() => {
          // Transient failure — the buyer can still refresh manually.
        });
    };

    const timer = setTimeout(poll, ORDER_STATUS_POLL_INTERVAL_MS);

    return () => {
      cancelled = true;
      clearTimeout(timer);
    };
  }, [orderId, order?.status]);

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

  // Fetch each ticket's QR image as an authenticated blob once tickets are known — a plain
  // <img src="..."> can't carry the bearer token the endpoint requires.
  useEffect(() => {
    if (tickets.length === 0) {
      return;
    }

    let cancelled = false;
    Promise.all(
      tickets.map((ticket) =>
        getTicketQrCodeUrl(ticket.id).then((url) => [ticket.id, url] as const),
      ),
    )
      .then((pairs) => {
        if (!cancelled) {
          setQrUrls(new Map(pairs));
        }
      })
      .catch(() => {
        // Not fatal — the raw token text below still lets the ticket be scanned manually.
      });

    return () => {
      cancelled = true;
    };
  }, [tickets]);

  // Blob URLs are only valid for this tab's lifetime — revoke them once replaced or on unmount.
  useEffect(
    () => () => {
      qrUrls.forEach((url) => URL.revokeObjectURL(url));
    },
    [qrUrls],
  );

  const handleCancel = () => {
    if (!orderId) {
      return;
    }

    Modal.confirm({
      title: 'Cancel this order?',
      content: 'Your tickets will be voided and the payment refunded. This cannot be undone.',
      okText: 'Cancel order',
      okButtonProps: { danger: true },
      cancelText: 'Keep order',
      onOk: async () => {
        setCancelling(true);
        try {
          await cancelOrder(orderId);
          toast.success('Order cancelled and refunded.');
          const refreshed = await getOrder(orderId);
          setOrder(refreshed);
        } catch (error) {
          const body = (error as AxiosError<ConflictBody>).response?.data;
          toast.error(body?.message ?? 'This order could not be cancelled.');
        } finally {
          setCancelling(false);
        }
      },
    });
  };

  if (loading) {
    return <DetailSkeleton />;
  }

  if (loadError) {
    return <ServerErrorPage />;
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

      {order.status === 'AwaitingPayment' && (
        <Alert
          type="info"
          showIcon
          message="Finishing your payment…"
          description="This page will update automatically once your bank or payment app confirms it — no need to refresh."
          style={{ marginBottom: 16 }}
        />
      )}

      <Card
        title="Order summary"
        styles={{ body: { padding: 24 } }}
        extra={
          <Space>
            {order.status === 'Confirmed' && (
              <Button danger size="small" loading={cancelling} onClick={handleCancel}>
                Cancel order
              </Button>
            )}
            <Tag color={ORDER_STATUS_COLOR[order.status]}>{order.status}</Tag>
          </Space>
        }
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
        <div style={{ marginTop: 16 }}>
          <PriceRow label="Subtotal" amountMinor={order.subtotalMinor} currency={order.currency} />
          {order.discountMinor > 0 && (
            <PriceRow
              label={`Discount${order.promoCode ? ` (${order.promoCode})` : ''}`}
              amountMinor={-order.discountMinor}
              currency={order.currency}
              emphasis="success"
            />
          )}
          {order.bookingFeeMinor > 0 && (
            <PriceRow
              label="Booking fee"
              amountMinor={order.bookingFeeMinor}
              currency={order.currency}
            />
          )}
          {order.taxMinor > 0 && (
            <PriceRow
              label={order.taxLabel ?? 'Tax'}
              amountMinor={order.taxMinor}
              currency={order.currency}
            />
          )}
        </div>
        <div
          style={{
            display: 'flex',
            justifyContent: 'space-between',
            alignItems: 'baseline',
            marginTop: 12,
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
                {qrUrls.has(ticket.id) && (
                  <div style={{ textAlign: 'center', marginBottom: 12 }}>
                    <img
                      src={qrUrls.get(ticket.id)}
                      alt="Ticket QR code"
                      style={{ width: 160, height: 160 }}
                    />
                  </div>
                )}
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
