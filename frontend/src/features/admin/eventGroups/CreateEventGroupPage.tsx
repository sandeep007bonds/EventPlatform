import { useState } from 'react';
import { Button, Card, DatePicker, Divider, Form, Input, Space, Typography } from 'antd';
import type { Dayjs } from 'dayjs';
import { useNavigate } from 'react-router-dom';
import {
  createEventGroup,
  updateEventGroup,
  type SocialLinkInput,
} from '../../../services/catalog/catalogApi';
import { toast } from '../../../components/common/feedback/toast';
import { SocialLinksEditor } from '../../../components/common/forms/SocialLinksEditor';

interface CreateEventGroupFormValues {
  title: string;
  startsAt?: Dayjs;
  endsAt?: Dayjs;
  contactPhone?: string;
  contactMobile?: string;
  contactEmail?: string;
  websiteUrl?: string;
  socialLinks?: SocialLinkInput[];
}

/**
 * Creates a new tour (event group) for the caller's tenant. Dates and contact/social fields are
 * the tour-level defaults each leg falls back to unless it sets its own (per-leg override).
 */
export function CreateEventGroupPage() {
  const navigate = useNavigate();
  const [submitting, setSubmitting] = useState(false);

  const handleSubmit = async (values: CreateEventGroupFormValues) => {
    setSubmitting(true);
    try {
      const result = await createEventGroup({ title: values.title });

      // Create is title-only server-side; the rest is set via the same Update the tour's
      // "Edit tour" flow would use — applied immediately so the fields aren't silently dropped.
      const hasMoreFields =
        values.startsAt ||
        values.endsAt ||
        values.contactPhone ||
        values.contactMobile ||
        values.contactEmail ||
        values.websiteUrl ||
        (values.socialLinks && values.socialLinks.length > 0);

      if (hasMoreFields) {
        await updateEventGroup(result.id, {
          title: values.title,
          startsAt: values.startsAt?.toISOString() ?? null,
          endsAt: values.endsAt?.toISOString() ?? null,
          contactPhone: values.contactPhone ?? null,
          contactMobile: values.contactMobile ?? null,
          contactEmail: values.contactEmail ?? null,
          websiteUrl: values.websiteUrl ?? null,
          socialLinks: values.socialLinks ?? [],
        });
      }

      toast.success('Tour created.');
      void navigate('/admin/tours');
    } catch {
      toast.error('Could not create the tour.');
    } finally {
      setSubmitting(false);
    }
  };

  return (
    <Card style={{ maxWidth: 560 }}>
      <Typography.Title level={3}>Create tour</Typography.Title>
      <Form<CreateEventGroupFormValues>
        layout="vertical"
        onFinish={(values) => {
          void handleSubmit(values);
        }}
      >
        <Form.Item name="title" label="Title" rules={[{ required: true }]}>
          <Input placeholder="e.g. Coldplay World Tour" />
        </Form.Item>

        <Space wrap>
          <Form.Item name="startsAt" label="Overall starts at (optional)">
            <DatePicker showTime />
          </Form.Item>
          <Form.Item name="endsAt" label="Overall ends at (optional)">
            <DatePicker showTime />
          </Form.Item>
        </Space>

        <Divider>Default contact details (each leg can override)</Divider>
        <Form.Item name="contactPhone" label="Phone">
          <Input />
        </Form.Item>
        <Form.Item name="contactMobile" label="Mobile">
          <Input />
        </Form.Item>
        <Form.Item name="contactEmail" label="Email" rules={[{ type: 'email' }]}>
          <Input />
        </Form.Item>
        <Form.Item name="websiteUrl" label="Website" rules={[{ type: 'url' }]}>
          <Input placeholder="https://..." />
        </Form.Item>

        <Divider>Social links</Divider>
        <SocialLinksEditor />

        <Form.Item>
          <Button type="primary" htmlType="submit" block loading={submitting}>
            Create
          </Button>
        </Form.Item>
      </Form>
    </Card>
  );
}
