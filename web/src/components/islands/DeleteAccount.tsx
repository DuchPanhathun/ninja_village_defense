import { useEffect, useState } from 'preact/hooks';
import { doc, getDoc } from 'firebase/firestore/lite';
import type { DeletionStatus } from '@shared/types';
import { call, errorMessage } from '../../lib/api';
import { db } from '../../lib/db';
import { signInUrl, useSession } from '../../lib/session';
import { site } from '../../config';
import { t } from '../../i18n';
import { Loading } from '../SignInPrompt';

function mailtoLink(uid = '') {
  const body = `Please delete my Ninja Village Defense account.\n\nAccount ID: ${uid || '(from the game: Profile)'}\n`;
  return `mailto:${site.supportEmail}?subject=${encodeURIComponent('Account deletion request')}&body=${encodeURIComponent(body)}`;
}

/** Signed in: request (or cancel) deletion. Signed out: sign in, or email support with the Account ID. */
export default function DeleteAccount() {
  const session = useSession();
  const [status, setStatus] = useState<DeletionStatus | null | undefined>(undefined);
  const [confirm, setConfirm] = useState('');
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState('');
  const [message, setMessage] = useState('');
  const uid = session.status === 'signed-in' ? session.user.uid : null;

  useEffect(() => {
    if (!uid) return;
    getDoc(doc(db(), 'deletionRequests', uid))
      .then((snap) => setStatus(snap.exists() ? (snap.data().status as DeletionStatus) : null))
      .catch(() => setStatus(null));
  }, [uid]);

  async function run(action: () => Promise<{ status: DeletionStatus }>, text: string) {
    setBusy(true);
    setError('');
    try {
      setStatus((await action()).status);
      setMessage(text);
      setConfirm('');
    } catch (err) {
      setError(errorMessage(err));
    } finally {
      setBusy(false);
    }
  }

  const emailOption = (
    <div class="panel panel-accent stack">
      <h3>{t.deletion.noEmailTitle}</h3>
      <p class="muted">{t.deletion.noEmail}</p>
      <a class="btn" href={mailtoLink(uid ?? '')}>{t.deletion.emailUs}</a>
    </div>
  );

  if (session.status === 'loading' || (uid && status === undefined)) return <Loading />;

  if (session.status === 'signed-out') {
    return (
      <div class="stack">
        <div class="panel stack">
          <a class="btn btn-primary" href={signInUrl()}>{t.deletion.signIn}</a>
        </div>
        {emailOption}
      </div>
    );
  }

  return (
    <div class="stack">
      <div class="panel stack">
        <h2>{t.deletion.signedInTitle}</h2>
        <p class="muted small">{session.user.email} · <span class="mono">{uid}</span></p>
        {message && <p class="notice success" role="status">{message}</p>}
        {status === 'pending' ? (
          <>
            {!message && <p class="notice warn">{t.deletion.pending}</p>}
            <button class="btn" disabled={busy} onClick={() => run(() => call('cancelAccountDeletion', {}), t.deletion.canceled)}>
              {t.deletion.cancel}
            </button>
          </>
        ) : (
          <form class="stack" onSubmit={(e) => {
            e.preventDefault();
            void run(() => call('requestAccountDeletion', { confirm: 'DELETE' }), t.deletion.pending);
          }}>
            <label class="field">
              <span>{t.deletion.confirmLabel}</span>
              <input value={confirm} autoComplete="off" onInput={(e) => setConfirm(e.currentTarget.value)} />
            </label>
            <button class="btn btn-danger" disabled={busy || confirm.trim() !== 'DELETE'}>{t.deletion.submit}</button>
          </form>
        )}
        {error && <p class="notice error" role="alert">{error}</p>}
      </div>
      {emailOption}
    </div>
  );
}
