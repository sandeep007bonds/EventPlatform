import { useState } from 'react';
import { Button, Card, Input, Space, Tag, Typography } from 'antd';
import { PlusOutlined } from '@ant-design/icons';
import { createEntryGate, type EntryGateResponse } from '../../../services/catalog/catalogApi';
import { toast } from '../../../components/common/feedback/toast';

interface EntryGatesPanelProps {
  eventId: string;
  gates: EntryGateResponse[];
  onGateCreated: () => void;
}

/**
 * Organizer's entry-gate manager for an event's location — define named gates (e.g. "Gate A")
 * before defining the seat map, so each section can be restricted to one. Rendered above the
 * "Define seat map" form, since gate assignment happens once, at seat-map creation time.
 */
export function EntryGatesPanel({ eventId, gates, onGateCreated }: EntryGatesPanelProps) {
  const [name, setName] = useState('');
  const [submitting, setSubmitting] = useState(false);

  const handleAdd = async () => {
    const trimmed = name.trim();
    if (!trimmed) {
      return;
    }

    setSubmitting(true);
    try {
      await createEntryGate(eventId, trimmed);
      setName('');
      onGateCreated();
    } catch {
      toast.error('Could not add the entry gate.');
    } finally {
      setSubmitting(false);
    }
  };

  return (
    <Card title="Entry gates" style={{ marginBottom: 24 }} styles={{ body: { padding: 28 } }}>
      <Typography.Text type="secondary" style={{ display: 'block', marginBottom: 16 }}>
        Define named entry points for this event's location. A seat-map section can be restricted to
        one gate when you define the seat map below.
      </Typography.Text>
      <Space wrap style={{ marginBottom: 16 }}>
        {gates.length === 0 && <Typography.Text type="secondary">No gates yet.</Typography.Text>}
        {gates.map((gate) => (
          <Tag key={gate.id}>{gate.name}</Tag>
        ))}
      </Space>
      <Space.Compact style={{ maxWidth: 360 }}>
        <Input
          placeholder="e.g. Gate A"
          maxLength={100}
          value={name}
          onChange={(event) => setName(event.target.value)}
          onPressEnter={() => void handleAdd()}
        />
        <Button icon={<PlusOutlined />} loading={submitting} onClick={() => void handleAdd()}>
          Add gate
        </Button>
      </Space.Compact>
    </Card>
  );
}
