import { useEffect, useState } from 'preact/hooks';
import { onIdTokenChanged, signOut, type User } from 'firebase/auth';
import { isRole } from '@shared/roles';
import type { Role } from '@shared/types';
import { auth as firebaseAuth } from './firebase';

export type Session =
  | { status: 'loading' }
  | { status: 'signed-out' }
  | { status: 'signed-in'; user: User; role: Role | null; mfa: boolean };

let current: Session = { status: 'loading' };
let started = false;
const listeners = new Set<(session: Session) => void>();

function publish(session: Session) {
  current = session;
  listeners.forEach((listener) => listener(session));
}

function start() {
  if (started) return;
  started = true;
  let auth;
  try {
    auth = firebaseAuth();
  } catch (error) {
    // Missing or wrong web config (see .env.example): behave as signed out rather than spin forever.
    console.error('Firebase could not start', error);
    return publish({ status: 'signed-out' });
  }
  // onIdTokenChanged (not onAuthStateChanged) so a role granted later shows up after the token refreshes.
  onIdTokenChanged(auth, async (user) => {
    if (!user) return publish({ status: 'signed-out' });
    const token = await user.getIdTokenResult();
    const role = isRole(token.claims.role) ? token.claims.role : null;
    publish({ status: 'signed-in', user, role, mfa: !!token.signInSecondFactor });
  });
}

/** The signed-in player (shared by every island on the page). 'loading' until Firebase has restored the session. */
export function useSession(): Session {
  const [session, setSession] = useState<Session>(current);
  useEffect(() => {
    start();
    listeners.add(setSession);
    setSession(current);
    return () => void listeners.delete(setSession);
  }, []);
  return session;
}

export async function signOutEverywhere(redirect = '/') {
  await signOut(firebaseAuth());
  window.location.href = redirect;
}

/** /signin/?next=<this page>, so players come back here after signing in. */
export function signInUrl(next = typeof window === 'undefined' ? '/' : window.location.pathname + window.location.search): string {
  return `/signin/?next=${encodeURIComponent(next)}`;
}
