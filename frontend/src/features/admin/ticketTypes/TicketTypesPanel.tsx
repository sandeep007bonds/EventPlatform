import { useCallback, useEffect, useState } from 'react';
import {
  Alert,
  Button,
  Card,
  DatePicker,
  Form,
  Input,
  InputNumber,
  Modal,
  Popconfirm,
  Space,
  Table,
  Tag,
  Typography,
} from 'antd';
import dayjs, { type Dayjs } from 'dayjs';
import {
  createTicketType,
  deactivateTicketType,
  listTicketTypes,
  updateTicketType,
  type TicketTypeResponse,
} from '../../../services/catalog/catalogApi';
import { LoadError } from '../../../components/common/errors/LoadError';
import { toast } from '../../../components/common/feedback/toast';
import { formatMoney, toMajor, toMinor } from '../../../utils/money';

interface TicketTypesPanelProps {
  eventId: string;
  /** The event's currency, for rendering prices. */
  currency: string;
  /**
   * Whether the event is still a draft. Only a draft's ticket types may be repriced — see the
   * price field's note below.
   */
  isDraft: boolean;
}

interface TicketTypeFormValues {
  name: string;
  /** Major units, as typed — converted to minor on submit. */
  price: number;
  description?: string;
  salesWindow?: [Dayjs, Dayjs];
  maxPerBuyer?: number | null;
  sortOrder?: number;
}

const PRICE_LOCKED_NOTE =
  'The price is fixed once the event is published: inventory was priced when the event went live, ' +
  'so changing it here would move what buyers see without moving what they are charged.';

/**
 * Organizer's ticket-type manager for one event — the named, priced kinds of ticket a seat-map
 * section is sold as.
 *
 * Two things differ from `PromoCodesPanel`, which this otherwise mirrors. Types **are** editable,
 * because renaming a tier or correcting a price is ordinary work, whereas an advertised discount
 * code must not silently change value. And the price field disables after publish rather than
 * letting someone type a new one and be refused — the server rejects that with a 409, which is
 * still handled here, since this component's idea of the event's status can be stale.
 */
