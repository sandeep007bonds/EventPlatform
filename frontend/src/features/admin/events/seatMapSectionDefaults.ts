import type { SeatMapSectionInput } from '../../../services/catalog/catalogApi';

/**
 * A blank Reserved section, used as the initial/newly-added row in a sections `Form.List`.
 * Split into its own file (not exported alongside `SeatMapSectionsFields`) purely so
 * react-refresh's "a file should only export components" lint rule stays happy.
 */
export const DEFAULT_SEAT_MAP_SECTION: SeatMapSectionInput = {
  name: '',
  priceTier: '',
  priceAmount: 0,
  allocationType: 'Reserved',
  rows: 1,
  seatsPerRow: 1,
};
