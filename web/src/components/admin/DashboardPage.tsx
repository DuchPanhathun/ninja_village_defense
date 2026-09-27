import { collection, documentId, getCount, getDocs, orderBy, query, where } from 'firebase/firestore/lite';
import { formatMoney } from '@shared/money';
import type { DayStatsDoc } from '@shared/types';
import { call } from '../../lib/api';
import { db } from '../../lib/db';
import { formatNumber } from '../../lib/format';
import AdminGate from './AdminGate';
import BarChart, { type Bar } from './BarChart';
import { DAY, DataState, Section, useAsync } from './ui';

const DAYS = 30;

function dayKeys(): string[] {
  const today = Date.now();
  return Array.from({ length: DAYS }, (_, i) => new Date(today - (DAYS - 1 - i) * DAY).toISOString().slice(0, 10));
}

function compact(value: number): string {
  return new Intl.NumberFormat(undefined, { notation: 'compact', maximumFractionDigits: 1 }).format(value);
}

async function loadDashboard() {
  const database = db();
  const keys = dayKeys();
  const [statsSnap, counts, review, deletions] = await Promise.all([
    getDocs(query(collection(database, 'stats'), where(documentId(), '>=', keys[0]), orderBy(documentId()))),
    call('adminCounts', {}),
    getCount(query(collection(database, 'orders'), where('status', '==', 'review'))),
    getCount(query(collection(database, 'deletionRequests'), where('status', '==', 'pending'))),
  ]);
  const byDay = new Map(statsSnap.docs.map((d) => [d.id, d.data() as DayStatsDoc]));
  const days = keys.map((key) => ({ key, stats: byDay.get(key) ?? {} }));

  const revenue = new Map<string, number>();
  days.forEach(({ stats }) => Object.entries(stats.revenue ?? {}).forEach(([c, v]) => revenue.set(c, (revenue.get(c) ?? 0) + v)));
  const mainCurrency = [...revenue].sort((a, b) => b[1] - a[1])[0]?.[0] ?? 'USD';
  const sum = (field: 'orders' | 'refunds' | 'newPlayers' | 'redemptions' | 'gifts') => days.reduce((n, d) => n + (d.stats[field] ?? 0), 0);

  return {
    counts,
    review: review.data().count,
    deletions: deletions.data().count,
    revenue,
    mainCurrency,
    totals: { orders: sum('orders'), refunds: sum('refunds'), newPlayers: sum('newPlayers'), redemptions: sum('redemptions'), gifts: sum('gifts') },
    revenueBars: days.map(({ key, stats }): Bar => ({ key, label: shortDay(key), value: stats.revenue?.[mainCurrency] ?? 0 })),
    playerBars: days.map(({ key, stats }): Bar => ({ key, label: shortDay(key), value: stats.newPlayers ?? 0 })),
  };
}

function shortDay(key: string): string {
  return new Date(`${key}T00:00:00Z`).toLocaleDateString(undefined, { month: 'short', day: 'numeric', timeZone: 'UTC' });
}

function Tile({ label, value, href }: { label: string; value: string; href?: string }) {
  const body = <><b>{value}</b><span>{label}</span></>;
  return href ? <a class="stat" href={href} style="color: inherit; text-decoration: none">{body}</a> : <div class="stat">{body}</div>;
}

function Dashboard() {
  const state = useAsync(loadDashboard, []);
  const d = state.data;
  return (
    <DataState state={state}>
      {d && (
        <div class="stack-lg">
          <section class="panel stack">
            <div class="row" style="justify-content: space-between; align-items: flex-end">
              <div>
                <p class="label">Web shop revenue, last {DAYS} days</p>
                <p class="hero-figure">{formatMoney(d.revenue.get(d.mainCurrency) ?? 0, d.mainCurrency)}</p>
                {d.revenue.size > 1 && <p class="small muted">Also {[...d.revenue].filter(([c]) => c !== d.mainCurrency).map(([c, v]) => formatMoney(v, c)).join(' · ')}</p>}
              </div>
              <p class="small muted">UTC days · net of refunds</p>
            </div>
            <div class="stat-grid">
              <Tile label="Paid orders" value={formatNumber(d.totals.orders)} href="/admin/orders/" />
              <Tile label="Refunds & disputes" value={formatNumber(d.totals.refunds)} />
              <Tile label="New players" value={compact(d.totals.newPlayers)} />
              <Tile label="Codes redeemed" value={compact(d.totals.redemptions)} href="/admin/codes/" />
              <Tile label="Gifts sent" value={formatNumber(d.totals.gifts)} />
            </div>
          </section>

          {(d.review > 0 || d.deletions > 0) && (
            <Section title="Needs attention">
              <ul class="list">
                {d.review > 0 && <li><span><span class="badge bad">⚠ review</span> {d.review === 1 ? '1 order needs' : `${d.review} orders need`} a human (wrong amount or duplicate once-only offer)</span><a href="/admin/orders/">Open orders →</a></li>}
                {d.deletions > 0 && <li><span><span class="badge warn">⏳ pending</span> {d.deletions} account deletion request{d.deletions > 1 ? 's' : ''}</span><a href="/admin/deletions/">Open deletions →</a></li>}
              </ul>
            </Section>
          )}

          <div class="admin-grid two">
            <section class="panel"><BarChart title={`Revenue per day (${d.mainCurrency})`} bars={d.revenueBars} color="#c4862a" format={(v) => formatMoney(v, d.mainCurrency).replace(/\.00$/, '')} /></section>
            <section class="panel"><BarChart title="New players per day" bars={d.playerBars} color="#2fa89d" format={(v) => formatNumber(v)} /></section>
          </div>

          <Section title="Players">
            <div class="stat-grid">
              <Tile label="All accounts with a save" value={compact(d.counts.players)} />
              <Tile label="Active today" value={compact(d.counts.activeDay)} />
              <Tile label="Active this week" value={compact(d.counts.activeWeek)} />
              <Tile label="Active this month" value={compact(d.counts.activeMonth)} />
              <Tile label="Banned" value={formatNumber(d.counts.banned)} />
            </div>
            <p class="hint" style="margin-top: 0.75rem">“Active” = the game uploaded a cloud save in that period.</p>
          </Section>
        </div>
      )}
    </DataState>
  );
}

export default function DashboardPage() {
  return <AdminGate>{() => <Dashboard />}</AdminGate>;
}
