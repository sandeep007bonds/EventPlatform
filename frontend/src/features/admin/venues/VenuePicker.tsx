import { useEffect, useState } from 'react';
import { Modal, Select } from 'antd';
import {
  createVenue,
  listVenues,
  type VenueRequest,
  type VenueResponse,
} from '../../../services/catalog/catalogApi';
import { toast } from '../../../components/common/feedback/toast';
import { VenueForm } from './VenueForm';

const NEW_VENUE_OPTION = '__new__';

/**
 * Venue picker for the event-creation form. Fetches the caller's own venues (up to 100 — a
 * picker, not the full manage view; see `VenuesPage` for that) and offers a "+ New venue"
 * option that opens an inline create form, so the common one-event-one-new-venue case doesn't
 * force a context switch to a separate page.
 */
export function VenuePicker({
  value,
  onChange,
}: {
  value?: string;
  onChange?: (venueId: string) => void;
}) {
  const [venues, setVenues] = useState<VenueResponse[]>([]);
  const [loading, setLoading] = useState(true);
  const [creating, setCreating] = useState(false);

  // Deliberately doesn't set `loading` true itself (only `false`, once the fetch settles) — the
  // initial `true` state below covers first mount, and re-running this after creating a venue
  // shouldn't flash the picker back into a loading state for what's a near-instant refetch.
  const loadVenues = () =>
    listVenues({ page: 1, pageSize: 100 })
      .then((result) => setVenues(result.venues))
      .catch(() => toast.error('Could not load your venues.'))
      .finally(() => setLoading(false));

  useEffect(() => {
    void loadVenues();
  }, []);

  const handleCreate = async (request: VenueRequest) => {
    try {
      const result = await createVenue(request);
      toast.success('Venue created.');
      setCreating(false);
      await loadVenues();
      onChange?.(result.id);
    } catch {
      toast.error('Could not create the venue.');
    }
  };

  return (
    <>
      <Select
        placeholder="Select a venue"
        loading={loading}
        value={value}
        onChange={(selected) => {
          if (selected === NEW_VENUE_OPTION) {
            setCreating(true);
            return;
          }
          onChange?.(selected);
        }}
        options={[
          ...venues.map((venue) => ({ value: venue.id, label: `${venue.name} — ${venue.city}` })),
          { value: NEW_VENUE_OPTION, label: '+ New venue' },
        ]}
      />
      <Modal
        title="New venue"
        open={creating}
        onCancel={() => setCreating(false)}
        footer={null}
        destroyOnHidden
      >
        <VenueForm
          onSubmit={handleCreate}
          onCancel={() => setCreating(false)}
          submitLabel="Create"
        />
      </Modal>
    </>
  );
}
