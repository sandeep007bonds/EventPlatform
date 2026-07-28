import type { EventStatus } from '../services/catalog/catalogApi';

/** Ant `Tag` color for each event lifecycle status, kept consistent across buyer and admin views. */
export const eventStatusColor: Record<EventStatus, string> = {
  Draft: 'default',
  Published: 'blue',
  OnSale: 'green',
  SoldOut: 'orange',
  Cancelled: 'red',
  Completed: 'purple',
};
