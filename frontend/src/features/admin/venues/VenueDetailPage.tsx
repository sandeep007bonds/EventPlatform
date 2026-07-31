import { useEffect, useState } from 'react';
import { Card, Typography } from 'antd';
import { useNavigate, useParams } from 'react-router-dom';
import {
  getVenue,
  updateVenue,
  type VenueRequest,
  type VenueResponse,
} from '../../../services/catalog/catalogApi';
import { DetailSkeleton } from '../../../components/common/skeletons/DetailSkeleton';
import { NotFoundPage } from '../../../components/common/errors/NotFoundPage';
import { toast } from '../../../components/common/feedback/toast';
import { VenueForm } from './VenueForm';

/** Edits an existing venue the caller's tenant owns. */
export function VenueDetailPage() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const [venue, setVenue] = useState<VenueResponse | null>(null);
  const [loading, setLoading] = useState(true);
  const [notFound, setNotFound] = useState(false);

  useEffect(() => {
    if (!id) {
      return;
    }

    let cancelled = false;

    getVenue(id)
      .then((result) => {
        if (!cancelled) {
          setVenue(result);
        }
      })
      .catch(() => setNotFound(true))
      .finally(() => {
        if (!cancelled) {
          setLoading(false);
        }
      });

    return () => {
      cancelled = true;
    };
  }, [id]);

  const handleSubmit = async (request: VenueRequest) => {
    if (!id) {
      return;
    }
    try {
      await updateVenue(id, request);
      toast.success('Venue updated.');
      void navigate('/admin/venues');
    } catch {
      toast.error('Could not update the venue.');
    }
  };

  if (loading) {
    return <DetailSkeleton />;
  }

  if (notFound || !venue) {
    return <NotFoundPage />;
  }

  return (
    <Card style={{ maxWidth: 480 }}>
      <Typography.Title level={3}>Edit venue</Typography.Title>
      <VenueForm initialValues={venue} onSubmit={handleSubmit} />
    </Card>
  );
}
