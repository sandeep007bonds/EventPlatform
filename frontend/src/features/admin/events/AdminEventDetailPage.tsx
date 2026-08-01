import { useEffect, useState } from 'react';
import {
  Button,
  Card,
  Col,
  DatePicker,
  Descriptions,
  Divider,
  Form,
  Input,
  InputNumber,
  Radio,
  Row,
  Space,
  Tag,
  Typography,
  Upload,
} from 'antd';
import { MinusCircleOutlined, PlusOutlined, UploadOutlined } from '@ant-design/icons';
import dayjs, { type Dayjs } from 'dayjs';
import { useNavigate, useParams } from 'react-router-dom';
import {
  defineSeatMap,
  getEvent,
  getSeatMap,
  publishEvent,
  updateEventDetails,
  type EventResponse,
  type SeatMapResponse,
  type SeatMapSectionInput,
  type SocialLinkInput,
  type UpdateEventDetailsRequest,
} from '../../../services/catalog/catalogApi';
import { uploadImage } from '../../../services/media/mediaApi';
import { DetailSkeleton } from '../../../components/common/skeletons/DetailSkeleton';
import { NotFoundPage } from '../../../components/common/errors/NotFoundPage';
import { PageHeader } from '../../../components/common/layout/PageHeader';
import { eventStatusColor } from '../../../utils/eventStatus';
import { toast } from '../../../components/common/feedback/toast';
import { SocialLinksEditor } from '../../../components/common/forms/SocialLinksEditor';
import { SeatBlockPanel } from '../inventory/SeatBlockPanel';

interface SeatMapFormValues {
  name: string;
  sections: SeatMapSectionInput[];
}

interface EventDetailsFormValues {
  description?: string;
  category?: string;
  endsAt: Dayjs;
  doorsOpenAt?: Dayjs;
  onSaleAt?: Dayjs;
  bookingEndsAt?: Dayjs;
  maxTicketsPerBuyer?: number;
  ageRestriction?: string;
  bannerImageUrl?: string;
  videoUrl?: string;
  contactPhone?: string;
  contactMobile?: string;
  contactEmail?: string;
  websiteUrl?: string;
  socialLinks?: SocialLinkInput[];
}

const DEFAULT_SEAT_MAP_SECTION: SeatMapSectionInput = {
  name: '',
  priceTier: '',
  priceAmount: 0,
  allocationType: 'Reserved',
  rows: 1,
  seatsPerRow: 1,
};

const SECTION_HEADING_STYLE = { marginTop: 0, marginBottom: 16 };

