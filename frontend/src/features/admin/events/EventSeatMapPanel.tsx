import { useState } from 'react';
import { Button, Divider, Form, Input, Modal, Popconfirm, Space, Typography } from 'antd';
import { DeleteOutlined, EditOutlined } from '@ant-design/icons';
import {
  addSeatMapSections,
  defineSeatMap,
  removeSeatMapSection,
  updateSeatMapSection,
  type EntryGateResponse,
  type SeatMapResponse,
  type SeatMapSectionInput,
} from '../../../services/catalog/catalogApi';
import { toast } from '../../../components/common/feedback/toast';
import { SeatMapSectionsFields } from './SeatMapSectionsFields';
import { DEFAULT_SEAT_MAP_SECTION } from './seatMapSectionDefaults';

interface EventSeatMapPanelProps {
  eventId: string;
  /** The event's current seat map, or null if none has been defined yet. */
  seatMap: SeatMapResponse | null;
  entryGates: EntryGateResponse[];
  /** Whether the event is still a draft — the seat map is only editable while it is. */
  isDraft: boolean;
  /** Called after any successful change, so the parent can re-fetch. */
  onChanged: () => void;
}

interface SeatMapFormValues {
  name: string;
  sections: SeatMapSectionInput[];
}

interface AddSeatMapSectionsFormValues {
  sections: SeatMapSectionInput[];
}

interface EditSeatMapSectionFormValues {
  sections: [SeatMapSectionInput];
}

const SECTION_HEADING_STYLE = { marginTop: 0, marginBottom: 16 };

/**
 * Create and edit an event's seat map: define it once, then add, edit and remove sections.
 *
 * Draft-only throughout, and not because of a UI preference — Inventory provisions seat rows from
 * this at publish, and nothing outside Catalog holds a seat id before that happens. After publish
 * the map is shown read-only; changing capacity on a live event is the "add a late release" flow,
 * which goes through ticket types instead.
 */
