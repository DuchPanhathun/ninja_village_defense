import { useRef, useState } from 'preact/hooks';
import {
  PhoneAuthProvider,
  PhoneMultiFactorGenerator,
  RecaptchaVerifier,
  TotpMultiFactorGenerator,
  getMultiFactorResolver,
  sendPasswordResetEmail,
  signInWithEmailAndPassword,
  type MultiFactorError,
  type MultiFactorResolver,
  type PhoneMultiFactorInfo,
} from 'firebase/auth';
import { auth } from '../../lib/firebase';
import { fill, queryParam } from '../../lib/format';
import { signOutEverywhere, useSession } from '../../lib/session';
import { t } from '../../i18n';

/**
 * Only same-site paths, so ?next= can't send people to another website. Resolved like the browser would, which also
 * catches tricks such as "//evil.com" and "/\evil.com".
 */
function nextPath(): string {
  const next = queryParam('next');
  if (!next.startsWith('/')) return '/account/';
  const url = new URL(next, window.location.origin);
  return url.origin === window.location.origin ? url.pathname + url.search + url.hash : '/account/';
}

function authError(error: unknown): string {
  const code = (error as { code?: string })?.code ?? '';
  if (['auth/invalid-credential', 'auth/wrong-password', 'auth/user-not-found', 'auth/invalid-email', 'auth/missing-password'].includes(code))
    return t.errors['wrong-credentials'];
  if (code === 'auth/too-many-requests') return t.errors['too-many-requests'];
  if (code === 'auth/user-disabled') return t.errors['user-disabled'];
  if (code === 'auth/invalid-verification-code' || code === 'auth/missing-code') return t.errors['bad-mfa-code'];
  if (code === 'auth/network-request-failed') return t.errors.network;
  return t.errors.generic;
}

/**
 * Email + password sign-in with the account linked in the game (there is no sign-up here: accounts are made in the
 * game). Staff with 2-step verification get a second step for their authenticator code.
 */