/** Organizer's event detail: define seat map, publish, and (once published) block/unblock seats. */
export function AdminEventDetailPage() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();

  const [event, setEvent] = useState<EventResponse | null>(null);
  const [seatMap, setSeatMap] = useState<SeatMapResponse | null>(null);
  const [loading, setLoading] = useState(true);
  const [notFound, setNotFound] = useState(false);
  const [submitting, setSubmitting] = useState(false);
  const [savingDetails, setSavingDetails] = useState(false);
  const [detailsForm] = Form.useForm<EventDetailsFormValues>();

  const load = (eventId: string) => {
    Promise.all([getEvent(eventId), getSeatMap(eventId).catch(() => null)])
      .then(([eventResult, seatMapResult]) => {
        setEvent(eventResult);
        setSeatMap(seatMapResult);
      })
      .catch(() => setNotFound(true))
      .finally(() => setLoading(false));
  };

  useEffect(() => {
    if (id) {
      load(id);
    }
  }, [id]);

  // Form.initialValues only applies on first mount, not on later reloads (e.g. after saving) —
  // sync explicitly so a re-fetched event's details always reflect what's actually saved.
  useEffect(() => {
    if (!event) {
      return;
    }
    detailsForm.setFieldsValue({
      description: event.description ?? undefined,
      category: event.category ?? undefined,
      endsAt: dayjs(event.endsAt),
      doorsOpenAt: event.doorsOpenAt ? dayjs(event.doorsOpenAt) : undefined,
      onSaleAt: event.onSaleAt ? dayjs(event.onSaleAt) : undefined,
      bookingEndsAt: event.bookingEndsAt ? dayjs(event.bookingEndsAt) : undefined,
      maxTicketsPerBuyer: event.maxTicketsPerBuyer ?? undefined,
      ageRestriction: event.ageRestriction ?? undefined,
      bannerImageUrl: event.bannerImageUrl ?? undefined,
      videoUrl: event.videoUrl ?? undefined,
      contactPhone: event.contactPhone ?? undefined,
      contactMobile: event.contactMobile ?? undefined,
      contactEmail: event.contactEmail ?? undefined,
      websiteUrl: event.websiteUrl ?? undefined,
      socialLinks: event.socialLinks,
    });
  }, [event, detailsForm]);

  const handleDefineSeatMap = async (values: SeatMapFormValues) => {
    if (!id) {
      return;
    }
    setSubmitting(true);
    try {
      await defineSeatMap(id, values);
      toast.success('Seat map created.');
      load(id);
    } catch {
      toast.error('Could not create the seat map.');
    } finally {
      setSubmitting(false);
    }
  };

  const handleSaveDetails = async (values: EventDetailsFormValues) => {
    if (!id) {
      return;
    }
    setSavingDetails(true);
    try {
      const request: UpdateEventDetailsRequest = {
        description: values.description ?? null,
        category: values.category ?? null,
        endsAt: values.endsAt.toISOString(),
        doorsOpenAt: values.doorsOpenAt?.toISOString() ?? null,
        onSaleAt: values.onSaleAt?.toISOString() ?? null,
        bookingEndsAt: values.bookingEndsAt?.toISOString() ?? null,
        maxTicketsPerBuyer: values.maxTicketsPerBuyer ?? null,
        ageRestriction: values.ageRestriction ?? null,
        bannerImageUrl: values.bannerImageUrl ?? null,
        videoUrl: values.videoUrl ?? null,
        contactPhone: values.contactPhone ?? null,
        contactMobile: values.contactMobile ?? null,
        contactEmail: values.contactEmail ?? null,
        websiteUrl: values.websiteUrl ?? null,
        socialLinks: values.socialLinks ?? [],
      };
      await updateEventDetails(id, request);
      toast.success('Event details saved.');
      load(id);
    } catch {
      toast.error('Could not save event details.');
    } finally {
      setSavingDetails(false);
    }
  };

  const handlePublish = async () => {
    if (!id) {
      return;
    }
    setSubmitting(true);
    try {
      await publishEvent(id);
      toast.success('Event published.');
      load(id);
    } catch {
      toast.error('Could not publish this event — check it has a seat map and is still a draft.');
    } finally {
      setSubmitting(false);
    }
  };

  if (loading) {
    return <DetailSkeleton />;
  }

  if (notFound || !event) {
    return <NotFoundPage />;
  }

  return (
    <>
      <PageHeader
        title={
          <Space align="center">
            {event.title}
            <Tag color={eventStatusColor[event.status]} style={{ marginLeft: 4 }}>
              {event.status}
            </Tag>
          </Space>
        }
        extra={
          <>
            {event.status === 'Draft' && seatMap && (
              <Button type="primary" loading={submitting} onClick={() => void handlePublish()}>
                Publish
              </Button>
            )}
            <Button onClick={() => void navigate('/admin')}>← Back to events</Button>
          </>
        }
      />

      <Card style={{ marginBottom: 24 }}>
        <Descriptions column={{ xs: 1, sm: 2, md: 4 }} size="small">
          <Descriptions.Item label="Starts">
            {dayjs(event.startsAt).format('MMM D, YYYY · h:mm A')}
          </Descriptions.Item>
          <Descriptions.Item label="Ends">
            {dayjs(event.endsAt).format('MMM D, YYYY · h:mm A')}
          </Descriptions.Item>
          <Descriptions.Item label="Currency">{event.currency}</Descriptions.Item>
          <Descriptions.Item label="Capacity">
            {seatMap ? `${seatMap.capacity} total` : '—'}
          </Descriptions.Item>
        </Descriptions>
        {event.status === 'Draft' && !seatMap && (
          <Typography.Text type="warning" style={{ display: 'block', marginTop: 12 }}>
            Define a seat map below before you can publish.
          </Typography.Text>
        )}
      </Card>

      {event.status === 'Draft' && (
        <Card title="Event details" style={{ marginBottom: 24 }} styles={{ body: { padding: 28 } }}>
          <Form<EventDetailsFormValues>
            form={detailsForm}
            layout="vertical"
            onFinish={(values) => {
              void handleSaveDetails(values);
            }}
          >
            <Typography.Title level={5} style={SECTION_HEADING_STYLE}>
              Basics
            </Typography.Title>
            <Row gutter={20}>
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
              Schedule
            </Typography.Title>
            <Row gutter={20}>
              <Col xs={24} sm={12} md={6}>
                <Form.Item name="doorsOpenAt" label="Doors open">
                  <DatePicker showTime style={{ width: '100%' }} />
                </Form.Item>
              </Col>
              <Col xs={24} sm={12} md={6}>
                <Form.Item
                  name="endsAt"
                  label="Ends at"
                  rules={[
                    { required: true },
                    {
                      validator: (_rule, value: Dayjs | undefined) =>
                        !value || value.isAfter(dayjs(event.startsAt))
                          ? Promise.resolve()
                          : Promise.reject(new Error('Ends at must be after Starts at.')),
                    },
                  ]}
                >
                  <DatePicker showTime style={{ width: '100%' }} />
                </Form.Item>
              </Col>
              <Col xs={24} sm={12} md={6}>
                <Form.Item name="onSaleAt" label="On sale from">
                  <DatePicker showTime style={{ width: '100%' }} />
                </Form.Item>
              </Col>
              <Col xs={24} sm={12} md={6}>
                <Form.Item
                  name="bookingEndsAt"
                  label="Booking closes at"
                  tooltip="After this time, no new tickets can be held or sold for this event."
                  dependencies={['onSaleAt']}
                  rules={[
                    ({ getFieldValue }) => ({
                      validator: (_rule, value: Dayjs | undefined) => {
                        const onSaleAt = getFieldValue('onSaleAt') as Dayjs | undefined;
                        return !value || !onSaleAt || value.isAfter(onSaleAt)
                          ? Promise.resolve()
                          : Promise.reject(
                              new Error('Booking closes at must be after On sale from.'),
                            );
                      },
                    }),
                  ]}
                >
                  <DatePicker showTime style={{ width: '100%' }} />
                </Form.Item>
              </Col>
              <Col xs={24} sm={12} md={6}>
                <Form.Item
                  name="maxTicketsPerBuyer"
                  label="Max tickets per buyer (optional)"
                  tooltip="Sums a buyer's held and purchased tickets for this event across all their orders."
                  rules={[{ type: 'number', min: 1 }]}
                >
                  <InputNumber min={1} style={{ width: '100%' }} placeholder="No limit" />
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
                    const currentUrl: string | undefined =
                      detailsForm.getFieldValue('bannerImageUrl');
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
                                detailsForm.setFieldValue('bannerImageUrl', url);
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
                <Form.Item
                  name="contactEmail"
                  label="Email"
                  rules={[{ type: 'email' }, { max: 200 }]}
                >
                  <Input />
                </Form.Item>
              </Col>
              <Col xs={24} sm={12}>
                <Form.Item
                  name="websiteUrl"
                  label="Website"
                  rules={[{ type: 'url' }, { max: 2000 }]}
                >
                  <Input placeholder="https://..." />
                </Form.Item>
              </Col>
            </Row>

            <Divider />
            <Typography.Title level={5} style={SECTION_HEADING_STYLE}>
              Social links
            </Typography.Title>
            <SocialLinksEditor />

            <Divider />
            <Space style={{ display: 'flex', justifyContent: 'flex-end' }}>
              <Button type="primary" htmlType="submit" loading={savingDetails}>
                Save details
              </Button>
            </Space>
          </Form>
        </Card>
      )}

      {!seatMap && event.status === 'Draft' && (
        <Card
          title="Define seat map"
          style={{ marginBottom: 24 }}
          styles={{ body: { padding: 28 } }}
        >
          <Form<SeatMapFormValues>
            layout="vertical"
            initialValues={{ sections: [DEFAULT_SEAT_MAP_SECTION] }}
            onFinish={(values) => {
              void handleDefineSeatMap(values);
            }}
          >
            <Form.Item name="name" label="Seat map name" rules={[{ required: true }, { max: 200 }]}>
              <Input placeholder="e.g. Main Floor" style={{ maxWidth: 360 }} />
            </Form.Item>

            <Form.List name="sections">
              {(fields, { add, remove }) => (
                <>
                  {fields.map((field, index) => (
                    <Card
                      key={field.key}
                      size="small"
                      title={`Section ${index + 1}`}
                      style={{ marginBottom: 16 }}
                      extra={
                        fields.length > 1 && (
                          <MinusCircleOutlined onClick={() => remove(field.name)} />
                        )
                      }
                    >
                      <Row gutter={16}>
                        <Col xs={24} sm={8}>
                          <Form.Item
                            name={[field.name, 'name']}
                            label="Section name"
                            rules={[
                              { required: true, message: 'Required' },
                              { max: 100 },
                              ({ getFieldValue }) => ({
                                validator: (_rule, value: string | undefined) => {
                                  if (!value) {
                                    return Promise.resolve();
                                  }
                                  const sections =
                                    (getFieldValue('sections') as
                                      SeatMapSectionInput[] | undefined) ?? [];
                                  const isDuplicate = sections.some(
                                    (section, i) =>
                                      i !== field.name &&
                                      section?.name &&
                                      section.name.trim().toLowerCase() ===
                                        value.trim().toLowerCase(),
                                  );
                                  return isDuplicate
                                    ? Promise.reject(new Error('Section names must be unique.'))
                                    : Promise.resolve();
                                },
                              }),
                            ]}
                          >
                            <Input placeholder="e.g. Orchestra" />
                          </Form.Item>
                        </Col>
                        <Col xs={24} sm={8}>
                          <Form.Item
                            name={[field.name, 'priceTier']}
                            label="Price tier"
                            rules={[{ required: true, message: 'Required' }, { max: 50 }]}
                          >
                            <Input placeholder="e.g. Gold" />
                          </Form.Item>
                        </Col>
                        <Col xs={24} sm={8}>
                          <Form.Item
                            name={[field.name, 'priceAmount']}
                            label="Price"
                            rules={[
                              { required: true, message: 'Required' },
                              { type: 'number', min: 0 },
                            ]}
                          >
                            <InputNumber min={0} style={{ width: '100%' }} />
                          </Form.Item>
                        </Col>
                        <Col span={24}>
                          <Form.Item
                            name={[field.name, 'allocationType']}
                            label="Allocation"
                            initialValue="Reserved"
                          >
                            <Radio.Group>
                              <Radio.Button value="Reserved">Reserved seating</Radio.Button>
                              <Radio.Button value="GeneralAdmission">
                                General admission
                              </Radio.Button>
                            </Radio.Group>
                          </Form.Item>
                        </Col>
                        <Form.Item shouldUpdate noStyle>
                          {({ getFieldValue }) => {
                            const allocationType: SeatMapSectionInput['allocationType'] =
                              getFieldValue(['sections', field.name, 'allocationType']) ??
                              'Reserved';
                            return allocationType === 'Reserved' ? (
                              <>
                                <Col xs={12} sm={6}>
                                  <Form.Item
                                    name={[field.name, 'rows']}
                                    label="Rows"
                                    rules={[
                                      { required: true, message: 'Required' },
                                      { type: 'number', min: 1, max: 500 },
                                    ]}
                                  >
                                    <InputNumber min={1} max={500} style={{ width: '100%' }} />
                                  </Form.Item>
                                </Col>
                                <Col xs={12} sm={6}>
                                  <Form.Item
                                    name={[field.name, 'seatsPerRow']}
                                    label="Seats per row"
                                    rules={[
                                      { required: true, message: 'Required' },
                                      { type: 'number', min: 1, max: 500 },
                                    ]}
                                  >
                                    <InputNumber min={1} max={500} style={{ width: '100%' }} />
                                  </Form.Item>
                                </Col>
                              </>
                            ) : (
                              <Col xs={24} sm={12}>
                                <Form.Item
                                  name={[field.name, 'capacity']}
                                  label="Total capacity"
                                  rules={[
                                    { required: true, message: 'Required' },
                                    { type: 'number', min: 1, max: 1000000 },
                                  ]}
                                >
                                  <InputNumber min={1} max={1000000} style={{ width: '100%' }} />
                                </Form.Item>
                              </Col>
                            );
                          }}
                        </Form.Item>
                      </Row>
                    </Card>
                  ))}
                  <Button
                    type="dashed"
                    block
                    onClick={() => add({ ...DEFAULT_SEAT_MAP_SECTION })}
                    icon={<PlusOutlined />}
                    style={{ marginBottom: 16 }}
                  >
                    Add section
                  </Button>
                </>
              )}
            </Form.List>

            <Divider />
            <Space style={{ display: 'flex', justifyContent: 'flex-end' }}>
              <Button type="primary" htmlType="submit" loading={submitting}>
                Create seat map
              </Button>
            </Space>
          </Form>
        </Card>
      )}

      {event.status !== 'Draft' && id && <SeatBlockPanel eventId={id} />}
    </>
  );
}
