import { Card } from 'antd';
import { PageHeader } from '../../../components/common/layout/PageHeader';
import { PolicyDocumentsPanel } from './PolicyDocumentsPanel';

/**
 * The organizer's tenant-wide terms, privacy notice and refund policy — the defaults every event
 * inherits unless it overrides one on its own Policies tab.
 */
export function OrganizerPoliciesPage() {
  return (
    <>
      <PageHeader title="Policies" />
      <Card styles={{ body: { padding: 28 } }}>
        <PolicyDocumentsPanel />
      </Card>
    </>
  );
}
