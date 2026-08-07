import { useEffect, useState } from 'react';
import { Select } from 'antd';
import { listEventGroups, type EventGroupResponse } from '../../../services/catalog/catalogApi';

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
  hideStandaloneOption = false,
  disabled = false,
}: {
  value?: string;
  onChange?: (eventGroupId: string | undefined) => void;
  /**
   * Prevents clearing back to "no tour" — used once a form has more than one leg, since a
   * multi-leg batch always needs a tour to attach the legs to (either an existing one or
   * {@link NEW_TOUR_OPTION}).
   */
  hideStandaloneOption?: boolean;
  /** Locks the picker once a tour has already been created/chosen for this page visit. */
  disabled?: boolean;
}) {
  const [groups, setGroups] = useState<EventGroupResponse[]>([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    // Silently degrades to just the "+ New tour" option on failure — this picker sits inside
    // CreateEventPage's form, where a toast would be noisy for what's still a fully usable form
    // (standalone and new-tour creation both work without this list).
    listEventGroups({ page: 1, pageSize: 100 })
      .then((result) => setGroups(result.eventGroups))
      .catch(() => {})
      .finally(() => setLoading(false));
  }, []);

  return (
    <Select
      placeholder={
        hideStandaloneOption ? 'Pick a tour, or "+ New tour"' : 'Standalone event (no tour)'
      }
      allowClear={!hideStandaloneOption}
      disabled={disabled}
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
