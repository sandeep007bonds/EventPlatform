/**
 * Single source of truth for the brand accent, shared by both themed sections (buyer storefront +
 * admin console — see ADR-0015) and by plain-CSS components (e.g. `SeatChip`) that render outside
 * Ant's token system and can't reach a `ConfigProvider` token via CSS variables.
 */
export const PRIMARY_COLOR = '#3ea8c4';
export const PRIMARY_COLOR_DARK = '#2f8aa3';
