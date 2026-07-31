import { useEffect, useState } from 'react';
import { Button, Input, Table, Typography } from 'antd';
import { Link } from 'react-router-dom';
import { listEventGroups, type EventGroupResponse } from '../../../services/catalog/catalogApi';
import { TableSkeleton } from '../../../components/common/skeletons/TableSkeleton';
import { toast } from '../../../components/common/feedback/toast';

const PAGE_SIZE = 20;

/** The organizer's own tours — not a public directory. */
export function EventGroupsPage() {
  const [groups, setGroups] = useState<EventGroupResponse[]>([]);
  const [totalCount, setTotalCount] = useState(0);
  const [page, setPage] = useState(1);
  const [titleFilter, setTitleFilter] = useState('');
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    let cancelled = false;

    listEventGroups({ page, pageSize: PAGE_SIZE })
      .then((result) => {
        if (cancelled) {
          return;
        }
        setGroups(result.eventGroups);
        setTotalCount(result.totalCount);
      })
      .catch(() => toast.error('Could not load your tours.'))
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

  const visibleGroups = titleFilter
    ? groups.filter((group) => group.title.toLowerCase().includes(titleFilter.toLowerCase()))
    : groups;

  return (
    <>
      <div style={{ display: 'flex', justifyContent: 'space-between', marginBottom: 16 }}>
        <Typography.Title level={3} style={{ margin: 0 }}>
          Tours
        </Typography.Title>
        <Link to="/admin/tours/new">
          <Button type="primary">Create tour</Button>
        </Link>
      </div>

      <Input.Search
        placeholder="Search title"
        allowClear
        onChange={(event) => setTitleFilter(event.target.value)}
        style={{ width: 220, marginBottom: 16 }}
      />

      <Table<EventGroupResponse>
        rowKey="id"
        dataSource={visibleGroups}
        pagination={{
          current: page,
          pageSize: PAGE_SIZE,
          total: totalCount,
          onChange: setPage,
          showSizeChanger: false,
        }}
        columns={[
          { title: 'Title', dataIndex: 'title', sorter: (a, b) => a.title.localeCompare(b.title) },
        ]}
      />
    </>
  );
}
