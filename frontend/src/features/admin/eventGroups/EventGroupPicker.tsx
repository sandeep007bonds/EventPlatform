import { useEffect, useState } from 'react';
import { Select } from 'antd';
import { listEventGroups, type EventGroupResponse } from '../../../services/catalog/catalogApi';
import { toast } from '../../../components/common/feedback/toast';

/** Sentinel value for "create a new tour inline" — never a real tour id. */
export const NEW_TOUR_OPTION = '__new__';

/**
 * Optional tour picker for the event-creation form. Fetches the caller's own tours (up to 100 —
 * a picker, not a full manage view) and offers a "+ New tour" option ({@link NEW_TOUR_OPTION}).
 * Purely a `Select` — the caller (`CreateEventPage`) renders whatever comes next (a new-tour title
 * field, or the picked tour's existing legs) based on the selected value, and owns the actual tour
 * creation at submit time so an abandoned form never leaves an orphan tour behind.
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

  useEffect(() => {
    listEventGroups({ page: 1, pageSize: 100 })
      .then((result) => setGroups(result.eventGroups))
      .catch(() => toast.error('Could not load your tours.'))
      .finally(() => setLoading(false));
  }, []);

  return (
    <Select
      placeholder="Standalone event (no tour)"
      allowClear
      loading={loading}
      value={value}
      onChange={onChange}
      options={[
        ...groups.map((group) => ({ value: group.id, label: group.title })),
        { value: NEW_TOUR_OPTION, label: '+ New tour' },
      ]}
    />
  );
}
