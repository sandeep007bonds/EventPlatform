import { useEffect, useState } from 'react';
import {
  Alert,
  Button,
  Card,
  Descriptions,
  Form,
  Input,
  List,
  Modal,
  Space,
  Tabs,
  Tag,
  Typography,
} from 'antd';
import { PlusOutlined } from '@ant-design/icons';
import type { AxiosError } from 'axios';
import { useNavigate, useParams, useSearchParams } from 'react-router-dom';
import {
  activateVenue,
  addVenueFacility,
  addVenueGate,
  archiveVenue,
  getVenue,
  updateVenue,
  type VenueResponse,
} from '../../../services/venue/venueApi';
import { DetailSkeleton } from '../../../components/common/skeletons/DetailSkeleton';
import { NotFoundPage } from '../../../components/common/errors/NotFoundPage';
import { ServerErrorPage } from '../../../components/common/errors/ServerErrorPage';
import { PageHeader } from '../../../components/common/layout/PageHeader';
import { toast } from '../../../components/common/feedback/toast';
import { VenueForm } from './VenueForm';
import { toFormValues, toVenueRequest, type VenueFormValues } from './venueFormValues';
import { VenueSeatMapsPanel } from './VenueSeatMapsPanel';

const TAB_QUERY_PARAM = 'tab';

/**
 * One venue: its address, its entry gates and facilities, and its seat maps.
 *
 * Gates live here rather than on an event because they are doors in a building — the same doors
 * for every show. A seat-map section restricts itself to one, and Ticketing warms that restriction
 * per performance from the version the performance pinned (ADR-0025), so a scan enforces the gates
 * that were in force when its tickets sold.
 */
