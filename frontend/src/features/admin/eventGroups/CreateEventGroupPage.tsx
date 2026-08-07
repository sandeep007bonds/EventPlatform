import { useState } from 'react';
import { Button, Card, Col, DatePicker, Divider, Form, Input, Row, Space } from 'antd';
import type { Dayjs } from 'dayjs';
import { useNavigate } from 'react-router-dom';
import {
  createEventGroup,
  updateEventGroup,
  type SocialLinkInput,
} from '../../../services/catalog/catalogApi';
import { toast } from '../../../components/common/feedback/toast';
import { PageHeader } from '../../../components/common/layout/PageHeader';
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
      // Straight to the tour's own page, not the list — "create tour once, keep adding legs to
      // it" via the "Add leg" button there (TourDetailPage), not a re-navigate-and-find-it step.
      void navigate(`/admin/tours/${result.id}`);
    } catch {
      toast.error('Could not create the tour.');
    } finally {
      setSubmitting(false);
    }
  };

  return (
    <div style={{ maxWidth: 760 }}>
      <PageHeader
        title="Create tour"
        description="Cluster multiple city/date legs under one promotional umbrella."
      />
      <Card styles={{ body: { padding: 28 } }}>
        <Form<CreateEventGroupFormValues>
          layout="vertical"
          onFinish={(values) => {
            void handleSubmit(values);
          }}
        >
          <Row gutter={20}>
            <Col span={24}>
              <Form.Item name="title" label="Title" rules={[{ required: true }, { max: 200 }]}>
                <Input placeholder="e.g. Coldplay World Tour" size="large" />
              </Form.Item>
            </Col>
            <Col xs={24} sm={12}>
              <Form.Item name="startsAt" label="Overall starts at (optional)">
                <DatePicker showTime style={{ width: '100%' }} />
              </Form.Item>
            </Col>
            <Col xs={24} sm={12}>
              <Form.Item
                name="endsAt"
                label="Overall ends at (optional)"
                dependencies={['startsAt']}
                rules={[
                  ({ getFieldValue }) => ({
                    validator: (_rule, value: Dayjs | undefined) => {
                      const startsAt = getFieldValue('startsAt') as Dayjs | undefined;
                      return !value || !startsAt || value.isAfter(startsAt)
                        ? Promise.resolve()
                        : Promise.reject(
                            new Error('Overall ends at must be after Overall starts at.'),
                          );
                    },
                  }),
                ]}
              >
                <DatePicker showTime style={{ width: '100%' }} />
              </Form.Item>
            </Col>
          </Row>

          <Divider titlePlacement="left">Default contact details (each leg can override)</Divider>
          <Row gutter={20}>
            <Col xs={24} sm={12}>
              <Form.Item name="contactPhone" label="Phone" rules={[{ max: 30 }]}>
                <Input />
              </Form.Item>
            </Col>
            <Col xs={24} sm={12}>
              <Form.Item name="contactMobile" label="Mobile" rules={[{ max: 30 }]}>
                <Input />
              </Form.Item>
            </Col>
            <Col xs={24} sm={12}>
              <Form.Item
                name="contactEmail"
                label="Email"
                rules={[{ type: 'email' }, { max: 200 }]}
              >
                <Input />
              </Form.Item>
            </Col>
            <Col xs={24} sm={12}>
              <Form.Item name="websiteUrl" label="Website" rules={[{ type: 'url' }, { max: 2000 }]}>
                <Input placeholder="https://..." />
              </Form.Item>
            </Col>
          </Row>

          <Divider titlePlacement="left">Social links</Divider>
          <SocialLinksEditor />

          <Divider />
          <Space style={{ display: 'flex', justifyContent: 'flex-end' }}>
            <Button type="primary" htmlType="submit" size="large" loading={submitting}>
              Create tour
            </Button>
          </Space>
        </Form>
      </Card>
    </div>
  );
}
