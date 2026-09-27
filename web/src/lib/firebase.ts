import { getApps, initializeApp, type FirebaseApp } from 'firebase/app';
import { browserLocalPersistence, connectAuthEmulator, getAuth, setPersistence, type Auth } from 'firebase/auth';
import { firebaseSettings } from '../config';

/**
 * Firebase, started on first use in the browser (never during the static build). Each service lives in its own
 * module (auth here, Firestore in db.ts, Cloud Functions in functions.ts) so a page only downloads the SDKs its
 * islands use — the landing page needs Auth for the header, not Firestore. Every island on a page shares these
 * instances, so they all see the same signed-in user.
 */
export function app(): FirebaseApp {
  if (typeof window === 'undefined') throw new Error('Firebase is only available in the browser');
  return getApps()[0] ?? initializeApp(firebaseSettings.options);
}

/** Host of the local emulators (the page's own host, so phones on the LAN work too). */
export function emulatorHost(): string | null {
  return firebaseSettings.useEmulators ? window.location.hostname : null;
}

let authInstance: Auth | null = null;

export function auth(): Auth {
  if (authInstance) return authInstance;
  const instance = getAuth(app());
  const host = emulatorHost();
  if (host) {
    connectAuthEmulator(instance, `http://${host}:9099`, { disableWarnings: true });
    // The emulator sends no real SMS and needs no reCAPTCHA for 2-step sign-in.
    instance.settings.appVerificationDisabledForTesting = true;
  }
  void setPersistence(instance, browserLocalPersistence);
  authInstance = instance;
  return instance;
}
