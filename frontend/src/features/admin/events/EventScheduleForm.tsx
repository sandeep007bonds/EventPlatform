import { useEffect, useState } from 'react';
import {
  Alert,
  Button,
  Checkbox,
  Col,
  DatePicker,
  Divider,
  Form,
  Input,
  InputNumber,
  Row,
  Select,
  Space,
  Typography,
} from 'antd';
import dayjs, { type Dayjs } from 'dayjs';
import { updateEventDetails, type EventResponse } from '../../../services/catalog/catalogApi';
import { toast } from '../../../components/common/feedback/toast';
import { toMajor, toMinor } from '../../../utils/money';

interface EventScheduleFormProps {
  event: EventResponse;
  /** Called after a successful save, so the parent can re-fetch. */
  onSaved: () => void;
}

interface ScheduleFormValues {
  startsAt: Dayjs;
  endsAt: Dayjs;
  doorsOpenAt?: Dayjs;
  onSaleAt?: Dayjs;
  bookingEndsAt?: Dayjs;
  timeZoneId?: string;
  locationName: string;
  addressLine1: string;
  addressLine2?: string;
  city: string;
  region?: string;
  postalCode?: string;
  country: string;
  latitude?: number;
  longitude?: number;
  maxTicketsPerBuyer?: number;
  requiresQueue?: boolean;
  taxRatePercent?: number;
  taxLabel?: string;
  /** Major units, as typed — converted to minor on submit. */
  bookingFeePerTicket?: number;
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

/**
 * Dates, venue and money — the half of the old form that locks at publish.
 *
 * Every field here is part of what a ticket holder paid for, so after publish the form renders
 * read-only rather than disappearing: an organizer still needs to *see* the booking cutoff they
 * set, and hiding the section makes it look like the setting is gone rather than fixed. The server
 * refuses the write regardless (409), so the read-only state is a courtesy, not the guard.
 */
export function EventScheduleForm({ event, onSaved }: EventScheduleFormProps) {
  const [form] = Form.useForm<ScheduleFormValues>();
  const [saving, setSaving] = useState(false);
  const locked = event.status !== 'Draft';

  useEffect(() => {
    form.setFieldsValue({
      startsAt: dayjs(event.startsAt),
      endsAt: dayjs(event.endsAt),
      doorsOpenAt: event.doorsOpenAt ? dayjs(event.doorsOpenAt) : undefined,
      onSaleAt: event.onSaleAt ? dayjs(event.onSaleAt) : undefined,
      bookingEndsAt: event.bookingEndsAt ? dayjs(event.bookingEndsAt) : undefined,
      timeZoneId: event.timeZoneId ?? undefined,
      locationName: event.locationName,
      addressLine1: event.addressLine1,
      addressLine2: event.addressLine2 ?? undefined,
      city: event.city,
      region: event.region ?? undefined,
      postalCode: event.postalCode ?? undefined,
      country: event.country,
      latitude: event.latitude ?? undefined,
      longitude: event.longitude ?? undefined,
      maxTicketsPerBuyer: event.maxTicketsPerBuyer ?? undefined,
      requiresQueue: event.requiresQueue,
      taxRatePercent: event.taxRatePercent ?? undefined,
      taxLabel: event.taxLabel ?? undefined,
      bookingFeePerTicket: event.bookingFeePerTicketMinor
        ? toMajor(event.bookingFeePerTicketMinor)
        : undefined,
    });
  }, [event, form]);

  const handleSave = async (values: ScheduleFormValues) => {
    setSaving(true);
    try {
      await updateEventDetails(event.id, {
        startsAt: values.startsAt.toISOString(),
        endsAt: values.endsAt.toISOString(),
        doorsOpenAt: values.doorsOpenAt?.toISOString() ?? null,
        onSaleAt: values.onSaleAt?.toISOString() ?? null,
        bookingEndsAt: values.bookingEndsAt?.toISOString() ?? null,
        locationName: values.locationName.trim(),
        addressLine1: values.addressLine1.trim(),
        addressLine2: values.addressLine2?.trim() || null,
        city: values.city.trim(),
        region: values.region?.trim() || null,
        postalCode: values.postalCode?.trim() || null,
        country: values.country.trim().toUpperCase(),
        latitude: values.latitude ?? null,
        longitude: values.longitude ?? null,
        maxTicketsPerBuyer: values.maxTicketsPerBuyer ?? null,
        requiresQueue: values.requiresQueue ?? false,
        taxRatePercent: values.taxRatePercent ?? null,
        taxLabel: values.taxLabel?.trim() || null,
        bookingFeePerTicketMinor: values.bookingFeePerTicket
          ? toMinor(values.bookingFeePerTicket)
          : 0,
        timeZoneId: values.timeZoneId || null,
      });
      toast.success('Schedule and venue saved.');
      onSaved();
    } catch {
      // Covers the 409 the read-only state tries to prevent: this component's idea of the event's
      // status is stale if someone published in another tab.
      toast.error('Could not save — check the dates, or whether the event has been published.');
    } finally {
      setSaving(false);
    }
  };

  return (
    <Form<ScheduleFormValues>
      form={form}
      layout="vertical"
      disabled={locked}
      onFinish={(values) => void handleSave(values)}
    >
      {locked && (
        <Alert
          type="info"
          showIcon
          style={{ marginBottom: 24 }}
          message="Fixed now that the event is published"
          description={
            'Dates, venue, tax and fees are part of what a ticket holder bought, so they stop being ' +
            'editable at publish. The title, description, images and contact details are on the ' +
            'Event page tab and can still be changed. Moving or postponing a live event is a ' +
            'separate process, because buyers have to be told.'
          }
        />
      )}

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
          <Form.Item name="startsAt" label="Starts at" rules={[{ required: true }]}>
            <DatePicker showTime style={{ width: '100%' }} />
          </Form.Item>
        </Col>
        <Col xs={24} sm={12} md={6}>
          <Form.Item
            name="endsAt"
            label="Ends at"
            dependencies={['startsAt']}
            rules={[
              { required: true },
              ({ getFieldValue }) => ({
                validator: (_rule, value: Dayjs | undefined) => {
                  const startsAt = getFieldValue('startsAt') as Dayjs | undefined;
                  return !value || !startsAt || value.isAfter(startsAt)
                    ? Promise.resolve()
                    : Promise.reject(new Error('Ends at must be after Starts at.'));
                },
              }),
            ]}
          >
            <DatePicker showTime style={{ width: '100%' }} />
          </Form.Item>
        </Col>
        <Col xs={24} sm={12} md={6}>
          <Form.Item name="doorsOpenAt" label="Doors open">
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
            dependencies={['onSaleAt', 'startsAt']}
            rules={[
              ({ getFieldValue }) => ({
                validator: (_rule, value: Dayjs | undefined) => {
                  const onSaleAt = getFieldValue('onSaleAt') as Dayjs | undefined;
                  const startsAt = getFieldValue('startsAt') as Dayjs | undefined;
                  if (value && onSaleAt && !value.isAfter(onSaleAt)) {
                    return Promise.reject(
                      new Error('Booking closes at must be after On sale from.'),
                    );
                  }
                  if (value && startsAt && value.isAfter(startsAt)) {
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
        Venue
      </Typography.Title>
      <Row gutter={20}>
        <Col xs={24} md={12}>
          <Form.Item
            name="locationName"
            label="Venue name"
            rules={[{ required: true, message: 'Required' }, { max: 200 }]}
          >
            <Input placeholder="e.g. DY Patil Stadium" />
          </Form.Item>
        </Col>
        <Col xs={24} md={12}>
          <Form.Item
            name="addressLine1"
            label="Address line 1"
            rules={[{ required: true, message: 'Required' }, { max: 200 }]}
          >
            <Input />
          </Form.Item>
        </Col>
        <Col xs={24} md={12}>
          <Form.Item name="addressLine2" label="Address line 2" rules={[{ max: 200 }]}>
            <Input />
          </Form.Item>
        </Col>
        <Col xs={24} sm={12} md={6}>
          <Form.Item
            name="city"
            label="City"
            rules={[{ required: true, message: 'Required' }, { max: 100 }]}
          >
            <Input />
          </Form.Item>
        </Col>
        <Col xs={24} sm={12} md={6}>
          <Form.Item name="region" label="State / region" rules={[{ max: 100 }]}>
            <Input />
          </Form.Item>
        </Col>
        <Col xs={24} sm={12} md={6}>
          <Form.Item name="postalCode" label="Postal code" rules={[{ max: 20 }]}>
            <Input />
          </Form.Item>
        </Col>
        <Col xs={24} sm={12} md={6}>
          <Form.Item
            name="country"
            label="Country"
            tooltip="Two-letter ISO country code, e.g. IN."
            rules={[
              { required: true, message: 'Required' },
              { len: 2, message: 'Two letters' },
            ]}
          >
            <Input placeholder="IN" style={{ textTransform: 'uppercase' }} />
          </Form.Item>
        </Col>
        <Col xs={24} sm={12} md={6}>
          <Form.Item
            name="latitude"
            label="Latitude"
            rules={[{ type: 'number', min: -90, max: 90 }]}
          >
            <InputNumber style={{ width: '100%' }} placeholder="Optional" />
          </Form.Item>
        </Col>
        <Col xs={24} sm={12} md={6}>
          <Form.Item
            name="longitude"
            label="Longitude"
            rules={[{ type: 'number', min: -180, max: 180 }]}
          >
            <InputNumber style={{ width: '100%' }} placeholder="Optional" />
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

      {!locked && (
        <>
          <Divider />
          <Space style={{ display: 'flex', justifyContent: 'flex-end' }}>
            <Button type="primary" htmlType="submit" loading={saving}>
              Save schedule and venue
            </Button>
          </Space>
        </>
      )}
    </Form>
  );
}
