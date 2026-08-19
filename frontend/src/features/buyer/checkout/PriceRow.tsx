import { Typography } from 'antd';
import { formatMoney } from '../../../utils/money';

interface PriceRowProps {
  label: string;
  /** Minor units. Negative renders with a leading minus, for a discount. */
  amountMinor: number;
  currency: string;
  /** `success` tints the amount green — used for the discount line. */
  emphasis?: 'success';
}

/**
 * One line of an order's price breakdown (Subtotal / Discount / Tax). Shared by the checkout
 * summary and the placed-order view so the two can never drift apart visually.
 */
export function PriceRow({ label, amountMinor, currency, emphasis }: PriceRowProps) {
  const negative = amountMinor < 0;
  return (
    <div style={{ display: 'flex', justifyContent: 'space-between', padding: '4px 0' }}>
      <Typography.Text type="secondary">{label}</Typography.Text>
      <Typography.Text type={emphasis === 'success' ? 'success' : undefined}>
        {negative ? '−' : ''}
        {formatMoney(Math.abs(amountMinor), currency)}
      </Typography.Text>
    </div>
  );
}
