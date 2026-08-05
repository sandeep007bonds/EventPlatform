import { useEffect, useState } from 'react';
import {
  Button,
  Card,
  Col,
  DatePicker,
  Divider,
  Form,
  Input,
  InputNumber,
  Row,
  Select,
  Space,
} from 'antd';
import dayjs, { type Dayjs } from 'dayjs';
import { useNavigate } from 'react-router-dom';
import { createEvent, getEventGroup } from '../../../services/catalog/catalogApi';
import { toast } from '../../../components/common/feedback/toast';
import { PageHeader } from '../../../components/common/layout/PageHeader';
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
  maxTicketsPerBuyer?: number;
}

const CURRENCIES = ['USD', 'EUR', 'GBP', 'INR'];

/** Creates a new draft event for the caller's tenant. */
export function CreateEventPage() {
  const navigate = useNavigate();
  const [form] = Form.useForm<CreateEventFormValues>();
  const [submitting, setSubmitting] = useState(false);
  // The tour's own advertised date range, if the organizer picked one above — a leg's dates must
  // lie inside it (mirrored server-side; this is the inline-feedback copy, not the enforcement).
  const [groupRange, setGroupRange] = useState<{
    startsAt: string | null;
    endsAt: string | null;
  } | null>(null);
  const selectedGroupId = Form.useWatch('eventGroupId', form);

  useEffect(() => {
    let cancelled = false;
    const fetchRange = selectedGroupId
      ? getEventGroup(selectedGroupId).then((group) => ({
          startsAt: group.startsAt,
          endsAt: group.endsAt,
        }))
      : Promise.resolve(null);

    fetchRange
      .then((range) => {
        if (!cancelled) {
          setGroupRange(range);
        }
      })
      .catch(() => {
        if (!cancelled) {
          setGroupRange(null);
        }
      });

    return () => {
      cancelled = true;
    };
  }, [selectedGroupId]);

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
        maxTicketsPerBuyer: values.maxTicketsPerBuyer ?? null,
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
    <div style={{ maxWidth: 760 }}>
      <PageHeader
        title="Create event"
        description="Set the basics now — you can add media, description, and pricing after."
      />
      <Card styles={{ body: { padding: 28 } }}>
        <Form<CreateEventFormValues>
          form={form}
          layout="vertical"
          initialValues={{ currency: 'USD' }}
          onFinish={(values) => {
            void handleSubmit(values);
          }}
        >
          <Row gutter={20}>
            <Col span={24}>
              <Form.Item name="title" label="Title" rules={[{ required: true }, { max: 200 }]}>
                <Input placeholder="e.g. Coldplay: Music of the Spheres" size="large" />
              </Form.Item>
            </Col>
            <Col xs={24} sm={8}>
              <Form.Item
                name="startsAt"
                label="Starts at"
                rules={[
                  { required: true },
                  {
                    validator: (_rule, value: Dayjs | undefined) => {
                      if (value && !value.isAfter(dayjs())) {
                        return Promise.reject(new Error('Starts at must be in the future.'));
                      }
                      if (
                        value &&
                        groupRange?.startsAt &&
                        value.isBefore(dayjs(groupRange.startsAt))
                      ) {
                        return Promise.reject(
                          new Error("Starts at must be on or after the tour's own start date."),
                        );
                      }
                      return Promise.resolve();
                    },
                  },
                ]}
              >
                <DatePicker showTime style={{ width: '100%' }} />
              </Form.Item>
            </Col>
            <Col xs={24} sm={8}>
              <Form.Item
                name="endsAt"
                label="Ends at"
                dependencies={['startsAt']}
                rules={[
                  { required: true },
                  ({ getFieldValue }) => ({
                    validator: (_rule, value: Dayjs | undefined) => {
                      const startsAt = getFieldValue('startsAt') as Dayjs | undefined;
                      if (value && startsAt && !value.isAfter(startsAt)) {
                        return Promise.reject(new Error('Ends at must be after Starts at.'));
                      }
                      if (value && groupRange?.endsAt && value.isAfter(dayjs(groupRange.endsAt))) {
                        return Promise.reject(
                          new Error("Ends at must be on or before the tour's own end date."),
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
            <Col xs={24} sm={8}>
              <Form.Item name="currency" label="Currency" rules={[{ required: true }]}>
                <Select options={CURRENCIES.map((code) => ({ value: code, label: code }))} />
              </Form.Item>
            </Col>
            <Col span={24}>
              <Form.Item name="eventGroupId" label="Part of a tour? (optional)">
                <EventGroupPicker />
              </Form.Item>
            </Col>
            <Col xs={24} sm={8}>
              <Form.Item
                name="maxTicketsPerBuyer"
                label="Max tickets per buyer (optional)"
                rules={[{ type: 'number', min: 1 }]}
              >
                <InputNumber min={1} style={{ width: '100%' }} placeholder="No limit" />
              </Form.Item>
            </Col>
          </Row>

          <Divider titlePlacement="left">Location</Divider>
          <Row gutter={20}>
            <Col xs={24} sm={12}>
              <Form.Item
                name="locationName"
                label="Venue name"
                rules={[{ required: true }, { max: 200 }]}
              >
                <Input placeholder="e.g. Wankhede Stadium" />
              </Form.Item>
            </Col>
            <Col xs={24} sm={12}>
              <Form.Item
                name="addressLine1"
                label="Address line 1"
                rules={[{ required: true }, { max: 200 }]}
              >
                <Input />
              </Form.Item>
            </Col>
            <Col xs={24} sm={12}>
              <Form.Item name="addressLine2" label="Address line 2" rules={[{ max: 200 }]}>
                <Input />
              </Form.Item>
            </Col>
            <Col xs={24} sm={12}>
              <Form.Item name="city" label="City" rules={[{ required: true }, { max: 100 }]}>
                <Input />
              </Form.Item>
            </Col>
            <Col xs={24} sm={8}>
              <Form.Item name="region" label="State / region" rules={[{ max: 100 }]}>
                <Input />
              </Form.Item>
            </Col>
            <Col xs={24} sm={8}>
              <Form.Item name="postalCode" label="Postal code" rules={[{ max: 20 }]}>
                <Input />
              </Form.Item>
            </Col>
            <Col xs={24} sm={8}>
              <Form.Item
                name="country"
                label="Country (ISO 3166-1 alpha-2)"
                rules={[{ required: true, len: 2 }]}
              >
                <Input maxLength={2} placeholder="US" style={{ textTransform: 'uppercase' }} />
              </Form.Item>
            </Col>
            <Col xs={24} sm={12}>
              <Form.Item
                name="latitude"
                label="Latitude (optional)"
                rules={[{ type: 'number', min: -90, max: 90 }]}
              >
                <InputNumber min={-90} max={90} step={0.000001} style={{ width: '100%' }} />
              </Form.Item>
            </Col>
            <Col xs={24} sm={12}>
              <Form.Item
                name="longitude"
                label="Longitude (optional)"
                rules={[{ type: 'number', min: -180, max: 180 }]}
              >
                <InputNumber min={-180} max={180} step={0.000001} style={{ width: '100%' }} />
              </Form.Item>
            </Col>
          </Row>

          <Divider />
          <Space style={{ display: 'flex', justifyContent: 'flex-end' }}>
            <Button type="primary" htmlType="submit" size="large" loading={submitting}>
              Create event
            </Button>
          </Space>
        </Form>
      </Card>
    </div>
  );
}
