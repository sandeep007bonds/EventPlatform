import { Card, Typography } from 'antd';
import { useNavigate } from 'react-router-dom';
import { createVenue, type VenueRequest } from '../../../services/catalog/catalogApi';
import { toast } from '../../../components/common/feedback/toast';
import { VenueForm } from './VenueForm';

/** Creates a new venue for the caller's tenant. */
export function CreateVenuePage() {
  const navigate = useNavigate();

  const handleSubmit = async (request: VenueRequest) => {
    try {
      await createVenue(request);
      toast.success('Venue created.');
      void navigate('/admin/venues');
    } catch {
      toast.error('Could not create the venue.');
    }
  };

  return (
    <Card style={{ maxWidth: 480 }}>
      <Typography.Title level={3}>Create venue</Typography.Title>
      <VenueForm onSubmit={handleSubmit} />
    </Card>
  );
}
