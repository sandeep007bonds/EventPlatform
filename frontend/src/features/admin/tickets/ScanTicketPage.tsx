import { useState } from 'react';
import { Alert, Button, Card, Descriptions, Input, Space, Tag, Typography } from 'antd';
import { QrcodeOutlined } from '@ant-design/icons';
import type { AxiosError } from 'axios';
import dayjs from 'dayjs';
import { scanTicket, type TicketResponse } from '../../../services/ticketing/ticketingApi';
import { PageHeader } from '../../../components/common/layout/PageHeader';

interface ScanErrorBody {
  message?: string;
}

const STATUS_COLOR: Record<TicketResponse['status'], string> = {
  Issued: 'default',
  CheckedIn: 'success',
  Void: 'error',
};

/** Gate scan: paste (or wedge-scan) a ticket's token, check it in, and see a clear result. */
export function ScanTicketPage() {
  const [token, setToken] = useState('');
  const [submitting, setSubmitting] = useState(false);
  const [result, setResult] = useState<TicketResponse | null>(null);
  const [error, setError] = useState<string | null>(null);

  const handleScan = async () => {
    if (!token.trim()) {
      return;
    }

    setSubmitting(true);
    setResult(null);
    setError(null);
    try {
      const ticket = await scanTicket(token.trim());
      setResult(ticket);
      setToken('');
    } catch (caught) {
      const axiosError = caught as AxiosError<ScanErrorBody>;
      const status = axiosError.response?.status;
      const message = axiosError.response?.data?.message;

      if (status === 404) {
        setError('No ticket matches that token.');
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
        description="Paste or scan a ticket's token to check it in at the gate."
      />
      <Card styles={{ body: { padding: 24 } }}>
        <Space.Compact style={{ width: '100%', marginBottom: 20 }}>
          <Input
            size="large"
            prefix={<QrcodeOutlined />}
            placeholder="Ticket token"
            value={token}
            onChange={(event) => setToken(event.target.value)}
            onPressEnter={() => void handleScan()}
            autoFocus
          />
          <Button
            type="primary"
            size="large"
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
            Waiting for a ticket token — scan a QR code or paste it above.
          </Typography.Text>
        )}
      </Card>
    </div>
  );
}
