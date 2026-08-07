import {
  Button,
  Card,
  Checkbox,
  Col,
  ConfigProvider,
  DatePicker,
  Divider,
  Form,
  Input,
  InputNumber,
  Row,
  Select,
  Tag,
  Typography,
} from 'antd';
import { MinusCircleOutlined, PlusOutlined } from '@ant-design/icons';
import dayjs, { type Dayjs } from 'dayjs';
import { NEW_TOUR_OPTION } from '../eventGroups/EventGroupPicker';
import { DEFAULT_LEG, type EventLegFormValues, type LegStatus } from './eventLegDefaults';

const CURRENCIES = ['USD', 'EUR', 'GBP', 'INR'];

export interface EventLegFieldsProps {
  /** The tour's own advertised date range, if one is already picked — a leg's dates must lie
   * inside it (mirrored server-side; this is the inline-feedback copy, not the enforcement). */
  groupRange: { startsAt: string | null; endsAt: string | null } | null;
  /** One entry per leg, parallel to the `legs` `Form.List` — tracks what's already been submitted
   * successfully so a retry after a partial failure never re-creates the same leg twice. */
  legStatuses: LegStatus[];
  /** Server error message for a `'failed'` leg, parallel to `legStatuses`. */
  legErrors: (string | undefined)[];
  /** Keeps `legStatuses`/`legErrors` in sync when a leg card is removed. */
  onRemoveLeg: (index: number) => void;
  /** Disables every field/button while a submit is in flight. */
  disabled?: boolean;
}

/**
 * The repeatable "one card per city/date" fields inside `CreateEventPage`'s `Form` — every field
 * `CreateEventPage` used to render once now lives here, addressed via `[field.name, ...]` nested
 * paths, following `SeatMapSectionsFields.tsx`'s exact shape (one `Card` per item, a trailing
 * dashed "Add" button, a remove affordance hidden once there's only one item left). Clicking "Add
 * another city/date" also seeds the new leg's `currency`/`country` from the first leg's current
 * values (mirrors `SeatMapSectionsFields.tsx`'s `DEFAULT_SEAT_MAP_SECTION` seed-on-add pattern) and,
 * if no tour is picked yet, switches the (now-required) tour picker to "+ New tour" automatically —
 * a multi-leg batch always needs a tour to attach its legs to.
 */
