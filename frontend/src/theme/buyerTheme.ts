import type { ThemeConfig } from 'antd';

// PouchNation-referenced storefront look: teal accent, soft off-white ground,
// generous rounding. Same primary color as adminTheme for brand consistency —
// only density/warmth differ between the two sections.
export const buyerTheme: ThemeConfig = {
  token: {
    colorPrimary: '#3ea8c4',
    colorBgLayout: '#f7f5f0',
    borderRadius: 12,
    fontFamily: "'Segoe UI', system-ui, -apple-system, 'Helvetica Neue', Arial, sans-serif",
  },
  components: {
    Button: {
      borderRadius: 999,
      controlHeight: 44,
    },
    Card: {
      borderRadiusLG: 16,
    },
  },
};