export function TicketTypesPanel({ eventId, currency, isDraft }: TicketTypesPanelProps) {
  const [createForm] = Form.useForm<TicketTypeFormValues>();
  const [editForm] = Form.useForm<TicketTypeFormValues>();
  const [types, setTypes] = useState<TicketTypeResponse[]>([]);
  const [loading, setLoading] = useState(true);
  const [loadError, setLoadError] = useState(false);
  const [submitting, setSubmitting] = useState(false);
  const [editing, setEditing] = useState<TicketTypeResponse | null>(null);

  // Sets no state synchronously — `loading` starts true and only flips inside a promise callback,
  // so the mount effect doesn't cascade. Same shape as PromoCodesPanel and QueueSettingsPanel.
  const load = useCallback(
    () =>
      listTicketTypes(eventId)
        .then((result) => {
          setTypes(result);
          setLoadError(false);
        })
        .catch(() => setLoadError(true))
        .finally(() => setLoading(false)),
    [eventId],
  );

  useEffect(() => {
    void load();
  }, [load]);

  const handleRetry = () => {
    setLoading(true);
    setLoadError(false);
    void load();
  };

  const handleCreate = async (values: TicketTypeFormValues) => {
    setSubmitting(true);
    try {
      await createTicketType(eventId, {
        name: values.name.trim(),
        priceMinor: toMinor(values.price),
        description: values.description?.trim() || null,
        salesStartsAt: values.salesWindow?.[0]?.toISOString() ?? null,
        salesEndsAt: values.salesWindow?.[1]?.toISOString() ?? null,
        maxPerBuyer: values.maxPerBuyer ?? null,
        sortOrder: values.sortOrder ?? 0,
      });
      createForm.resetFields();
      await load();
      toast.success('Ticket type created.');
    } catch (error) {
      toast.error(messageFrom(error) ?? 'Could not create the ticket type.');
    } finally {
      setSubmitting(false);
    }
  };

  const openEditor = (ticketType: TicketTypeResponse) => {
    setEditing(ticketType);
    editForm.setFieldsValue({
      name: ticketType.name,
      price: toMajor(ticketType.priceMinor),
      description: ticketType.description ?? undefined,
      salesWindow:
        ticketType.salesStartsAt && ticketType.salesEndsAt
          ? [dayjs(ticketType.salesStartsAt), dayjs(ticketType.salesEndsAt)]
          : undefined,
      maxPerBuyer: ticketType.maxPerBuyer,
      sortOrder: ticketType.sortOrder,
    });
  };

  const handleEdit = async (values: TicketTypeFormValues) => {
    if (!editing) {
      return;
    }

    setSubmitting(true);
    try {
      await updateTicketType(eventId, editing.id, {
        name: values.name.trim(),
        // Sent unchanged on a published event: the field is disabled, so `values.price` still holds
        // what was loaded, and the server treats an unchanged price as no reprice at all.
        priceMinor: toMinor(values.price),
        description: values.description?.trim() || null,
        salesStartsAt: values.salesWindow?.[0]?.toISOString() ?? null,
        salesEndsAt: values.salesWindow?.[1]?.toISOString() ?? null,
        maxPerBuyer: values.maxPerBuyer ?? null,
        sortOrder: values.sortOrder ?? 0,
      });
      setEditing(null);
      await load();
      toast.success('Ticket type updated.');
    } catch (error) {
      // Covers the 409 this panel tries to prevent: if the event was published in another tab, the
      // disabled-price guard above was computed from stale state and the server is the authority.
      toast.error(messageFrom(error) ?? 'Could not update the ticket type.');
    } finally {
      setSubmitting(false);
    }
  };

  const handleDeactivate = async (id: string) => {
    try {
      await deactivateTicketType(eventId, id);
      await load();
    } catch {
      toast.error('Could not deactivate the ticket type.');
    }
  };

  return (
    <Card title="Ticket types" style={{ marginBottom: 24 }} styles={{ body: { padding: 28 } }}>
      <Typography.Text type="secondary" style={{ display: 'block', marginBottom: 16 }}>
        The kinds of ticket this event sells — a name, a price, and optionally its own sales window
        and per-buyer limit. Defining a seat map creates these automatically from its price tiers,
        so you only need this panel to add one, correct a name, or retire one.
      </Typography.Text>

      {!isDraft && (
        <Alert
          type="info"
          showIcon
          style={{ marginBottom: 16 }}
          message="This event is published"
          description={`New types can still be added, and names, windows and limits can be changed. ${PRICE_LOCKED_NOTE}`}
        />
      )}

      {loadError ? (
        <LoadError onRetry={handleRetry} />
      ) : (
        <Table<TicketTypeResponse>
          size="small"
          rowKey="id"
          loading={loading}
          dataSource={types}
          pagination={false}
          locale={{ emptyText: 'No ticket types yet — defining a seat map will create them.' }}
          style={{ marginBottom: 24 }}
          columns={[
            {
              title: 'Name',
              dataIndex: 'name',
              render: (name: string, record) => (
                <Space>
                  <Typography.Text strong>{name}</Typography.Text>
                  {!record.isActive && <Tag>Inactive</Tag>}
                </Space>
              ),
            },
            {
              title: 'Price',
              dataIndex: 'priceMinor',
              render: (priceMinor: number) => formatMoney(priceMinor, currency),
            },
            {
              title: 'On sale',
              render: (_, record) =>
                record.salesStartsAt || record.salesEndsAt
                  ? `${formatDate(record.salesStartsAt)} → ${formatDate(record.salesEndsAt)}`
                  : "The event's own window",
            },
            {
              title: 'Limit',
              dataIndex: 'maxPerBuyer',
              render: (maxPerBuyer: number | null) =>
                maxPerBuyer === null ? 'No cap' : `${maxPerBuyer} per buyer`,
            },
            {
              title: '',
              render: (_, record) => (
                <Space>
                  <Button size="small" type="text" onClick={() => openEditor(record)}>
                    Edit
                  </Button>
                  {record.isActive && (
                    <Popconfirm
                      title="Retire this ticket type?"
                      description="It stops being offered. Seats and orders already using it are unaffected."
                      okText="Retire"
                      onConfirm={() => void handleDeactivate(record.id)}
                    >
                      <Button size="small" danger type="text">
                        Deactivate
                      </Button>
                    </Popconfirm>
                  )}
                </Space>
              ),
            },
          ]}
        />
      )}

      <Typography.Title level={5}>Add a ticket type</Typography.Title>
      <Form<TicketTypeFormValues>
        form={createForm}
        layout="vertical"
        initialValues={{ sortOrder: 0 }}
        onFinish={(values) => void handleCreate(values)}
      >
        <Space align="start" wrap size="large">
          <TicketTypeFields currency={currency} priceDisabled={false} />
          <Form.Item label=" ">
            <Button type="primary" htmlType="submit" loading={submitting}>
              Add type
            </Button>
          </Form.Item>
        </Space>
      </Form>

      <Modal
        title={`Edit “${editing?.name ?? ''}”`}
        open={editing !== null}
        onCancel={() => setEditing(null)}
        onOk={() => void editForm.submit()}
        okText="Save"
        confirmLoading={submitting}
        destroyOnHidden
      >
        <Form<TicketTypeFormValues>
          form={editForm}
          layout="vertical"
          onFinish={(values) => void handleEdit(values)}
        >
          <TicketTypeFields currency={currency} priceDisabled={!isDraft} />
        </Form>
      </Modal>
    </Card>
  );
}

