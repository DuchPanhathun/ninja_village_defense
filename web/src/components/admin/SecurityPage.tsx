import { useState } from 'preact/hooks';
import { TotpMultiFactorGenerator, multiFactor, type TotpSecret } from 'firebase/auth';
import { renderSVG } from 'uqr';
import { auth } from '../../lib/firebase';
import { formatDate } from '../../lib/format';
import { signOutEverywhere } from '../../lib/session';
import AdminGate from './AdminGate';
import { Section } from './ui';

const ISSUER = 'Ninja Village Admin';

function authMessage(error: unknown): string {
  const code = (error as { code?: string })?.code ?? '';
  if (code === 'auth/requires-recent-login') return 'For safety, sign out and back in, then try again right away.';
  if (code === 'auth/invalid-verification-code') return 'That code didn’t work. Check your phone’s clock and try the newest code.';
  if (code === 'auth/operation-not-allowed' || code === 'auth/admin-restricted-operation')
    return 'Authenticator apps aren’t turned on for this Firebase project yet (Identity Platform → multi-factor → TOTP).';
  if (code === 'auth/unverified-email') return 'Verify this account’s email address first (use “Forgot password” to set one if needed).';
  return (error as Error)?.message ?? 'Something went wrong.';
}

/**
 * Set up 2-step sign-in with an authenticator app (TOTP). Admins need it before the admin tools open; everyone
 * else on the staff is encouraged to use it too.
 */
function Security() {
  const user = auth().currentUser!;
  const [factors, setFactors] = useState(() => multiFactor(user).enrolledFactors);
  const [secret, setSecret] = useState<TotpSecret | null>(null);
  const [code, setCode] = useState('');
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState('');
  const [enrolled, setEnrolled] = useState(false);

  async function start() {
    setBusy(true);
    setError('');
    try {
      const session = await multiFactor(user).getSession();
      setSecret(await TotpMultiFactorGenerator.generateSecret(session));
    } catch (err) {
      setError(authMessage(err));
    } finally {
      setBusy(false);
    }
  }

  async function finish(event: Event) {
    event.preventDefault();
    if (!secret) return;
    setBusy(true);
    setError('');
    try {
      await multiFactor(user).enroll(TotpMultiFactorGenerator.assertionForEnrollment(secret, code.trim()), 'Authenticator app');
      setFactors(multiFactor(user).enrolledFactors);
      setSecret(null);
      setEnrolled(true);
    } catch (err) {
      setError(authMessage(err));
    } finally {
      setBusy(false);
    }
  }

  async function remove(uid: string) {
    if (!window.confirm('Remove this second step? Admin tools will be locked until you add one again.')) return;
    try {
      await multiFactor(user).unenroll(uid);
      setFactors(multiFactor(user).enrolledFactors);
    } catch (err) {
      setError(authMessage(err));
    }
  }

  const otpUrl = secret?.generateQrCodeUrl(user.email ?? user.uid, ISSUER);
  return (
    <div class="stack" style="max-width: 720px">
      <Section title="Your second steps">
        {factors.length === 0 ? <p class="muted">None yet.</p> : (
          <ul class="list">{factors.map((f) => (
            <li key={f.uid}>
              <span><b>{f.displayName ?? f.factorId}</b> <span class="badge">{f.factorId}</span></span>
              <span class="row">{!Number.isNaN(Date.parse(f.enrollmentTime)) && <span class="small muted">added {formatDate(Date.parse(f.enrollmentTime))}</span>}
                <button class="btn btn-small" onClick={() => remove(f.uid)}>Remove</button></span>
            </li>
          ))}</ul>
        )}
        {enrolled && (
          <div class="notice success stack">
            <p>Done! Sign out and back in with your code to open the admin tools.</p>
            <button class="btn btn-primary" onClick={() => signOutEverywhere('/signin/?next=/admin/')}>Sign in again</button>
          </div>
        )}
      </Section>

      <Section title="Add an authenticator app">
        {!secret ? (
          <div class="stack">
            <p class="muted">Use Google Authenticator, Microsoft Authenticator, 1Password or any app that shows 6-digit codes.</p>
            <button class="btn btn-primary" disabled={busy} onClick={start}>Start</button>
          </div>
        ) : (
          <form class="stack" onSubmit={finish}>
            <p>1. Scan this with your authenticator app:</p>
            {/* renderSVG builds the QR from our own otpauth:// URL (no user-supplied markup). */}
            <div class="qr" dangerouslySetInnerHTML={{ __html: renderSVG(otpUrl!) }} />
            <p class="small muted">Can’t scan? Enter this key: <code class="mono">{secret.secretKey}</code></p>
            <label class="field">
              <span>2. Enter the 6-digit code it shows</span>
              <input class="code-input" inputMode="numeric" autoComplete="one-time-code" pattern="[0-9]{6}" maxLength={6} required value={code} onInput={(e) => setCode(e.currentTarget.value)} />
            </label>
            <button class="btn btn-primary" disabled={busy}>Turn on 2-step sign-in</button>
          </form>
        )}
        {error && <p class="notice error" role="alert">{error}</p>}
      </Section>
    </div>
  );
}

export default function SecurityPage() {
  return <AdminGate allowWithoutMfa>{() => <Security />}</AdminGate>;
}
