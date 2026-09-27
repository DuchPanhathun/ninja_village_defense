import { signInUrl } from '../lib/session';
import { t } from '../i18n';

/** Shown in place of a signed-in-only feature. */
export default function SignInPrompt({ message }: { message: string }) {
  return (
    <div class="panel center stack">
      <img class="pixel" src="/art/logo_emblem.png" alt="" width="72" height="72" />
      <p>{message}</p>
      <a class="btn btn-primary" href={signInUrl()}>{t.common.goSignIn}</a>
    </div>
  );
}

export function Loading() {
  return (
    <div class="panel stack" aria-busy="true">
      <span class="visually-hidden">{t.common.loading}</span>
      <div class="skeleton" style="height: 1.6rem; width: 45%" />
      <div class="skeleton" />
      <div class="skeleton" style="width: 80%" />
    </div>
  );
}
