import { useEffect, useState } from 'react';
import {
  Alert,
  Button,
  Card,
  Divider,
  Form,
  Input,
  InputNumber,
  Modal,
  Select,
  Space,
  Tag,
  Typography,
} from 'antd';
import { MinusCircleOutlined, PlusOutlined } from '@ant-design/icons';
import type { AxiosError } from 'axios';
import {
  getSeatMap,
  publishSeatMap,
  saveSeatMapLayout,
  startSeatMapDraft,
  type SeatMapResponse,
  type SeatMapSectionInput,
  type VenueGateResponse,
} from '../../../services/venue/venueApi';
import { toast } from '../../../components/common/feedback/toast';

/** One reserved-seat block, as the form collects it. */
interface SectionFields {
  code: string;
  name: string;
  gateId?: string | null;
  rows: number;
  seatsPerRow: number;
  /** The label of the first row; subsequent rows continue from it (A, B, C… or 1, 2, 3…). */
  firstRowLabel: string;
  /** The number of the first seat in each row. */
  firstSeatNumber: number;
}

/** One capacity-only block, as the form collects it. */
interface AreaFields {
  code: string;
  name: string;
  capacity: number;
  gateId?: string | null;
}

interface LayoutFormValues {
  sections: SectionFields[];
  admissionAreas: AreaFields[];
}

const DEFAULT_SECTION: SectionFields = {
  code: '',
  name: '',
  gateId: null,
  rows: 10,
  seatsPerRow: 20,
  firstRowLabel: 'A',
  firstSeatNumber: 1,
};

const DEFAULT_AREA: AreaFields = { code: '', name: '', capacity: 100, gateId: null };

/**
 * The form-based seat-map editor: reserved blocks described as rows × seats, and admission areas
 * described as a capacity. No canvas — that is the graphical designer, a later step.
 *
 * A map with **no shapes at all is valid** and publishes fine; Venue only rejects a map that is
 * *partly* drawn, because a plan with a hole in it is worse than no plan (a buyer cannot tell a
 * missing block from a sold-out one). So this editor sends no elements, deliberately, and the
 * designer will fill them in later without invalidating anything made here.
 *
 * **Codes are the contract.** A performance's allocation map keys on a block's code, so renaming a
 * block is safe and changing its code is not — the allocation silently stops matching. The form
 * says so, and the code field is only editable while the block is new to this draft.
 */
