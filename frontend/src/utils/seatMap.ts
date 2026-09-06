import type { AdmissionAreaResponse, SeatMapVersionResponse } from '../services/venue/venueApi';

/**
 * One seat, flattened out of a Venue seat-map version's section → row → seat nesting.
 *
 * The nesting is right for the Venue service, which owns the structure, and wrong for everything
 * that renders or looks a seat up: a picker needs "every seat", a status overlay needs "the seat
 * with this id". Flattening once at the edge keeps the three-level walk out of every component.
 *
 * The section's **code** travels alongside its name because the code is what a performance's
 * allocation map keys on — the name is a label an organizer may rename at any time.
 */
export interface FlatSeat {
  seatId: string;
  sectionCode: string;
  sectionName: string;
  sectionOrder: number;
  rowLabel: string;
  rowOrder: number;
  /** A string, not a number: real venues number seats `12A`, and an integer cannot hold that. */
  number: string;
  /** A human label for the whole seat, e.g. `Lower Tier · A · 12`. */
  label: string;
  isSellable: boolean;
  attributes: string;
  /** The gate this seat's section is restricted to, if any. */
  gateId: string | null;
}

/** Flattens a version's sections, rows and seats into one list, in display order. */
export function flattenSeatMap(version: SeatMapVersionResponse): FlatSeat[] {
  const seats: FlatSeat[] = [];

  for (const section of version.sections) {
    for (const row of section.rows) {
      for (const seat of row.seats) {
        seats.push({
          seatId: seat.id,
          sectionCode: section.code,
          sectionName: section.name,
          sectionOrder: section.displayOrder,
          rowLabel: row.label,
          rowOrder: row.displayOrder,
          number: seat.number,
          label: `${section.name} · ${row.label} · ${seat.number}`,
          isSellable: seat.isSellable,
          attributes: seat.attributes,
          gateId: section.gateId,
        });
      }
    }
  }

  return seats;
}

/** Every admission area of a version, in display order — the capacity-only half of a map. */
export function admissionAreasOf(version: SeatMapVersionResponse): AdmissionAreaResponse[] {
  return [...version.admissionAreas].sort((a, b) => a.displayOrder - b.displayOrder);
}

/**
 * Every block of a version — reserved sections and admission areas alike — as the `{ code, name }`
 * pairs a performance has to allocate. Publishing refuses a performance with any of them unmapped,
 * so this is also the checklist an allocation editor works from.
 */
export function blocksOf(version: SeatMapVersionResponse): {
  code: string;
  name: string;
  kind: 'Reserved' | 'GeneralAdmission';
  capacity: number;
  tierLabel: string | null;
}[] {
  const sections = version.sections.map((section) => ({
    code: section.code,
    name: section.name,
    kind: 'Reserved' as const,
    capacity: section.sellableSeatCount,
    tierLabel: section.tierLabel,
    displayOrder: section.displayOrder,
  }));

  const areas = version.admissionAreas.map((area) => ({
    code: area.code,
    name: area.name,
    kind: 'GeneralAdmission' as const,
    capacity: area.capacity,
    tierLabel: area.tierLabel,
    displayOrder: area.displayOrder,
  }));

  return [...sections, ...areas]
    .sort((a, b) => a.displayOrder - b.displayOrder)
    .map(({ code, name, kind, capacity, tierLabel }) => ({
      code,
      name,
      kind,
      capacity,
      tierLabel,
    }));
}

/**
 * Compares two seat numbers the way a person reads them, so `2` sorts before `10` and `12A` sits
 * next to `12B`. `Intl`'s numeric collation does the work; a plain string sort would put `10`
 * between `1` and `2`.
 */
export function compareSeatNumbers(a: string, b: string): number {
  return a.localeCompare(b, undefined, { numeric: true, sensitivity: 'base' });
}
