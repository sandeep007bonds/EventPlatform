import type { ThemeConfig } from 'antd';
import { PRIMARY_COLOR } from './colors';

// PouchNation-referenced storefront look: teal accent, soft off-white ground, generous rounding,
// a dark header for contrast. Same primary color as adminTheme for brand consistency — only
// density/warmth differ between the two sections.
export const buyerTheme: ThemeConfig = {
  token: {
    colorPrimary: PRIMARY_COLOR,
    colorInfo: PRIMARY_COLOR,
    colorBgLayout: '#f7f5f0',
    colorTextSecondary: '#6b6459',
    colorTextTertiary: '#8f8a7e',
    colorBorderSecondary: '#ece7dc',
    borderRadius: 12,
    borderRadiusLG: 16,
    fontFamily: "'Segoe UI', system-ui, -apple-system, 'Helvetica Neue', Arial, sans-serif",
    fontSize: 15,
    boxShadowTertiary: '0 2px 10px rgba(31, 28, 20, 0.06)',
  },
  components: {
    Layout: {
      headerBg: '#1c2b30',
      bodyBg: '#f7f5f0',
      footerBg: 'transparent',
      headerPadding: '0 48px',
    },
    Menu: {
      darkItemBg: 'transparent',
      darkItemColor: 'rgba(255,255,255,0.75)',
      darkItemHoverColor: '#ffffff',
      darkItemSelectedColor: '#ffffff',
      darkItemSelectedBg: 'rgba(255,255,255,0.14)',
    },
    Button: {
      borderRadius: 999,
      controlHeight: 44,
      controlHeightLG: 48,
      fontWeight: 600,
    },
    Card: {
      borderRadiusLG: 16,
      paddingLG: 24,
    },
    Input: {
      borderRadius: 10,
      controlHeight: 42,
    },
    InputNumber: {
      borderRadius: 10,
      controlHeight: 42,
    },
    Select: {
      borderRadius: 10,
      controlHeight: 42,
    },
    Tag: {
      borderRadiusSM: 6,
    },
  },
};
