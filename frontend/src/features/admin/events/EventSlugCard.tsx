import { useEffect, useState } from 'react';
import { Alert, Button, Form, Input, Space, Typography } from 'antd';
import { changeEventSlug, type EventResponse } from '../../../services/catalog/catalogApi';
import { toast } from '../../../components/common/feedback/toast';

interface EventSlugCardProps {
  event: EventResponse;
  /** Called after a successful save, so the parent can re-fetch. */
  onSaved: () => void;
}

interface SlugFormValues {
  slug: string;
}

/**
 * The event's public web address.
 *
 * Editable while the event is a draft and fixed afterwards — once the link has been printed on a
 * poster or posted to a feed, moving it breaks something nobody here controls. The server enforces
 * that; this only explains it.
 */
export function EventSlugCard({ event, onSaved }: EventSlugCardProps) {
  const [form] = Form.useForm<SlugFormValues>();
  const [saving, setSaving] = useState(false);
  const locked = event.status !== 'Draft';
  const publicUrl = `${window.location.origin}/events/${event.slug}`;

  useEffect(() => {
    form.setFieldsValue({ slug: event.slug });
  }, [event, form]);

  const handleSave = async (values: SlugFormValues) => {
    setSaving(true);
    try {
      await changeEventSlug(event.id, values.slug);
      toast.success('Web address updated.');
      onSaved();
    } catch (error) {
      toast.error(messageFrom(error) ?? 'Could not change the web address.');
    } finally {
      setSaving(false);
    }
  };

  return (
    <>
      <Typography.Text type="secondary" style={{ display: 'block', marginBottom: 16 }}>
        Buyers reach this event at{' '}
        <Typography.Text code copyable={{ text: publicUrl }}>
          {publicUrl}
        </Typography.Text>
      </Typography.Text>

      {locked ? (
        <Alert
          type="info"
          showIcon
          message="This address is now fixed"
          description="The event is published, so its link may already be on a poster or in someone's feed. Renaming the event on the Event page tab does not move it."
        />
      ) : (
        <Form<SlugFormValues>
          form={form}
          layout="inline"
          onFinish={(values) => void handleSave(values)}
        >
          <Form.Item
            name="slug"
            label="Address"
            rules={[{ required: true, message: 'Required' }, { max: 120 }]}
            tooltip="Letters, numbers and hyphens. Anything else is converted — “Coldplay Mumbai” becomes “coldplay-mumbai”."
            style={{ flex: 1, minWidth: 320 }}
          >
            <Input addonBefore="/events/" />
          </Form.Item>
          <Form.Item>
            <Space>
              <Button type="primary" htmlType="submit" loading={saving}>
                Save address
              </Button>
            </Space>
          </Form.Item>
        </Form>
      )}
    </>
  );
}

function messageFrom(error: unknown): string | undefined {
  return (error as { response?: { data?: { message?: string } } }).response?.data?.message;
}
