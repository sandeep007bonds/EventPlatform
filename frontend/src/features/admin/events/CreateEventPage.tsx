import { useEffect, useState } from 'react';
import { Alert, Button, Card, Divider, Form, Input, Space, Typography } from 'antd';
import type { AxiosError } from 'axios';
import { useNavigate, useSearchParams } from 'react-router-dom';
import {
  createEvent,
  createEventGroup,
  getEventGroup,
  updateEventGroup,
} from '../../../services/catalog/catalogApi';
import { toast } from '../../../components/common/feedback/toast';
import { PageHeader } from '../../../components/common/layout/PageHeader';
import { EventGroupPicker, NEW_TOUR_OPTION } from '../eventGroups/EventGroupPicker';
import { TourLegsList } from '../eventGroups/TourLegsList';
import { EventLegFields } from './EventLegFields';
import { DEFAULT_LEG, type EventLegFormValues, type LegStatus } from './eventLegDefaults';

interface CreateEventFormValues {
  eventGroupId?: string;
  newTourTitle?: string;
  legs: EventLegFormValues[];
}

function extractErrorMessage(error: unknown): string | undefined {
  return (error as AxiosError<{ message?: string }>).response?.data?.message;
}

/**
 * Creates one event, or several at once — clicking "Add another city/date" (inside
 * `EventLegFields`) grows this same form into a multi-leg tour, all submitted together in one
 * visit. Defaults to looking like a plain single-event form with no tour concept visible; once
 * there's more than one leg, a tour (existing or new) becomes required so the legs have somewhere
 * to attach to. There is no delete/rollback endpoint for an `Event` or `EventGroup`, so a batch
 * that fails partway through leaves already-created legs saved — `legStatuses`/`legErrors` track
 * that so a retry only (re-)submits what's left, never re-creating an already-committed leg.
 */