export function EventLegFields({
  groupRange,
  legStatuses,
  legErrors,
  onRemoveLeg,
  disabled = false,
}: EventLegFieldsProps) {
  const form = Form.useFormInstance();

  return (
    <Form.List name="legs">
      {(fields, { add, remove }) => (
        <>
          {fields.map((field, index) => {
            const status = legStatuses[index] ?? 'pending';
            const isCreated = status === 'created';
            return (
              <Card
                key={field.key}
                size="small"
                title={fields.length === 1 ? 'Event details' : `City/date ${index + 1}`}
                style={{ marginBottom: 16 }}
                extra={
                  <>
                    {status === 'created' && <Tag color="success">Created</Tag>}
                    {status === 'failed' && <Tag color="error">Failed</Tag>}
                    {!isCreated && fields.length > 1 && (
                      <MinusCircleOutlined
                        style={{ marginLeft: 8 }}
                        onClick={() => {
                          remove(field.name);
                          onRemoveLeg(index);
                        }}
                      />
                    )}
                  </>
                }
              >
                <ConfigProvider componentDisabled={disabled || isCreated}>
                  {status === 'failed' && legErrors[index] && (
                    <Typography.Text type="danger" style={{ display: 'block', marginBottom: 12 }}>
                      {legErrors[index]}
                    </Typography.Text>
                  )}
                  <Row gutter={16}>
                    <Col span={24}>
                      <Form.Item
                        name={[field.name, 'title']}
                        label="Title"
                        rules={[{ required: true }, { max: 200 }]}
                      >
                        <Input placeholder="e.g. Coldplay: Music of the Spheres" size="large" />
                      </Form.Item>
                    </Col>
                    <Col xs={24} sm={8}>
                      <Form.Item
                        name={[field.name, 'startsAt']}
                        label="Starts at"
                        rules={[
                          { required: true },
                          {
                            validator: (_rule, value: Dayjs | undefined) => {
                              if (value && !value.isAfter(dayjs())) {
                                return Promise.reject(
                                  new Error('Starts at must be in the future.'),
                                );
                              }
                              if (
                                value &&
                                groupRange?.startsAt &&
                                value.isBefore(dayjs(groupRange.startsAt))
                              ) {
                                return Promise.reject(
                                  new Error(
                                    "Starts at must be on or after the tour's own start date.",
                                  ),
                                );
                              }
                              return Promise.resolve();
                            },
                          },
                        ]}
                      >
                        <DatePicker showTime style={{ width: '100%' }} />
                      </Form.Item>
                    </Col>
                    <Col xs={24} sm={8}>
                      <Form.Item
                        name={[field.name, 'endsAt']}
                        label="Ends at"
                        dependencies={[['legs', field.name, 'startsAt']]}
                        rules={[
                          { required: true },
                          ({ getFieldValue }) => ({
                            validator: (_rule, value: Dayjs | undefined) => {
                              const startsAt = getFieldValue(['legs', field.name, 'startsAt']) as
                                Dayjs | undefined;
                              if (value && startsAt && !value.isAfter(startsAt)) {
                                return Promise.reject(
                                  new Error('Ends at must be after Starts at.'),
                                );
                              }
                              if (
                                value &&
                                groupRange?.endsAt &&
                                value.isAfter(dayjs(groupRange.endsAt))
                              ) {
                                return Promise.reject(
                                  new Error(
                                    "Ends at must be on or before the tour's own end date.",
                                  ),
                                );
                              }
                              return Promise.resolve();
                            },
                          }),
                        ]}
                      >
                        <DatePicker showTime style={{ width: '100%' }} />
                      </Form.Item>
                    </Col>
                    <Col xs={24} sm={8}>
                      <Form.Item
                        name={[field.name, 'currency']}
                        label="Currency"
                        rules={[{ required: true }]}
                      >
                        <Select
                          options={CURRENCIES.map((code) => ({ value: code, label: code }))}
                        />
                      </Form.Item>
                    </Col>
                    <Col xs={24} sm={8}>
                      <Form.Item
                        name={[field.name, 'maxTicketsPerBuyer']}
                        label="Max tickets per buyer (optional)"
                        rules={[{ type: 'number', min: 1 }]}
                      >
                        <InputNumber min={1} style={{ width: '100%' }} placeholder="No limit" />
                      </Form.Item>
                    </Col>
                    <Col xs={24} sm={8}>
                      <Form.Item
                        name={[field.name, 'requiresQueue']}
                        label=" "
                        valuePropName="checked"
                        tooltip="Gates seat selection behind a virtual waiting room for high-demand on-sales."
                      >
                        <Checkbox>Requires queue (waiting room)</Checkbox>
                      </Form.Item>
                    </Col>
                  </Row>

                  <Divider titlePlacement="left">Location</Divider>
                  <Row gutter={16}>
                    <Col xs={24} sm={12}>
                      <Form.Item
                        name={[field.name, 'locationName']}
                        label="Venue name"
                        rules={[{ required: true }, { max: 200 }]}
                      >
                        <Input placeholder="e.g. Wankhede Stadium" />
                      </Form.Item>
                    </Col>
                    <Col xs={24} sm={12}>
                      <Form.Item
                        name={[field.name, 'addressLine1']}
                        label="Address line 1"
                        rules={[{ required: true }, { max: 200 }]}
                      >
                        <Input />
                      </Form.Item>
                    </Col>
                    <Col xs={24} sm={12}>
                      <Form.Item
                        name={[field.name, 'addressLine2']}
                        label="Address line 2"
                        rules={[{ max: 200 }]}
                      >
                        <Input />
                      </Form.Item>
                    </Col>
                    <Col xs={24} sm={12}>
                      <Form.Item
                        name={[field.name, 'city']}
                        label="City"
                        rules={[{ required: true }, { max: 100 }]}
                      >
                        <Input />
                      </Form.Item>
                    </Col>
                    <Col xs={24} sm={8}>
                      <Form.Item
                        name={[field.name, 'region']}
                        label="State / region"
                        rules={[{ max: 100 }]}
                      >
                        <Input />
                      </Form.Item>
                    </Col>
                    <Col xs={24} sm={8}>
                      <Form.Item
                        name={[field.name, 'postalCode']}
                        label="Postal code"
                        rules={[{ max: 20 }]}
                      >
                        <Input />
                      </Form.Item>
                    </Col>
                    <Col xs={24} sm={8}>
                      <Form.Item
                        name={[field.name, 'country']}
                        label="Country (ISO 3166-1 alpha-2)"
                        rules={[{ required: true, len: 2 }]}
                      >
                        <Input
                          maxLength={2}
                          placeholder="US"
                          style={{ textTransform: 'uppercase' }}
                        />
                      </Form.Item>
                    </Col>
                    <Col xs={24} sm={12}>
                      <Form.Item
                        name={[field.name, 'latitude']}
                        label="Latitude (optional)"
                        rules={[{ type: 'number', min: -90, max: 90 }]}
                      >
                        <InputNumber min={-90} max={90} step={0.000001} style={{ width: '100%' }} />
                      </Form.Item>
                    </Col>
                    <Col xs={24} sm={12}>
                      <Form.Item
                        name={[field.name, 'longitude']}
                        label="Longitude (optional)"
                        rules={[{ type: 'number', min: -180, max: 180 }]}
                      >
                        <InputNumber
                          min={-180}
                          max={180}
                          step={0.000001}
                          style={{ width: '100%' }}
                        />
                      </Form.Item>
                    </Col>
                  </Row>
                </ConfigProvider>
              </Card>
            );
          })}
          <Button
            type="dashed"
            block
            disabled={disabled}
            icon={<PlusOutlined />}
            style={{ marginBottom: 16 }}
            onClick={() => {
              const legs =
                (form.getFieldValue('legs') as Partial<EventLegFormValues>[] | undefined) ?? [];
              const firstLeg = legs[0];
              add({
                ...DEFAULT_LEG,
                currency: firstLeg?.currency ?? DEFAULT_LEG.currency,
                country: firstLeg?.country,
              });

              const currentGroupId = form.getFieldValue('eventGroupId') as string | undefined;
              if (!currentGroupId) {
                form.setFieldValue('eventGroupId', NEW_TOUR_OPTION);
              }
            }}
          >
            Add another city/date
          </Button>
        </>
      )}
    </Form.List>
  );
}
