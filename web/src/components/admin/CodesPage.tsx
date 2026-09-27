import { useEffect, useState } from 'preact/hooks';
import {
  collection,
  doc,
  getDoc,
  getDocs,
  limit,
  orderBy,
  query,
  startAfter,
  where,
  type QueryDocumentSnapshot,
} from 'firebase/firestore/lite';
import { formatCode, normalizeCode } from '@shared/codes';
import type { CodeDoc, CodeKind, Reward } from '@shared/types';
import { call } from '../../lib/api';
import { db } from '../../lib/db';
import { formatDate } from '../../lib/format';
import RewardChips from '../RewardChips';
import AdminGate, { type Staff } from './AdminGate';
import { DAY, DateTimeInput, PlayerLink, RewardBuilder, Section, downloadCsv, useAction } from './ui';

const PAGE = 25;
const KIND_HELP: Record<CodeKind, string> = {
  single: 'Single-use: the first player to redeem it uses it up. Best for bulk codes you hand out one by one.',
  campaign: 'Campaign: one shared code with a total use limit (e.g. the first 1,000 players).',
  open: 'Open: any number of players, once each (e.g. a launch or stream code).',
};

function CreateCodes({ onCreated }: { onCreated: () => void }) {
  const action = useAction();
  const [mode, setMode] = useState<'custom' | 'bulk'>('custom');
  const [code, setCode] = useState('');
  const [count, setCount] = useState(100);
  const [prefix, setPrefix] = useState('');
  const [kind, setKind] = useState<CodeKind>('open');
  const [maxUses, setMaxUses] = useState(1000);
  const [rewards, setRewards] = useState<Reward[]>([{ type: 'gems', id: 'gems', amount: 100 }]);
  const [message, setMessage] = useState('');
  const [startsAt, setStartsAt] = useState<number | null>(null);
  const [expiresAt, setExpiresAt] = useState<number | null>(Date.now() + 30 * DAY);
  const [note, setNote] = useState('');
  const [created, setCreated] = useState<{ codes: string[]; batchId: string | null } | null>(null);

  async function submit(event: Event) {
    event.preventDefault();
    let result: { codes: string[]; batchId: string | null } | null = null;
    const ok = await action.run(async () => {
      result = await call('adminCreateCodes', {
        code: mode === 'custom' ? code : '',
        count: mode === 'bulk' ? count : 0,
        prefix,
        kind: mode === 'bulk' && kind === 'open' ? 'single' : kind,
        maxUses: kind === 'campaign' ? maxUses : null,
        rewards,
        message,
        startsAt,
        expiresAt,
        note,
      });
    }, mode === 'custom' ? 'Code created.' : 'Codes created — download the CSV now.');
    if (ok && result) {
      setCreated(result);
      setCode('');
      onCreated();
    }
  }

  return (
    <Section title="Create codes">
      <form class="stack" onSubmit={submit}>
        <div class="tabs" role="tablist">
          <button type="button" role="tab" aria-selected={mode === 'custom'} onClick={() => setMode('custom')}>One custom code</button>
          <button type="button" role="tab" aria-selected={mode === 'bulk'} onClick={() => { setMode('bulk'); if (kind === 'open') setKind('single'); }}>Generate many</button>
        </div>
        {mode === 'custom' ? (
          <label class="field">
            <span>Code</span>
            <input class="mono" value={code} required minLength={4} maxLength={30} placeholder="SAKURA2026"
              onInput={(e) => setCode(e.currentTarget.value.toUpperCase())} />
            <span class="hint">Stored as {normalizeCode(code) || '…'} — letters and digits; dashes and spaces are ignored.</span>
          </label>
        ) : (
          <div class="form-grid two">
            <label class="field"><span>How many</span><input type="number" min={1} max={5000} value={count} onInput={(e) => setCount(Number(e.currentTarget.value))} /></label>
            <label class="field"><span>Prefix (optional)</span><input class="mono" value={prefix} maxLength={8} placeholder="YT" onInput={(e) => setPrefix(e.currentTarget.value.toUpperCase())} /></label>
          </div>
        )}
        <label class="field">
          <span>Type</span>
          <select value={kind} onChange={(e) => setKind(e.currentTarget.value as CodeKind)}>
            <option value="single">Single-use</option>
            <option value="campaign">Campaign (use limit)</option>
            {mode === 'custom' && <option value="open">Open (unlimited)</option>}
          </select>
          <span class="hint">{KIND_HELP[kind]}</span>
        </label>
        {kind === 'campaign' && (
          <label class="field"><span>Total uses</span><input type="number" min={1} value={maxUses} onInput={(e) => setMaxUses(Number(e.currentTarget.value))} /></label>
        )}
        <RewardBuilder value={rewards} onChange={setRewards} />
        <label class="field"><span>Inbox message (optional)</span><input value={message} maxLength={200} placeholder="Thanks for watching the stream!" onInput={(e) => setMessage(e.currentTarget.value)} /></label>
        <div class="form-grid two">
          <DateTimeInput label="Starts" value={startsAt} onChange={setStartsAt} hint="Empty = now" />
          <DateTimeInput label="Ends" value={expiresAt} onChange={setExpiresAt} hint="Empty = never" />
        </div>
        <label class="field"><span>Note for staff (optional)</span><input value={note} maxLength={200} placeholder="Where these codes are going" onInput={(e) => setNote(e.currentTarget.value)} /></label>
        {action.feedback}
        <button class="btn btn-primary" disabled={action.busy}>{action.busy ? 'Creating…' : mode === 'custom' ? 'Create code' : `Generate ${count} codes`}</button>
        {created && created.codes.length > 1 && (
          <button type="button" class="btn" onClick={() => downloadCsv(`codes-${created.batchId}.csv`, [['code', 'formatted'], ...created.codes.map((c) => [c, formatCode(c)])])}>
            Download {created.codes.length} codes (CSV)
          </button>
        )}
      </form>
    </Section>
  );
}

