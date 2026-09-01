import { useEffect, useState } from 'react';
import { Collapse, Divider, Typography } from 'antd';
import {
  getEventPolicies,
  type PolicyDocumentResponse,
} from '../../../services/catalog/catalogApi';

interface EventPoliciesSectionProps {
  eventId: string;
}

const LABELS: Record<string, string> = {
  Terms: 'Terms & conditions',
  Privacy: 'Privacy notice',
  Refund: 'Refund policy',
};

/**
 * The organizer's terms, privacy notice and refund policy, collapsed by default.
 *
 * Fails silently when nothing loads: an organizer who has not written these yet is the common case
 * early on, and an error box on an otherwise-fine event page tells a buyer nothing they can act on.
 * The refund policy still has to be *reachable* before purchase, which is why it is on this page
 * rather than behind a link at checkout.
 */
export function EventPoliciesSection({ eventId }: EventPoliciesSectionProps) {
  const [documents, setDocuments] = useState<PolicyDocumentResponse[]>([]);

  useEffect(() => {
    let cancelled = false;

    getEventPolicies(eventId)
      .then((result) => {
        if (!cancelled) {
          setDocuments(result);
        }
      })
      .catch(() => {
        if (!cancelled) {
          setDocuments([]);
        }
      });

    return () => {
      cancelled = true;
    };
  }, [eventId]);

  if (documents.length === 0) {
    return null;
  }

  return (
    <>
      {/* Inside the component, not beside it: nothing renders when an organizer has written no
          documents, and a divider on its own is a line the page did not ask for. */}
      <Divider />
      <Typography.Title level={5} style={{ marginTop: 0 }}>
        Before you book
      </Typography.Title>
      <Collapse
        items={documents.map((document) => ({
          key: document.kind,
          label: LABELS[document.kind] ?? document.kind,
          children: (
            <div
              style={{ overflowX: 'auto' }}
              // The server strips scripts, event handlers and non-http(s)/mailto links on write —
              // see PolicyHtmlSanitizer — so what arrives here has already been through a
              // whitelist parser. Rendering it as markup is the point: an organizer's policy is
              // structured prose, and escaping it would show buyers a page of tags.
              dangerouslySetInnerHTML={{ __html: document.bodyHtml }}
            />
          ),
        }))}
      />
    </>
  );
}