export function SeatMapEditorModal({
  seatMapId,
  gates,
  onClose,
  onChanged,
}: {
  seatMapId: string;
  gates: VenueGateResponse[];
  onClose: () => void;
  onChanged: () => void;
}) {
  const [form] = Form.useForm<LayoutFormValues>();
  const [seatMap, setSeatMap] = useState<SeatMapResponse | null>(null);
  const [loadFailed, setLoadFailed] = useState(false);
  const [saving, setSaving] = useState(false);
  const [publishing, setPublishing] = useState(false);
  const [validationErrors, setValidationErrors] = useState<string[]>([]);
  const [reloadToken, setReloadToken] = useState(0);

  useEffect(() => {
    let cancelled = false;

    getSeatMap(seatMapId)
      .then((map) => {
        if (cancelled) {
          return;
        }
        setSeatMap(map);
        form.setFieldsValue({
          sections: map.version.sections.map((section) => ({
            code: section.code,
            name: section.name,
            gateId: section.gateId,
            rows: section.rows.length,
            seatsPerRow: section.rows[0]?.seats.length ?? 0,
            firstRowLabel: section.rows[0]?.label ?? 'A',
            firstSeatNumber: Number(section.rows[0]?.seats[0]?.number ?? 1),
          })),
          admissionAreas: map.version.admissionAreas.map((area) => ({
            code: area.code,
            name: area.name,
            capacity: area.capacity,
            gateId: area.gateId,
          })),
        });
      })
      .catch(() => {
        if (!cancelled) {
          setLoadFailed(true);
        }
      });

    return () => {
      cancelled = true;
    };
  }, [seatMapId, form, reloadToken]);

  const version = seatMap?.version;
  const editable = version?.status === 'Draft';

  const handleSave = async (values: LayoutFormValues) => {
    setSaving(true);
    setValidationErrors([]);
    try {
      await saveSeatMapLayout(seatMapId, {
        sections: (values.sections ?? []).map(toSectionInput),
        admissionAreas: (values.admissionAreas ?? []).map((area) => ({
          code: area.code.trim(),
          name: area.name.trim(),
          capacity: area.capacity,
          displayOrder: 0,
          gateId: area.gateId || null,
        })),
      });
      toast.success('Draft saved.');
      setReloadToken((token) => token + 1);
      onChanged();
    } catch (error) {
      const body = (error as AxiosError<{ message?: string }>).response?.data;
      toast.error(
        typeof body === 'string' ? body : (body?.message ?? 'Could not save this layout.'),
      );
    } finally {
      setSaving(false);
    }
  };

  const handleStartDraft = async () => {
    setSaving(true);
    try {
      await startSeatMapDraft(seatMapId);
      toast.success('New draft opened, pre-filled from the published layout.');
      setReloadToken((token) => token + 1);
      onChanged();
    } catch (error) {
      const body = (error as AxiosError<{ message?: string }>).response?.data;
      toast.error(body?.message ?? 'Could not open a new draft.');
    } finally {
      setSaving(false);
    }
  };

  const handlePublish = async () => {
    setPublishing(true);
    setValidationErrors([]);
    try {
      const result = await publishSeatMap(seatMapId);
      toast.success(`Published v${result.versionNumber} — ${result.capacity} seats.`);
      setReloadToken((token) => token + 1);
      onChanged();
    } catch (error) {
      // Venue answers an invalid publish with *every* problem, not the first — the person fixing a
      // stadium plan needs the whole list at once, so show the whole list.
      const body = (error as AxiosError<{ errors?: { code: string; message: string }[] }>).response
        ?.data;
      if (body?.errors?.length) {
        setValidationErrors(body.errors.map((entry) => entry.message));
      } else {
        toast.error('Could not publish this seat map.');
      }
    } finally {
      setPublishing(false);
    }
  };

  return (
    <Modal
      open
      width={900}
      title={
        <Space>
          {seatMap?.name ?? 'Seat map'}
          {version && (
            <Tag color={version.status === 'Published' ? 'green' : 'default'}>
              v{version.versionNumber} · {version.status}
            </Tag>
          )}
          {version && <Tag>{version.capacity} seats</Tag>}
        </Space>
      }
      onCancel={onClose}
      footer={[
        <Button key="close" onClick={onClose}>
          Close
        </Button>,
        editable ? (
          <Button key="save" loading={saving} onClick={() => void form.submit()}>
            Save draft
          </Button>
        ) : (
          <Button key="draft" loading={saving} onClick={() => void handleStartDraft()}>
            Start a new draft
          </Button>
        ),
        editable && (
          <Button
            key="publish"
            type="primary"
            loading={publishing}
            onClick={() => void handlePublish()}
          >
            Publish
          </Button>
        ),
      ].filter(Boolean)}
    >
      {loadFailed ? (
        <Alert type="error" showIcon message="Could not load this seat map." />
      ) : (
        <>
          {!editable && version && (
            <Alert
              type="info"
              showIcon
              style={{ marginBottom: 16 }}
              message={`v${version.versionNumber} is published and cannot be changed`}
              description="Tickets already sold resolve their seats against this exact layout. Start a new draft to revise it — performances already selling stay pinned to the version they sold against."
            />
          )}

          {validationErrors.length > 0 && (
            <Alert
              type="error"
              showIcon
              style={{ marginBottom: 16 }}
              message="This draft cannot be published yet"
              description={
                <ul style={{ margin: '8px 0 0', paddingInlineStart: 20 }}>
                  {validationErrors.map((message) => (
                    <li key={message}>{message}</li>
                  ))}
                </ul>
              }
            />
          )}

          <Alert
            type="info"
            showIcon
            style={{ marginBottom: 16 }}
            message="Codes are what pricing binds to"
            description="An event's performance maps each block's code to a ticket type. Renaming a block is safe; changing its code breaks that mapping silently, so pick codes you can live with."
          />

          <Form<LayoutFormValues>
            form={form}
            layout="vertical"
            disabled={!editable}
            onFinish={(values) => void handleSave(values)}
          >
            <Typography.Title level={5}>Reserved sections</Typography.Title>
            <Typography.Paragraph type="secondary">
              Individually addressable seats, generated as rows × seats per row. Row labels continue
              from the first — <code>A</code> gives A, B, C…; <code>1</code> gives 1, 2, 3…
            </Typography.Paragraph>
            <Form.List name="sections">
              {(fields, { add, remove }) => (
                <>
                  {fields.map((field) => (
                    <Card
                      key={field.key}
                      size="small"
                      style={{ marginBottom: 12 }}
                      extra={
                        editable && (
                          <Button
                            type="text"
                            size="small"
                            icon={<MinusCircleOutlined />}
                            onClick={() => remove(field.name)}
                          />
                        )
                      }
                    >
                      <Space wrap align="start" size={12}>
                        <Form.Item
                          name={[field.name, 'code']}
                          label="Code"
                          rules={[{ required: true }, { max: 40 }]}
                        >
                          <Input placeholder="LOWER" style={{ width: 120 }} />
                        </Form.Item>
                        <Form.Item
                          name={[field.name, 'name']}
                          label="Name"
                          rules={[{ required: true }, { max: 100 }]}
                        >
                          <Input placeholder="Lower Tier" style={{ width: 180 }} />
                        </Form.Item>
                        <Form.Item name={[field.name, 'gateId']} label="Gate">
                          <Select
                            allowClear
                            placeholder="Any"
                            style={{ width: 160 }}
                            options={gates.map((gate) => ({
                              value: gate.id,
                              label: `${gate.code} — ${gate.name}`,
                            }))}
                          />
                        </Form.Item>
                        <Form.Item
                          name={[field.name, 'rows']}
                          label="Rows"
                          rules={[{ required: true, type: 'number', min: 1, max: 200 }]}
                        >
                          <InputNumber min={1} max={200} style={{ width: 90 }} />
                        </Form.Item>
                        <Form.Item
                          name={[field.name, 'seatsPerRow']}
                          label="Seats per row"
                          rules={[{ required: true, type: 'number', min: 1, max: 200 }]}
                        >
                          <InputNumber min={1} max={200} style={{ width: 120 }} />
                        </Form.Item>
                        <Form.Item
                          name={[field.name, 'firstRowLabel']}
                          label="First row"
                          rules={[{ required: true }, { max: 10 }]}
                        >
                          <Input style={{ width: 90 }} />
                        </Form.Item>
                        <Form.Item
                          name={[field.name, 'firstSeatNumber']}
                          label="First seat"
                          rules={[{ required: true, type: 'number', min: 0 }]}
                        >
                          <InputNumber min={0} style={{ width: 90 }} />
                        </Form.Item>
                      </Space>
                    </Card>
                  ))}
                  {editable && (
                    <Button
                      type="dashed"
                      icon={<PlusOutlined />}
                      onClick={() => add({ ...DEFAULT_SECTION })}
                      style={{ width: '100%' }}
                    >
                      Add a reserved section
                    </Button>
                  )}
                </>
              )}
            </Form.List>

            <Divider />
            <Typography.Title level={5}>Admission areas</Typography.Title>
            <Typography.Paragraph type="secondary">
              Capacity with no seat identity — a standing floor, a lawn. Inventory counts these
              rather than tracking individual seats.
            </Typography.Paragraph>
            <Form.List name="admissionAreas">
              {(fields, { add, remove }) => (
                <>
                  {fields.map((field) => (
                    <Card
                      key={field.key}
                      size="small"
                      style={{ marginBottom: 12 }}
                      extra={
                        editable && (
                          <Button
                            type="text"
                            size="small"
                            icon={<MinusCircleOutlined />}
                            onClick={() => remove(field.name)}
                          />
                        )
                      }
                    >
                      <Space wrap align="start" size={12}>
                        <Form.Item
                          name={[field.name, 'code']}
                          label="Code"
                          rules={[{ required: true }, { max: 40 }]}
                        >
                          <Input placeholder="FLOOR" style={{ width: 120 }} />
                        </Form.Item>
                        <Form.Item
                          name={[field.name, 'name']}
                          label="Name"
                          rules={[{ required: true }, { max: 100 }]}
                        >
                          <Input placeholder="Standing floor" style={{ width: 180 }} />
                        </Form.Item>
                        <Form.Item
                          name={[field.name, 'capacity']}
                          label="Capacity"
                          rules={[{ required: true, type: 'number', min: 1 }]}
                        >
                          <InputNumber min={1} style={{ width: 120 }} />
                        </Form.Item>
                        <Form.Item name={[field.name, 'gateId']} label="Gate">
                          <Select
                            allowClear
                            placeholder="Any"
                            style={{ width: 160 }}
                            options={gates.map((gate) => ({
                              value: gate.id,
                              label: `${gate.code} — ${gate.name}`,
                            }))}
                          />
                        </Form.Item>
                      </Space>
                    </Card>
                  ))}
                  {editable && (
                    <Button
                      type="dashed"
                      icon={<PlusOutlined />}
                      onClick={() => add({ ...DEFAULT_AREA })}
                      style={{ width: '100%' }}
                    >
                      Add an admission area
                    </Button>
                  )}
                </>
              )}
            </Form.List>
          </Form>
        </>
      )}
    </Modal>
  );
}

