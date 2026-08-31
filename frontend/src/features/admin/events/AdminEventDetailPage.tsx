import { useEffect, useState } from 'react';
import {
  Button,
  Card,
  Checkbox,
  Col,
  DatePicker,
  Descriptions,
  Divider,
  Form,
  Input,
  InputNumber,
  Modal,
  Popconfirm,
  Row,
  Select,
  Space,
  Tag,
  Typography,
  Upload,
} from 'antd';
import { DeleteOutlined, EditOutlined, UploadOutlined } from '@ant-design/icons';
import dayjs, { type Dayjs } from 'dayjs';
import { formatEventDateTime } from '../../../utils/eventTime';
import type { AxiosError } from 'axios';
import { useNavigate, useParams } from 'react-router-dom';
import {
  addSeatMapSections,
  defineSeatMap,
  getEvent,
  getSeatMap,
  listEntryGates,
  pauseSales,
  publishEvent,
  removeSeatMapSection,
  resumeSales,
  updateEventDetails,
  updateSeatMapSection,
  type EntryGateResponse,
  type EventResponse,
  type SeatMapResponse,
  type SeatMapSectionInput,
  type SocialLinkInput,
  type UpdateEventDetailsRequest,
} from '../../../services/catalog/catalogApi';
import { uploadImage } from '../../../services/media/mediaApi';
import { DetailSkeleton } from '../../../components/common/skeletons/DetailSkeleton';
import { NotFoundPage } from '../../../components/common/errors/NotFoundPage';
import { ServerErrorPage } from '../../../components/common/errors/ServerErrorPage';
import { PageHeader } from '../../../components/common/layout/PageHeader';
import { eventStatusColor } from '../../../utils/eventStatus';
import { toast } from '../../../components/common/feedback/toast';
import { formatMoney, toMajor, toMinor } from '../../../utils/money';
import { SocialLinksEditor } from '../../../components/common/forms/SocialLinksEditor';
import { SeatBlockPanel } from '../inventory/SeatBlockPanel';
import { TourLegsList } from '../eventGroups/TourLegsList';
import { EntryGatesPanel } from './EntryGatesPanel';
import { QueueSettingsPanel } from './QueueSettingsPanel';
import { PromoCodesPanel } from '../promoCodes/PromoCodesPanel';
import { SeatMapSectionsFields } from './SeatMapSectionsFields';
import { DEFAULT_SEAT_MAP_SECTION } from './seatMapSectionDefaults';

interface SeatMapFormValues {
  name: string;
  sections: SeatMapSectionInput[];
}

interface AddSeatMapSectionsFormValues {
  sections: SeatMapSectionInput[];
}

interface EditSeatMapSectionFormValues {
  sections: [SeatMapSectionInput];
}

interface EventDetailsFormValues {
  description?: string;
  category?: string;
  endsAt: Dayjs;
  doorsOpenAt?: Dayjs;
  onSaleAt?: Dayjs;
  bookingEndsAt?: Dayjs;
  maxTicketsPerBuyer?: number;
  requiresQueue?: boolean;
  taxRatePercent?: number;
  taxLabel?: string;
  /** Major units, as typed — converted to minor on submit. */
  bookingFeePerTicket?: number;
  timeZoneId?: string;
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
 * Every IANA zone the browser knows, straight from the platform's own tz database — so the list
 * cannot go stale against a bundled copy, and the zones offered are exactly the ones
 * `formatEventDateTime` can render. `supportedValuesOf` is not in every engine; falling back to a
 * short list of common zones keeps the field usable rather than empty.
 */
const TIME_ZONE_OPTIONS = (
  typeof Intl.supportedValuesOf === 'function'
    ? Intl.supportedValuesOf('timeZone')
    : ['Asia/Kolkata', 'Asia/Qatar', 'Asia/Dubai', 'Europe/London', 'America/New_York', 'UTC']
).map((zone) => ({ value: zone, label: zone }));

/** Organizer's event detail: define seat map, publish, and (once published) block/unblock seats. */
export function AdminEventDetailPage() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();

  const [event, setEvent] = useState<EventResponse | null>(null);
  const [seatMap, setSeatMap] = useState<SeatMapResponse | null>(null);
  const [entryGates, setEntryGates] = useState<EntryGateResponse[]>([]);
  const [loading, setLoading] = useState(true);
  const [notFound, setNotFound] = useState(false);
  const [loadError, setLoadError] = useState(false);
  const [submitting, setSubmitting] = useState(false);
  const [togglingSales, setTogglingSales] = useState(false);
  const [savingDetails, setSavingDetails] = useState(false);
  const [addingSections, setAddingSections] = useState(false);
  const [editingSectionName, setEditingSectionName] = useState<string | null>(null);
  const [savingSection, setSavingSection] = useState(false);
  const [deletingSectionName, setDeletingSectionName] = useState<string | null>(null);
  const [detailsForm] = Form.useForm<EventDetailsFormValues>();
  const [addSectionsForm] = Form.useForm<AddSeatMapSectionsFormValues>();
  const [editSectionForm] = Form.useForm<EditSeatMapSectionFormValues>();