function WhoUsed({ code }: { code: string }) {
  const [rows, setRows] = useState<{ uid: string; at: number }[] | null>(null);
  if (rows === null) {
    return <button class="link-button small" onClick={async () => {
      const snap = await getDocs(query(collection(db(), 'codes', code, 'redemptions'), orderBy('at', 'desc'), limit(100)));
      setRows(snap.docs.map((d) => ({ uid: d.id, at: d.data().at.toMillis() })));
    }}>who used it</button>;
  }
  if (rows.length === 0) return <span class="small muted">nobody yet</span>;
  return (
    <ul class="small" style="margin: 0.25rem 0 0; padding-left: 1rem">
      {rows.map((r) => <li key={r.uid}><PlayerLink uid={r.uid} /> · {formatDate(r.at, true)}</li>)}
    </ul>
  );
}

function CodeList({ staff, version }: { staff: Staff; version: number }) {
  const toggle = useAction();
  const [batch, setBatch] = useState('');
  const [find, setFind] = useState('');
  const [codes, setCodes] = useState<CodeDoc[]>([]);
  const [cursor, setCursor] = useState<QueryDocumentSnapshot | null>(null);
  const [busy, setBusy] = useState(false);

  async function load(reset: boolean) {
    setBusy(true);
    const database = db();
    try {
      const exact = normalizeCode(find);
      if (exact) {
        const snap = await getDoc(doc(database, 'codes', exact));
        setCodes(snap.exists() ? [snap.data() as CodeDoc] : []);
        setCursor(null);
      } else {
        const filters = batch ? [where('batchId', '==', batch)] : [];
        const page = await getDocs(query(collection(database, 'codes'), ...filters, orderBy('createdAt', 'desc'), ...(reset || !cursor ? [] : [startAfter(cursor)]), limit(PAGE)));
        const docs = page.docs.map((d) => d.data() as CodeDoc);
        setCodes(reset ? docs : [...codes, ...docs]);
        setCursor(page.docs.length === PAGE ? page.docs[page.docs.length - 1] : null);
      }
    } finally {
      setBusy(false);
    }
  }

  useEffect(() => {
    void load(true);
  }, [version, batch, find]);

  return (
    <Section title="Codes">
      <div class="toolbar">
        <label class="field"><span>Find a code</span><input class="mono" value={find} placeholder="exact code" onChange={(e) => setFind(e.currentTarget.value)} /></label>
        <label class="field"><span>Batch</span><input class="mono" value={batch} placeholder="batch_…" onChange={(e) => setBatch(e.currentTarget.value.trim())} /></label>
      </div>
      {toggle.feedback}
      {codes.length === 0 && !busy ? <p class="muted">No codes.</p> : (
        <div class="table-wrap"><table>
          <thead><tr><th>Code</th><th>Rewards</th><th class="num">Used</th><th>Window</th><th>Status</th><th /></tr></thead>
          <tbody>{codes.map((c) => (
            <tr key={c.code}>
              <td>
                <b class="mono" style="white-space: nowrap">{c.batchId ? formatCode(c.code) : c.code}</b>
                <div class="small muted">{c.kind}{c.batchId && <> · <button class="link-button" onClick={() => { setFind(''); setBatch(c.batchId!); }}>{c.batchId}</button></>}</div>
                {c.note && <div class="small muted">{c.note}</div>}
              </td>
              <td><RewardChips rewards={c.rewards} showInternal /></td>
              <td class="num">{c.uses}{c.maxUses !== null && ` / ${c.maxUses}`}<div><WhoUsed code={c.code} /></div></td>
              <td class="small">{c.startsAt ? formatDate(c.startsAt) : 'now'} → {c.expiresAt ? formatDate(c.expiresAt) : 'never'}</td>
              <td>{c.active ? <span class="badge good">on</span> : <span class="badge">off</span>}</td>
              <td>{staff.can('codes.write') && (
                <button class="btn btn-small" disabled={toggle.busy} onClick={async () => {
                  if (await toggle.run(() => call('adminSetCodeActive', { code: c.code, active: !c.active }), c.active ? `${c.code} switched off.` : `${c.code} switched on.`)) {
                    setCodes(codes.map((x) => (x.code === c.code ? { ...x, active: !c.active } : x)));
                  }
                }}>{c.active ? 'Turn off' : 'Turn on'}</button>
              )}</td>
            </tr>
          ))}</tbody>
        </table></div>
      )}
      {cursor && <button class="btn btn-small" disabled={busy} onClick={() => load(false)}>Load more</button>}
    </Section>
  );
}

function Codes({ staff }: { staff: Staff }) {
  const [version, setVersion] = useState(0);
  return (
    <div class={staff.can('codes.write') ? 'admin-grid side' : 'stack'}>
      <CodeList staff={staff} version={version} />
      {staff.can('codes.write') && <CreateCodes onCreated={() => setVersion((v) => v + 1)} />}
    </div>
  );
}

export default function CodesPage() {
  return <AdminGate>{(staff) => <Codes staff={staff} />}</AdminGate>;
}
