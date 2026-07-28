import { useEffect, useState } from 'react';
import {
  Button,
  Card,
  Descriptions,
  Divider,
  Form,
  Input,
  InputNumber,
  Space,
  Tag,
  Typography,
} from 'antd';
import { MinusCircleOutlined, PlusOutlined } from '@ant-design/icons';
import dayjs from 'dayjs';
import { useNavigate, useParams } from 'react-router-dom';
import {
  defineSeatMap,
  getEvent,
  getSeatMap,
  publishEvent,
  type EventResponse,
  type SeatMapResponse,
  type SeatMapSectionInput,
} from '../../../services/catalog/catalogApi';
import { DetailSkeleton } from '../../../components/common/skeletons/DetailSkeleton';
import { NotFoundPage } from '../../../components/common/errors/NotFoundPage';
import { eventStatusColor } from '../../../utils/eventStatus';
import { toast } from '../../../components/common/feedback/toast';
import { SeatBlockPanel } from '../inventory/SeatBlockPanel';

interface SeatMapFormValues {
  name: string;
  sections: SeatMapSectionInput[];
}

/** Organizer's event detail: define seat map, publish, and (once published) block/unblock seats. */
export function AdminEventDetailPage() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();

  const [event, setEvent] = useState<EventResponse | null>(null);
  const [seatMap, setSeatMap] = useState<SeatMapResponse | null>(null);
  const [loading, setLoading] = useState(true);
  const [notFound, setNotFound] = useState(false);
  const [submitting, setSubmitting] = useState(false);

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
          <Descriptions.Item label="Currency">{event.currency}</Descriptions.Item>
          {seatMap && (
            <Descriptions.Item label="Capacity">{seatMap.capacity} seats</Descriptions.Item>
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

      {!seatMap && event.status === 'Draft' && (
        <Card title="Define seat map" style={{ marginTop: 24 }}>
          <Form<SeatMapFormValues>
            layout="vertical"
            initialValues={{
              sections: [{ name: '', priceTier: '', priceAmount: 0, rows: 1, seatsPerRow: 1 }],
            }}
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
                    <Space key={field.key} align="baseline" wrap>
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
                      {fields.length > 1 && (
                        <MinusCircleOutlined onClick={() => remove(field.name)} />
                      )}
                    </Space>
                  ))}
                  <Form.Item>
                    <Button
                      type="dashed"
                      onClick={() =>
                        add({ name: '', priceTier: '', priceAmount: 0, rows: 1, seatsPerRow: 1 })
                      }
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
