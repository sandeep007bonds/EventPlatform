import { useEffect, useState } from 'react';
import { Button, Col, Divider, Form, Input, Row, Space, Typography, Upload } from 'antd';
import { UploadOutlined } from '@ant-design/icons';
import {
  updateEventPresentation,
  type EventResponse,
  type SocialLinkInput,
} from '../../../services/catalog/catalogApi';
import { uploadImage } from '../../../services/media/mediaApi';
import { toast } from '../../../components/common/feedback/toast';
import { StickyActionBar } from '../../../components/common/layout/StickyActionBar';
import { SocialLinksEditor } from '../../../components/common/forms/SocialLinksEditor';

interface EventPresentationFormProps {
  event: EventResponse;
  /** Called after a successful save, so the parent can re-fetch. */
  onSaved: () => void;
}

interface PresentationFormValues {
  title: string;
  description?: string;
  category?: string;
  ageRestriction?: string;
  bannerImageUrl?: string;
  videoUrl?: string;
  contactPhone?: string;
  contactMobile?: string;
  contactEmail?: string;
  websiteUrl?: string;
  socialLinks?: SocialLinkInput[];
}

const SECTION_HEADING_STYLE = { marginTop: 0, marginBottom: 16 };

/**
 * How the event is presented — the half of the old "Event details" form that stays editable for
 * the life of the event.
 *
 * Nothing here changes what a ticket holder bought, so there is no draft-only guard and no
 * disabled state after publish: fixing a typo in a title or swapping a banner mid-sale is ordinary
 * work, and the audit fields record who did it.
 */
export function EventPresentationForm({ event, onSaved }: EventPresentationFormProps) {
  const [form] = Form.useForm<PresentationFormValues>();
  const [saving, setSaving] = useState(false);

  // initialValues only applies on first mount, so a re-fetch after saving would otherwise leave
  // the form showing what was typed rather than what was stored.
  useEffect(() => {
    form.setFieldsValue({
      title: event.title,
      description: event.description ?? undefined,
      category: event.category ?? undefined,
      ageRestriction: event.ageRestriction ?? undefined,
      bannerImageUrl: event.bannerImageUrl ?? undefined,
      videoUrl: event.videoUrl ?? undefined,
      contactPhone: event.contactPhone ?? undefined,
      contactMobile: event.contactMobile ?? undefined,
      contactEmail: event.contactEmail ?? undefined,
      websiteUrl: event.websiteUrl ?? undefined,
      socialLinks: event.socialLinks,
    });
  }, [event, form]);

  const handleSave = async (values: PresentationFormValues) => {
    setSaving(true);
    try {
      await updateEventPresentation(event.id, {
        title: values.title.trim(),
        description: values.description ?? null,
        category: values.category ?? null,
        ageRestriction: values.ageRestriction ?? null,
        bannerImageUrl: values.bannerImageUrl ?? null,
        videoUrl: values.videoUrl ?? null,
        contactPhone: values.contactPhone ?? null,
        contactMobile: values.contactMobile ?? null,
        contactEmail: values.contactEmail ?? null,
        websiteUrl: values.websiteUrl ?? null,
        socialLinks: values.socialLinks ?? [],
      });
      toast.success('Event page saved.');
      onSaved();
    } catch {
      toast.error('Could not save the event page.');
    } finally {
      setSaving(false);
    }
  };

  return (
    <Form<PresentationFormValues>
      form={form}
      layout="vertical"
      onFinish={(values) => void handleSave(values)}
    >
      <Typography.Title level={5} style={SECTION_HEADING_STYLE}>
        Basics
      </Typography.Title>
      <Row gutter={20}>
        <Col span={24}>
          <Form.Item
            name="title"
            label="Title"
            rules={[{ required: true, message: 'Required' }, { max: 200 }]}
          >
            <Input placeholder="e.g. Coldplay — Music of the Spheres, Mumbai" />
          </Form.Item>
        </Col>
        <Col span={24}>
          <Form.Item name="description" label="Description" rules={[{ max: 4000 }]}>
            <Input.TextArea rows={4} maxLength={4000} showCount />
          </Form.Item>
        </Col>
        <Col xs={24} sm={12}>
          <Form.Item name="category" label="Category" rules={[{ max: 100 }]}>
            <Input placeholder="e.g. Concert, Comedy" />
          </Form.Item>
        </Col>
        <Col xs={24} sm={12}>
          <Form.Item name="ageRestriction" label="Age restriction" rules={[{ max: 50 }]}>
            <Input placeholder="e.g. 18+, All ages" />
          </Form.Item>
        </Col>
      </Row>

      <Divider />
      <Typography.Title level={5} style={SECTION_HEADING_STYLE}>
        Media
      </Typography.Title>
      <Row gutter={20}>
        <Col xs={24} md={12}>
          <Form.Item
            name="videoUrl"
            label="Video URL (YouTube or Vimeo link)"
            rules={[{ max: 2000 }]}
          >
            <Input placeholder="https://youtube.com/watch?v=..." />
          </Form.Item>
        </Col>
        <Col xs={24} md={12}>
          <Form.Item
            name="bannerImageUrl"
            label="Banner image"
            rules={[{ max: 2000 }]}
            style={{ marginBottom: 0 }}
          >
            <Input type="hidden" />
          </Form.Item>
          <Form.Item shouldUpdate noStyle>
            {() => {
              const currentUrl: string | undefined = form.getFieldValue('bannerImageUrl');
              return (
                <Space align="center" style={{ marginBottom: 24 }}>
                  {currentUrl && (
                    <img
                      src={currentUrl}
                      alt="Current banner"
                      style={{ height: 42, borderRadius: 6, objectFit: 'cover' }}
                    />
                  )}
                  <Upload
                    accept="image/png,image/jpeg,image/webp,image/gif"
                    maxCount={1}
                    showUploadList={false}
                    customRequest={(options) => {
                      const { file, onSuccess, onError } = options;
                      uploadImage(file as File)
                        .then(({ url }) => {
                          form.setFieldValue('bannerImageUrl', url);
                          onSuccess?.(url);
                        })
                        .catch((error: unknown) => {
                          onError?.(error as Error);
                          toast.error('Image upload failed.');
                        });
                    }}
                  >
                    <Button icon={<UploadOutlined />}>
                      {currentUrl ? 'Replace banner image' : 'Upload banner image'}
                    </Button>
                  </Upload>
                </Space>
              );
            }}
          </Form.Item>
        </Col>
      </Row>

      <Divider />
      <Typography.Title level={5} style={SECTION_HEADING_STYLE}>
        Contact details
      </Typography.Title>
      <Typography.Text type="secondary" style={{ display: 'block', marginBottom: 16 }}>
        Overrides the tour's defaults, if any — leave blank to use them.
      </Typography.Text>
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
          <Form.Item name="contactEmail" label="Email" rules={[{ type: 'email' }, { max: 200 }]}>
            <Input />
          </Form.Item>
        </Col>
        <Col xs={24} sm={12}>
          <Form.Item name="websiteUrl" label="Website" rules={[{ type: 'url' }, { max: 2000 }]}>
            <Input placeholder="https://..." />
          </Form.Item>
        </Col>
      </Row>

      <Divider />
      <Typography.Title level={5} style={SECTION_HEADING_STYLE}>
        Social links
      </Typography.Title>
      <SocialLinksEditor />

      <StickyActionBar bleed={28}>
        <Button type="primary" htmlType="submit" loading={saving}>
          Save event page
        </Button>
      </StickyActionBar>
    </Form>
  );
}
