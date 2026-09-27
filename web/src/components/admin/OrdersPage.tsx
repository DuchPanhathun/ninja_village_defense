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
  type QueryConstraint,
  type QueryDocumentSnapshot,
} from 'firebase/firestore/lite';
import { formatMoney } from '@shared/money';
import type { OrderDoc, OrderStatus } from '@shared/types';
import { db } from '../../lib/db';
import { formatDate } from '../../lib/format';
import AdminGate from './AdminGate';
import { PlayerLink, Section } from './ui';

const PAGE = 50;
const STATUSES: OrderStatus[] = ['paid', 'pending', 'review', 'refunded', 'chargeback', 'canceled', 'failed'];
const BADGE: Record<OrderStatus, string> = { paid: 'good', pending: 'warn', review: 'bad', refunded: 'bad', chargeback: 'bad', canceled: '', failed: '' };

type Row = OrderDoc & { id: string };

function Orders() {
  const [status, setStatus] = useState<OrderStatus | ''>('');
  const [search, setSearch] = useState('');
  const [rows, setRows] = useState<Row[]>([]);
  const [cursor, setCursor] = useState<QueryDocumentSnapshot | null>(null);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState('');

  async function load(reset: boolean) {
    setBusy(true);
    setError('');
    const database = db();
    try {
      const term = search.trim();
      if (term && !term.includes(' ')) {
        // An order ID, or else a player's account ID.
        const byId = await getDoc(doc(database, 'orders', term));
        if (byId.exists()) {
          setRows([{ id: byId.id, ...(byId.data() as OrderDoc) }]);
          setCursor(null);
          return;
        }
      }
      const filters: QueryConstraint[] = [];
      if (term) filters.push(where('uid', '==', term));
      if (status) filters.push(where('status', '==', status));
      const page = await getDocs(query(collection(database, 'orders'), ...filters, orderBy('createdAt', 'desc'), ...(reset || !cursor ? [] : [startAfter(cursor)]), limit(PAGE)));
      const docs = page.docs.map((d) => ({ id: d.id, ...(d.data() as OrderDoc) }));
      setRows(reset ? docs : [...rows, ...docs]);
      setCursor(page.docs.length === PAGE ? page.docs[page.docs.length - 1] : null);
    } catch (err) {
      setError(String((err as Error).message));
    } finally {
      setBusy(false);
    }
  }

  useEffect(() => {
    void load(true);
  }, [status, search]);

  const totals = new Map<string, number>();
  rows.filter((r) => r.status === 'paid').forEach((r) => totals.set(r.currency, (totals.get(r.currency) ?? 0) + r.amount));

  return (
    <Section title="Web orders">
      <div class="toolbar">
        <label class="field"><span>Status</span>
          <select value={status} onChange={(e) => setStatus(e.currentTarget.value as OrderStatus | '')}>
            <option value="">All</option>
            {STATUSES.map((s) => <option value={s}>{s}</option>)}
          </select>
        </label>
        <label class="field"><span>Order ID or account ID</span><input class="mono" value={search} onChange={(e) => setSearch(e.currentTarget.value)} /></label>
      </div>
      {error && <p class="notice error">{error}</p>}
      <p class="small muted">
        Paid in this list: {totals.size === 0 ? '—' : [...totals].map(([currency, sum]) => formatMoney(sum, currency)).join(' · ')}
      </p>
      <div class="table-wrap"><table>
        <thead><tr><th>Created</th><th>Order</th><th>Product</th><th>Player</th><th class="num">Amount</th><th>Status</th><th>Note</th></tr></thead>
        <tbody>{rows.map((o) => (
          <tr key={o.id}>
            <td>{formatDate(o.createdAt, true)}</td>
            <td class="mono small">{o.id}<div class="muted">{o.provider}{o.paymentId && ` · ${o.paymentId}`}</div></td>
            <td>{o.productName}</td>
            <td><PlayerLink uid={o.uid} name={o.email ?? undefined} /></td>
            <td class="num">{formatMoney(o.amount, o.currency)}</td>
            <td><span class={`badge ${BADGE[o.status]}`}>{o.status}</span></td>
            <td class="small">{o.note}</td>
          </tr>
        ))}</tbody>
      </table></div>
      {rows.length === 0 && !busy && <p class="muted">No orders.</p>}
      {cursor && <button class="btn btn-small" disabled={busy} onClick={() => load(false)}>Load more</button>}
    </Section>
  );
}

export default function OrdersPage() {
  return <AdminGate>{() => <Orders />}</AdminGate>;
}
