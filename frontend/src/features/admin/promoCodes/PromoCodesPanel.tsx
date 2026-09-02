import { useCallback, useEffect, useState } from 'react';
import {
  Button,
  Card,
  DatePicker,
  Form,
  Input,
  InputNumber,
  Popconfirm,
  Select,
  Space,
  Switch,
  Table,
  Tag,
  Typography,
} from 'antd';
import type { Dayjs } from 'dayjs';
import {
  createPromoCode,
  deactivatePromoCode,
  listPromoCodes,
  type DiscountType,
  type PromoCodeResponse,
} from '../../../services/catalog/catalogApi';
import { LoadError } from '../../../components/common/errors/LoadError';
import { toast } from '../../../components/common/feedback/toast';
import { formatMoney } from '../../../utils/money';

interface PromoCodesPanelProps {
  eventId: string;
  /** The event's currency, for rendering fixed-amount discounts. */
  currency: string;
  /** Every price tier in the event's seat map, for the applicability picker. */
  priceTiers: string[];
}

interface PromoCodeFormValues {
  code: string;
  description?: string;
  discountType: DiscountType;
  discountValue: number;
  validity?: [Dayjs, Dayjs];
  isPublic: boolean;
  maxRedemptions?: number | null;
  maxRedemptionsPerBuyer?: number | null;
  priceTiers?: string[];
}

/**
 * Organizer's discount-code manager for one event. Create codes, see what exists, retire what's
 * finished.
 *
 * There is no edit, by design (see Catalog's `PromoCode`): a code that has already been advertised
 * should not silently change what it's worth. Deactivate it and create another.
 */
