import { Button, Card, Col, Form, Input, InputNumber, Radio, Row, Select } from 'antd';
import { MinusCircleOutlined, PlusOutlined } from '@ant-design/icons';
import type { EntryGateResponse, SeatMapSectionInput } from '../../../services/catalog/catalogApi';
import { DEFAULT_SEAT_MAP_SECTION } from './seatMapSectionDefaults';

export interface SeatMapSectionsFieldsProps {
  /** The event's entry gates, for the per-section "restrict to a gate" picker. */
  entryGates: EntryGateResponse[];
  /**
   * Names already present in the seat map (case-insensitive) — only relevant when adding more
   * sections to an existing map, so a new section can't collide with one that's already saved.
   * Omit (or leave empty) when defining a brand-new map, where every name is necessarily new.
   */
  existingSectionNames?: string[];
  /**
   * Whether the "Add section"/remove-this-section affordances render — `false` locks the list to
   * exactly the sections already in `initialValues` (used when editing a single existing section,
   * where growing/shrinking the list makes no sense). Defaults to `true`.
   */
  allowAddRemove?: boolean;
}

/**
 * The repeatable "one card per section" fields inside a seat-map `Form`'s `sections` list —
 * shared by "Define seat map" (a brand-new map) and "Add sections" (appending to one that already
 * exists), so the two forms can't drift apart. Must be rendered inside a `Form<{ sections: ... }>`.
 */
export function SeatMapSectionsFields({
  entryGates,
  existingSectionNames = [],
  allowAddRemove = true,
}: SeatMapSectionsFieldsProps) {
  return (
    <Form.List name="sections">
      {(fields, { add, remove }) => (
        <>
          {fields.map((field, index) => (
            <Card
              key={field.key}
              size="small"
              title={`Section ${index + 1}`}
              style={{ marginBottom: 16 }}
              extra={
                allowAddRemove &&
                fields.length > 1 && <MinusCircleOutlined onClick={() => remove(field.name)} />
              }
            >
              <Row gutter={16}>
                <Col xs={24} sm={8}>
                  <Form.Item
                    name={[field.name, 'name']}
                    label="Section name"
                    rules={[
                      { required: true, message: 'Required' },
                      { max: 100 },
                      ({ getFieldValue }) => ({
                        validator: (_rule, value: string | undefined) => {
                          if (!value) {
                            return Promise.resolve();
                          }
                          const normalized = value.trim().toLowerCase();
                          const sections =
                            (getFieldValue('sections') as SeatMapSectionInput[] | undefined) ?? [];
                          const isDuplicateWithinForm = sections.some(
                            (section, i) =>
                              i !== field.name &&
                              section?.name &&
                              section.name.trim().toLowerCase() === normalized,
                          );
                          const isDuplicateWithExisting = existingSectionNames.some(
                            (name) => name.trim().toLowerCase() === normalized,
                          );
                          return isDuplicateWithinForm || isDuplicateWithExisting
                            ? Promise.reject(new Error('Section names must be unique.'))
                            : Promise.resolve();
                        },
                      }),
                    ]}
                  >
                    <Input placeholder="e.g. Orchestra" />
                  </Form.Item>
                </Col>
                <Col xs={24} sm={8}>
                  <Form.Item
                    name={[field.name, 'priceTier']}
                    label="Price tier"
                    rules={[{ required: true, message: 'Required' }, { max: 50 }]}
                  >
                    <Input placeholder="e.g. Gold" />
                  </Form.Item>
                </Col>
                <Col xs={24} sm={8}>
                  <Form.Item
                    name={[field.name, 'priceAmount']}
                    label="Price"
                    rules={[
                      { required: true, message: 'Required' },
                      { type: 'number', min: 0 },
                    ]}
                  >
                    <InputNumber min={0} style={{ width: '100%' }} />
                  </Form.Item>
                </Col>
                <Col xs={24} sm={16}>
                  <Form.Item
                    name={[field.name, 'allocationType']}
                    label="Allocation"
                    initialValue="Reserved"
                  >
                    <Radio.Group>
                      <Radio.Button value="Reserved">Reserved seating</Radio.Button>
                      <Radio.Button value="GeneralAdmission">General admission</Radio.Button>
                    </Radio.Group>
                  </Form.Item>
                </Col>
                <Col xs={24} sm={8}>
                  <Form.Item name={[field.name, 'entryGateId']} label="Entry gate (optional)">
                    <Select
                      allowClear
                      placeholder="No restriction"
                      options={entryGates.map((gate) => ({ value: gate.id, label: gate.name }))}
                    />
                  </Form.Item>
                </Col>
                <Form.Item shouldUpdate noStyle>
                  {({ getFieldValue }) => {
                    const allocationType: SeatMapSectionInput['allocationType'] =
                      getFieldValue(['sections', field.name, 'allocationType']) ?? 'Reserved';
                    return allocationType === 'Reserved' ? (
                      <>
                        <Col xs={12} sm={6}>
                          <Form.Item
                            name={[field.name, 'rows']}
                            label="Rows"
                            rules={[
                              { required: true, message: 'Required' },
                              { type: 'number', min: 1, max: 500 },
                            ]}
                          >
                            <InputNumber min={1} max={500} style={{ width: '100%' }} />
                          </Form.Item>
                        </Col>
                        <Col xs={12} sm={6}>
                          <Form.Item
                            name={[field.name, 'seatsPerRow']}
                            label="Seats per row"
                            rules={[
                              { required: true, message: 'Required' },
                              { type: 'number', min: 1, max: 500 },
                            ]}
                          >
                            <InputNumber min={1} max={500} style={{ width: '100%' }} />
                          </Form.Item>
                        </Col>
                      </>
                    ) : (
                      <Col xs={24} sm={12}>
                        <Form.Item
                          name={[field.name, 'capacity']}
                          label="Total capacity"
                          rules={[
                            { required: true, message: 'Required' },
                            { type: 'number', min: 1, max: 1000000 },
                          ]}
                        >
                          <InputNumber min={1} max={1000000} style={{ width: '100%' }} />
                        </Form.Item>
                      </Col>
                    );
                  }}
                </Form.Item>
              </Row>
            </Card>
          ))}
          {allowAddRemove && (
            <Button
              type="dashed"
              block
              onClick={() => add({ ...DEFAULT_SEAT_MAP_SECTION })}
              icon={<PlusOutlined />}
              style={{ marginBottom: 16 }}
            >
              Add section
            </Button>
          )}
        </>
      )}
    </Form.List>
  );
}