export function EventSeatMapPanel({
  eventId,
  seatMap,
  entryGates,
  isDraft,
  onChanged,
}: EventSeatMapPanelProps) {
  const [addSectionsForm] = Form.useForm<AddSeatMapSectionsFormValues>();
  const [editSectionForm] = Form.useForm<EditSeatMapSectionFormValues>();
  const [creating, setCreating] = useState(false);
  const [addingSections, setAddingSections] = useState(false);
  const [editingSectionName, setEditingSectionName] = useState<string | null>(null);
  const [savingSection, setSavingSection] = useState(false);
  const [deletingSectionName, setDeletingSectionName] = useState<string | null>(null);

  const existingSectionNames = [
    ...(seatMap?.seats.map((seat) => seat.section) ?? []),
    ...(seatMap?.generalAdmissionSections.map((section) => section.sectionName) ?? []),
  ];

  const handleDefineSeatMap = async (values: SeatMapFormValues) => {
    setCreating(true);
    try {
      await defineSeatMap(eventId, values);
      toast.success('Seat map created.');
      onChanged();
    } catch {
      toast.error('Could not create the seat map.');
    } finally {
      setCreating(false);
    }
  };

  const handleAddSeatMapSections = async (values: AddSeatMapSectionsFormValues) => {
    setAddingSections(true);
    try {
      await addSeatMapSections(eventId, { sections: values.sections });
      toast.success('Sections added to the seat map.');
      addSectionsForm.resetFields();
      onChanged();
    } catch {
      toast.error(
        'Could not add those sections — check for a duplicate name or an invalid entry gate.',
      );
    } finally {
      setAddingSections(false);
    }
  };

  const openEditSection = (section: SeatMapSectionInput) => {
    setEditingSectionName(section.name);
    editSectionForm.setFieldsValue({ sections: [section] });
  };

  const handleSaveSection = async (values: EditSeatMapSectionFormValues) => {
    if (!editingSectionName) {
      return;
    }
    setSavingSection(true);
    try {
      await updateSeatMapSection(eventId, {
        currentSectionName: editingSectionName,
        section: values.sections[0],
      });
      toast.success('Section updated.');
      setEditingSectionName(null);
      onChanged();
    } catch {
      toast.error(
        'Could not save that section — check for a duplicate name or an invalid entry gate.',
      );
    } finally {
      setSavingSection(false);
    }
  };

  const handleDeleteSection = async (sectionName: string) => {
    setDeletingSectionName(sectionName);
    try {
      await removeSeatMapSection(eventId, sectionName);
      toast.success('Section removed.');
      onChanged();
    } catch {
      toast.error('Could not remove that section.');
    } finally {
      setDeletingSectionName(null);
    }
  };

  if (!seatMap) {
    return isDraft ? (
      <Form<SeatMapFormValues>
        layout="vertical"
        initialValues={{ sections: [DEFAULT_SEAT_MAP_SECTION] }}
        onFinish={(values) => void handleDefineSeatMap(values)}
      >
        <Typography.Text type="secondary" style={{ display: 'block', marginBottom: 16 }}>
          Define the seat map before publishing. Reserved sections generate individual seats;
          general admission sections are a capacity pool. One event can have both.
        </Typography.Text>

        <Form.Item name="name" label="Seat map name" rules={[{ required: true }, { max: 200 }]}>
          <Input placeholder="e.g. Main Floor" style={{ maxWidth: 360 }} />
        </Form.Item>

        <SeatMapSectionsFields entryGates={entryGates} />

        <Divider />
        <Space style={{ display: 'flex', justifyContent: 'flex-end' }}>
          <Button type="primary" htmlType="submit" loading={creating}>
            Create seat map
          </Button>
        </Space>
      </Form>
    ) : (
      <Typography.Text type="secondary">This event has no seat map.</Typography.Text>
    );
  }

  return (
    <>
      <Typography.Title level={5} style={SECTION_HEADING_STYLE}>
        {seatMap.name} · {seatMap.capacity} total
      </Typography.Title>

      <Space direction="vertical" style={{ width: '100%', marginBottom: 8 }}>
        {[...new Set(seatMap.seats.map((seat) => seat.section))].map((section) => {
          const seatsInSection = seatMap.seats.filter((seat) => seat.section === section);
          const first = seatsInSection[0];
          return (
            <div
              key={section}
              style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}
            >
              <Typography.Text type="secondary">
                {section} — Reserved · {first?.priceTier} · {seatsInSection.length} seats
              </Typography.Text>
              {isDraft && (
                <Space size="small">
                  <Button
                    type="text"
                    size="small"
                    icon={<EditOutlined />}
                    onClick={() =>
                      openEditSection({
                        name: section,
                        priceTier: first?.priceTier ?? '',
                        priceAmount: first?.priceAmount ?? 0,
                        allocationType: 'Reserved',
                        rows: new Set(seatsInSection.map((seat) => seat.row)).size,
                        seatsPerRow: Math.max(...seatsInSection.map((seat) => seat.number)),
                        entryGateId: first?.entryGateId ?? null,
                      })
                    }
                  />
                  <Popconfirm
                    title="Remove this section?"
                    description="Every seat in it is deleted."
                    onConfirm={() => void handleDeleteSection(section)}
                  >
                    <Button
                      type="text"
                      size="small"
                      danger
                      icon={<DeleteOutlined />}
                      loading={deletingSectionName === section}
                    />
                  </Popconfirm>
                </Space>
              )}
            </div>
          );
        })}

        {seatMap.generalAdmissionSections.map((section) => (
          <div
            key={section.id}
            style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}
          >
            <Typography.Text type="secondary">
              {section.sectionName} — General admission · {section.priceTier} · {section.capacity}{' '}
              capacity
            </Typography.Text>
            {isDraft && (
              <Space size="small">
                <Button
                  type="text"
                  size="small"
                  icon={<EditOutlined />}
                  onClick={() =>
                    openEditSection({
                      name: section.sectionName,
                      priceTier: section.priceTier,
                      priceAmount: section.priceAmount,
                      allocationType: 'GeneralAdmission',
                      capacity: section.capacity,
                      entryGateId: section.entryGateId,
                    })
                  }
                />
                <Popconfirm
                  title="Remove this section?"
                  onConfirm={() => void handleDeleteSection(section.sectionName)}
                >
                  <Button
                    type="text"
                    size="small"
                    danger
                    icon={<DeleteOutlined />}
                    loading={deletingSectionName === section.sectionName}
                  />
                </Popconfirm>
              </Space>
            )}
          </div>
        ))}
      </Space>

      {isDraft && (
        <>
          <Divider />
          <Typography.Title level={5} style={SECTION_HEADING_STYLE}>
            Add more sections
          </Typography.Title>
          <Form<AddSeatMapSectionsFormValues>
            form={addSectionsForm}
            layout="vertical"
            initialValues={{ sections: [DEFAULT_SEAT_MAP_SECTION] }}
            onFinish={(values) => void handleAddSeatMapSections(values)}
          >
            <SeatMapSectionsFields
              entryGates={entryGates}
              existingSectionNames={existingSectionNames}
            />

            <Divider />
            <Space style={{ display: 'flex', justifyContent: 'flex-end' }}>
              <Button type="primary" htmlType="submit" loading={addingSections}>
                Add sections
              </Button>
            </Space>
          </Form>
        </>
      )}

      <Modal
        open={editingSectionName !== null}
        onCancel={() => setEditingSectionName(null)}
        title={`Edit section: ${editingSectionName ?? ''}`}
        okText="Save"
        confirmLoading={savingSection}
        onOk={() => editSectionForm.submit()}
        destroyOnHidden
      >
        <Form<EditSeatMapSectionFormValues>
          form={editSectionForm}
          layout="vertical"
          onFinish={(values) => void handleSaveSection(values)}
        >
          <SeatMapSectionsFields
            entryGates={entryGates}
            allowAddRemove={false}
            existingSectionNames={existingSectionNames.filter(
              (name) => name !== editingSectionName,
            )}
          />
        </Form>
      </Modal>
    </>
  );
}
