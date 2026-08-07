import { useEffect, useState } from 'react';
import { Button, Card, DatePicker, Descriptions, Form, Modal, Space, Typography } from 'antd';
import { EditOutlined, PlusOutlined } from '@ant-design/icons';
import dayjs, { type Dayjs } from 'dayjs';
import { Link, useNavigate, useParams } from 'react-router-dom';
import type { AxiosError } from 'axios';
import {
  getEventGroup,
  updateEventGroup,
  type EventGroupResponse,
} from '../../../services/catalog/catalogApi';
import { toast } from '../../../components/common/feedback/toast';
import { DetailSkeleton } from '../../../components/common/skeletons/DetailSkeleton';
import { NotFoundPage } from '../../../components/common/errors/NotFoundPage';
import { ServerErrorPage } from '../../../components/common/errors/ServerErrorPage';
import { PageHeader } from '../../../components/common/layout/PageHeader';
import { TourLegsList } from './TourLegsList';

interface EditDatesFormValues {
  startsAt?: Dayjs;
  endsAt?: Dayjs;
}

/**
 * A tour's own page: its dates/contact defaults, and every leg created under it — with an
 * "Add leg" action that goes straight into `CreateEventPage` pre-scoped to this tour (via
 * `?eventGroupId=`), so growing a tour is create-tour-once, then keep adding legs from here,
 * rather than re-picking the tour from a dropdown on each new event.
 */
export function TourDetailPage() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();

  const [group, setGroup] = useState<EventGroupResponse | null>(null);
  const [loading, setLoading] = useState(true);
  const [notFound, setNotFound] = useState(false);
  const [loadError, setLoadError] = useState(false);
  const [editDatesOpen, setEditDatesOpen] = useState(false);
  const [savingDates, setSavingDates] = useState(false);
  const [editDatesForm] = Form.useForm<EditDatesFormValues>();

  useEffect(() => {
    if (!id) {
      return;
    }

    let cancelled = false;
    getEventGroup(id)
      .then((result) => {
        if (!cancelled) {
          setGroup(result);
          setNotFound(false);
          setLoadError(false);
        }
      })
      .catch((error: AxiosError) => {
        if (cancelled) {
          return;
        }
        if (error.response?.status === 404) {
          setNotFound(true);
        } else {
          setLoadError(true);
        }
      })
      .finally(() => {
        if (!cancelled) {
          setLoading(false);
        }
      });

    return () => {
      cancelled = true;
    };
  }, [id]);

  if (loading) {
    return <DetailSkeleton />;
  }

  if (loadError) {
    return <ServerErrorPage />;
  }

  if (notFound || !group || !id) {
    return <NotFoundPage />;
  }

  const hasContact =
    group.contactPhone ?? group.contactMobile ?? group.contactEmail ?? group.websiteUrl;

  const openEditDates = () => {
    editDatesForm.setFieldsValue({
      startsAt: group.startsAt ? dayjs(group.startsAt) : undefined,
      endsAt: group.endsAt ? dayjs(group.endsAt) : undefined,
    });
    setEditDatesOpen(true);
  };

  const handleSaveDates = async (values: EditDatesFormValues) => {
    setSavingDates(true);
    try {
      await updateEventGroup(id, {
        title: group.title,
        startsAt: values.startsAt?.toISOString() ?? null,
        endsAt: values.endsAt?.toISOString() ?? null,
      });
      setGroup({
        ...group,
        startsAt: values.startsAt?.toISOString() ?? null,
        endsAt: values.endsAt?.toISOString() ?? null,
      });
      toast.success('Tour dates updated.');
      setEditDatesOpen(false);
    } catch {
      toast.error('Could not update the tour dates.');
    } finally {
      setSavingDates(false);
    }
  };

  return (
    <>
      <PageHeader
        title={group.title}
        extra={
          <>
            <Link to={`/admin/events/new?eventGroupId=${id}`}>
              <Button type="primary" icon={<PlusOutlined />}>
                Add leg
              </Button>
            </Link>
            <Button onClick={() => void navigate('/admin/tours')} style={{ marginLeft: 8 }}>
              ← Back to tours
            </Button>
          </>
        }
      />

      <Card
        style={{ marginBottom: 24 }}
        extra={
          <Button size="small" icon={<EditOutlined />} onClick={openEditDates}>
            Edit dates
          </Button>
        }
      >
        <Descriptions column={{ xs: 1, sm: 2, md: 4 }} size="small">
          <Descriptions.Item label="Overall starts">
            {group.startsAt ? dayjs(group.startsAt).format('MMM D, YYYY') : '—'}
          </Descriptions.Item>
          <Descriptions.Item label="Overall ends">
            {group.endsAt ? dayjs(group.endsAt).format('MMM D, YYYY') : '—'}
          </Descriptions.Item>
          <Descriptions.Item label="Phone">{group.contactPhone ?? '—'}</Descriptions.Item>
          <Descriptions.Item label="Email">{group.contactEmail ?? '—'}</Descriptions.Item>
        </Descriptions>
        {!hasContact && (
          <Typography.Text type="secondary" style={{ display: 'block', marginTop: 12 }}>
            No tour-wide dates or contact details set yet — each leg can still set its own, or use
            "Edit dates" above to set the tour's own overall range.
          </Typography.Text>
        )}
      </Card>

      <Card title="Legs" styles={{ body: { padding: 28 } }}>
        <Space direction="vertical" style={{ width: '100%' }}>
          <TourLegsList eventGroupId={id} showTitle={false} />
        </Space>
      </Card>

      <Modal
        title="Edit tour dates"
        open={editDatesOpen}
        onCancel={() => setEditDatesOpen(false)}
        onOk={() => editDatesForm.submit()}
        confirmLoading={savingDates}
        okText="Save"
      >
        <Form<EditDatesFormValues>
          form={editDatesForm}
          layout="vertical"
          onFinish={(values) => {
            void handleSaveDates(values);
          }}
        >
          <Form.Item name="startsAt" label="Overall starts at (optional)">
            <DatePicker showTime style={{ width: '100%' }} />
          </Form.Item>
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
                    : Promise.reject(new Error('Overall ends at must be after Overall starts at.'));
                },
              }),
            ]}
          >
            <DatePicker showTime style={{ width: '100%' }} />
          </Form.Item>
        </Form>
      </Modal>
    </>
  );
}
