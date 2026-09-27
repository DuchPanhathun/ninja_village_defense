import { useState } from 'preact/hooks';
import { collection, getDocs, limit, orderBy, query, where } from 'firebase/firestore/lite';
import type { DeletionRequestDoc } from '@shared/types';
import { call } from '../../lib/api';
import { db } from '../../lib/db';
import { formatDate, relativeTime } from '../../lib/format';
import AdminGate, { type Staff } from './AdminGate';
import { DataState, PlayerLink, Section, useAction, useAsync } from './ui';

const DEADLINE_DAYS = 30;

function Deletions({ staff }: { staff: Staff }) {
  const action = useAction();
  const [uid, setUid] = useState('');
  const canDelete = staff.can('players.delete');
  const database = db();
  const pending = useAsync(async () => {
    const snap = await getDocs(query(collection(database, 'deletionRequests'), where('status', '==', 'pending'), orderBy('createdAt', 'desc'), limit(100)));
    return snap.docs.map((d) => d.data() as DeletionRequestDoc);
  }, []);
  const recent = useAsync(async () => {
    const snap = await getDocs(query(collection(database, 'deletionRequests'), orderBy('createdAt', 'desc'), limit(50)));
    return snap.docs.map((d) => d.data() as DeletionRequestDoc).filter((r) => r.status !== 'pending');
  }, []);
  const reload = () => { pending.reload(); recent.reload(); };

  async function process(target: string, act: 'delete' | 'reject') {
    const note = window.prompt(act === 'delete' ? `Delete account ${target} and all its data? This can't be undone.\nNote (optional):` : 'Why reject this request?');
    if (note === null) return;
    if (await action.run(() => call('adminProcessDeletion', { uid: target, action: act, note }), act === 'delete' ? 'Account deleted.' : 'Request rejected.')) reload();
  }

  return (
    <div class="stack">
      <Section title="Waiting">
        <p class="hint">Google Play expects deletions within a reasonable time — we promise {DEADLINE_DAYS} days on the deletion page.</p>
        {action.feedback}
        <DataState state={pending} empty={pending.data?.length === 0}>
          <ul class="list">{pending.data?.map((r) => {
            const age = (Date.now() - r.createdAt.toMillis()) / 86_400_000;
            return (
              <li key={r.uid}>
                <div>
                  <PlayerLink uid={r.uid} name={r.displayName || r.uid} />
                  <div class="small muted">{r.email ?? 'no email'} · requested {relativeTime(r.createdAt)} on the {r.source}</div>
                </div>
                <div class="row">
                  {age > DEADLINE_DAYS - 7 && <span class="badge bad">due soon</span>}
                  {canDelete && <>
                    <button class="btn btn-danger btn-small" disabled={action.busy} onClick={() => process(r.uid, 'delete')}>Delete now</button>
                    <button class="btn btn-small" disabled={action.busy} onClick={() => process(r.uid, 'reject')}>Reject</button>
                  </>}
                </div>
              </li>
            );
          })}</ul>
        </DataState>
      </Section>

      {canDelete && (
        <Section title="Request by email">
          <form class="toolbar" onSubmit={(e) => { e.preventDefault(); void process(uid.trim(), 'delete'); }}>
            <label class="field"><span>Account ID (after confirming it’s really them)</span><input class="mono" value={uid} required onInput={(e) => setUid(e.currentTarget.value)} /></label>
            <button class="btn btn-danger" disabled={action.busy}>Delete account</button>
          </form>
        </Section>
      )}

      <Section title="Recently handled">
        <DataState state={recent} empty={recent.data?.length === 0}>
          <ul class="list">{recent.data?.map((r) => (
            <li key={r.uid}>
              <span><b>{r.displayName || r.uid}</b> <span class="badge">{r.status}</span>{r.note && <span class="small muted"> — {r.note}</span>}</span>
              <span class="small muted">{r.processedBy ?? ''} · {formatDate(r.processedAt, true)}</span>
            </li>
          ))}</ul>
        </DataState>
      </Section>
    </div>
  );
}

export default function DeletionsPage() {
  return <AdminGate>{(staff) => <Deletions staff={staff} />}</AdminGate>;
}
