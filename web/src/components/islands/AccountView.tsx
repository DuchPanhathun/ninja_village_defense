import { useEffect, useState } from 'preact/hooks';
import { collection, doc, getDoc, getDocs, limit, orderBy, query, where, type DocumentData } from 'firebase/firestore/lite';
import { formatMoney } from '@shared/money';
import type { GrantDoc, MyRedemptionDoc, OrderDoc } from '@shared/types';
import { db } from '../../lib/db';
import { fill, formatDate, formatNumber } from '../../lib/format';
import { useSession } from '../../lib/session';
import { t } from '../../i18n';
import RewardChips from '../RewardChips';
import SignInPrompt, { Loading } from '../SignInPrompt';

interface AccountData {
  user: DocumentData | null;
  village: DocumentData | null;
  grants: { id: string; data: GrantDoc }[];
  redemptions: MyRedemptionDoc[];
  orders: { id: string; data: OrderDoc }[];
  deletionPending: boolean;
}

async function load(uid: string): Promise<AccountData> {
  const database = db();
  // allSettled: one missing piece (e.g. no public village yet) shouldn't blank the whole page.
  const [user, village, grants, redemptions, orders, deletion] = await Promise.allSettled([
    getDoc(doc(database, 'users', uid)),
    getDoc(doc(database, 'villages', uid)),
    getDocs(query(collection(database, 'users', uid, 'grants'), orderBy('createdAt', 'desc'), limit(20))),
    getDocs(query(collection(database, 'users', uid, 'redemptions'), orderBy('at', 'desc'), limit(20))),
    getDocs(query(collection(database, 'orders'), where('uid', '==', uid), orderBy('createdAt', 'desc'), limit(20))),
    getDoc(doc(database, 'deletionRequests', uid)),
  ]);
  const ok = <T,>(r: PromiseSettledResult<T>) => (r.status === 'fulfilled' ? r.value : null);
  return {
    user: ok(user)?.data() ?? null,
    village: ok(village)?.data() ?? null,
    grants: ok(grants)?.docs.map((d) => ({ id: d.id, data: d.data() as GrantDoc })) ?? [],
    redemptions: ok(redemptions)?.docs.map((d) => d.data() as MyRedemptionDoc) ?? [],
    orders: ok(orders)?.docs.map((d) => ({ id: d.id, data: d.data() as OrderDoc })) ?? [],
    deletionPending: ok(deletion)?.data()?.status === 'pending',
  };
}

function CopyButton({ text }: { text: string }) {
  const [copied, setCopied] = useState(false);
  return (
    <button type="button" class="btn btn-small" onClick={async () => {
      await navigator.clipboard?.writeText(text);
      setCopied(true);
      setTimeout(() => setCopied(false), 1500);
    }}>{copied ? t.account.copied : t.account.copy}</button>
  );
}

function grantStatus(grant: GrantDoc) {
  if (grant.revokedAt) return <span class="badge bad">{t.account.revoked}</span>;
  if (grant.claimedAt) return <span class="badge good">{t.account.collected}</span>;
  return <span class="badge warn">{t.account.waiting}</span>;
}

const ORDER_BADGE: Record<string, string> = { paid: 'good', pending: 'warn', review: 'warn', refunded: 'bad', chargeback: 'bad', canceled: '', failed: 'bad' };