/** Expands a section's rows × seats-per-row description into the explicit rows the API takes. */
function toSectionInput(section: SectionFields, index: number): SeatMapSectionInput {
  const rows = Array.from({ length: section.rows }, (_unused, rowIndex) => ({
    label: nextRowLabel(section.firstRowLabel, rowIndex),
    displayOrder: rowIndex,
    seats: Array.from({ length: section.seatsPerRow }, (_ignored, seatIndex) => ({
      // Seat numbers are strings on the wire because real venues use "12A"; a generated grid only
      // ever produces plain integers, and a hand-edited map can carry the rest.
      number: String(section.firstSeatNumber + seatIndex),
      isSellable: true,
    })),
  }));

  return {
    code: section.code.trim(),
    name: section.name.trim(),
    displayOrder: index,
    gateId: section.gateId || null,
    rows,
  };
}

/**
 * The label `offset` rows after `first`. Letters advance alphabetically and roll over to AA, AB…
 * past Z; anything numeric just counts. Falls back to appending the offset for a label that is
 * neither, which keeps every row distinct rather than silently colliding.
 */
function nextRowLabel(first: string, offset: number): string {
  if (offset === 0) {
    return first;
  }

  if (/^\d+$/.test(first)) {
    return String(Number(first) + offset);
  }

  if (/^[A-Za-z]$/.test(first)) {
    const base = first.toUpperCase().charCodeAt(0) - 65 + offset;
    let label = '';
    for (let remaining = base; remaining >= 0; remaining = Math.floor(remaining / 26) - 1) {
      label = String.fromCharCode(65 + (remaining % 26)) + label;
    }
    return first === first.toLowerCase() ? label.toLowerCase() : label;
  }

  return `${first}${offset + 1}`;
}
