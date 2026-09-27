import { useState } from 'preact/hooks';
import { collection, getDocs, limit, orderBy, query } from 'firebase/firestore/lite';
import type { MailDoc, Reward } from '@shared/types';
import { call } from '../../lib/api';
import { db } from '../../lib/db';
import { formatDate, formatNumber } from '../../lib/format';
import RewardChips from '../RewardChips';
import AdminGate, { type Staff } from './AdminGate';
import { DAY, DataState, DateTimeInput, RewardBuilder, Section, useAction, useAsync } from './ui';

function Compose({ onSent }: { onSent: () => void }) {
  const action = useAction();
  const [kind, setKind] = useState<'gift' | 'compensation'>('compensation');
  const [title, setTitle] = useState('Sorry for the downtime!');
  const [message, setMessage] = useState('');
  const [rewards, setRewards] = useState<Reward[]>([{ type: 'gems', id: 'gems', amount: 100 }]);
  const [startsAt, setStartsAt] = useState<number | null>(Date.now());
  const [expiresAt, setExpiresAt] = useState<number | null>(Date.now() + 14 * DAY);
  const [newPlayersToo, setNewPlayersToo] = useState(false);

  return (
    <Section title="Send to everyone">
      <form class="stack" onSubmit={async (e) => {
        e.preventDefault();
        const ok = await action.run(
          () => call('adminSendMail', { kind, title, message, rewards, startsAt: startsAt ?? Date.now(), expiresAt: expiresAt ?? Date.now() + 14 * DAY, newPlayersToo }),
          'Sent. Each player collects it once, the next time their game syncs.',
          `Send "${title}" to every player?`,
        );
        if (ok) onSent();
      }}>
        <p class="hint">One mail for all players: each game turns it into an inbox reward once (no need to write to every player).</p>
        <label class="field"><span>Kind</span>
          <select value={kind} onChange={(e) => { const k = e.currentTarget.value as 'gift' | 'compensation'; setKind(k); setNewPlayersToo(k === 'gift'); }}>
            <option value="compensation">Compensation (e.g. after downtime)</option>
            <option value="gift">Gift / celebration</option>
          </select>
        </label>
        <label class="field"><span>Inbox title</span><input value={title} required maxLength={60} onInput={(e) => setTitle(e.currentTarget.value)} /></label>
        <label class="field"><span>Message</span><textarea value={message} maxLength={300} onInput={(e) => setMessage(e.currentTarget.value)} /></label>
        <RewardBuilder value={rewards} onChange={setRewards} />
        <div class="form-grid two">
          <DateTimeInput label="Collectable from" value={startsAt} onChange={setStartsAt} />
          <DateTimeInput label="Until" value={expiresAt} onChange={setExpiresAt} />
        </div>
        <label class="check"><input type="checkbox" checked={newPlayersToo} onChange={(e) => setNewPlayersToo(e.currentTarget.checked)} />
          Also for players who join after it’s sent</label>
        {action.feedback}
        <button class="btn btn-primary" disabled={action.busy}>Send to everyone</button>
      </form>
    </Section>
  );
}

function Mail({ staff }: { staff: Staff }) {
  const toggle = useAction();
  const state = useAsync(async () => {
    const snap = await getDocs(query(collection(db(), 'mail'), orderBy('createdAt', 'desc'), limit(50)));
    return snap.docs.map((d) => ({ id: d.id, ...(d.data() as MailDoc) }));
  }, []);
  const canWrite = staff.can('mail.write');
  const now = Date.now();

  return (
    <div class={canWrite ? 'admin-grid side' : 'stack'}>
      <Section title="Sent mail">
        {toggle.feedback}
        <DataState state={state} empty={state.data?.length === 0}>
          <ul class="list">{state.data?.map((m) => {
            const live = m.active && m.startsAt.toMillis() <= now && m.expiresAt.toMillis() > now;
            return (
              <li key={m.id}>
                <div class="stack" style="gap: 0.3rem">
                  <div class="row"><b>{m.title}</b><span class="badge">{m.kind}</span>{live ? <span class="badge good">live</span> : m.active ? <span class="badge warn">scheduled / ended</span> : <span class="badge">off</span>}</div>
                  <RewardChips rewards={m.rewards} showInternal />
                  <span class="small muted">
                    {formatDate(m.startsAt, true)} → {formatDate(m.expiresAt, true)} · {m.newPlayersToo ? 'everyone' : 'existing players only'} · collected by {formatNumber(m.collected)} · by {m.createdBy}
                  </span>
                </div>
                {canWrite && <button class="btn btn-small" disabled={toggle.busy} onClick={async () => {
                  if (await toggle.run(() => call('adminSetMailActive', { id: m.id, active: !m.active }), m.active ? 'Mail switched off.' : 'Mail switched on.')) state.reload();
                }}>{m.active ? 'Turn off' : 'Turn on'}</button>}
              </li>
            );
          })}</ul>
        </DataState>
      </Section>
      {canWrite && <Compose onSent={state.reload} />}
    </div>
  );
}

export default function MailPage() {
  return <AdminGate>{(staff) => <Mail staff={staff} />}</AdminGate>;
}
