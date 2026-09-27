import type { ComponentChildren } from 'preact';
import { useCallback, useEffect, useState } from 'preact/hooks';
import { REWARD_CATALOG, isRewardType } from '@shared/rewards';
import { REWARD_TYPES, type Reward, type RewardType } from '@shared/types';
import { errorMessage } from '../../lib/api';

// ---------------------------------------------------------------- data loading

export interface AsyncState<T> {
  data: T | undefined;
  error: string;
  loading: boolean;
  reload: () => void;
}

/** Runs `load` on mount and whenever `deps` change; `reload()` runs it again. */
export function useAsync<T>(load: () => Promise<T>, deps: unknown[]): AsyncState<T> {
  const [state, setState] = useState<{ data?: T; error: string; loading: boolean }>({ error: '', loading: true });
  const [tick, setTick] = useState(0);
  useEffect(() => {
    let live = true;
    setState((s) => ({ ...s, loading: true, error: '' }));
    load().then(
      (data) => live && setState({ data, error: '', loading: false }),
      (err) => live && setState({ error: errorMessage(err), loading: false }),
    );
    return () => void (live = false);
  }, [...deps, tick]);
  const reload = useCallback(() => setTick((n) => n + 1), []);
  return { data: state.data, error: state.error, loading: state.loading, reload };
}

/** Wraps a server action: busy flag, error and success messages, optional confirm() first. */
export function useAction() {
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState('');
  const [ok, setOk] = useState('');
  async function run(action: () => Promise<unknown>, success: string, confirmText?: string): Promise<boolean> {
    if (confirmText && !window.confirm(confirmText)) return false;
    setBusy(true);
    setError('');
    setOk('');
    try {
      await action();
      setOk(success);
      return true;
    } catch (err) {
      setError(errorMessage(err));
      return false;
    } finally {
      setBusy(false);
    }
  }
  const feedback = (
    <>
      {error && <p class="notice error" role="alert">{error}</p>}
      {ok && <p class="notice success" role="status">{ok}</p>}
    </>
  );
  return { busy, run, feedback, clear: () => (setError(''), setOk('')) };
}

export function DataState({ state, children, empty }: { state: AsyncState<unknown>; children: ComponentChildren; empty?: boolean }) {
  if (state.error) return <p class="notice error">{state.error} <button class="link-button" onClick={state.reload}>Try again</button></p>;
  if (state.loading && state.data === undefined) return <div class="stack" aria-busy="true"><div class="skeleton" /><div class="skeleton" style="width: 70%" /></div>;
  if (empty) return <p class="muted">Nothing here yet.</p>;
  return <>{children}</>;
}

// ---------------------------------------------------------------- small pieces

export function Section({ title, actions, children }: { title: string; actions?: ComponentChildren; children: ComponentChildren }) {
  return (
    <section class="panel">
      <div class="section-head"><h2>{title}</h2>{actions && <div class="row">{actions}</div>}</div>
      {children}
    </section>
  );
}

export function PlayerLink({ uid, name }: { uid: string; name?: string }) {
  return <a href={`/admin/player/?uid=${encodeURIComponent(uid)}`}>{name || <span class="mono">{uid}</span>}</a>;
}

export function Json({ value }: { value: unknown }) {
  return (
    <details class="json">
      <summary>details</summary>
      <pre>{JSON.stringify(value, null, 2)}</pre>
    </details>
  );
}

/** A datetime-local input bound to epoch ms (local time), or null when empty. */
export function DateTimeInput({ value, onChange, label, hint }: { value: number | null; onChange: (ms: number | null) => void; label: string; hint?: string }) {
  const toLocal = (ms: number) => {
    const d = new Date(ms);
    d.setMinutes(d.getMinutes() - d.getTimezoneOffset());
    return d.toISOString().slice(0, 16);
  };
  return (
    <label class="field">
      <span>{label}</span>
      <input type="datetime-local" value={value === null ? '' : toLocal(value)}
        onInput={(e) => onChange(e.currentTarget.value ? new Date(e.currentTarget.value).getTime() : null)} />
      {hint && <span class="hint">{hint}</span>}
    </label>
  );
}

/** Pick rewards from the game's catalog (the server validates them again). */
export function RewardBuilder({ value, onChange }: { value: Reward[]; onChange: (rewards: Reward[]) => void }) {
  const update = (index: number, patch: Partial<Reward>) => onChange(value.map((r, i) => (i === index ? { ...r, ...patch } : r)));
  const setType = (index: number, type: RewardType) => update(index, { type, id: REWARD_CATALOG[type].entries[0].id, amount: 1 });
  return (
    <div class="stack" style="gap: 0.5rem">
      <span class="label">Rewards</span>
      {value.map((reward, index) => {
        const info = REWARD_CATALOG[reward.type];
        return (
          <div class="reward-row" key={index}>
            <select aria-label="Type" value={reward.type} onChange={(e) => isRewardType(e.currentTarget.value) && setType(index, e.currentTarget.value)}>
              {REWARD_TYPES.map((type) => <option value={type}>{REWARD_CATALOG[type].label}</option>)}
            </select>
            <select aria-label="Item" value={reward.id} disabled={info.entries.length === 1} onChange={(e) => update(index, { id: e.currentTarget.value })}>
              {info.entries.map((entry) => <option value={entry.id}>{entry.name}</option>)}
            </select>
            <input aria-label="Amount" type="number" min={1} max={info.maxAmount} value={reward.amount}
              onInput={(e) => update(index, { amount: Math.floor(Number(e.currentTarget.value) || 0) })} />
            <button type="button" class="btn btn-ghost btn-small" aria-label="Remove reward" disabled={value.length === 1}
              onClick={() => onChange(value.filter((_, i) => i !== index))}>✕</button>
          </div>
        );
      })}
      <button type="button" class="btn btn-small" style="justify-self: start" disabled={value.length >= 10}
        onClick={() => onChange([...value, { type: 'gems', id: 'gems', amount: 100 }])}>+ Add reward</button>
    </div>
  );
}

export function downloadCsv(filename: string, rows: (string | number)[][]) {
  const csv = rows.map((row) => row.map((cell) => `"${String(cell).replace(/"/g, '""')}"`).join(',')).join('\r\n');
  const url = URL.createObjectURL(new Blob([csv], { type: 'text/csv;charset=utf-8' }));
  const link = Object.assign(document.createElement('a'), { href: url, download: filename });
  link.click();
  URL.revokeObjectURL(url);
}

export const DAY = 86_400_000;
