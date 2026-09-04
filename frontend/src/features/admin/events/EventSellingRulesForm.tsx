import { useEffect, useState } from 'react';
import {
  Alert,
  Button,
  Checkbox,
  Col,
  DatePicker,
  Divider,
  Form,
  Input,
  InputNumber,
  Row,
  Typography,
} from 'antd';
import dayjs, { type Dayjs } from 'dayjs';
import { updateSellingRules, type EventResponse } from '../../../services/catalog/catalogApi';
import { toast } from '../../../components/common/feedback/toast';
import { StickyActionBar } from '../../../components/common/layout/StickyActionBar';
import { toMajor, toMinor } from '../../../utils/money';

interface EventSellingRulesFormProps {
  event: EventResponse;
  /** Called after a successful save, so the parent can re-fetch. */
  onSaved: () => void;
}

interface SellingRulesFormValues {
  onSaleAt?: Dayjs;
  maxTicketsPerBuyer?: number;
  requiresQueue?: boolean;
  taxRatePercent?: number;
  taxLabel?: string;
  /** Major units, as typed — converted to minor on submit. */
  bookingFeePerTicket?: number;
}

const SECTION_HEADING_STYLE = { marginTop: 0, marginBottom: 16 };

/**
 * The money and the rules that govern the **whole run** — the half of the old Schedule & venue form
 * that survived ADR-0039.
 *
 * Dates, doors, the booking cutoff and the venue all moved to the performances that own them: they
 * are different for every night, and pretending otherwise is what made a three-night run three
 * separate events. What is left here is genuinely event-level: a run is advertised as going on sale
 * once, gated by one waiting room, and capped at so many tickets per person across every night.
 *
 * Every field is part of what a ticket holder paid for, so after publish the form renders read-only
 * rather than disappearing: an organizer still needs to *see* the fee they set, and hiding the
 * section makes it look like the setting is gone rather than fixed. The server refuses the write
 * regardless (409), so the read-only state is a courtesy, not the guard.
 */
