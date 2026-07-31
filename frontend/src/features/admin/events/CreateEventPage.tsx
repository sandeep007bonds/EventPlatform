import { useState } from 'react';
import {
  Button,
  Card,
  DatePicker,
  Divider,
  Form,
  Input,
  InputNumber,
  Select,
  Typography,
} from 'antd';
import type { Dayjs } from 'dayjs';
import { useNavigate } from 'react-router-dom';
import { createEvent } from '../../../services/catalog/catalogApi';
import { toast } from '../../../components/common/feedback/toast';
import { EventGroupPicker } from '../eventGroups/EventGroupPicker';

interface CreateEventFormValues {
  title: string;
  startsAt: Dayjs;
  endsAt: Dayjs;
  currency: string;
  locationName: string;
  addressLine1: string;
  addressLine2?: string;
  city: string;
  region?: string;
  postalCode?: string;
  country: string;
  latitude?: number;
  longitude?: number;
  eventGroupId?: string;
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
        startsAt: values.startsAt.toISOString(),
        endsAt: values.endsAt.toISOString(),
        currency: values.currency,
        locationName: values.locationName,
        addressLine1: values.addressLine1,
        addressLine2: values.addressLine2 ?? null,
        city: values.city,
        region: values.region ?? null,
        postalCode: values.postalCode ?? null,
        country: values.country,
        latitude: values.latitude ?? null,
        longitude: values.longitude ?? null,
        eventGroupId: values.eventGroupId ?? null,
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
        <Form.Item name="startsAt" label="Starts at" rules={[{ required: true }]}>
          <DatePicker showTime style={{ width: '100%' }} />
        </Form.Item>
        <Form.Item name="endsAt" label="Ends at" rules={[{ required: true }]}>
          <DatePicker showTime style={{ width: '100%' }} />
        </Form.Item>
        <Form.Item name="currency" label="Currency" rules={[{ required: true }]}>
          <Select options={CURRENCIES.map((code) => ({ value: code, label: code }))} />
        </Form.Item>
        <Form.Item name="eventGroupId" label="Part of a tour? (optional)">
          <EventGroupPicker />
        </Form.Item>

        <Divider>Location</Divider>
        <Form.Item name="locationName" label="Venue name" rules={[{ required: true }]}>
          <Input placeholder="e.g. Wankhede Stadium" />
        </Form.Item>
        <Form.Item name="addressLine1" label="Address line 1" rules={[{ required: true }]}>
          <Input />
        </Form.Item>
        <Form.Item name="addressLine2" label="Address line 2">
          <Input />
        </Form.Item>
        <Form.Item name="city" label="City" rules={[{ required: true }]}>
          <Input />
        </Form.Item>
        <Form.Item name="region" label="State / region">
          <Input />
        </Form.Item>
        <Form.Item name="postalCode" label="Postal code">
          <Input />
        </Form.Item>
        <Form.Item
          name="country"
          label="Country (ISO 3166-1 alpha-2, e.g. US)"
          rules={[{ required: true, len: 2 }]}
        >
          <Input maxLength={2} style={{ textTransform: 'uppercase' }} />
        </Form.Item>
        <Form.Item name="latitude" label="Latitude (optional)">
          <InputNumber min={-90} max={90} step={0.000001} style={{ width: '100%' }} />
        </Form.Item>
        <Form.Item name="longitude" label="Longitude (optional)">
          <InputNumber min={-180} max={180} step={0.000001} style={{ width: '100%' }} />
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
