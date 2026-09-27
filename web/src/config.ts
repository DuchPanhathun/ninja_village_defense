/** Site-wide settings from the PUBLIC_* environment variables (see .env.example). */
const env = import.meta.env;

export const site = {
  gameName: 'Ninja Village Defense',
  studioName: env.PUBLIC_STUDIO_NAME || 'Ninja Village Studio',
  supportEmail: env.PUBLIC_SUPPORT_EMAIL || 'support@example.com',
  playStoreUrl: env.PUBLIC_PLAY_STORE_URL || '',
  paymentsTestMode: env.PUBLIC_PAYMENTS_TEST_MODE !== 'false',
  legalDraft: env.PUBLIC_LEGAL_DRAFT !== 'false',
  /** Players younger than this need a parent's consent (Terms, Privacy). */
  minimumAge: 13,
  legalUpdated: '2026-09-27',
};

export const firebaseSettings = {
  useEmulators: env.PUBLIC_USE_EMULATORS === 'true',
  options: {
    apiKey: env.PUBLIC_FIREBASE_API_KEY,
    authDomain: env.PUBLIC_FIREBASE_AUTH_DOMAIN,
    projectId: env.PUBLIC_FIREBASE_PROJECT_ID,
    appId: env.PUBLIC_FIREBASE_APP_ID,
  },
  functionsRegion: env.PUBLIC_FUNCTIONS_REGION || 'us-central1',
  recaptchaKey: env.PUBLIC_RECAPTCHA_ENTERPRISE_KEY || '',
};
