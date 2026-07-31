import { useState } from 'react';
import { Button, Form, Input, InputNumber } from 'antd';
import type { VenueRequest, VenueResponse } from '../../../services/catalog/catalogApi';

interface VenueFormValues {
  name: string;
  addressLine1: string;
  addressLine2?: string;
  city: string;
  region?: string;
  postalCode?: string;
  country: string;
  latitude?: number;
  longitude?: number;
  capacity?: number;
}

/** Shared venue create/edit form — used standalone on {@link CreateVenuePage} and inline in a modal from `VenuePicker`. */
export function VenueForm({
  initialValues,
  onSubmit,
  onCancel,
  submitLabel = 'Save',
}: {
  initialValues?: VenueResponse;
  onSubmit: (request: VenueRequest) => Promise<void>;
  onCancel?: () => void;
  submitLabel?: string;
}) {
  const [submitting, setSubmitting] = useState(false);

  const handleFinish = async (values: VenueFormValues) => {
    setSubmitting(true);
    try {
      await onSubmit({
        name: values.name,
        addressLine1: values.addressLine1,
        addressLine2: values.addressLine2 ?? null,
        city: values.city,
        region: values.region ?? null,
        postalCode: values.postalCode ?? null,
        country: values.country,
        latitude: values.latitude ?? null,
        longitude: values.longitude ?? null,
        capacity: values.capacity ?? null,
      });
    } finally {
      setSubmitting(false);
    }
  };

  return (
    <Form<VenueFormValues>
      layout="vertical"
      initialValues={
        initialValues && {
          name: initialValues.name,
          addressLine1: initialValues.addressLine1,
          addressLine2: initialValues.addressLine2 ?? undefined,
          city: initialValues.city,
          region: initialValues.region ?? undefined,
          postalCode: initialValues.postalCode ?? undefined,
          country: initialValues.country,
          latitude: initialValues.latitude ?? undefined,
          longitude: initialValues.longitude ?? undefined,
          capacity: initialValues.capacity ?? undefined,
        }
      }
      onFinish={(values) => {
        void handleFinish(values);
      }}
    >
      <Form.Item name="name" label="Venue name" rules={[{ required: true }]}>
        <Input />
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
      <Form.Item name="capacity" label="Nominal capacity (optional)">
        <InputNumber min={1} style={{ width: '100%' }} />
      </Form.Item>
      <Form.Item name="latitude" label="Latitude (optional)">
        <InputNumber min={-90} max={90} step={0.000001} style={{ width: '100%' }} />
      </Form.Item>
      <Form.Item name="longitude" label="Longitude (optional)">
        <InputNumber min={-180} max={180} step={0.000001} style={{ width: '100%' }} />
      </Form.Item>
      <Form.Item>
        <Button type="primary" htmlType="submit" loading={submitting} block={!onCancel}>
          {submitLabel}
        </Button>
        {onCancel && (
          <Button style={{ marginLeft: 8 }} onClick={onCancel}>
            Cancel
          </Button>
        )}
      </Form.Item>
    </Form>
  );
}