export function PromoCodesPanel({ eventId, currency, priceTiers }: PromoCodesPanelProps) {
  const [form] = Form.useForm<PromoCodeFormValues>();
  const [codes, setCodes] = useState<PromoCodeResponse[]>([]);
  const [loading, setLoading] = useState(true);
  const [loadError, setLoadError] = useState(false);
  const [submitting, setSubmitting] = useState(false);

  // Deliberately sets no state synchronously — `loading` starts true and only ever flips inside a
  // promise callback, so the mount effect below doesn't trigger a cascading render (the same shape
  // QueueSettingsPanel and SeatBlockPanel use).
  const load = useCallback(
    () =>
      listPromoCodes(eventId)
        .then((result) => {
          setCodes(result);
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

  const handleCreate = async (values: PromoCodeFormValues) => {
    setSubmitting(true);
    try {
      await createPromoCode(eventId, {
        code: values.code.trim(),
        description: values.description?.trim() || null,
        discountType: values.discountType,
        discountValue: values.discountValue,
        validFrom: values.validity?.[0]?.toISOString() ?? null,
        validTo: values.validity?.[1]?.toISOString() ?? null,
        isPublic: values.isPublic,
        maxRedemptions: values.maxRedemptions ?? null,
        maxRedemptionsPerBuyer: values.maxRedemptionsPerBuyer ?? null,
        priceTiers: values.priceTiers ?? [],
      });
      form.resetFields();
      await load();
      toast.success('Promo code created.');
    } catch (error) {
      const body = (error as { response?: { data?: { message?: string } } }).response?.data;
      toast.error(body?.message ?? 'Could not create the promo code.');
    } finally {
      setSubmitting(false);
    }
  };

  const handleDeactivate = async (id: string) => {
    try {
      await deactivatePromoCode(eventId, id);
      await load();
    } catch {
      toast.error('Could not deactivate the promo code.');
    }
  };

  const describeDiscount = (record: PromoCodeResponse) =>
    record.discountType === 'Percentage'
      ? `${record.discountValue}% off`
      : `${formatMoney(Math.round(record.discountValue * 100), currency)} off`;

  return (
    <Card title="Promo codes" style={{ marginBottom: 24 }} styles={{ body: { padding: 28 } }}>
      <Typography.Text type="secondary" style={{ display: 'block', marginBottom: 16 }}>
        Discount codes for this event. Tax is calculated on the price <em>after</em> a discount is
        applied. Codes cannot be edited once created — deactivate and create another instead.
      </Typography.Text>

      {loadError ? (
        <LoadError onRetry={handleRetry} />
      ) : (
        <Table<PromoCodeResponse>
          size="small"
          rowKey="id"
          loading={loading}
          dataSource={codes}
          pagination={false}
          locale={{ emptyText: 'No promo codes yet.' }}
          style={{ marginBottom: 24 }}
          columns={[
            {
              title: 'Code',
              dataIndex: 'code',
              render: (code: string, record) => (
                <Space>
                  <Typography.Text strong>{code}</Typography.Text>
                  {record.isPublic && <Tag color="blue">Public</Tag>}
                  {!record.isActive && <Tag>Inactive</Tag>}
                </Space>
              ),
            },
            { title: 'Discount', render: (_, record) => describeDiscount(record) },
            {
              title: 'Applies to',
              render: (_, record) =>
                record.priceTiers.length === 0 ? (
                  <Typography.Text type="secondary">All tiers</Typography.Text>
                ) : (
                  record.priceTiers.map((tier) => <Tag key={tier}>{tier}</Tag>)
                ),
            },
            {
              title: 'Valid',
              render: (_, record) =>
                record.validFrom || record.validTo
                  ? `${record.validFrom ? new Date(record.validFrom).toLocaleDateString() : '—'} → ${
                      record.validTo ? new Date(record.validTo).toLocaleDateString() : '—'
                    }`
                  : 'Always',
            },
            {
              title: 'Limit',
              render: (_, record) => {
                const parts: string[] = [];
                if (record.maxRedemptions != null) {
                  parts.push(`${record.maxRedemptions} total`);
                }
                if (record.maxRedemptionsPerBuyer != null) {
                  parts.push(`${record.maxRedemptionsPerBuyer} per buyer`);
                }
                return parts.length > 0 ? parts.join(', ') : 'Unlimited';
              },
            },
            {
              title: '',
              render: (_, record) =>
                record.isActive ? (
                  <Popconfirm
                    title="Deactivate this code?"
                    description="Buyers will no longer be able to redeem it. This cannot be undone."
                    okText="Deactivate"
                    onConfirm={() => void handleDeactivate(record.id)}
                  >
                    <Button size="small" danger type="text">
                      Deactivate
                    </Button>
                  </Popconfirm>
                ) : null,
            },
          ]}
        />
      )}

      <Typography.Title level={5}>Add a code</Typography.Title>
      <Form<PromoCodeFormValues>
        form={form}
        layout="vertical"
        initialValues={{ discountType: 'Percentage', isPublic: false }}
        onFinish={(values) => void handleCreate(values)}
      >
        <Space align="start" wrap size="large">
          <Form.Item
            name="code"
            label="Code"
            rules={[
              { required: true, message: 'Required' },
              { max: 50, message: 'At most 50 characters' },
              {
                pattern: /^[A-Za-z0-9_-]+$/,
                message: 'Letters, digits, hyphens and underscores only',
              },
            ]}
          >
            <Input placeholder="EARLYBIRD" style={{ width: 200 }} />
          </Form.Item>

          <Form.Item name="discountType" label="Type" rules={[{ required: true }]}>
            <Select
              style={{ width: 160 }}
              options={[
                { value: 'Percentage', label: 'Percentage' },
                { value: 'FixedAmount', label: 'Fixed amount' },
              ]}
            />
          </Form.Item>

          {/* dependencies: the bounds differ by type — a percentage is capped at 100, an amount isn't. */}
          <Form.Item noStyle dependencies={['discountType']}>
            {({ getFieldValue }) => {
              const isPercentage = getFieldValue('discountType') === 'Percentage';
              return (
                <Form.Item
                  name="discountValue"
                  label={isPercentage ? 'Percent off' : `Amount off (${currency})`}
                  rules={[
                    { required: true, message: 'Required' },
                    {
                      type: 'number',
                      min: 0.01,
                      max: isPercentage ? 100 : undefined,
                      message: isPercentage ? 'Between 0.01 and 100' : 'Must be greater than zero',
                    },
                  ]}
                >
                  <InputNumber style={{ width: 160 }} min={0.01} step={1} />
                </Form.Item>
              );
            }}
          </Form.Item>

          <Form.Item name="validity" label="Valid between (optional)">
            <DatePicker.RangePicker showTime />
          </Form.Item>
        </Space>

        <Space align="start" wrap size="large">
          <Form.Item
            name="priceTiers"
            label="Applies to tiers"
            tooltip="Leave empty to discount every tier."
          >
            <Select
              mode="multiple"
              allowClear
              placeholder="All tiers"
              style={{ minWidth: 240 }}
              options={priceTiers.map((tier) => ({ value: tier, label: tier }))}
            />
          </Form.Item>

          <Form.Item
            name="maxRedemptions"
            label="Max total uses"
            rules={[{ type: 'number', min: 1, message: 'At least 1' }]}
          >
            <InputNumber style={{ width: 150 }} min={1} placeholder="Unlimited" />
          </Form.Item>

          <Form.Item
            name="maxRedemptionsPerBuyer"
            label="Max per buyer"
            rules={[{ type: 'number', min: 1, message: 'At least 1' }]}
          >
            <InputNumber style={{ width: 150 }} min={1} placeholder="Unlimited" />
          </Form.Item>

          <Form.Item
            name="isPublic"
            label="Show to buyers"
            valuePropName="checked"
            tooltip="Public codes appear in a picker at checkout. Private ones only work if typed in."
          >
            <Switch />
          </Form.Item>
        </Space>

        <Form.Item name="description" label="Note (optional, organizer-only)">
          <Input maxLength={500} placeholder="What this code is for" style={{ maxWidth: 480 }} />
        </Form.Item>

        <Button type="primary" htmlType="submit" loading={submitting}>
          Add code
        </Button>
      </Form>
    </Card>
  );
}
