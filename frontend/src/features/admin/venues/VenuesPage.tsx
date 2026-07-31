import { useEffect, useState } from 'react';
import { Button, Input, Table, Typography } from 'antd';
import { Link, useNavigate } from 'react-router-dom';
import { listVenues, type VenueResponse } from '../../../services/catalog/catalogApi';
import { TableSkeleton } from '../../../components/common/skeletons/TableSkeleton';
import { toast } from '../../../components/common/feedback/toast';

const PAGE_SIZE = 20;

/** The organizer's own reusable venues — not a public directory. */
export function VenuesPage() {
  const navigate = useNavigate();
  const [venues, setVenues] = useState<VenueResponse[]>([]);
  const [totalCount, setTotalCount] = useState(0);
  const [page, setPage] = useState(1);
  const [nameFilter, setNameFilter] = useState('');
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    let cancelled = false;

    listVenues({ page, pageSize: PAGE_SIZE })
      .then((result) => {
        if (cancelled) {
          return;
        }
        setVenues(result.venues);
        setTotalCount(result.totalCount);
      })
      .catch(() => toast.error('Could not load your venues.'))
      .finally(() => {
        if (!cancelled) {
          setLoading(false);
        }
      });

    return () => {
      cancelled = true;
    };
  }, [page]);

  if (loading) {
    return <TableSkeleton />;
  }

  const visibleVenues = nameFilter
    ? venues.filter((venue) => venue.name.toLowerCase().includes(nameFilter.toLowerCase()))
    : venues;

  return (
    <>
      <div style={{ display: 'flex', justifyContent: 'space-between', marginBottom: 16 }}>
        <Typography.Title level={3} style={{ margin: 0 }}>
          Venues
        </Typography.Title>
        <Link to="/admin/venues/new">
          <Button type="primary">Create venue</Button>
        </Link>
      </div>

      <Input.Search
        placeholder="Search name"
        allowClear
        onChange={(event) => setNameFilter(event.target.value)}
        style={{ width: 220, marginBottom: 16 }}
      />

      <Table<VenueResponse>
        rowKey="id"
        dataSource={visibleVenues}
        pagination={{
          current: page,
          pageSize: PAGE_SIZE,
          total: totalCount,
          onChange: setPage,
          showSizeChanger: false,
        }}
        onRow={(record) => ({
          onClick: () => void navigate(`/admin/venues/${record.id}`),
          style: { cursor: 'pointer' },
        })}
        columns={[
          { title: 'Name', dataIndex: 'name', sorter: (a, b) => a.name.localeCompare(b.name) },
          { title: 'City', dataIndex: 'city' },
          { title: 'Country', dataIndex: 'country' },
          {
            title: 'Capacity',
            dataIndex: 'capacity',
            render: (capacity: VenueResponse['capacity']) => capacity ?? '—',
          },
        ]}
      />
    </>
  );
}
