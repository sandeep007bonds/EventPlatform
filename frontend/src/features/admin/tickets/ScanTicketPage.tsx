import { useEffect, useState } from 'react';
import { Alert, Button, Card, Descriptions, Input, Select, Space, Tag, Typography } from 'antd';
import { QrcodeOutlined } from '@ant-design/icons';
import type { AxiosError } from 'axios';
import dayjs from 'dayjs';
import {
  listEntryGates,
  listEvents,
  type EntryGateResponse,
} from '../../../services/catalog/catalogApi';
import { scanTicket, type TicketResponse } from '../../../services/ticketing/ticketingApi';
import { PageHeader } from '../../../components/common/layout/PageHeader';
import { toast } from '../../../components/common/feedback/toast';

interface ScanErrorBody {
  message?: string;
}

interface EventOption {
  id: string;
  title: string;
}

const ANY_GATE_OPTION = '__any__';

const STATUS_COLOR: Record<TicketResponse['status'], string> = {
  Issued: 'default',
  CheckedIn: 'success',
  Void: 'error',
};

/**
 * Gate scan: pick which event and (optionally) which physical gate this device represents, then
 * paste (or wedge-scan) a ticket's token to check it in. "Any gate" is an unscoped supervisor
 * scanner that bypasses a section's gate restriction, if it has one.
 */
export function ScanTicketPage() {
  const [events, setEvents] = useState<EventOption[]>([]);
  const [eventsLoading, setEventsLoading] = useState(true);
  const [eventId, setEventId] = useState<string | undefined>();
  const [gates, setGates] = useState<EntryGateResponse[]>([]);
  const [gateId, setGateId] = useState<string>(ANY_GATE_OPTION);
  const [token, setToken] = useState('');
  const [submitting, setSubmitting] = useState(false);
  const [result, setResult] = useState<TicketResponse | null>(null);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    listEvents({ mine: true, page: 1, pageSize: 100 })
      .then((response) => setEvents(response.events.map((e) => ({ id: e.id, title: e.title }))))
      .catch(() => toast.error('Could not load your events.'))
      .finally(() => setEventsLoading(false));
  }, []);

  useEffect(() => {
    let cancelled = false;
    const fetchGates = eventId ? listEntryGates(eventId) : Promise.resolve([]);

    fetchGates
      .then((fetched) => {
        if (cancelled) {
          return;
        }
        setGateId(ANY_GATE_OPTION);
        setGates(fetched);
      })
      .catch(() => toast.error('Could not load this event’s entry gates.'));

    return () => {
      cancelled = true;
    };
  }, [eventId]);

  const handleScan = async () => {
    if (!eventId || !token.trim()) {
      return;
    }

    setSubmitting(true);
    setResult(null);
    setError(null);
    try {
      const ticket = await scanTicket(
        token.trim(),
        eventId,
        gateId === ANY_GATE_OPTION ? undefined : gateId,
      );
      setResult(ticket);
      setToken('');
    } catch (caught) {
      const axiosError = caught as AxiosError<ScanErrorBody>;
      const status = axiosError.response?.status;
      const message = axiosError.response?.data?.message;

      if (status === 404) {
        setError(message ?? 'No ticket matches that token.');
      } else if (status === 409) {
        setError(message ?? 'This ticket has already been checked in (or voided).');
      } else {
        setError('Something went wrong. Please try again.');
      }
    } finally {
      setSubmitting(false);
    }
  };

  return (
    <div style={{ maxWidth: 560, margin: '0 auto' }}>
      <PageHeader
        title="Scan tickets"
        description="Pick the event and gate this device is scanning for, then paste or scan a ticket's token to check it in."
      />
      <Card styles={{ body: { padding: 24 } }}>
        <Space direction="vertical" style={{ width: '100%', marginBottom: 20 }}>
          <Select
            placeholder="Select event"
            loading={eventsLoading}
            value={eventId}
            onChange={setEventId}
            options={events.map((e) => ({ value: e.id, label: e.title }))}
            style={{ width: '100%' }}
          />
          <Select
            placeholder="Gate"
            disabled={!eventId}
            value={eventId ? gateId : undefined}
            onChange={setGateId}
            options={[
              { value: ANY_GATE_OPTION, label: 'Any gate (supervisor)' },
              ...gates.map((gate) => ({ value: gate.id, label: gate.name })),
            ]}
            style={{ width: '100%' }}
          />
        </Space>

        <Space.Compact style={{ width: '100%', marginBottom: 20 }}>
          <Input
            size="large"
            prefix={<QrcodeOutlined />}
            placeholder="Ticket token"
            value={token}
            disabled={!eventId}
            onChange={(event) => setToken(event.target.value)}
            onPressEnter={() => void handleScan()}
            autoFocus
          />
          <Button
            type="primary"
            size="large"
            disabled={!eventId}
            loading={submitting}
            onClick={() => void handleScan()}
          >
            Check in
          </Button>
        </Space.Compact>

        {error && <Alert type="error" showIcon message={error} style={{ marginBottom: 20 }} />}

        {result && (
          <Alert
            type="success"
            showIcon
            message="Ticket checked in"
            description={
              <Descriptions column={1} size="small" style={{ marginTop: 8 }}>
                <Descriptions.Item label="Ticket">{result.id}</Descriptions.Item>
                <Descriptions.Item label="Status">
                  <Tag color={STATUS_COLOR[result.status]}>{result.status}</Tag>
                </Descriptions.Item>
                {result.checkedInAt && (
                  <Descriptions.Item label="Checked in at">
                    {dayjs(result.checkedInAt).format('MMM D, YYYY · h:mm:ss A')}
                  </Descriptions.Item>
                )}
              </Descriptions>
            }
          />
        )}

        {!error && !result && (
          <Typography.Text type="secondary">
            {eventId
              ? 'Waiting for a ticket token — scan a QR code or paste it above.'
              : 'Select an event to start scanning.'}
          </Typography.Text>
        )}
      </Card>
    </div>
  );
}