export function EventSellingRulesForm({ event, onSaved }: EventSellingRulesFormProps) {
  const [form] = Form.useForm<SellingRulesFormValues>();
  const [saving, setSaving] = useState(false);
  const locked = event.status !== 'Draft';

  useEffect(() => {
    form.setFieldsValue({
      onSaleAt: event.onSaleAt ? dayjs(event.onSaleAt) : undefined,
      maxTicketsPerBuyer: event.maxTicketsPerBuyer ?? undefined,
      requiresQueue: event.requiresQueue,
      taxRatePercent: event.taxRatePercent ?? undefined,
      taxLabel: event.taxLabel ?? undefined,
      bookingFeePerTicket: event.bookingFeePerTicketMinor
        ? toMajor(event.bookingFeePerTicketMinor)
        : undefined,
    });
  }, [event, form]);

  const handleSave = async (values: SellingRulesFormValues) => {
    setSaving(true);
    try {
      await updateSellingRules(event.id, {
        onSaleAt: values.onSaleAt?.toISOString() ?? null,
        maxTicketsPerBuyer: values.maxTicketsPerBuyer ?? null,
        requiresQueue: values.requiresQueue ?? false,
        taxRatePercent: values.taxRatePercent ?? null,
        taxLabel: values.taxLabel?.trim() || null,
        bookingFeePerTicketMinor: values.bookingFeePerTicket
          ? toMinor(values.bookingFeePerTicket)
          : 0,
      });
      toast.success('Selling rules saved.');
      onSaved();
    } catch {
      // Covers the 409 the read-only state tries to prevent: this component's idea of the event's
      // status is stale if someone published in another tab.
      toast.error('Could not save — check the values, or whether the event has been published.');
    } finally {
      setSaving(false);
    }
  };

  return (
    <Form<SellingRulesFormValues>
      form={form}
      layout="vertical"
      disabled={locked}
      onFinish={(values) => void handleSave(values)}
    >
      {locked && (
        <Alert
          type="info"
          showIcon
          style={{ marginBottom: 24 }}
          message="Fixed now that the event is published"
          description={
            'The on-sale time, buyer limit, tax and fees are part of what a ticket holder bought, ' +
            'so they stop being editable at publish. The title, description, images and contact ' +
            'details are on the Event page tab and can still be changed; each performance has its ' +
            'own times and venue on the Performances tab.'
          }
        />
      )}

      <Typography.Title level={5} style={SECTION_HEADING_STYLE}>
        On sale
      </Typography.Title>
      <Alert
        type="info"
        showIcon
        style={{ marginBottom: 20 }}
        message="These apply to every performance"
        description={
          'A run goes on sale once, at one advertised moment, behind one waiting room — so these ' +
          'live here rather than on each night. A limit counted per night would let one buyer ' +
          'take the cap three times over on a three-night run. Each performance sets its own ' +
          'times and booking cutoff on the Performances tab.'
        }
      />
      <Row gutter={20}>
        <Col xs={24} sm={12} md={6}>
          <Form.Item
            name="onSaleAt"
            label="On sale from"
            tooltip="Holds are refused before this moment, across every performance."
          >
            <DatePicker showTime style={{ width: '100%' }} />
          </Form.Item>
        </Col>
        <Col xs={24} sm={12} md={6}>
          <Form.Item
            name="maxTicketsPerBuyer"
            label="Max tickets per buyer"
            tooltip="Counted across every performance of this run, not per night. Leave empty for no limit."
            rules={[{ type: 'number', min: 1, max: 100 }]}
          >
            <InputNumber min={1} max={100} style={{ width: '100%' }} placeholder="No limit" />
          </Form.Item>
        </Col>
        <Col xs={24} sm={12} md={6}>
          <Form.Item
            name="requiresQueue"
            label=" "
            valuePropName="checked"
            tooltip="Gates seat selection behind a virtual waiting room for high-demand on-sales. One waiting room covers the whole run."
          >
            <Checkbox>Requires queue (waiting room)</Checkbox>
          </Form.Item>
        </Col>
      </Row>

      <Divider />
      <Typography.Title level={5} style={SECTION_HEADING_STYLE}>
        Fees and tax
      </Typography.Title>
      <Row gutter={20}>
        <Col xs={24} sm={12} md={6}>
          <Form.Item
            name="taxRatePercent"
            label="Tax rate %"
            tooltip="Applied to the order total after any discount code. Leave empty for no tax."
            rules={[{ type: 'number', min: 0, max: 100 }]}
          >
            <InputNumber
              min={0}
              max={100}
              step={0.5}
              style={{ width: '100%' }}
              placeholder="No tax"
            />
          </Form.Item>
        </Col>
        <Col xs={24} sm={12} md={6}>
          <Form.Item
            name="taxLabel"
            label="Tax label"
            tooltip="What buyers see next to the tax line, e.g. “GST 18%”."
            rules={[{ max: 50 }]}
          >
            <Input placeholder="e.g. GST 18%" />
          </Form.Item>
        </Col>
        <Col xs={24} sm={12} md={6}>
          <Form.Item
            name="bookingFeePerTicket"
            label={`Booking fee per ticket (${event.currency})`}
            tooltip="Charged on every ticket, taxed at the rate above, and not refunded if the buyer cancels. Leave empty for none."
            rules={[{ type: 'number', min: 0 }]}
          >
            <InputNumber min={0} step={1} style={{ width: '100%' }} placeholder="No fee" />
          </Form.Item>
        </Col>
      </Row>

      {/* No bar at all when locked, rather than a disabled one: after publish there is nothing
          here to save, and a greyed-out button invites a click that can only ever be refused. */}
      {!locked && (
        <StickyActionBar bleed={28}>
          <Button type="primary" htmlType="submit" loading={saving}>
            Save selling rules
          </Button>
        </StickyActionBar>
      )}
    </Form>
  );
}
