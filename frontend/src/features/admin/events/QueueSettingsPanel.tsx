import { useEffect, useState } from 'react';
import { Alert, Button, Card, Form, InputNumber, Space, Tag, Typography } from 'antd';
import {
  getQueueSettings,
  updateQueueSettings,
  type QueueSettingsResponse,
} from '../../../services/queue/queueApi';
import { toast } from '../../../components/common/feedback/toast';

interface QueueSettingsFormValues {
  admissionRatePerInterval: number;
  intervalSeconds: number;
  sessionTtlSeconds: number;
}

// Queue's own settings row is provisioned off the same EventPublished message Inventory reacts
// to, so it can briefly 404 right after publish — poll rather than trust a single fetch, mirroring
// SeatBlockPanel's identical provisioning-race handling.
const SETTINGS_POLL_INTERVAL_MS = 1500;
const SETTINGS_POLL_MAX_ATTEMPTS = 6;

/**
 * Lets an organizer tune an event's queue pacing (admission rate/interval/session TTL) once it's
 * published. Whether queueing is enabled at all is fixed at creation (`Event.RequiresQueue`) and
 * is not editable here — this panel only appears when that flag is already set.
 */
export function QueueSettingsPanel({ eventId }: { eventId: string }) {
  const [settings, setSettings] = useState<QueueSettingsResponse | null>(null);
  const [loading, setLoading] = useState(true);
  const [provisioning, setProvisioning] = useState(false);
  const [submitting, setSubmitting] = useState(false);
  const [form] = Form.useForm<QueueSettingsFormValues>();

  useEffect(() => {
    let cancelled = false;
    let attempts = 0;

    const poll = () => {
      getQueueSettings(eventId)
        .then((result) => {
          if (cancelled) {
            return;
          }
          setSettings(result);
          setProvisioning(false);
          setLoading(false);
          form.setFieldsValue({
            admissionRatePerInterval: result.admissionRatePerInterval,
            intervalSeconds: result.intervalSeconds,
            sessionTtlSeconds: result.sessionTtlSeconds,
          });
        })
        .catch(() => {
          if (cancelled) {
            return;
          }
          attempts += 1;
          if (attempts >= SETTINGS_POLL_MAX_ATTEMPTS) {
            setProvisioning(false);
            setLoading(false);
            return;
          }
          setProvisioning(true);
          setTimeout(poll, SETTINGS_POLL_INTERVAL_MS);
        });
    };

    poll();

    return () => {
      cancelled = true;
    };
  }, [eventId, form]);

  const handleSubmit = async (values: QueueSettingsFormValues) => {
    setSubmitting(true);
    try {
      await updateQueueSettings(eventId, values);
      toast.success('Queue settings saved.');
    } catch {
      toast.error('Could not save queue settings.');
    } finally {
      setSubmitting(false);
    }
  };

  if (loading) {
    return null;
  }

  if (!settings) {
    return (
      <Card title="Queue settings" style={{ marginTop: 24 }}>
        {provisioning ? (
          <Alert type="info" showIcon message="Queue settings are still being set up." />
        ) : (
          <Typography.Text type="secondary">Could not load queue settings.</Typography.Text>
        )}
      </Card>
    );
  }

  return (
    <Card
      title={
        <Space>
          Queue settings
          <Tag color={settings.enabled ? 'success' : 'default'}>
            {settings.enabled ? 'Enabled' : 'Disabled'}
          </Tag>
        </Space>
      }
      style={{ marginTop: 24 }}
    >
      <Typography.Paragraph type="secondary">
        Tunes how fast waiting buyers are let through — unlike other event details, this can be
        adjusted after publish since pacing only matters once the event is actually live.
      </Typography.Paragraph>
      <Form form={form} layout="vertical" onFinish={(values) => void handleSubmit(values)}>
        <Space size={20} wrap>
          <Form.Item
            name="admissionRatePerInterval"
            label="Admissions per interval"
            rules={[{ required: true }, { type: 'number', min: 1 }]}
          >
            <InputNumber min={1} style={{ width: 160 }} />
          </Form.Item>
          <Form.Item
            name="intervalSeconds"
            label="Interval (seconds)"
            rules={[{ required: true }, { type: 'number', min: 1 }]}
          >
            <InputNumber min={1} style={{ width: 160 }} />
          </Form.Item>
          <Form.Item
            name="sessionTtlSeconds"
            label="Admission window (seconds)"
            tooltip="How long a buyer has to hold a seat once admitted before losing their spot."
            rules={[{ required: true }, { type: 'number', min: 1 }]}
          >
            <InputNumber min={1} style={{ width: 160 }} />
          </Form.Item>
        </Space>
        <Form.Item>
          <Button type="primary" htmlType="submit" loading={submitting}>
            Save queue settings
          </Button>
        </Form.Item>
      </Form>
    </Card>
  );
}
