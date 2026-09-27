import { connectFirestoreEmulator, getFirestore, type Firestore } from 'firebase/firestore/lite';
import { ensureAppCheck } from './appCheck';
import { app, emulatorHost } from './firebase';

let instance: Firestore | null = null;

/** Firestore (see firebase.ts for why each service has its own module). */
export function db(): Firestore {
  if (instance) return instance;
  ensureAppCheck();
  instance = getFirestore(app());
  const host = emulatorHost();
  if (host) connectFirestoreEmulator(instance, host, 8080);
  return instance;
}
