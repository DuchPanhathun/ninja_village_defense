import { ReCaptchaEnterpriseProvider, initializeAppCheck } from 'firebase/app-check';
import { firebaseSettings } from '../config';
import { app, emulatorHost } from './firebase';

let started = false;

/**
 * App Check (reCAPTCHA Enterprise) proves requests come from this website, so scripts can't call the functions or
 * read Firestore directly. Started before the first Firestore or Functions call; off until a site key is set.
 */
export function ensureAppCheck(): void {
  if (started) return;
  started = true;
  if (emulatorHost() || !firebaseSettings.recaptchaKey) return;
  initializeAppCheck(app(), { provider: new ReCaptchaEnterpriseProvider(firebaseSettings.recaptchaKey), isTokenAutoRefreshEnabled: true });
}
