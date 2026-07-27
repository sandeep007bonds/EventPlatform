import type { ThemeConfig } from 'antd';

// Same primary accent as buyerTheme for brand consistency; otherwise stays
// close to Ant's information-dense defaults — organizers work through tables
// and forms, not a storefront.
export const adminTheme: ThemeConfig = {
  token: {
    colorPrimary: '#3ea8c4',
    borderRadius: 6,
  },
};