export function CreateEventPage() {
  const navigate = useNavigate();
  // Arriving via a tour's own "Add leg" button (TourDetailPage) pre-scopes this form to that
  // tour, skipping the picker interaction entirely — "create tour once, keep adding legs to it."
  const [searchParams] = useSearchParams();
  const preselectedGroupId = searchParams.get('eventGroupId') ?? undefined;
  const [form] = Form.useForm<CreateEventFormValues>();
  const [submitting, setSubmitting] = useState(false);

  // The tour's own advertised date range, if an *existing* tour is picked — a leg's dates must
  // lie inside it (mirrored server-side; this is the inline-feedback copy, not the enforcement).
  const [groupRange, setGroupRange] = useState<{
    startsAt: string | null;
    endsAt: string | null;
  } | null>(null);
  // The existing tour's own title, if one is picked — purely for the "under X" sentence below;
  // a brand-new tour uses `newTourTitle` from the form instead, so this stays null for that case.
  const [selectedGroupTitle, setSelectedGroupTitle] = useState<string | null>(null);

  // Set once a tour is created or picked on this page visit, then reused across any retry so a
  // resubmit after a partial failure never creates a second tour or re-resolves the picker.
  const [resolvedGroupId, setResolvedGroupId] = useState<string | null>(null);
  const [tourCreatedHere, setTourCreatedHere] = useState(false);

  // Parallel to the `legs` `Form.List` — what's already been committed, so a retry skips it.
  const [legStatuses, setLegStatuses] = useState<LegStatus[]>(['pending']);
  const [legErrors, setLegErrors] = useState<(string | undefined)[]>([undefined]);

  const selectedGroupId = Form.useWatch('eventGroupId', form);
  const legsCount = Form.useWatch('legs', form)?.length ?? 1;
  const hasFailedLeg = legStatuses.includes('failed');
  const createdCount = legStatuses.filter((status) => status === 'created').length;

  useEffect(() => {
    let cancelled = false;
    // A pending "+ New tour" selection has no id to fetch a range for yet — it inherits the
    // legs' own dates instead (computed at submit time), so there's no existing range to warn
    // against here.
    const fetchRange =
      selectedGroupId && selectedGroupId !== NEW_TOUR_OPTION
        ? getEventGroup(selectedGroupId).then((group) => ({
            startsAt: group.startsAt,
            endsAt: group.endsAt,
            title: group.title,
          }))
        : Promise.resolve(null);

    fetchRange
      .then((range) => {
        if (!cancelled) {
          setGroupRange(range ? { startsAt: range.startsAt, endsAt: range.endsAt } : null);
          setSelectedGroupTitle(range?.title ?? null);
        }
      })
      .catch(() => {
        if (!cancelled) {
          setGroupRange(null);
          setSelectedGroupTitle(null);
        }
      });

    return () => {
      cancelled = true;
    };
  }, [selectedGroupId]);

  const handleRemoveLeg = (index: number) => {
    setLegStatuses((previous) => previous.filter((_, i) => i !== index));
    setLegErrors((previous) => previous.filter((_, i) => i !== index));
  };

  const handleSubmit = async (values: CreateEventFormValues) => {
    setSubmitting(true);
    try {
      let groupId = resolvedGroupId;
      let isNewTour = tourCreatedHere;

      // A brand-new tour is created here, not eagerly when "+ New tour" is picked — an abandoned
      // form should never leave an orphan tour behind. Nothing else has been attempted yet at
      // this point, so a failure here is fully safe to just retry from scratch.
      if (!groupId) {
        try {
          if (values.eventGroupId === NEW_TOUR_OPTION) {
            const group = await createEventGroup({ title: values.newTourTitle! });
            groupId = group.id;
            isNewTour = true;
            setResolvedGroupId(group.id);
            setTourCreatedHere(true);
          } else if (values.eventGroupId) {
            groupId = values.eventGroupId;
            setResolvedGroupId(values.eventGroupId);
          }
        } catch {
          toast.error('Could not create the tour — nothing else was attempted, try again.');
          return;
        }
      }

      const nextStatuses = [...legStatuses];
      const nextErrors = [...legErrors];
      let allSucceeded = true;
      let lastCreatedEventId: string | null = null;

      for (let i = 0; i < values.legs.length; i += 1) {
        if (nextStatuses[i] === 'created') {
          continue;
        }
        const leg = values.legs[i];
        try {
          // Sequential by design, not Promise.all: the server's sibling-overlap check re-queries
          // fresh state per call, so legs must land one at a time for that check to be correct.
          const result = await createEvent({
            title: leg.title,
            startsAt: leg.startsAt.toISOString(),
            endsAt: leg.endsAt.toISOString(),
            currency: leg.currency,
            eventGroupId: groupId ?? null,
            maxTicketsPerBuyer: leg.maxTicketsPerBuyer ?? null,
            requiresQueue: leg.requiresQueue ?? false,
          });
          nextStatuses[i] = 'created';
          nextErrors[i] = undefined;
          lastCreatedEventId = result.id;
        } catch (error) {
          nextStatuses[i] = 'failed';
          nextErrors[i] = extractErrorMessage(error) ?? 'Could not create this leg.';
          allSucceeded = false;
          // Stop rather than attempt the rest — the remaining legs' overlap checks would be
          // running against an incomplete set, and compounding failures just muddy the retry.
          break;
        }
      }

      setLegStatuses(nextStatuses);
      setLegErrors(nextErrors);

      if (!allSucceeded) {
        toast.error(
          'Some legs could not be created — fix the highlighted one below and try again.',
        );
        return;
      }

      if (groupId && isNewTour) {
        const startsAtValues = values.legs.map((leg) => leg.startsAt);
        const endsAtValues = values.legs.map((leg) => leg.endsAt);
        const minStartsAt = startsAtValues.reduce((min, current) =>
          current.isBefore(min) ? current : min,
        );
        const maxEndsAt = endsAtValues.reduce((max, current) =>
          current.isAfter(max) ? current : max,
        );
        try {
          await updateEventGroup(groupId, {
            title: values.newTourTitle!,
            startsAt: minStartsAt.toISOString(),
            endsAt: maxEndsAt.toISOString(),
          });
        } catch {
          toast.error(
            "Legs created, but couldn't update the tour's date range — edit it from the tour page.",
          );
        }
      }

      toast.success(
        !groupId
          ? 'Event created.'
          : isNewTour
            ? 'Tour created.'
            : values.legs.length > 1
              ? 'Legs added to the tour.'
              : 'Event added to the tour.',
      );
      void navigate(groupId ? `/admin/tours/${groupId}` : `/admin/events/${lastCreatedEventId}`);
    } finally {
      setSubmitting(false);
    }
  };

  const isMultiLeg = legsCount > 1;
  const submitLabel = hasFailedLeg
    ? 'Create remaining legs'
    : isMultiLeg
      ? selectedGroupId && selectedGroupId !== NEW_TOUR_OPTION
        ? `Add ${legsCount} leg${legsCount === 1 ? '' : 's'} to tour`
        : 'Create tour'
      : 'Create event';

  return (
    <div style={{ maxWidth: 760 }}>
      <PageHeader
        title={isMultiLeg ? 'Create tour' : 'Create event'}
        description={
          isMultiLeg
            ? "Add each city/date below as its own leg of the tour — they'll all be created together."
            : 'Set the basics now — you can add media, description, and pricing after.'
        }
      />
      <Card styles={{ body: { padding: 28 } }}>
        {hasFailedLeg && (
          <Alert
            type="warning"
            showIcon
            style={{ marginBottom: 20 }}
            message={`${createdCount} of ${legsCount} leg${legsCount === 1 ? '' : 's'} created and saved.`}
            description={
              'Fix the highlighted city/date below, then click "Create remaining legs" to continue.'
            }
          />
        )}
        <Form<CreateEventFormValues>
          form={form}
          layout="vertical"
          initialValues={{ eventGroupId: preselectedGroupId, legs: [{ ...DEFAULT_LEG }] }}
          onFinish={(values) => {
            void handleSubmit(values);
          }}
        >
          <Form.Item
            name="eventGroupId"
            label={isMultiLeg ? 'Tour' : 'Part of a tour? (optional)'}
            rules={
              isMultiLeg
                ? [
                    {
                      required: true,
                      message:
                        'Adding more than one city/date needs a tour to attach them to — pick one or use "+ New tour."',
                    },
                  ]
                : []
            }
          >
            <EventGroupPicker
              hideStandaloneOption={isMultiLeg}
              disabled={resolvedGroupId !== null}
            />
          </Form.Item>

          {selectedGroupId === NEW_TOUR_OPTION && (
            <Form.Item
              name="newTourTitle"
              label="Tour title"
              rules={[{ required: true, message: 'A new tour needs a title.' }, { max: 200 }]}
              style={{ marginTop: -8 }}
            >
              <Input placeholder="e.g. Coldplay World Tour" disabled={resolvedGroupId !== null} />
            </Form.Item>
          )}

          {selectedGroupId && selectedGroupId !== NEW_TOUR_OPTION && (
            <div
              style={{
                marginTop: -8,
                marginBottom: 24,
                padding: 16,
                background: 'rgba(0, 0, 0, 0.02)',
                borderRadius: 8,
              }}
            >
              <Typography.Text type="secondary" style={{ display: 'block', marginBottom: 8 }}>
                Other legs of this tour:
              </Typography.Text>
              <TourLegsList eventGroupId={selectedGroupId} showTitle={false} />
            </div>
          )}

          {isMultiLeg && (
            <Typography.Text style={{ display: 'block', marginBottom: 16 }}>
              This tour has {legsCount} legs — each city/date below becomes its own event under{' '}
              {selectedGroupTitle ? `"${selectedGroupTitle}"` : 'the new tour'}.
            </Typography.Text>
          )}

          <EventLegFields
            groupRange={groupRange}
            legStatuses={legStatuses}
            legErrors={legErrors}
            onRemoveLeg={handleRemoveLeg}
            disabled={submitting}
          />

          <Divider />
          <Space style={{ display: 'flex', justifyContent: 'flex-end' }}>
            <Button type="primary" htmlType="submit" size="large" loading={submitting}>
              {submitLabel}
            </Button>
          </Space>
        </Form>
      </Card>
    </div>
  );
}
