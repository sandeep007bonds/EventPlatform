import { useState } from 'react';
import {
  Alert,
  Button,
  Card,
  DatePicker,
  Empty,
  Form,
  Input,
  Modal,
  Popconfirm,
  Space,
  Table,
  Tag,
  Typography,
} from 'antd';
import { PlusOutlined } from '@ant-design/icons';
import dayjs, { type Dayjs } from 'dayjs';
import type { AxiosError } from 'axios';
import {
  addEventSession,
  cancelEventSession,
  pauseSessionSales,
  publishEventSession,
  removeEventSession,
  resumeSessionSales,
  updateEventSession,
  type EventResponse,
  type EventSessionResponse,
} from '../../../services/catalog/catalogApi';
import { formatEventDateTime } from '../../../utils/eventTime';
import { inStartOrder, venueLabel } from '../../../utils/eventSessions';
import { toast } from '../../../components/common/feedback/toast';
import { SessionSeatMapModal } from './SessionSeatMapModal';

const SESSION_STATUS_COLOR: Record<EventSessionResponse['status'], string> = {
  Draft: 'default',
  Published: 'green',
  Cancelled: 'red',
  Completed: 'blue',
};

interface SessionFormValues {
  name?: string;
  startsAt: Dayjs;
  endsAt: Dayjs;
  doorsOpenAt?: Dayjs;
  bookingEndsAt?: Dayjs;
}

/**
 * The performances of an event — one row per night, each with its own times, venue, pinned
 * seat-map version and allocation map.
 *
 * This is where the grain change becomes visible to an organizer (ADR-0039). Everything downstream
 * hangs off a performance: inventory is provisioned per night, an order and a ticket name one, and
 * a scan is validated against one. A three-night run is one event with three rows here, not three
 * events.
 */
