import { useEffect, useState } from 'react';
import {
  Button,
  Card,
  DatePicker,
  Descriptions,
  Divider,
  Form,
  Input,
  InputNumber,
  Radio,
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
      <Card>
        <Typography.Title level={2}>{event.title}</Typography.Title>
        <Tag color={eventStatusColor[event.status]}>{event.status}</Tag>
        <Descriptions column={1} style={{ marginTop: 16 }}>
          <Descriptions.Item label="Starts">
            {dayjs(event.startsAt).format('dddd, MMMM D, YYYY · h:mm A')}
          </Descriptions.Item>
          <Descriptions.Item label="Ends">
            {dayjs(event.endsAt).format('dddd, MMMM D, YYYY · h:mm A')}
          </Descriptions.Item>
          <Descriptions.Item label="Currency">{event.currency}</Descriptions.Item>
          {seatMap && (
            <Descriptions.Item label="Capacity">{seatMap.capacity} total</Descriptions.Item>
          )}
        </Descriptions>

        {event.status === 'Draft' && seatMap && (
          <Button
            type="primary"
            style={{ marginTop: 16 }}
            loading={submitting}
            onClick={() => void handlePublish()}
          >
            Publish
          </Button>
        )}
        {event.status === 'Draft' && !seatMap && (
          <Typography.Text type="secondary" style={{ display: 'block', marginTop: 16 }}>
            Define a seat map before publishing.
          </Typography.Text>
        )}
      </Card>

      {event.status === 'Draft' && (
        <Card title="Event details" style={{ marginTop: 24 }}>
          <Form<EventDetailsFormValues>
            form={detailsForm}
            layout="vertical"
            onFinish={(values) => {
              void handleSaveDetails(values);
            }}
          >
            <Form.Item name="description" label="Description">
              <Input.TextArea rows={4} maxLength={4000} showCount />
            </Form.Item>
            <Form.Item name="category" label="Category">
              <Input placeholder="e.g. Concert, Comedy" />
            </Form.Item>
            <Space wrap>
              <Form.Item name="doorsOpenAt" label="Doors open">
                <DatePicker showTime />
              </Form.Item>
              <Form.Item name="endsAt" label="Ends at" rules={[{ required: true }]}>
                <DatePicker showTime />
              </Form.Item>
              <Form.Item name="onSaleAt" label="On sale from">
                <DatePicker showTime />
              </Form.Item>
              <Form.Item
                name="bookingEndsAt"
                label="Booking closes at"
                tooltip="After this time, no new tickets can be held or sold for this event."
              >
                <DatePicker showTime />
              </Form.Item>
            </Space>
            <Form.Item name="ageRestriction" label="Age restriction">
              <Input placeholder="e.g. 18+, All ages" style={{ maxWidth: 240 }} />
            </Form.Item>
            <Form.Item name="videoUrl" label="Video URL (YouTube or Vimeo link)">
              <Input placeholder="https://youtube.com/watch?v=..." />
            </Form.Item>
            <Form.Item name="bannerImageUrl" label="Banner image">
              <Input type="hidden" />
            </Form.Item>
            <Form.Item shouldUpdate>
              {() => {
                const currentUrl: string | undefined = detailsForm.getFieldValue('bannerImageUrl');
                return (
                  <Space direction="vertical">
                    {currentUrl && (
                      <img
                        src={currentUrl}
                        alt="Current banner"
                        style={{ maxWidth: 320, borderRadius: 8 }}
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
                      <Button icon={<UploadOutlined />}>Upload banner image</Button>
                    </Upload>
                  </Space>
                );
              }}
            </Form.Item>

            <Divider>
              Contact details (overrides the tour's defaults; leave blank to use them)
            </Divider>
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

            <Divider>Social links (overrides the tour's defaults)</Divider>
            <SocialLinksEditor />

            <Button type="primary" htmlType="submit" loading={savingDetails}>
              Save details
            </Button>
          </Form>
        </Card>
      )}

      {!seatMap && event.status === 'Draft' && (
        <Card title="Define seat map" style={{ marginTop: 24 }}>
          <Form<SeatMapFormValues>
            layout="vertical"
            initialValues={{ sections: [DEFAULT_SEAT_MAP_SECTION] }}
            onFinish={(values) => {
              void handleDefineSeatMap(values);
            }}
          >
            <Form.Item name="name" label="Seat map name" rules={[{ required: true }]}>
              <Input placeholder="e.g. Main Floor" />
            </Form.Item>

            <Form.List name="sections">
              {(fields, { add, remove }) => (
                <>
                  {fields.map((field) => (
                    <Card key={field.key} size="small" style={{ marginBottom: 12 }}>
                      <Space align="baseline" wrap>
                        <Form.Item
                          name={[field.name, 'name']}
                          rules={[{ required: true, message: 'Required' }]}
                        >
                          <Input placeholder="Section (e.g. Orchestra)" />
                        </Form.Item>
                        <Form.Item
                          name={[field.name, 'priceTier']}
                          rules={[{ required: true, message: 'Required' }]}
                        >
                          <Input placeholder="Price tier" />
                        </Form.Item>
                        <Form.Item
                          name={[field.name, 'priceAmount']}
                          rules={[{ required: true, message: 'Required' }]}
                        >
                          <InputNumber min={0} placeholder="Price" />
                        </Form.Item>
                        <Form.Item name={[field.name, 'allocationType']} initialValue="Reserved">
                          <Radio.Group>
                            <Radio.Button value="Reserved">Reserved seating</Radio.Button>
                            <Radio.Button value="GeneralAdmission">General admission</Radio.Button>
                          </Radio.Group>
                        </Form.Item>
                        {fields.length > 1 && (
                          <MinusCircleOutlined onClick={() => remove(field.name)} />
                        )}
                      </Space>
                      <Form.Item shouldUpdate noStyle>
                        {({ getFieldValue }) => {
                          const allocationType: SeatMapSectionInput['allocationType'] =
                            getFieldValue(['sections', field.name, 'allocationType']) ?? 'Reserved';
                          return allocationType === 'Reserved' ? (
                            <Space wrap>
                              <Form.Item
                                name={[field.name, 'rows']}
                                rules={[{ required: true, message: 'Required' }]}
                              >
                                <InputNumber min={1} placeholder="Rows" />
                              </Form.Item>
                              <Form.Item
                                name={[field.name, 'seatsPerRow']}
                                rules={[{ required: true, message: 'Required' }]}
                              >
                                <InputNumber min={1} placeholder="Seats/row" />
                              </Form.Item>
                            </Space>
                          ) : (
                            <Form.Item
                              name={[field.name, 'capacity']}
                              rules={[{ required: true, message: 'Required' }]}
                            >
                              <InputNumber min={1} placeholder="Total capacity" />
                            </Form.Item>
                          );
                        }}
                      </Form.Item>
                    </Card>
                  ))}
                  <Form.Item>
                    <Button
                      type="dashed"
                      onClick={() => add({ ...DEFAULT_SEAT_MAP_SECTION })}
                      icon={<PlusOutlined />}
                    >
                      Add section
                    </Button>
                  </Form.Item>
                </>
              )}
            </Form.List>

            <Divider />
            <Button type="primary" htmlType="submit" loading={submitting}>
              Create seat map
            </Button>
          </Form>
        </Card>
      )}

      {event.status !== 'Draft' && id && (
        <div style={{ marginTop: 24 }}>
          <SeatBlockPanel eventId={id} />
        </div>
      )}

      <Button type="link" onClick={() => void navigate('/admin')} style={{ marginTop: 16 }}>
        ← Back to events
      </Button>
    </>
  );
}
