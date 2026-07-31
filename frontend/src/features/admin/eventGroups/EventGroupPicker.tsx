import { useEffect, useState } from 'react';
import { Input, Modal, Select } from 'antd';
import {
  createEventGroup,
  listEventGroups,
  type EventGroupResponse,
} from '../../../services/catalog/catalogApi';
import { toast } from '../../../components/common/feedback/toast';

const NEW_GROUP_OPTION = '__new__';

/**
 * Optional tour picker for the event-creation form. Fetches the caller's own tours (up to 100 —
 * a picker, not a full manage view) and offers a "+ New tour" option that opens an inline create
 * form. Leaving this blank keeps today's "standalone one-off event" behavior unchanged.
 */
export function EventGroupPicker({
  value,
  onChange,
}: {
  value?: string;
  onChange?: (eventGroupId: string | undefined) => void;
}) {
  const [groups, setGroups] = useState<EventGroupResponse[]>([]);
  const [loading, setLoading] = useState(true);
  const [creating, setCreating] = useState(false);
  const [newTitle, setNewTitle] = useState('');
  const [submitting, setSubmitting] = useState(false);

  // Deliberately doesn't set `loading` true itself (only `false`, once the fetch settles) — the
  // initial `true` state below covers first mount, and re-running this after creating a tour
  // shouldn't flash the picker back into a loading state for what's a near-instant refetch.
  const loadGroups = () =>
    listEventGroups({ page: 1, pageSize: 100 })
      .then((result) => setGroups(result.eventGroups))
      .catch(() => toast.error('Could not load your tours.'))
      .finally(() => setLoading(false));

  useEffect(() => {
    void loadGroups();
  }, []);

  const handleCreate = async () => {
    setSubmitting(true);
    try {
      const result = await createEventGroup({ title: newTitle });
      toast.success('Tour created.');
      setCreating(false);
      setNewTitle('');
      await loadGroups();
      onChange?.(result.id);
    } catch {
      toast.error('Could not create the tour.');
    } finally {
      setSubmitting(false);
    }
  };

  return (
    <>
      <Select
        placeholder="Standalone event (no tour)"
        allowClear
        loading={loading}
        value={value}
        onChange={(selected) => {
          if (selected === NEW_GROUP_OPTION) {
            setCreating(true);
            return;
          }
          onChange?.(selected);
        }}
        options={[
          ...groups.map((group) => ({ value: group.id, label: group.title })),
          { value: NEW_GROUP_OPTION, label: '+ New tour' },
        ]}
      />
      <Modal
        title="New tour"
        open={creating}
        onCancel={() => setCreating(false)}
        onOk={() => void handleCreate()}
        confirmLoading={submitting}
        okButtonProps={{ disabled: !newTitle.trim() }}
      >
        <Input
          placeholder="e.g. Coldplay World Tour"
          value={newTitle}
          onChange={(event) => setNewTitle(event.target.value)}
        />
      </Modal>
    </>
  );
}
