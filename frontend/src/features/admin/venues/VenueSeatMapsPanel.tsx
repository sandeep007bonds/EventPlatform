import { useEffect, useState } from 'react';
import { Button, Form, Input, List, Modal, Space, Tag, Typography } from 'antd';
import { PlusOutlined } from '@ant-design/icons';
import type { AxiosError } from 'axios';
import {
  createSeatMap,
  listSeatMaps,
  type SeatMapSummaryResponse,
  type VenueResponse,
} from '../../../services/venue/venueApi';
import { LoadError } from '../../../components/common/errors/LoadError';
import { ListSkeleton } from '../../../components/common/skeletons/ListSkeleton';
import { toast } from '../../../components/common/feedback/toast';
import { SeatMapEditorModal } from './SeatMapEditorModal';

/**
 * A venue's seat maps, and the way into editing one.
 *
 * A venue can have several: a stadium sells a football configuration and a concert configuration,
 * and they are different maps, not different versions of one. Versions are for *revisions* of the
 * same configuration — and a published version is immutable, because tickets already sold resolve
 * their seats against it.
 */
export function VenueSeatMapsPanel({ venue }: { venue: VenueResponse }) {
  const [maps, setMaps] = useState<SeatMapSummaryResponse[]>([]);
  const [loading, setLoading] = useState(true);
  const [loadError, setLoadError] = useState(false);
  const [reloadToken, setReloadToken] = useState(0);
  const [creating, setCreating] = useState(false);
  const [saving, setSaving] = useState(false);
  const [editingMapId, setEditingMapId] = useState<string | null>(null);
  const [form] = Form.useForm<{ name: string }>();

  useEffect(() => {
    let cancelled = false;

    listSeatMaps(venue.id)
      .then((result) => {
        if (!cancelled) {
          setMaps(result);
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
  }, [venue.id, reloadToken]);

  const reload = () => setReloadToken((token) => token + 1);

  const handleCreate = async (values: { name: string }) => {
    setSaving(true);
    try {
      const result = await createSeatMap(venue.id, values.name.trim());
      toast.success('Seat map created with an empty draft.');
      setCreating(false);
      form.resetFields();
      reload();
      setEditingMapId(result.id);
    } catch (error) {
      const body = (error as AxiosError<{ message?: string }>).response?.data;
      toast.error(body?.message ?? 'Could not create this seat map.');
    } finally {
      setSaving(false);
    }
  };

  if (loading) {
    return <ListSkeleton />;
  }

  if (loadError) {
    return (
      <LoadError
        description="Could not load this venue’s seat maps."
        onRetry={() => {
          setLoading(true);
          reload();
        }}
      />
    );
  }

  return (
    <>
      <Typography.Paragraph type="secondary">
        A seat map describes one configuration of the hall. Edit its open draft, then publish —
        published versions are immutable, so a performance already selling keeps the exact layout
        its tickets were sold against.
      </Typography.Paragraph>

      <List
        bordered
        dataSource={maps}
        locale={{ emptyText: 'No seat maps yet.' }}
        renderItem={(map) => (
          <List.Item
            actions={[
              <Button key="edit" size="small" onClick={() => setEditingMapId(map.id)}>
                {map.hasOpenDraft ? 'Edit draft' : 'Open'}
              </Button>,
            ]}
          >
            <Space direction="vertical" size={0}>
              <Typography.Text strong>{map.name}</Typography.Text>
              <Space size={6}>
                {map.publishedVersionNumber == null ? (
                  <Tag color="warning">Not published</Tag>
                ) : (
                  <Tag color="green">Published v{map.publishedVersionNumber}</Tag>
                )}
                {map.hasOpenDraft && <Tag>Draft open</Tag>}
                <Typography.Text type="secondary" style={{ fontSize: 12 }}>
                  {map.versionCount} version{map.versionCount === 1 ? '' : 's'}
                </Typography.Text>
              </Space>
            </Space>
          </List.Item>
        )}
      />

      <Button
        type="dashed"
        icon={<PlusOutlined />}
        onClick={() => setCreating(true)}
        style={{ width: '100%', marginTop: 16 }}
      >
        Add a seat map
      </Button>

      <Modal
        open={creating}
        title="New seat map"
        okText="Create"
        confirmLoading={saving}
        onOk={() => void form.submit()}
        onCancel={() => setCreating(false)}
        destroyOnHidden
      >
        <Form form={form} layout="vertical" onFinish={(values) => void handleCreate(values)}>
          <Form.Item
            name="name"
            label="Name"
            tooltip="Name the configuration, not the venue — e.g. “Concert (end stage)”."
            rules={[{ required: true }, { max: 200 }]}
          >
            <Input placeholder="e.g. Concert (end stage)" />
          </Form.Item>
        </Form>
      </Modal>

      {editingMapId && (
        <SeatMapEditorModal
          seatMapId={editingMapId}
          gates={venue.gates}
          onClose={() => setEditingMapId(null)}
          onChanged={reload}
        />
      )}
    </>
  );
}
