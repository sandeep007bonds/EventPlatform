import { useEffect, useState } from 'react';
import { Button, Card, Descriptions, Space, Typography } from 'antd';
import { PlusOutlined } from '@ant-design/icons';
import dayjs from 'dayjs';
import { Link, useNavigate, useParams } from 'react-router-dom';
import type { AxiosError } from 'axios';
import { getEventGroup, type EventGroupResponse } from '../../../services/catalog/catalogApi';
import { DetailSkeleton } from '../../../components/common/skeletons/DetailSkeleton';
import { NotFoundPage } from '../../../components/common/errors/NotFoundPage';
import { ServerErrorPage } from '../../../components/common/errors/ServerErrorPage';
import { PageHeader } from '../../../components/common/layout/PageHeader';
import { TourLegsList } from './TourLegsList';

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

      <Card style={{ marginBottom: 24 }}>
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
            No tour-wide dates or contact details set yet — each leg can still set its own, or edit
            this tour from the Tours list.
          </Typography.Text>
        )}
      </Card>

      <Card title="Legs" styles={{ body: { padding: 28 } }}>
        <Space direction="vertical" style={{ width: '100%' }}>
          <TourLegsList eventGroupId={id} showTitle={false} />
        </Space>
      </Card>
    </>
  );
}
