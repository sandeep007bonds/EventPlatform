import { useEffect, useState } from 'react';
import { Alert, Button, Modal, Tag } from 'antd';
import { PlusOutlined } from '@ant-design/icons';
import { useNavigate } from 'react-router-dom';
import {
  createVenue,
  listVenues,
  type VenueStatus,
  type VenueSummaryResponse,
} from '../../../services/venue/venueApi';
import { DataGrid } from '../../../components/common/grid/DataGrid';
import { TableSkeleton } from '../../../components/common/skeletons/TableSkeleton';
import { PageHeader } from '../../../components/common/layout/PageHeader';
import { LoadError } from '../../../components/common/errors/LoadError';
import { toast } from '../../../components/common/feedback/toast';
import { VenueForm } from './VenueForm';
import { toVenueRequest, type VenueFormValues } from './venueFormValues';

const STATUS_COLOR: Record<VenueStatus, string> = {
  Draft: 'default',
  Active: 'green',
  Archived: 'default',
};

/**
 * The organizer's venue library.
 *
 * A venue is reusable across events, which is the whole reason it lives in its own service
 * (ADR-0038): the same hall hosts a hundred shows, and copying its seat map into each one made a
 * map that could never be corrected once and a "venue" that was really just an address field.
 */
export function VenuesPage() {
  const navigate = useNavigate();
  const [venues, setVenues] = useState<VenueSummaryResponse[]>([]);
  const [loading, setLoading] = useState(true);
  const [loadError, setLoadError] = useState(false);
  const [reloadToken, setReloadToken] = useState(0);
  const [creating, setCreating] = useState(false);
  const [saving, setSaving] = useState(false);

  useEffect(() => {
    let cancelled = false;

    listVenues()
      .then((result) => {
        if (!cancelled) {
          setVenues(result);
          setLoadError(false);
        }
      })
      .catch(() => {
        if (!cancelled) {
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
  }, [reloadToken]);

  const handleCreate = async (values: VenueFormValues) => {
    setSaving(true);
    try {
      const result = await createVenue(toVenueRequest(values));
      toast.success('Venue created. Add its gates and a seat map next.');
      setCreating(false);
      void navigate(`/admin/venues/${result.id}`);
    } catch {
      toast.error('Could not create this venue.');
    } finally {
      setSaving(false);
    }
  };

  return (
    <>
      <PageHeader
        title="Venues"
        description="Places you sell tickets for, and the seat maps that describe them. A venue is reused across events; each performance points at one published version of a map."
        extra={
          <Button type="primary" icon={<PlusOutlined />} onClick={() => setCreating(true)}>
            New venue
          </Button>
        }
      />

      {loading ? (
        <TableSkeleton />
      ) : loadError ? (
        <LoadError
          description="Could not load your venues."
          onRetry={() => {
            setLoading(true);
            setReloadToken((token) => token + 1);
          }}
        />
      ) : (
        <DataGrid<VenueSummaryResponse>
          rowKey="id"
          rows={venues}
          searchPlaceholder="Search venues"
          exportFileName="venues"
          countLabel={`${venues.length.toLocaleString()} venue${venues.length === 1 ? '' : 's'}`}
          onRow={(record) => ({
            style: { cursor: 'pointer' },
            onClick: () => void navigate(`/admin/venues/${record.id}`),
          })}
          columns={[
            { title: 'Name', dataIndex: 'name', searchable: true },
            { title: 'Type', dataIndex: 'venueType', render: (type: string | null) => type ?? '—' },
            { title: 'City', dataIndex: 'city', searchable: true },
            { title: 'Country', dataIndex: 'country' },
            {
              title: 'Gates',
              dataIndex: 'gateCount',
              sorter: (a, b) => a.gateCount - b.gateCount,
            },
            {
              title: 'Status',
              dataIndex: 'status',
              render: (status: VenueStatus) => <Tag color={STATUS_COLOR[status]}>{status}</Tag>,
            },
          ]}
        />
      )}

      <Modal
        open={creating}
        title="New venue"
        okText="Create"
        confirmLoading={saving}
        onCancel={() => setCreating(false)}
        footer={null}
        destroyOnHidden
      >
        <Alert
          type="info"
          showIcon
          style={{ marginBottom: 16 }}
          message="A venue starts as a draft"
          description="Add its gates and publish a seat map, then activate it — only active venues can be attached to a performance."
        />
        <VenueForm saving={saving} onSubmit={(values) => void handleCreate(values)} />
      </Modal>
    </>
  );
}
