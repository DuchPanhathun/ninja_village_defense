import { connectFunctionsEmulator, getFunctions, httpsCallable, type Functions } from 'firebase/functions';
import type { Api, ApiName } from '@shared/api';
import { firebaseSettings } from '../config';
import { t } from '../i18n';
import { ensureAppCheck } from './appCheck';
import { app, emulatorHost } from './firebase';

let instance: Functions | null = null;

function functions(): Functions {
  if (instance) return instance;
  ensureAppCheck();
  instance = getFunctions(app(), firebaseSettings.functionsRegion);
  const host = emulatorHost();
  if (host) connectFunctionsEmulator(instance, host, 5001);
  return instance;
}

/** Calls a Cloud Function with the request/response types from the shared contract. */
export async function call<N extends ApiName>(name: N, data: Api[N]['req']): Promise<Api[N]['res']> {
  const fn = httpsCallable<Api[N]['req'], Api[N]['res']>(functions(), name);
  return (await fn(data)).data;
}

/** The machine reason the functions attach to their errors (see lib/callable.ts `fail`). */
export function errorReason(error: unknown): string {
  const details = (error as { details?: { reason?: unknown } })?.details;
  return typeof details?.reason === 'string' ? details.reason : '';
}

/**
 * A message for people. Known reasons get the site's own (translatable) text; otherwise the function's message,
 * which is written for people too. Network and unexpected errors get a generic line.
 */
export function errorMessage(error: unknown): string {
  const reason = errorReason(error);
  const known = (t.errors as Record<string, string>)[reason];
  if (known) return known;
  const code = (error as { code?: string })?.code ?? '';
  if (code === 'functions/unavailable' || code === 'functions/deadline-exceeded') return t.errors.network;
  if (code.startsWith('functions/') && code !== 'functions/internal' && error instanceof Error && error.message) return error.message;
  return t.errors.generic;
}
