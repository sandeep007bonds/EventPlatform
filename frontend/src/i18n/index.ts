import i18n from 'i18next';
import { initReactI18next } from 'react-i18next';
import LanguageDetector from 'i18next-browser-languagedetector';
import common from './locales/en/common.json';
import auth from './locales/en/auth.json';
import errors from './locales/en/errors.json';
import buyer from './locales/en/buyer.json';
import admin from './locales/en/admin.json';

// English-only for now; additional languages are additive (new `locales/<lng>/`
// folders registered as further `resources` entries), not a rework of this file.
void i18n
  .use(LanguageDetector)
  .use(initReactI18next)
  .init({
    resources: {
      en: { common, auth, errors, buyer, admin },
    },
    fallbackLng: 'en',
    defaultNS: 'common',
    ns: ['common', 'auth', 'errors', 'buyer', 'admin'],
    interpolation: { escapeValue: false },
  });

export default i18n;
