import { useCallback, useEffect, useState } from 'react';
import { Alert, Button, Input, Space, Tabs, Tag, Typography } from 'antd';
import {
  getEventPolicies,
  getTenantPolicies,
  setEventPolicy,
  setTenantPolicy,
  type PolicyDocumentResponse,
  type PolicyKind,
} from '../../../services/catalog/catalogApi';
import { LoadError } from '../../../components/common/errors/LoadError';
import { toast } from '../../../components/common/feedback/toast';

interface PolicyDocumentsPanelProps {
  /**
   * The event these documents apply to, or omitted for the organizer's tenant-wide defaults.
   * Passing an event id switches every save to an override for that event alone.
   */
  eventId?: string;
}

const KINDS: { key: PolicyKind; label: string; hint: string }[] = [
  {
    key: 'Terms',
    label: 'Terms & conditions',
    hint: 'The terms of sale a buyer accepts at checkout.',
  },
  {
    key: 'Privacy',
    label: 'Privacy notice',
    hint: 'What data you collect from a buyer, and what you do with it.',
  },
  {
    key: 'Refund',
    label: 'Refund policy',
    hint: 'When a buyer can get their money back, and how. Read most often after something goes wrong.',
  },
];

/**
 * Organizer's terms, privacy and refund documents — either the tenant-wide defaults or one event's
 * overrides of them.
 *
 * Two deliberate constraints show up in the UI. The editor is a plain textarea with a live preview
 * rather than a WYSIWYG, because Ant Design ships no rich-text editor and `frontend/CLAUDE.md`
 * forbids adding a UI library without discussion; the stored format is HTML, so swapping in TipTap
 * later is a component change and not a data migration. And the preview renders the text the
 * browser has, not the text the server will store — the server strips scripts on write, so what
 * ends up saved can be *less* than what the preview shows, never more.
 */
export function PolicyDocumentsPanel({ eventId }: PolicyDocumentsPanelProps) {
  const [documents, setDocuments] = useState<PolicyDocumentResponse[]>([]);
  const [drafts, setDrafts] = useState<Partial<Record<PolicyKind, string>>>({});
  const [loading, setLoading] = useState(true);
  const [loadError, setLoadError] = useState(false);
  const [savingKind, setSavingKind] = useState<PolicyKind | null>(null);

  const load = useCallback(
    () =>
      (eventId ? getEventPolicies(eventId) : getTenantPolicies())
        .then((result) => {
          setDocuments(result);
          setDrafts(Object.fromEntries(result.map((d) => [d.kind, d.bodyHtml])));
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

  const handleSave = async (kind: PolicyKind) => {
    const bodyHtml = drafts[kind]?.trim();
    if (!bodyHtml) {
      toast.error('Write the document first.');
      return;
    }

    setSavingKind(kind);
    try {
      const { version } = eventId
        ? await setEventPolicy(eventId, kind, bodyHtml)
        : await setTenantPolicy(kind, bodyHtml);
      await load();
      toast.success(`Saved as version ${version}.`);
    } catch (error) {
      toast.error(messageFrom(error) ?? 'Could not save that document.');
    } finally {
      setSavingKind(null);
    }
  };

  if (loadError) {
    return <LoadError onRetry={handleRetry} />;
  }

  return (
    <>
      <Typography.Text type="secondary" style={{ display: 'block', marginBottom: 16 }}>
        {eventId
          ? "These override your organizer defaults for this event only. An event you don't override keeps showing the default."
          : 'These apply to every event you run, unless an event overrides one of them.'}{' '}
        Each save creates a new version; orders record the version that was in force when they were
        placed, so a dispute months later can be answered from the record rather than from memory.
      </Typography.Text>

      <Tabs
        items={KINDS.map(({ key, label, hint }) => {
          const current = documents.find((d) => d.kind === key);
          const draft = drafts[key] ?? '';
          const dirty = draft !== (current?.bodyHtml ?? '');

          return {
            key,
            label: (
              <Space size={6}>
                {label}
                {current && <Tag>v{current.version}</Tag>}
                {eventId && current && !current.isEventOverride && <Tag color="blue">Default</Tag>}
              </Space>
            ),
            children: (
              <>
                <Typography.Text type="secondary" style={{ display: 'block', marginBottom: 12 }}>
                  {hint}
                </Typography.Text>

                {eventId && current && !current.isEventOverride && (
                  <Alert
                    type="info"
                    showIcon
                    style={{ marginBottom: 16 }}
                    message="Showing your organizer default"
                    description="Editing and saving here creates an override for this event. Your default is left alone."
                  />
                )}

                <Input.TextArea
                  rows={14}
                  value={draft}
                  onChange={(e) => setDrafts((prev) => ({ ...prev, [key]: e.target.value }))}
                  placeholder={
                    '<h2>Refunds</h2>\n<p>Tickets are refundable up to 48 hours before…</p>'
                  }
                  style={{ fontFamily: 'ui-monospace, SFMono-Regular, Menlo, monospace' }}
                />

                <Typography.Text
                  type="secondary"
                  style={{ display: 'block', margin: '16px 0 8px' }}
                >
                  Preview
                </Typography.Text>
                <div
                  style={{
                    border: '1px solid rgba(0,0,0,0.08)',
                    borderRadius: 8,
                    padding: 16,
                    minHeight: 80,
                    overflowX: 'auto',
                  }}
                  // Rendering the *unsaved draft*, which nothing has sanitised yet. Safe here only
                  // because the organizer is previewing their own keystrokes in their own browser —
                  // it is not a channel from anyone to anyone. What reaches other people goes
                  // through the server's sanitiser first, and is re-read from the server below.
                  dangerouslySetInnerHTML={{ __html: draft }}
                />

                <Space style={{ display: 'flex', justifyContent: 'flex-end', marginTop: 16 }}>
                  {current && (
                    <Typography.Text type="secondary">
                      Version {current.version} · updated{' '}
                      {new Date(current.updatedAt).toLocaleDateString()}
                    </Typography.Text>
                  )}
                  <Button
                    type="primary"
                    disabled={!dirty}
                    loading={savingKind === key}
                    onClick={() => void handleSave(key)}
                  >
                    {current && (!eventId || current.isEventOverride)
                      ? 'Save new version'
                      : 'Save document'}
                  </Button>
                </Space>
              </>
            ),
          };
        })}
      />

      {loading && (
        <Typography.Text type="secondary">Loading your existing documents…</Typography.Text>
      )}
    </>
  );
}

function messageFrom(error: unknown): string | undefined {
  return (error as { response?: { data?: { message?: string } } }).response?.data?.message;
}