export default function SignInForm() {
  const session = useSession();
  // Uncontrolled inputs, read on submit: whatever was typed or autofilled before the island woke up is kept.
  const form = useRef<HTMLFormElement>(null);
  const [code, setCode] = useState('');
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState('');
  const [info, setInfo] = useState('');
  const [resolver, setResolver] = useState<MultiFactorResolver | null>(null);
  /** Set when the second factor is a phone: the SMS verification id and where it went. */
  const [sms, setSms] = useState<{ verificationId: string; phone: string } | null>(null);

  const done = () => window.location.assign(nextPath());

  async function submit(event: Event) {
    event.preventDefault();
    setBusy(true);
    setError('');
    setInfo('');
    const data = new FormData(event.currentTarget as HTMLFormElement);
    try {
      await signInWithEmailAndPassword(auth(), String(data.get('email') ?? '').trim(), String(data.get('password') ?? ''));
      done();
    } catch (err) {
      if ((err as { code?: string }).code === 'auth/multi-factor-auth-required') {
        const mfa = getMultiFactorResolver(auth(), err as MultiFactorError);
        setResolver(mfa);
        // Authenticator app first; otherwise text a code to the enrolled phone.
        if (!mfa.hints.some((h) => h.factorId === TotpMultiFactorGenerator.FACTOR_ID)) await sendSms(mfa).catch((e) => setError(authError(e)));
      } else {
        setError(authError(err));
      }
    } finally {
      setBusy(false);
    }
  }

  async function sendSms(mfa: MultiFactorResolver) {
    const hint = mfa.hints.find((h) => h.factorId === PhoneMultiFactorGenerator.FACTOR_ID) as PhoneMultiFactorInfo | undefined;
    if (!hint) return setError(t.errors.generic);
    const instance = auth();
    const verifier = new RecaptchaVerifier(instance, 'mfa-recaptcha', { size: 'invisible' });
    try {
      const verificationId = await new PhoneAuthProvider(instance).verifyPhoneNumber({ multiFactorHint: hint, session: mfa.session }, verifier);
      setSms({ verificationId, phone: hint.phoneNumber });
    } finally {
      verifier.clear();
    }
  }

  async function verify(event: Event) {
    event.preventDefault();
    if (!resolver) return;
    setBusy(true);
    setError('');
    try {
      const totp = resolver.hints.find((h) => h.factorId === TotpMultiFactorGenerator.FACTOR_ID);
      if (totp) {
        await resolver.resolveSignIn(TotpMultiFactorGenerator.assertionForSignIn(totp.uid, code.trim()));
      } else if (sms) {
        const credential = PhoneAuthProvider.credential(sms.verificationId, code.trim());
        await resolver.resolveSignIn(PhoneMultiFactorGenerator.assertion(credential));
      } else {
        return setError(t.errors.generic);
      }
      done();
    } catch (err) {
      setError(authError(err));
    } finally {
      setBusy(false);
    }
  }

  async function forgot() {
    setError('');
    const email = String(new FormData(form.current!).get('email') ?? '').trim();
    if (!email) return setInfo(t.signIn.resetNeedsEmail);
    try {
      await sendPasswordResetEmail(auth(), email);
    } catch {
      // Same answer either way, so the form doesn't reveal which emails have accounts.
    }
    setInfo(t.signIn.resetSent);
  }

  if (session.status === 'signed-in' && !resolver) {
    return (
      <div class="panel stack">
        <p>{fill(t.signIn.signedInAs, { email: session.user.email ?? '' })}</p>
        <div class="row">
          <button class="btn btn-primary" type="button" onClick={done}>{t.signIn.continue}</button>
          <button class="link-button" type="button" onClick={() => signOutEverywhere('/signin/')}>{t.nav.signOut}</button>
        </div>
      </div>
    );
  }

  if (resolver) {
    return (
      <form class="panel stack" onSubmit={verify}>
        <h2>{t.signIn.mfaTitle}</h2>
        <p class="muted">
          {sms ? fill(t.signIn.mfaSmsLead, { phone: sms.phone })
            : resolver.hints.some((h) => h.factorId === TotpMultiFactorGenerator.FACTOR_ID) ? t.signIn.mfaLead : t.signIn.mfaSending}
        </p>
        <label class="field">
          <span>{t.signIn.mfaCode}</span>
          <input class="code-input" inputMode="numeric" autoComplete="one-time-code" pattern="[0-9]{6}" maxLength={6} required
            value={code} onInput={(e) => setCode(e.currentTarget.value)} autoFocus />
        </label>
        {error && <p class="notice error" role="alert">{error}</p>}
        <button class="btn btn-primary btn-block" disabled={busy}>{busy ? t.common.loading : t.signIn.mfaSubmit}</button>
      </form>
    );
  }


  return (
    <div class="stack">
      <form class="panel stack" onSubmit={submit} ref={form}>
        <label class="field">
          <span>{t.signIn.email}</span>
          <input name="email" type="email" autoComplete="email" required />
        </label>
        <label class="field">
          <span>{t.signIn.password}</span>
          <input name="password" type="password" autoComplete="current-password" required />
        </label>
        {error && <p class="notice error" role="alert">{error}</p>}
        {info && <p class="notice" role="status">{info}</p>}
        <button class="btn btn-primary btn-block" disabled={busy || session.status === 'loading'}>
          {busy ? t.common.loading : t.signIn.submit}
        </button>
        <button type="button" class="link-button small" onClick={forgot}>{t.signIn.forgot}</button>
      </form>
      <div class="notice warn">
        <img src="/art/icon_item_scroll.png" alt="" />
        <div>
          <strong>{t.signIn.noAccountTitle}</strong>
          <p class="small">{t.signIn.noAccountBody}</p>
        </div>
      </div>
    </div>
  );
}
