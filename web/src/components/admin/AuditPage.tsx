import { useEffect, useState } from 'preact/hooks';
import { collection, getDocs, limit, orderBy, query, startAfter, where, type QueryDocumentSnapshot } from 'firebase/firestore/lite';
import type { AuditDoc } from '@shared/types';
import { db } from '../../lib/db';
import { formatDate } from '../../lib/format';
import AdminGate from './AdminGate';
import { Json, PlayerLink, Section } from './ui';

const PAGE = 50;

/** Every staff action, newest first. Written only by the functions; nobody can edit or delete entries. */
function Audit() {
  const [target, setTarget] = useState('');
  const [actor, setActor] = useState('');
  const [rows, setRows] = useState<(AuditDoc & { id: string })[]>([]);
  const [cursor, setCursor] = useState<QueryDocumentSnapshot | null>(null);
  const [busy, setBusy] = useState(false);

  async function load(reset: boolean) {
    setBusy(true);
    try {
      const filters = target ? [where('targetUid', '==', target)] : actor ? [where('actorUid', '==', actor)] : [];
      const page = await getDocs(query(collection(db(), 'audit'), ...filters, orderBy('at', 'desc'), ...(reset || !cursor ? [] : [startAfter(cursor)]), limit(PAGE)));
      const docs = page.docs.map((d) => ({ id: d.id, ...(d.data() as AuditDoc) }));
      setRows(reset ? docs : [...rows, ...docs]);
      setCursor(page.docs.length === PAGE ? page.docs[page.docs.length - 1] : null);
    } finally {
      setBusy(false);
    }
  }

  useEffect(() => {
    void load(true);
  }, [target, actor]);

  return (
    <Section title="Staff actions">
      <div class="toolbar">
        <label class="field"><span>Player (account ID)</span><input class="mono" value={target} onChange={(e) => { setActor(''); setTarget(e.currentTarget.value.trim()); }} /></label>
        <label class="field"><span>Staff member (account ID)</span><input class="mono" value={actor} onChange={(e) => { setTarget(''); setActor(e.currentTarget.value.trim()); }} /></label>
      </div>
      <div class="table-wrap"><table>
        <thead><tr><th>When</th><th>Who</th><th>Action</th><th>Player</th><th>Details</th></tr></thead>
        <tbody>{rows.map((r) => (
          <tr key={r.id}>
            <td>{formatDate(r.at, true)}</td>
            <td>{r.actorEmail ?? r.actorUid} <span class="badge">{r.role}</span></td>
            <td><b>{r.action}</b></td>
            <td>{r.targetUid ? <PlayerLink uid={r.targetUid} /> : '—'}</td>
            <td><Json value={r.details} /></td>
          </tr>
        ))}</tbody>
      </table></div>
      {rows.length === 0 && !busy && <p class="muted">No entries.</p>}
      {cursor && <button class="btn btn-small" disabled={busy} onClick={() => load(false)}>Load more</button>}
    </Section>
  );
}

export default function AuditPage() {
  return <AdminGate>{() => <Audit />}</AdminGate>;
}
