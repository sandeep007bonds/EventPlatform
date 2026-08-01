import type { ThemeConfig } from 'antd';
import { PRIMARY_COLOR } from './colors';

// Same primary accent as buyerTheme for brand consistency; denser and cooler than the storefront
// — organizers work through tables and forms, not a storefront — but still a real, considered
// palette rather than Ant's raw defaults: a dark sider, a light content ground, and softer
// dividers/shadows for visual separation between sections.
export const adminTheme: ThemeConfig = {
  token: {
    colorPrimary: PRIMARY_COLOR,
    colorInfo: PRIMARY_COLOR,
    colorBgLayout: '#f3f5f7',
    colorBorderSecondary: '#e8ebee',
    borderRadius: 8,
    borderRadiusLG: 12,
    fontSize: 14,
    boxShadowTertiary: '0 1px 3px rgba(15, 23, 32, 0.06)',
  },
  components: {
    Layout: {
      siderBg: '#151f27',
      headerBg: '#ffffff',
      bodyBg: '#f3f5f7',
      headerPadding: '0 24px',
    },
    Menu: {
      darkItemBg: 'transparent',
      darkItemColor: 'rgba(255,255,255,0.68)',
      darkItemHoverColor: '#ffffff',
      darkItemSelectedColor: '#ffffff',
      darkItemSelectedBg: PRIMARY_COLOR,
    },
    Card: {
      borderRadiusLG: 12,
      paddingLG: 24,
    },
    Table: {
      headerBg: '#fafbfc',
      borderRadius: 8,
      cellPaddingBlock: 14,
    },
    Button: {
      controlHeight: 36,
      fontWeight: 500,
    },
  },
};
