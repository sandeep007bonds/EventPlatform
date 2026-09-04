import { Button, Col, Form, Input, InputNumber, Row, Select } from 'antd';
import { TIME_ZONE_OPTIONS, type VenueFormValues } from './venueFormValues';

/**
 * The create/edit fields for a venue — shared so the two paths cannot drift apart in what they
 * validate. The time zone lives here rather than on an event because it is a property of the
 * building: every performance held there runs in it, and buyers see the times in it.
 */
export function VenueForm({
  initialValues,
  saving,
  submitLabel = 'Save',
  onSubmit,
}: {
  initialValues?: VenueFormValues;
  saving: boolean;
  submitLabel?: string;
  onSubmit: (values: VenueFormValues) => void;
}) {
  const [form] = Form.useForm<VenueFormValues>();

  return (
    <Form<VenueFormValues>
      form={form}
      layout="vertical"
      initialValues={initialValues}
      onFinish={onSubmit}
    >
      <Row gutter={16}>
        <Col xs={24} sm={12}>
          <Form.Item name="name" label="Venue name" rules={[{ required: true }, { max: 200 }]}>
            <Input placeholder="e.g. Wankhede Stadium" />
          </Form.Item>
        </Col>
        <Col xs={24} sm={12}>
          <Form.Item name="venueType" label="Type (optional)" rules={[{ max: 100 }]}>
            <Input placeholder="e.g. Stadium, Theatre, Arena" />
          </Form.Item>
        </Col>
        <Col xs={24} sm={12}>
          <Form.Item
            name="timeZoneId"
            label="Time zone"
            tooltip="Buyers see every performance here in this zone, wherever they are."
          >
            <Select
              showSearch
              allowClear
              placeholder="e.g. Asia/Kolkata"
              optionFilterProp="value"
              options={TIME_ZONE_OPTIONS}
            />
          </Form.Item>
        </Col>
        <Col xs={24} sm={12}>
          <Form.Item
            name="addressLine1"
            label="Address line 1"
            rules={[{ required: true }, { max: 200 }]}
          >
            <Input />
          </Form.Item>
        </Col>
        <Col xs={24} sm={12}>
          <Form.Item name="addressLine2" label="Address line 2" rules={[{ max: 200 }]}>
            <Input />
          </Form.Item>
        </Col>
        <Col xs={24} sm={12}>
          <Form.Item name="city" label="City" rules={[{ required: true }, { max: 100 }]}>
            <Input />
          </Form.Item>
        </Col>
        <Col xs={24} sm={8}>
          <Form.Item name="region" label="State / region" rules={[{ max: 100 }]}>
            <Input />
          </Form.Item>
        </Col>
        <Col xs={24} sm={8}>
          <Form.Item name="postalCode" label="Postal code" rules={[{ max: 20 }]}>
            <Input />
          </Form.Item>
        </Col>
        <Col xs={24} sm={8}>
          <Form.Item
            name="country"
            label="Country (ISO 3166-1 alpha-2)"
            rules={[{ required: true, len: 2 }]}
          >
            <Input maxLength={2} placeholder="IN" style={{ textTransform: 'uppercase' }} />
          </Form.Item>
        </Col>
        <Col xs={24} sm={12}>
          <Form.Item
            name="latitude"
            label="Latitude (optional)"
            rules={[{ type: 'number', min: -90, max: 90 }]}
          >
            <InputNumber min={-90} max={90} step={0.000001} style={{ width: '100%' }} />
          </Form.Item>
        </Col>
        <Col xs={24} sm={12}>
          <Form.Item
            name="longitude"
            label="Longitude (optional)"
            rules={[{ type: 'number', min: -180, max: 180 }]}
          >
            <InputNumber min={-180} max={180} step={0.000001} style={{ width: '100%' }} />
          </Form.Item>
        </Col>
      </Row>

      <Button type="primary" htmlType="submit" loading={saving}>
        {submitLabel}
      </Button>
    </Form>
  );
}