export function EventPerformancesPanel({
  event,
  onChanged,
}: {
  event: EventResponse;
  onChanged: () => void;
}) {
  const [form] = Form.useForm<SessionFormValues>();
  const [editing, setEditing] = useState<EventSessionResponse | null>(null);
  const [modalOpen, setModalOpen] = useState(false);
  const [saving, setSaving] = useState(false);
  const [busyId, setBusyId] = useState<string | null>(null);
  const [seatMapFor, setSeatMapFor] = useState<EventSessionResponse | null>(null);

  const sessions = inStartOrder(event.sessions);

  const openAdd = () => {
    setEditing(null);
    form.resetFields();
    setModalOpen(true);
  };

  const openEdit = (session: EventSessionResponse) => {
    setEditing(session);
    form.setFieldsValue({
      name: session.name ?? undefined,
      startsAt: dayjs(session.startsAt),
      endsAt: dayjs(session.endsAt),
      doorsOpenAt: session.doorsOpenAt ? dayjs(session.doorsOpenAt) : undefined,
      bookingEndsAt: session.bookingEndsAt ? dayjs(session.bookingEndsAt) : undefined,
    });
    setModalOpen(true);
  };

  const handleSave = async (values: SessionFormValues) => {
    setSaving(true);
    const request = {
      name: values.name?.trim() || null,
      startsAt: values.startsAt.toISOString(),
      endsAt: values.endsAt.toISOString(),
      doorsOpenAt: values.doorsOpenAt?.toISOString() ?? null,
      bookingEndsAt: values.bookingEndsAt?.toISOString() ?? null,
    };

    try {
      if (editing) {
        await updateEventSession(event.id, editing.id, request);
        toast.success('Performance updated.');
      } else {
        await addEventSession(event.id, request);
        toast.success('Performance added.');
      }
      setModalOpen(false);
      onChanged();
    } catch (error) {
      // The server's refusals are specific — overlapping another performance, times outside the
      // tour's advertised range, a booking cutoff after the show starts — and worth repeating
      // verbatim rather than flattening into "could not save".
      toast.error(refusalMessage(error) ?? 'Could not save this performance.');
    } finally {
      setSaving(false);
    }
  };

  /** Runs one of the per-performance actions, with the id-scoped busy state they all share. */
  const run = async (
    session: EventSessionResponse,
    action: () => Promise<unknown>,
    success: string,
    failure: string,
  ) => {
    setBusyId(session.id);
    try {
      await action();
      toast.success(success);
      onChanged();
    } catch (error) {
      toast.error(refusalMessage(error) ?? failure);
    } finally {
      setBusyId(null);
    }
  };

  return (
    <div>
      <Alert
        type="info"
        showIcon
        style={{ marginBottom: 16 }}
        message="A performance is what gets sold"
        description={
          'Inventory, orders, tickets and scans all hang off a performance, not the event — so ' +
          'holding a seat on one night leaves it free on the next, and a scanner at the door ' +
          'turns away a ticket for another night. Each one names a published seat-map version ' +
          'from a venue, and allocates every block of it to a ticket type.'
        }
      />

      {sessions.length === 0 ? (
        <Card>
          <Empty description="This event has no performances yet.">
            <Button type="primary" icon={<PlusOutlined />} onClick={openAdd}>
              Add a performance
            </Button>
          </Empty>
        </Card>
      ) : (
        <>
          <Table<EventSessionResponse>
            rowKey="id"
            dataSource={sessions}
            pagination={false}
            size="middle"
            columns={[
              {
                title: 'Performance',
                key: 'when',
                render: (_, session) => (
                  <Space direction="vertical" size={0}>
                    <Typography.Text strong>
                      {formatEventDateTime(session.startsAt, session.timeZoneId)}
                    </Typography.Text>
                    {session.name && (
                      <Typography.Text type="secondary">{session.name}</Typography.Text>
                    )}
                  </Space>
                ),
              },
              {
                title: 'Doors',
                key: 'doors',
                render: (_, session) =>
                  session.doorsOpenAt
                    ? formatEventDateTime(session.doorsOpenAt, session.timeZoneId)
                    : '—',
              },
              {
                title: 'Booking closes',
                key: 'bookingEndsAt',
                render: (_, session) =>
                  session.bookingEndsAt
                    ? formatEventDateTime(session.bookingEndsAt, session.timeZoneId)
                    : 'At start',
              },
              {
                title: 'Venue & map',
                key: 'venue',
                render: (_, session) =>
                  session.seatMapVersionId == null ? (
                    <Typography.Text type="warning">Not attached</Typography.Text>
                  ) : (
                    <Space direction="vertical" size={0}>
                      <Typography.Text>{venueLabel(session) ?? 'Venue'}</Typography.Text>
                      <Typography.Text type="secondary" style={{ fontSize: 12 }}>
                        Seat map v{session.seatMapVersionNumber}
                      </Typography.Text>
                    </Space>
                  ),
              },
              {
                // The number a publish is refused over: a block with no ticket type is capacity
                // Inventory never hears about, so the map would render with a hole nobody can tell
                // from a sold-out block.
                title: 'Allocated blocks',
                key: 'allocations',
                render: (_, session) =>
                  session.allocations.length === 0 ? (
                    <Typography.Text type="warning">None</Typography.Text>
                  ) : (
                    session.allocations.length
                  ),
              },
              {
                title: 'Status',
                key: 'status',
                render: (_, session) => (
                  <Space size={6}>
                    <Tag color={SESSION_STATUS_COLOR[session.status]}>{session.status}</Tag>
                    {session.salesPaused && <Tag color="warning">Paused</Tag>}
                  </Space>
                ),
              },
              {
                title: '',
                key: 'actions',
                align: 'right',
                render: (_, session) => {
                  const busy = busyId === session.id;
                  return (
                    <Space size={4} wrap>
                      <Button size="small" onClick={() => setSeatMapFor(session)}>
                        Seat map
                      </Button>

                      {session.status === 'Draft' && (
                        <>
                          <Button size="small" onClick={() => openEdit(session)}>
                            Edit
                          </Button>
                          {/* Only offered once it can succeed. Catalog refuses a publish without
                              a pinned version and a fully allocated map, and a button that only
                              ever explains itself after being clicked is not a button. */}
                          {session.seatMapVersionId != null &&
                            session.allocations.length > 0 &&
                            event.status !== 'Draft' && (
                              <Button
                                size="small"
                                type="primary"
                                loading={busy}
                                onClick={() =>
                                  void run(
                                    session,
                                    () => publishEventSession(event.id, session.id),
                                    'Performance published — inventory is being provisioned.',
                                    'Could not publish this performance.',
                                  )
                                }
                              >
                                Publish
                              </Button>
                            )}
                          <Popconfirm
                            title="Remove this performance?"
                            okText="Remove"
                            okButtonProps={{ danger: true }}
                            onConfirm={() =>
                              void run(
                                session,
                                () => removeEventSession(event.id, session.id),
                                'Performance removed.',
                                'Could not remove this performance.',
                              )
                            }
                          >
                            <Button size="small" danger loading={busy}>
                              Remove
                            </Button>
                          </Popconfirm>
                        </>
                      )}

                      {session.status === 'Published' && (
                        <>
                          <Button
                            size="small"
                            loading={busy}
                            onClick={() =>
                              void run(
                                session,
                                () =>
                                  session.salesPaused
                                    ? resumeSessionSales(event.id, session.id)
                                    : pauseSessionSales(event.id, session.id),
                                session.salesPaused
                                  ? 'Sales resumed for this performance.'
                                  : 'Sales paused for this performance.',
                                'Could not change sales for this performance.',
                              )
                            }
                          >
                            {session.salesPaused ? 'Resume sales' : 'Pause sales'}
                          </Button>
                          <Popconfirm
                            title="Cancel this performance?"
                            description="Buyers who already hold tickets for this night will need to be told."
                            okText="Cancel performance"
                            okButtonProps={{ danger: true }}
                            onConfirm={() =>
                              void run(
                                session,
                                () => cancelEventSession(event.id, session.id),
                                'Performance cancelled.',
                                'Could not cancel this performance.',
                              )
                            }
                          >
                            <Button size="small" danger loading={busy}>
                              Cancel
                            </Button>
                          </Popconfirm>
                        </>
                      )}
                    </Space>
                  );
                },
              },
            ]}
          />

          {/* Adding a night to an event that is already selling is the late-show path: the new
              performance publishes on its own rather than reopening the whole run. */}
          <Button
            type="dashed"
            icon={<PlusOutlined />}
            onClick={openAdd}
            style={{ width: '100%', marginTop: 16 }}
          >
            Add a performance
          </Button>
        </>
      )}

      <Modal
        open={modalOpen}
        title={editing ? 'Edit performance' : 'Add a performance'}
        okText={editing ? 'Save' : 'Add'}
        confirmLoading={saving}
        onOk={() => void form.submit()}
        onCancel={() => setModalOpen(false)}
        destroyOnHidden
      >
        <Form<SessionFormValues>
          form={form}
          layout="vertical"
          onFinish={(values) => void handleSave(values)}
        >
          <Form.Item
            name="name"
            label="Name (optional)"
            tooltip="Only needed when the date alone doesn't distinguish it — e.g. “Matinee”."
            rules={[{ max: 100 }]}
          >
            <Input placeholder="e.g. Matinee" />
          </Form.Item>
          <Form.Item name="startsAt" label="Starts at" rules={[{ required: true }]}>
            <DatePicker showTime style={{ width: '100%' }} />
          </Form.Item>
          <Form.Item
            name="endsAt"
            label="Ends at"
            dependencies={['startsAt']}
            rules={[
              { required: true },
              ({ getFieldValue }) => ({
                validator: (_rule, value: Dayjs | undefined) => {
                  const startsAt = getFieldValue('startsAt') as Dayjs | undefined;
                  return !value || !startsAt || value.isAfter(startsAt)
                    ? Promise.resolve()
                    : Promise.reject(new Error('Must be after the start time.'));
                },
              }),
            ]}
          >
            <DatePicker showTime style={{ width: '100%' }} />
          </Form.Item>
          <Form.Item name="doorsOpenAt" label="Doors open (optional)">
            <DatePicker showTime style={{ width: '100%' }} />
          </Form.Item>
          <Form.Item
            name="bookingEndsAt"
            label="Booking closes (optional)"
            tooltip="Holds are refused after this moment. Defaults to the start time."
            dependencies={['startsAt']}
            rules={[
              ({ getFieldValue }) => ({
                validator: (_rule, value: Dayjs | undefined) => {
                  const startsAt = getFieldValue('startsAt') as Dayjs | undefined;
                  return !value || !startsAt || !value.isAfter(startsAt)
                    ? Promise.resolve()
                    : Promise.reject(new Error('Cannot be after this performance starts.'));
                },
              }),
            ]}
          >
            <DatePicker showTime style={{ width: '100%' }} />
          </Form.Item>
        </Form>
      </Modal>

      {seatMapFor && (
        <SessionSeatMapModal
          eventId={event.id}
          currency={event.currency}
          session={seatMapFor}
          siblings={event.sessions}
          onClose={() => setSeatMapFor(null)}
          onChanged={() => {
            setSeatMapFor(null);
            onChanged();
          }}
        />
      )}
    </div>
  );
}

/** The server's own words for a refusal, when it gave any — they are specific and worth showing. */
function refusalMessage(error: unknown): string | null {
  const body = (error as AxiosError<{ message?: string }>).response?.data;
  return typeof body?.message === 'string' ? body.message : null;
}
