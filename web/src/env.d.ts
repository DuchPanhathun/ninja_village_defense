/// <reference types="astro/client" />

interface ImportMetaEnv {
  readonly PUBLIC_USE_EMULATORS?: string;
  readonly PUBLIC_FIREBASE_API_KEY: string;
  readonly PUBLIC_FIREBASE_AUTH_DOMAIN: string;
  readonly PUBLIC_FIREBASE_PROJECT_ID: string;
  readonly PUBLIC_FIREBASE_APP_ID: string;
  readonly PUBLIC_FUNCTIONS_REGION?: string;
  readonly PUBLIC_RECAPTCHA_ENTERPRISE_KEY?: string;
  readonly PUBLIC_STUDIO_NAME?: string;
  readonly PUBLIC_SUPPORT_EMAIL?: string;
  readonly PUBLIC_PLAY_STORE_URL?: string;
  readonly PUBLIC_PAYMENTS_TEST_MODE?: string;
  readonly PUBLIC_LEGAL_DRAFT?: string;
}

interface ImportMeta {
  readonly env: ImportMetaEnv;
}