export function VenueDetailPage() {
  const { venueId } = useParams<{ venueId: string }>();
  const navigate = useNavigate();
  const [searchParams, setSearchParams] = useSearchParams();

  const [venue, setVenue] = useState<VenueResponse | null>(null);
  const [loading, setLoading] = useState(true);
  const [notFound, setNotFound] = useState(false);
  const [loadError, setLoadError] = useState(false);
  const [saving, setSaving] = useState(false);
  const [busy, setBusy] = useState(false);
  const [gateModalOpen, setGateModalOpen] = useState(false);
  const [facilityModalOpen, setFacilityModalOpen] = useState(false);

  const [gateForm] = Form.useForm<{ code: string; name: string }>();
  const [facilityForm] = Form.useForm<{ name: string; description?: string }>();

  const load = (id: string) => {
    getVenue(id)
      .then((result) => {
        setVenue(result);
        setNotFound(false);
        setLoadError(false);
      })
      .catch((error: AxiosError) => {
        if (error.response?.status === 404) {
          setNotFound(true);
        } else {
          setLoadError(true);
        }
      })
      .finally(() => setLoading(false));
  };

  useEffect(() => {
    if (venueId) {
      load(venueId);
    }
  }, [venueId]);

  if (loading) {
    return <DetailSkeleton />;
  }

  if (loadError) {
    return <ServerErrorPage />;
  }

  if (notFound || !venue || !venueId) {
    return <NotFoundPage />;
  }

  const reload = () => load(venueId);

  const handleSaveDetails = async (values: VenueFormValues) => {
    setSaving(true);
    try {
      await updateVenue(venueId, toVenueRequest(values));
      toast.success('Venue saved.');
      reload();
    } catch {
      toast.error('Could not save this venue.');
    } finally {
      setSaving(false);
    }
  };

  const handleStatusChange = async () => {
    setBusy(true);
    try {
      if (venue.status === 'Active') {
        await archiveVenue(venueId);
        toast.success('Venue archived. Performances already selling keep their pinned map.');
      } else {
        await activateVenue(venueId);
        toast.success('Venue activated — it can now be attached to a performance.');
      }
      reload();
    } catch (error) {
      const body = (error as AxiosError<{ message?: string }>).response?.data;
      toast.error(body?.message ?? 'Could not change this venue’s status.');
    } finally {
      setBusy(false);
    }
  };

  const handleAddGate = async (values: { code: string; name: string }) => {
    setBusy(true);
    try {
      await addVenueGate(venueId, { code: values.code.trim(), name: values.name.trim() });
      toast.success('Gate added.');
      setGateModalOpen(false);
      gateForm.resetFields();
      reload();
    } catch (error) {
      const body = (error as AxiosError<{ message?: string }>).response?.data;
      toast.error(body?.message ?? 'Could not add this gate.');
    } finally {
      setBusy(false);
    }
  };

  const handleAddFacility = async (values: { name: string; description?: string }) => {
    setBusy(true);
    try {
      await addVenueFacility(venueId, {
        name: values.name.trim(),
        description: values.description?.trim() || null,
      });
      toast.success('Facility added.');
      setFacilityModalOpen(false);
      facilityForm.resetFields();
      reload();
    } catch {
      toast.error('Could not add this facility.');
    } finally {
      setBusy(false);
    }
  };

  return (
    <>
      <PageHeader
        title={
          <Space align="center">
            {venue.name}
            <Tag color={venue.status === 'Active' ? 'green' : 'default'}>{venue.status}</Tag>
          </Space>
        }
        extra={
          <>
            <Button loading={busy} onClick={() => void handleStatusChange()}>
              {venue.status === 'Active' ? 'Archive' : 'Activate'}
            </Button>
            <Button onClick={() => void navigate('/admin/venues')}>← Back to venues</Button>
          </>
        }
      />

      {venue.status !== 'Active' && (
        <Alert
          type="warning"
          showIcon
          style={{ marginBottom: 24 }}
          message="Not yet available to events"
          description="Only an active venue can be attached to a performance. Add its gates, publish a seat map, then activate it."
        />
      )}

      <Card style={{ marginBottom: 24 }}>
        <Descriptions column={{ xs: 1, sm: 2, md: 4 }} size="small">
          <Descriptions.Item label="Type">{venue.venueType ?? '—'}</Descriptions.Item>
          <Descriptions.Item label="City">
            {venue.address.city}, {venue.address.country}
          </Descriptions.Item>
          <Descriptions.Item label="Time zone">{venue.timeZoneId ?? 'Not set'}</Descriptions.Item>
          <Descriptions.Item label="Gates">{venue.gates.length}</Descriptions.Item>
        </Descriptions>
      </Card>

      <Card styles={{ body: { padding: 28 } }}>
        <Tabs
          activeKey={searchParams.get(TAB_QUERY_PARAM) ?? 'maps'}
          onChange={(key) => setSearchParams({ [TAB_QUERY_PARAM]: key }, { replace: true })}
          items={[
            {
              key: 'maps',
              label: 'Seat maps',
              children: <VenueSeatMapsPanel venue={venue} />,
            },
            {
              key: 'gates',
              label: (
                <Space size={6}>
                  Gates
                  {venue.gates.length > 0 && <Tag>{venue.gates.length}</Tag>}
                </Space>
              ),
              children: (
                <>
                  <Typography.Paragraph type="secondary">
                    A gate is a door in the building. A seat-map section or admission area can be
                    restricted to one, and a scanner set to that gate turns away tickets for
                    anywhere else. A gate is deactivated rather than deleted — tickets already sold
                    still name it.
                  </Typography.Paragraph>
                  <List
                    bordered
                    dataSource={venue.gates}
                    locale={{ emptyText: 'No gates yet.' }}
                    renderItem={(gate) => (
                      <List.Item>
                        <Space>
                          <Tag>{gate.code}</Tag>
                          {gate.name}
                          {!gate.isActive && <Tag color="default">Inactive</Tag>}
                        </Space>
                      </List.Item>
                    )}
                  />
                  <Button
                    type="dashed"
                    icon={<PlusOutlined />}
                    onClick={() => setGateModalOpen(true)}
                    style={{ width: '100%', marginTop: 16 }}
                  >
                    Add a gate
                  </Button>
                </>
              ),
            },
            {
              key: 'facilities',
              label: 'Facilities',
              children: (
                <>
                  <Typography.Paragraph type="secondary">
                    Bars, restrooms, accessible entrances — anything a buyer might want to know
                    about but that sells no tickets. Free text on purpose: the useful set differs
                    completely between a stadium and a studio theatre.
                  </Typography.Paragraph>
                  <List
                    bordered
                    dataSource={venue.facilities}
                    locale={{ emptyText: 'No facilities listed.' }}
                    renderItem={(facility) => (
                      <List.Item>
                        <Space direction="vertical" size={0}>
                          <Typography.Text strong>{facility.name}</Typography.Text>
                          {facility.description && (
                            <Typography.Text type="secondary">
                              {facility.description}
                            </Typography.Text>
                          )}
                        </Space>
                      </List.Item>
                    )}
                  />
                  <Button
                    type="dashed"
                    icon={<PlusOutlined />}
                    onClick={() => setFacilityModalOpen(true)}
                    style={{ width: '100%', marginTop: 16 }}
                  >
                    Add a facility
                  </Button>
                </>
              ),
            },
            {
              key: 'details',
              label: 'Details',
              children: (
                <VenueForm
                  initialValues={toFormValues(venue)}
                  saving={saving}
                  onSubmit={(values) => void handleSaveDetails(values)}
                />
              ),
            },
          ]}
        />
      </Card>

      <Modal
        open={gateModalOpen}
        title="Add a gate"
        okText="Add"
        confirmLoading={busy}
        onOk={() => void gateForm.submit()}
        onCancel={() => setGateModalOpen(false)}
        destroyOnHidden
      >
        <Form form={gateForm} layout="vertical" onFinish={(values) => void handleAddGate(values)}>
          <Form.Item
            name="code"
            label="Code"
            tooltip="Short and stable — this is what staff and signage say. Unique within the venue."
            rules={[{ required: true }, { max: 20 }]}
          >
            <Input placeholder="e.g. N1" />
          </Form.Item>
          <Form.Item name="name" label="Name" rules={[{ required: true }, { max: 100 }]}>
            <Input placeholder="e.g. North Stand entrance" />
          </Form.Item>
        </Form>
      </Modal>

      <Modal
        open={facilityModalOpen}
        title="Add a facility"
        okText="Add"
        confirmLoading={busy}
        onOk={() => void facilityForm.submit()}
        onCancel={() => setFacilityModalOpen(false)}
        destroyOnHidden
      >
        <Form
          form={facilityForm}
          layout="vertical"
          onFinish={(values) => void handleAddFacility(values)}
        >
          <Form.Item name="name" label="Name" rules={[{ required: true }, { max: 100 }]}>
            <Input placeholder="e.g. Accessible entrance" />
          </Form.Item>
          <Form.Item name="description" label="Description (optional)" rules={[{ max: 500 }]}>
            <Input.TextArea rows={3} />
          </Form.Item>
        </Form>
      </Modal>
    </>
  );
}
