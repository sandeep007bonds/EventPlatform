import { useEffect, useState } from 'react';
import { Button, Card, Input } from 'antd';
import { PlusOutlined } from '@ant-design/icons';
import { Link, useNavigate } from 'react-router-dom';
import { listEventGroups, type EventGroupResponse } from '../../../services/catalog/catalogApi';
import { TableSkeleton } from '../../../components/common/skeletons/TableSkeleton';
import { DataGrid } from '../../../components/common/grid/DataGrid';
import { PageHeader } from '../../../components/common/layout/PageHeader';
import { Toolbar } from '../../../components/common/layout/Toolbar';
import { LoadError } from '../../../components/common/errors/LoadError';

const PAGE_SIZE = 20;

/** The organizer's own tours — not a public directory. */
export function EventGroupsPage() {
  const navigate = useNavigate();
  const [groups, setGroups] = useState<EventGroupResponse[]>([]);
  const [totalCount, setTotalCount] = useState(0);
  const [page, setPage] = useState(1);
  const [titleFilter, setTitleFilter] = useState('');
  const [loading, setLoading] = useState(true);
  const [loadError, setLoadError] = useState(false);
  const [reloadToken, setReloadToken] = useState(0);

  useEffect(() => {
    let cancelled = false;

    listEventGroups({ page, pageSize: PAGE_SIZE })
      .then((result) => {
        if (cancelled) {
          return;
        }
        setGroups(result.eventGroups);
        setTotalCount(result.totalCount);
        setLoadError(false);
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
  }, [page, reloadToken]);

  if (loading) {
    return (
      <>
        <PageHeader title="Tours" />
        <TableSkeleton />
      </>
    );
  }

  const visibleGroups = titleFilter
    ? groups.filter((group) => group.title.toLowerCase().includes(titleFilter.toLowerCase()))
    : groups;

  return (
    <>
      <PageHeader
        title="Tours"
        description="Group multiple city/date legs under one promotional umbrella."
        extra={
          <Link to="/admin/tours/new">
            <Button type="primary" icon={<PlusOutlined />}>
              Create tour
            </Button>
          </Link>
        }
      />

      <Toolbar>
        <Input.Search
          placeholder="Search title"
          allowClear
          onChange={(event) => setTitleFilter(event.target.value)}
          style={{ width: 240 }}
        />
      </Toolbar>

      {loadError ? (
        <Card>
          <LoadError
            description="Could not load your tours."
            onRetry={() => {
              setLoading(true);
              setReloadToken((token) => token + 1);
            }}
          />
        </Card>
      ) : (
        <DataGrid<EventGroupResponse>
          rowKey="id"
          rows={visibleGroups}
          exportFileName="tours"
          countLabel={`${totalCount.toLocaleString()} tour${totalCount === 1 ? '' : 's'}`}
          pagination={{
            current: page,
            pageSize: PAGE_SIZE,
            total: totalCount,
            onChange: setPage,
            showSizeChanger: false,
          }}
          onRow={(record) => ({
            onClick: () => void navigate(`/admin/tours/${record.id}`),
            style: { cursor: 'pointer' },
          })}
          columns={[
            {
              title: 'Title',
              dataIndex: 'title',
              sorter: (a, b) => a.title.localeCompare(b.title),
            },
          ]}
        />
      )}
    </>
  );
}