  const load = (eventId: string) => {
    Promise.all([
      getEvent(eventId),
      getSeatMap(eventId).catch(() => null),
      listEntryGates(eventId).catch(() => []),
    ])
      .then(([eventResult, seatMapResult, gatesResult]) => {
        setEvent(eventResult);
        setSeatMap(seatMapResult);
        setEntryGates(gatesResult);
        setNotFound(false);
        setLoadError(false);
      })
      .catch((error: AxiosError) => {
        if (error.response?.status === 404) {
          setNotFound(true);
        } else {
          setLoadError(true);
        }
      })
      .finally(() => setLoading(false));
  };

  const refreshEntryGates = () => {
    if (id) {
      listEntryGates(id)
        .then(setEntryGates)
        .catch(() => toast.error('Could not load entry gates.'));
    }
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
      requiresQueue: event.requiresQueue,
      taxRatePercent: event.taxRatePercent ?? undefined,
      taxLabel: event.taxLabel ?? undefined,
      bookingFeePerTicket: event.bookingFeePerTicketMinor
        ? toMajor(event.bookingFeePerTicketMinor)
        : undefined,
      timeZoneId: event.timeZoneId ?? undefined,
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

  const handleAddSeatMapSections = async (values: AddSeatMapSectionsFormValues) => {
    if (!id) {
      return;
    }
    setAddingSections(true);
    try {
      await addSeatMapSections(id, { sections: values.sections });
      toast.success('Sections added to the seat map.');
      addSectionsForm.resetFields();
      load(id);
    } catch {
      toast.error(
        'Could not add those sections — check for a duplicate name or an invalid entry gate.',
      );
    } finally {
      setAddingSections(false);
    }
  };

  const openEditSection = (section: SeatMapSectionInput) => {
    setEditingSectionName(section.name);
    editSectionForm.setFieldsValue({ sections: [section] });
  };

  const handleSaveSection = async (values: EditSeatMapSectionFormValues) => {
    if (!id || !editingSectionName) {
      return;
    }
    setSavingSection(true);
    try {
      await updateSeatMapSection(id, {
        currentSectionName: editingSectionName,
        section: values.sections[0],
      });
      toast.success('Section updated.');
      setEditingSectionName(null);
      load(id);
    } catch {
      toast.error(
        'Could not save that section — check for a duplicate name or an invalid entry gate.',
      );
    } finally {
      setSavingSection(false);
    }
  };

  const handleDeleteSection = async (sectionName: string) => {
    if (!id) {
      return;
    }
    setDeletingSectionName(sectionName);
    try {
      await removeSeatMapSection(id, sectionName);
      toast.success('Section removed.');
      load(id);
    } catch {
      toast.error('Could not remove that section.');
    } finally {
      setDeletingSectionName(null);
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
        requiresQueue: values.requiresQueue ?? false,
        taxRatePercent: values.taxRatePercent ?? null,
        taxLabel: values.taxLabel?.trim() || null,
        bookingFeePerTicketMinor: values.bookingFeePerTicket
          ? toMinor(values.bookingFeePerTicket)
          : 0,
        timeZoneId: values.timeZoneId || null,
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

  const handleToggleSales = async () => {
    if (!id || !event) {
      return;
    }
    setTogglingSales(true);
    try {
      if (event.salesPaused) {
        await resumeSales(id);
        toast.success('Sales resumed.');
      } else {
        await pauseSales(id);
        toast.success('Sales paused.');
      }
      load(id);
    } catch {
      toast.error('Could not update sales status for this event.');
    } finally {
      setTogglingSales(false);
    }
  };

  if (loading) {
    return <DetailSkeleton />;
  }

  if (loadError) {
    return <ServerErrorPage />;
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
            {event.salesPaused && <Tag color="warning">Sales paused</Tag>}
          </Space>
        }
        extra={
          <>
            {event.status === 'Draft' && seatMap && (
              <Button type="primary" loading={submitting} onClick={() => void handlePublish()}>
                Publish
              </Button>
            )}
            {event.status === 'Published' && (
              <Button
                danger={!event.salesPaused}
                loading={togglingSales}
                onClick={() => void handleToggleSales()}
              >
                {event.salesPaused ? 'Resume sales' : 'Pause sales'}
              </Button>
            )}
            <Button onClick={() => void navigate('/admin')}>← Back to events</Button>
          </>
        }
      />

      <Card style={{ marginBottom: 24 }}>
        <Descriptions column={{ xs: 1, sm: 2, md: 4 }} size="small">
          <Descriptions.Item label="Starts">
            {formatEventDateTime(event.startsAt, event.timeZoneId)}
          </Descriptions.Item>
          <Descriptions.Item label="Ends">
            {formatEventDateTime(event.endsAt, event.timeZoneId)}
          </Descriptions.Item>
          <Descriptions.Item label="Time zone">{event.timeZoneId ?? 'Not set'}</Descriptions.Item>
          <Descriptions.Item label="Currency">{event.currency}</Descriptions.Item>
          <Descriptions.Item label="Tax">
            {event.taxRatePercent ? (event.taxLabel ?? `${event.taxRatePercent}%`) : 'None'}
          </Descriptions.Item>
          <Descriptions.Item label="Booking fee">
            {event.bookingFeePerTicketMinor
              ? `${formatMoney(event.bookingFeePerTicketMinor, event.currency)} per ticket`
              : 'None'}
          </Descriptions.Item>
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

      {event.eventGroupId && (
        <Card
          title="Part of a tour"
          style={{ marginBottom: 24 }}
          styles={{ body: { padding: 28 } }}
        >
          <TourLegsList eventGroupId={event.eventGroupId} excludeEventId={event.id} />
        </Card>
      )}

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
                <Form.Item
                  name="timeZoneId"
                  label="Venue time zone"
                  tooltip="Buyers see this event's times in this zone, wherever they are. Leave empty to show each reader their own local time."
                >
                  <Select
                    showSearch
                    allowClear
                    placeholder="e.g. Asia/Kolkata"
                    optionFilterProp="value"
                    options={TIME_ZONE_OPTIONS}
                  />
                </Form.Item>
              </Col>
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
                        if (value && onSaleAt && !value.isAfter(onSaleAt)) {
                          return Promise.reject(
                            new Error('Booking closes at must be after On sale from.'),
                          );
                        }
                        if (value && value.isAfter(dayjs(event.startsAt))) {
                          return Promise.reject(
                            new Error('Booking closes at must not be later than the event start.'),
                          );
                        }
                        return Promise.resolve();
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
              <Col xs={24} sm={12} md={6}>
                <Form.Item
                  name="requiresQueue"
                  label=" "
                  valuePropName="checked"
                  tooltip="Gates seat selection behind a virtual waiting room for high-demand on-sales."
                >
                  <Checkbox>Requires queue (waiting room)</Checkbox>
                </Form.Item>
              </Col>
            </Row>

            <Divider />
            <Typography.Title level={5} style={SECTION_HEADING_STYLE}>
              Fees and tax
            </Typography.Title>
            <Row gutter={20}>
              <Col xs={24} sm={12} md={6}>
                <Form.Item
                  name="taxRatePercent"
                  label="Tax rate %"
                  tooltip="Applied to the order total after any discount code. Leave empty for no tax."
                  rules={[{ type: 'number', min: 0, max: 100 }]}
                >
                  <InputNumber
                    min={0}
                    max={100}
                    step={0.5}
                    style={{ width: '100%' }}
                    placeholder="No tax"
                  />
                </Form.Item>
              </Col>
              <Col xs={24} sm={12} md={6}>
                <Form.Item
                  name="taxLabel"
                  label="Tax label"
                  tooltip="What buyers see next to the tax line, e.g. “GST 18%”."
                  rules={[{ max: 50 }]}
                >
                  <Input placeholder="e.g. GST 18%" />
                </Form.Item>
              </Col>
              <Col xs={24} sm={12} md={6}>
                <Form.Item
                  name="bookingFeePerTicket"
                  label={`Booking fee per ticket (${event.currency})`}
                  tooltip="Charged on every ticket, taxed at the rate above, and not refunded if the buyer cancels. Leave empty for none."
                  rules={[{ type: 'number', min: 0 }]}
                >
                  <InputNumber min={0} step={1} style={{ width: '100%' }} placeholder="No fee" />
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

      {event.status === 'Draft' && id && (
        <EntryGatesPanel eventId={id} gates={entryGates} onGateCreated={refreshEntryGates} />
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

            <SeatMapSectionsFields entryGates={entryGates} />

            <Divider />
            <Space style={{ display: 'flex', justifyContent: 'flex-end' }}>
              <Button type="primary" htmlType="submit" loading={submitting}>
                Create seat map
              </Button>
            </Space>
          </Form>
        </Card>
      )}

      {seatMap && event.status === 'Draft' && (
        <Card title="Seat map" style={{ marginBottom: 24 }} styles={{ body: { padding: 28 } }}>
          <Typography.Title level={5} style={SECTION_HEADING_STYLE}>
            {seatMap.name} · {seatMap.capacity} total
          </Typography.Title>
          <Space direction="vertical" style={{ width: '100%', marginBottom: 8 }}>
            {[...new Set(seatMap.seats.map((seat) => seat.section))].map((section) => {
              const seatsInSection = seatMap.seats.filter((seat) => seat.section === section);
              const first = seatsInSection[0];
              return (
                <div
                  key={section}
                  style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}
                >
                  <Typography.Text type="secondary">
                    {section} — Reserved · {first?.priceTier} · {seatsInSection.length} seats
                  </Typography.Text>
                  <Space size="small">
                    <Button
                      type="text"
                      size="small"
                      icon={<EditOutlined />}
                      onClick={() =>
                        openEditSection({
                          name: section,
                          priceTier: first?.priceTier ?? '',
                          priceAmount: first?.priceAmount ?? 0,
                          allocationType: 'Reserved',
                          rows: new Set(seatsInSection.map((seat) => seat.row)).size,
                          seatsPerRow: Math.max(...seatsInSection.map((seat) => seat.number)),
                          entryGateId: first?.entryGateId ?? null,
                        })
                      }
                    />
                    <Popconfirm
                      title="Remove this section?"
                      description="Every seat in it is deleted."
                      onConfirm={() => void handleDeleteSection(section)}
                    >
                      <Button
                        type="text"
                        size="small"
                        danger
                        icon={<DeleteOutlined />}
                        loading={deletingSectionName === section}
                      />
                    </Popconfirm>
                  </Space>
                </div>
              );
            })}
            {seatMap.generalAdmissionSections.map((section) => (
              <div
                key={section.id}
                style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}
              >
                <Typography.Text type="secondary">
                  {section.sectionName} — General admission · {section.priceTier} ·{' '}
                  {section.capacity} capacity
                </Typography.Text>
                <Space size="small">
                  <Button
                    type="text"
                    size="small"
                    icon={<EditOutlined />}
                    onClick={() =>
                      openEditSection({
                        name: section.sectionName,
                        priceTier: section.priceTier,
                        priceAmount: section.priceAmount,
                        allocationType: 'GeneralAdmission',
                        capacity: section.capacity,
                        entryGateId: section.entryGateId,
                      })
                    }
                  />
                  <Popconfirm
                    title="Remove this section?"
                    onConfirm={() => void handleDeleteSection(section.sectionName)}
                  >
                    <Button
                      type="text"
                      size="small"
                      danger
                      icon={<DeleteOutlined />}
                      loading={deletingSectionName === section.sectionName}
                    />
                  </Popconfirm>
                </Space>
              </div>
            ))}
          </Space>

          <Divider />
          <Typography.Title level={5} style={SECTION_HEADING_STYLE}>
            Add more sections
          </Typography.Title>
          <Form<AddSeatMapSectionsFormValues>
            form={addSectionsForm}
            layout="vertical"
            initialValues={{ sections: [DEFAULT_SEAT_MAP_SECTION] }}
            onFinish={(values) => {
              void handleAddSeatMapSections(values);
            }}
          >
            <SeatMapSectionsFields
              entryGates={entryGates}
              existingSectionNames={[
                ...seatMap.seats.map((seat) => seat.section),
                ...seatMap.generalAdmissionSections.map((section) => section.sectionName),
              ]}
            />

            <Divider />
            <Space style={{ display: 'flex', justifyContent: 'flex-end' }}>
              <Button type="primary" htmlType="submit" loading={addingSections}>
                Add sections
              </Button>
            </Space>
          </Form>
        </Card>
      )}

      {id && (
        <PromoCodesPanel
          eventId={id}
          currency={event.currency}
          priceTiers={[
            ...new Set([
              ...(seatMap?.seats.map((seat) => seat.priceTier) ?? []),
              ...(seatMap?.generalAdmissionSections.map((section) => section.priceTier) ?? []),
            ]),
          ]}
        />
      )}

      {event.status !== 'Draft' && id && <SeatBlockPanel eventId={id} />}
      {event.status !== 'Draft' && event.requiresQueue && id && <QueueSettingsPanel eventId={id} />}

      <Modal
        open={editingSectionName !== null}
        onCancel={() => setEditingSectionName(null)}
        title={`Edit section: ${editingSectionName ?? ''}`}
        okText="Save"
        confirmLoading={savingSection}
        onOk={() => editSectionForm.submit()}
        destroyOnClose
      >
        <Form<EditSeatMapSectionFormValues>
          form={editSectionForm}
          layout="vertical"
          onFinish={(values) => {
            void handleSaveSection(values);
          }}
        >
          <SeatMapSectionsFields
            entryGates={entryGates}
            allowAddRemove={false}
            existingSectionNames={[
              ...(seatMap?.seats.map((seat) => seat.section) ?? []),
              ...(seatMap?.generalAdmissionSections.map((section) => section.sectionName) ?? []),
            ].filter((name) => name !== editingSectionName)}
          />
        </Form>
      </Modal>
    </>
  );
}
