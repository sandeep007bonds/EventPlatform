import { useState } from 'react';
import { Button, Card, DatePicker, Form, Input, Select, Typography } from 'antd';
import type { Dayjs } from 'dayjs';
import { useNavigate } from 'react-router-dom';
import { createEvent } from '../../../services/catalog/catalogApi';
import { toast } from '../../../components/common/feedback/toast';
import { VenuePicker } from '../venues/VenuePicker';

interface CreateEventFormValues {
  title: string;
  venueId: string;
  startsAt: Dayjs;
  currency: string;
}

const CURRENCIES = ['USD', 'EUR', 'GBP', 'INR'];

/** Creates a new draft event for the caller's tenant. */
export function CreateEventPage() {
  const navigate = useNavigate();
  const [submitting, setSubmitting] = useState(false);

  const handleSubmit = async (values: CreateEventFormValues) => {
    setSubmitting(true);
    try {
      const result = await createEvent({
        title: values.title,
        venueId: values.venueId,
        startsAt: values.startsAt.toISOString(),
        currency: values.currency,
      });
      toast.success('Event created.');
      void navigate(`/admin/events/${result.id}`);
    } catch {
      toast.error('Could not create the event.');
    } finally {
      setSubmitting(false);
    }
  };

  return (
    <Card style={{ maxWidth: 480 }}>
      <Typography.Title level={3}>Create event</Typography.Title>
      <Form<CreateEventFormValues>
        layout="vertical"
        initialValues={{ currency: 'USD' }}
        onFinish={(values) => {
          void handleSubmit(values);
        }}
      >
        <Form.Item name="title" label="Title" rules={[{ required: true }]}>
          <Input />
        </Form.Item>
        <Form.Item name="venueId" label="Venue" rules={[{ required: true }]}>
          <VenuePicker />
        </Form.Item>
        <Form.Item name="startsAt" label="Starts at" rules={[{ required: true }]}>
          <DatePicker showTime style={{ width: '100%' }} />
        </Form.Item>
        <Form.Item name="currency" label="Currency" rules={[{ required: true }]}>
          <Select options={CURRENCIES.map((code) => ({ value: code, label: code }))} />
        </Form.Item>
        <Form.Item>
          <Button type="primary" htmlType="submit" block loading={submitting}>
            Create
          </Button>
        </Form.Item>
      </Form>
    </Card>
  );
}
