import { useEffect, useRef, useState } from 'preact/hooks';
import type { Api } from '@shared/api';
import { normalizeCode } from '@shared/codes';
import { call, errorMessage } from '../../lib/api';
import { queryParam } from '../../lib/format';
import { useSession } from '../../lib/session';
import { t } from '../../i18n';
import RewardChips from '../RewardChips';
import SignInPrompt, { Loading } from '../SignInPrompt';

/** Enter a code → the redeemCode function puts the reward in the in-game inbox. ?code= pre-fills it (for links). */
export default function RedeemForm() {
  const session = useSession();
  // Uncontrolled input (see SignInForm): state mirrors it for the hint and the button.
  const input = useRef<HTMLInputElement>(null);
  const [code, setCode] = useState('');
  useEffect(() => {
    if (input.current && !input.current.value) input.current.value = queryParam('code').toUpperCase();
    setCode(input.current?.value ?? '');
  }, [session.status]);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState('');
  const [result, setResult] = useState<Api['redeemCode']['res'] | null>(null);

  async function submit(event: Event) {
    event.preventDefault();
    setBusy(true);
    setError('');
    try {
      setResult(await call('redeemCode', { code: input.current?.value ?? code }));
      setCode('');
    } catch (err) {
      setError(errorMessage(err));
    } finally {
      setBusy(false);
    }
  }

  if (session.status === 'loading') return <Loading />;
  if (session.status === 'signed-out') return <SignInPrompt message={t.redeem.signInPrompt} />;

  if (result) {
    return (
      <div class="panel stack center redeem-done" role="status">
        <img class="pixel" src="/art/chest_surprise_open.png" alt="" width="112" height="112" />
        <h2>{t.redeem.successTitle}</h2>
        <p class="muted">{result.message}</p>
        <p>{t.redeem.successBody}</p>
        <div style="display: flex; justify-content: center"><RewardChips rewards={result.rewards} /></div>
        <button class="btn" type="button" onClick={() => setResult(null)}>{t.redeem.another}</button>
      </div>
    );
  }

  const canonical = normalizeCode(code);
  return (
    <form class="panel stack" onSubmit={submit}>
      <label class="field">
        <span>{t.redeem.label}</span>
        <input ref={input} class="code-input" placeholder={t.redeem.placeholder} autoComplete="off" autoCapitalize="characters"
          spellcheck={false} maxLength={40} required onInput={(e) => setCode(e.currentTarget.value)} />
      </label>
      {error && <p class="notice error" role="alert">{error}</p>}
      <button class="btn btn-primary btn-block" disabled={busy || canonical.length < 4}>{busy ? t.common.loading : t.redeem.submit}</button>
      <p class="hint center">{t.redeem.note}</p>
    </form>
  );
}