/** The field set, shared by the create form and the edit modal so the two cannot drift apart. */
function TicketTypeFields({
  currency,
  priceDisabled,
}: {
  currency: string;
  priceDisabled: boolean;
}) {
  return (
    <>
      <Form.Item
        name="name"
        label="Name"
        rules={[
          { required: true, message: 'Required' },
          { max: 100, message: 'At most 100 characters' },
        ]}
      >
        <Input placeholder="Gold" style={{ width: 200 }} />
      </Form.Item>

      <Form.Item
        name="price"
        label={`Price (${currency})`}
        tooltip={priceDisabled ? PRICE_LOCKED_NOTE : undefined}
        rules={[
          { required: true, message: 'Required' },
          { type: 'number', min: 0 },
        ]}
      >
        <InputNumber min={0} step={1} disabled={priceDisabled} style={{ width: 160 }} />
      </Form.Item>

      <Form.Item name="description" label="Description" rules={[{ max: 500 }]}>
        <Input placeholder="What this ticket includes" style={{ width: 260 }} />
      </Form.Item>

      <Form.Item
        name="salesWindow"
        label="On sale"
        tooltip="Optional. Narrows the event's own on-sale window for this type only."
      >
        <DatePicker.RangePicker showTime />
      </Form.Item>

      <Form.Item
        name="maxPerBuyer"
        label="Max per buyer"
        tooltip="Optional, on top of the event's overall per-buyer limit."
        rules={[{ type: 'number', min: 1 }]}
      >
        <InputNumber min={1} placeholder="No cap" style={{ width: 130 }} />
      </Form.Item>

      <Form.Item name="sortOrder" label="Sort" tooltip="Lower sorts first in the buyer's list.">
        <InputNumber style={{ width: 90 }} />
      </Form.Item>
    </>
  );
}

function formatDate(iso: string | null): string {
  return iso ? new Date(iso).toLocaleDateString() : '—';
}

function messageFrom(error: unknown): string | undefined {
  return (error as { response?: { data?: { message?: string } } }).response?.data?.message;
}