export default function AccountView() {
  const session = useSession();
  const [data, setData] = useState<AccountData | null>(null);
  const uid = session.status === 'signed-in' ? session.user.uid : null;

  useEffect(() => {
    if (uid) load(uid).then(setData);
  }, [uid]);

  if (session.status === 'loading') return <Loading />;
  if (session.status === 'signed-out') return <SignInPrompt message={t.account.signInPrompt} />;
  if (!data) return <Loading />;

  const { user, village } = data;
  const name = (user?.displayName as string | undefined) || (village?.displayName as string | undefined) || 'Ninja';
  const until = user?.bannedUntil?.toMillis?.() as number | undefined;
  const banned = user?.banned === true && (until === undefined || until > Date.now());
  const num = (v: unknown) => (typeof v === 'number' ? formatNumber(v) : '—');

  return (
    <div class="account-grid">
      <section class="panel profile stack">
        <div class="row" style="justify-content: space-between">
          <div>
            <p class="label">{t.account.playerName}</p>
            <h2 style="margin: 0">{name}</h2>
          </div>
          <img class="pixel" src="/art/logo_emblem.png" alt="" width="64" height="64" />
        </div>
        {banned && (
          <div class="notice error" role="alert">
            <div>
              <strong>{until ? fill(t.account.bannedUntil, { date: formatDate(until) }) : t.account.banned}</strong>
              {user?.banReason && <p class="small">{fill(t.account.reason, { reason: String(user.banReason) })}</p>}
            </div>
          </div>
        )}
        <div>
          <p class="label">{t.account.accountId}</p>
          <div class="row"><code class="mono">{session.user.uid}</code><CopyButton text={session.user.uid} /></div>
          <p class="hint">{t.account.accountIdHelp}</p>
        </div>
        <div>
          <p class="label">{t.account.email}</p>
          <p>{session.user.email}</p>
        </div>
        <div>
          <p class="label">{t.account.progress}</p>
          {user || village ? (
            <div class="stat-grid">
              <div class="stat"><b>{num(village?.castleLevel)}</b><span>{t.account.castleLevel}</span></div>
              <div class="stat"><b>{num(user?.highestWave ?? village?.highestWave)}</b><span>{t.account.highestWave}</span></div>
              <div class="stat"><b>{num(village?.chaptersCleared)}</b><span>{t.account.chapters}</span></div>
              <div class="stat"><b>{num(user?.totalRuns)}</b><span>{t.account.runs}</span></div>
            </div>
          ) : <p class="muted">{t.account.noProfile}</p>}
        </div>
      </section>

      <section class="panel">
        <h2>{t.account.inbox}</h2>
        {data.grants.length === 0 ? <p class="muted">{t.account.inboxEmpty}</p> : (
          <ul class="list">
            {data.grants.map(({ id, data: grant }) => (
              <li key={id}>
                <div class="stack" style="gap: 0.3rem">
                  <strong>{grant.title}</strong>
                  <RewardChips rewards={grant.rewards} />
                  <span class="small muted">{formatDate(grant.createdAt, true)}</span>
                </div>
                {grantStatus(grant)}
              </li>
            ))}
          </ul>
        )}
      </section>

      <section class="panel">
        <h2>{t.account.redeemHistory}</h2>
        {data.redemptions.length === 0 ? <p class="muted">{t.account.redeemEmpty} <a href="/redeem/">{t.nav.redeem} →</a></p> : (
          <ul class="list">
            {data.redemptions.map((r) => (
              <li key={r.code}>
                <div class="stack" style="gap: 0.3rem">
                  <strong class="mono">{r.code}</strong>
                  <RewardChips rewards={r.rewards} />
                </div>
                <span class="small muted">{formatDate(r.at)}</span>
              </li>
            ))}
          </ul>
        )}
      </section>

      <section class="panel">
        <h2>{t.account.orders}</h2>
        {data.orders.length === 0 ? <p class="muted">{t.account.ordersEmpty} <a href="/shop/">{t.nav.shop} →</a></p> : (
          <ul class="list">
            {data.orders.map(({ id, data: order }) => (
              <li key={id}>
                <div>
                  <strong>{order.productName}</strong>
                  <div class="small muted">{formatDate(order.createdAt, true)} · <span class="mono">{fill(t.common.orderId, { id })}</span></div>
                </div>
                <div class="row">
                  <span>{formatMoney(order.amount, order.currency)}</span>
                  <span class={`badge ${ORDER_BADGE[order.status] ?? ''}`}>{t.status[order.status]}</span>
                </div>
              </li>
            ))}
          </ul>
        )}
      </section>

      <p class="small account-foot">
        <a href="/delete-account/">{data.deletionPending ? t.deletion.pending : t.account.deletion}</a>
      </p>
    </div>
  );
}
