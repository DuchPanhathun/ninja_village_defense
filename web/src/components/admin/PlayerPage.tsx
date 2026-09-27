import { useState } from 'preact/hooks';
import type { PlayerDetail } from '@shared/api';
import { formatMoney } from '@shared/money';
import type { Reward } from '@shared/types';
import { call } from '../../lib/api';
import { formatDate, formatNumber, queryParam, relativeTime } from '../../lib/format';
import RewardChips from '../RewardChips';
import AdminGate, { type Staff } from './AdminGate';
import { DataState, RewardBuilder, Section, useAction, useAsync } from './ui';

function Stat({ value, label }: { value: string | number; label: string }) {
  return <div class="stat"><b>{typeof value === 'number' ? formatNumber(value) : value}</b><span>{label}</span></div>;
}

function Overview({ p }: { p: PlayerDetail }) {
  const m = p.moderation;
  return (
    <section class="panel stack">
      <div class="player-head">
        <img class="pixel" src="/art/logo_emblem.png" alt="" width="56" height="56" />
        <div>
          <h2>{m.nameOverride ?? p.profile?.displayName ?? '(no profile yet)'}</h2>
          <div class="row small">
            <span class="mono">{p.uid}</span>
            {p.auth?.anonymous && <span class="badge">no email linked</span>}
            {m.banned && <span class="badge bad">banned{m.bannedUntil ? ` until ${formatDate(m.bannedUntil)}` : ''}</span>}
            {m.nameOverride && <span class="badge warn">name locked</span>}
            {m.leaderboardHidden && <span class="badge warn">leaderboard hidden</span>}
            {m.villageHidden && <span class="badge warn">village hidden</span>}
            {m.reviewFlag && <span class="badge bad">review: {m.reviewFlag.reason}</span>}
            {p.deletion?.status === 'pending' && <span class="badge bad">deletion requested</span>}
          </div>
        </div>
      </div>
      {m.banned && m.banReason && <p class="notice error">Ban reason: {m.banReason}</p>}
      <dl class="kv">
        <dt>Email</dt><dd>{p.auth?.email ?? '—'} {p.auth?.email && (p.auth.emailVerified ? <span class="badge good">verified</span> : <span class="badge">unverified</span>)}</dd>
        <dt>Sign-in</dt><dd>{p.auth ? (p.auth.providers.map((id) => (id === 'password' ? 'email & password' : id)).join(', ') || 'anonymous (game only)') : 'no Auth account'}{p.auth?.disabled && ' (disabled)'}</dd>
        <dt>Created</dt><dd>{formatDate(p.auth?.createdAt ?? null, true)}</dd>
        <dt>Last seen</dt><dd>{relativeTime(p.auth?.lastSignInAt ?? null)} · save uploaded {relativeTime(p.profile?.updatedAt ?? null)}</dd>
      </dl>
      <div class="stat-grid">
        <Stat value={p.profile?.gems ?? '—'} label="Gems (last save)" />
        <Stat value={p.profile?.coins ?? '—'} label="Coins (last save)" />
        <Stat value={p.profile?.highestWave ?? '—'} label="Highest wave" />
        <Stat value={p.profile?.totalRuns ?? '—'} label="Runs" />
        <Stat value={p.village?.castleLevel ?? '—'} label="Castle level" />
        <Stat value={p.village?.chaptersCleared ?? '—'} label="Chapters" />
        <Stat value={p.leaderboard?.bestWave ?? '—'} label="Leaderboard wave" />
        <Stat value={p.village?.likes ?? '—'} label="Village likes (week)" />
      </div>
      <div>
        <p class="label">Bought in the app (from the cloud save)</p>
        {p.appStore ? (
          <p class="small">
            {p.appStore.purchasedProductIds.length ? p.appStore.purchasedProductIds.map((id) => id.split('.').pop()).join(', ') : 'no one-time products'}
            {' · '}{p.appStore.transactions} store transactions
            {p.appStore.adsRemoved && ' · ads removed'}
            {p.appStore.premiumPassSeasons.length > 0 && ` · premium pass: ${p.appStore.premiumPassSeasons.join(', ')}`}
          </p>
        ) : <p class="small muted">No cloud save.</p>}
      </div>
    </section>
  );
}

function Actions({ p, staff, reload }: { p: PlayerDetail; staff: Staff; reload: () => void }) {
  const gift = useAction();
  const ban = useAction();
  const name = useAction();
  const hide = useAction();
  const del = useAction();
  const [rewards, setRewards] = useState<Reward[]>([{ type: 'gems', id: 'gems', amount: 100 }]);
  const [title, setTitle] = useState('A gift from the team');
  const [message, setMessage] = useState('');
  const [reason, setReason] = useState('');
  const [days, setDays] = useState('7');
  const [newName, setNewName] = useState('');
  const m = p.moderation;
  const uid = p.uid;
  const after = (ok: boolean) => ok && reload();

  async function moderate(field: 'leaderboardHidden' | 'villageHidden', hidden: boolean) {
    const reason = hidden ? window.prompt('Reason (for the moderation log)?') : '';
    if (reason === null) return;
    const done = field === 'leaderboardHidden'
      ? (hidden ? 'Removed from the leaderboard.' : 'Leaderboard entry allowed again.')
      : (hidden ? 'Public village hidden.' : 'Village can be published again.');
    after(await hide.run(() => call('adminModerate', { uid, [field]: hidden, reason }), done));
  }

  return (
    <div class="stack">
      {staff.can('players.gift') && (
        <Section title="Send a gift">
          <form class="stack" onSubmit={async (e) => {
            e.preventDefault();
            after(await gift.run(() => call('adminSendGift', { uid, rewards, title, message }), 'Gift sent — it’s in their in-game inbox.'));
          }}>
            <RewardBuilder value={rewards} onChange={setRewards} />
            <label class="field"><span>Inbox title</span><input value={title} maxLength={60} required onInput={(e) => setTitle(e.currentTarget.value)} /></label>
            <label class="field"><span>Message (optional)</span><textarea value={message} maxLength={300} onInput={(e) => setMessage(e.currentTarget.value)} /></label>
            {gift.feedback}
            <button class="btn btn-primary" disabled={gift.busy}>Send gift</button>
          </form>
        </Section>
      )}

      {staff.can('players.ban') && (
        <Section title={m.banned ? 'Suspended' : 'Ban'}>
          {m.banned ? (
            <div class="stack">
              <p class="muted small">Unbanning lets them publish their village and scores again the next time the game syncs.</p>
              {ban.feedback}
              <button class="btn" disabled={ban.busy} onClick={async () => after(await ban.run(() => call('adminSetBan', { uid, banned: false, reason: '', days: null }), 'Unbanned.', 'Lift the ban?'))}>Unban</button>
            </div>
          ) : (
            <form class="stack" onSubmit={async (e) => {
              e.preventDefault();
              const d = days === 'forever' ? null : Number(days);
              after(await ban.run(() => call('adminSetBan', { uid, banned: true, reason, days: d }), 'Banned. Their leaderboard entry and public village were removed.', `Ban this player ${d ? `for ${d} days` : 'permanently'}?`));
            }}>
              <label class="field"><span>Reason (shown to the player)</span><input value={reason} minLength={3} maxLength={200} required onInput={(e) => setReason(e.currentTarget.value)} /></label>
              <label class="field"><span>Length</span>
                <select value={days} onChange={(e) => setDays(e.currentTarget.value)}>
                  {['1', '3', '7', '30', '90'].map((d) => <option value={d}>{d} days</option>)}
                  <option value="forever">Permanent</option>
                </select>
              </label>
              {ban.feedback}
              <button class="btn btn-danger" disabled={ban.busy}>Ban player</button>
            </form>
          )}
        </Section>
      )}

      {staff.can('players.rename') && (
        <Section title="Display name">
          <form class="stack" onSubmit={async (e) => {
            e.preventDefault();
            after(await name.run(() => call('adminRenamePlayer', { uid, name: newName }), 'Renamed everywhere and locked.'));
          }}>
            <label class="field">
              <span>New name (3–16 characters)</span>
              <input value={newName} minLength={3} maxLength={16} required onInput={(e) => setNewName(e.currentTarget.value)} placeholder="e.g. Ninja4821" />
              <span class="hint">Updates the profile, leaderboard and village, and locks the name so the game can’t publish another one.</span>
            </label>
            {name.feedback}
            <div class="row">
              <button class="btn" disabled={name.busy}>Rename</button>
              {m.nameOverride && <button type="button" class="btn btn-ghost" disabled={name.busy}
                onClick={async () => after(await name.run(() => call('adminRenamePlayer', { uid, name: '' }), 'Name unlocked.'))}>Unlock name</button>}
            </div>
          </form>
        </Section>
      )}

      {staff.can('players.moderate') && (
        <Section title="Public content">
          <div class="stack">
            <div class="row">
              <button class="btn btn-small" disabled={hide.busy} onClick={() => moderate('leaderboardHidden', !m.leaderboardHidden)}>
                {m.leaderboardHidden ? 'Allow on leaderboard' : 'Remove from leaderboard'}
              </button>
              <button class="btn btn-small" disabled={hide.busy} onClick={() => moderate('villageHidden', !m.villageHidden)}>
                {m.villageHidden ? 'Allow public village' : 'Hide public village'}
              </button>
              {m.reviewFlag && <button class="btn btn-small" disabled={hide.busy} onClick={async () => after(await hide.run(() => call('adminClearReview', { uid }), 'Review flag cleared.'))}>Clear review flag</button>}
            </div>
            {hide.feedback}
          </div>
        </Section>
      )}

      {staff.can('players.delete') && (
        <Section title="Delete account">
          <p class="muted small">Deletes the sign-in account, cloud save, grants, village and leaderboard entry. Orders are kept (without the email). This can’t be undone.</p>
          {del.feedback}
          <button class="btn btn-danger" disabled={del.busy} onClick={async () => {
            const typed = window.prompt(`Type the account ID to delete it:\n${uid}`);
            if (typed !== uid) return;
            if (await del.run(() => call('adminProcessDeletion', { uid, action: 'delete', note: 'Deleted from the player page' }), 'Account deleted.')) {
              window.location.assign('/admin/deletions/');
            }
          }}>Delete account…</button>
        </Section>
      )}
    </div>
  );
}

function History({ p }: { p: PlayerDetail }) {
  return (
    <div class="stack">
      <Section title="Inbox grants">
        {p.grants.length === 0 ? <p class="muted">None.</p> : (
          <div class="table-wrap"><table>
            <thead><tr><th>Sent</th><th>Kind</th><th>Title</th><th>Rewards</th><th>Status</th></tr></thead>
            <tbody>{p.grants.map((g) => (
              <tr key={g.id}>
                <td>{formatDate(g.createdAt, true)}</td>
                <td>{g.kind}</td>
                <td>{g.title}</td>
                <td><RewardChips rewards={g.rewards} showInternal /></td>
                <td>{g.revokedAt ? <span class="badge bad">revoked</span> : g.claimedAt ? <span class="badge good">claimed {formatDate(g.claimedAt)}</span> : <span class="badge warn">waiting</span>}</td>
              </tr>
            ))}</tbody>
          </table></div>
        )}
      </Section>
      <Section title="Web orders">
        {p.orders.length === 0 ? <p class="muted">None.</p> : (
          <div class="table-wrap"><table>
            <thead><tr><th>Created</th><th>Product</th><th class="num">Amount</th><th>Status</th><th>Order</th></tr></thead>
            <tbody>{p.orders.map((o) => (
              <tr key={o.id}>
                <td>{formatDate(o.createdAt, true)}</td><td>{o.productName}</td>
                <td class="num">{formatMoney(o.amount, o.currency)}</td><td><span class="badge">{o.status}</span></td>
                <td class="mono small">{o.id}</td>
              </tr>
            ))}</tbody>
          </table></div>
        )}
      </Section>
      <Section title="Redeemed codes">
        {p.redemptions.length === 0 ? <p class="muted">None.</p> : (
          <ul class="list">{p.redemptions.map((r) => (
            <li key={r.code}><span class="mono">{r.code}</span><RewardChips rewards={r.rewards} showInternal /><span class="small muted">{formatDate(r.at, true)}</span></li>
          ))}</ul>
        )}
      </Section>
      <Section title="Moderation history">
        {p.moderationLog.length === 0 ? <p class="muted">None.</p> : (
          <ul class="list">{p.moderationLog.map((l, i) => (
            <li key={i}>
              <span><b>{l.action}</b>{l.reason && ` — ${l.reason}`}{l.until && ` (until ${formatDate(l.until)})`}</span>
              <span class="small muted">{l.by} · {formatDate(l.at, true)}</span>
            </li>
          ))}</ul>
        )}
      </Section>
    </div>
  );
}

function Player({ staff }: { staff: Staff }) {
  const uid = queryParam('uid');
  const state = useAsync(() => call('adminGetPlayer', { uid }), [uid]);
  if (!uid) return <p class="notice">Open a player from <a href="/admin/players/">Players</a>.</p>;
  return (
    <DataState state={state}>
      {state.data && (
        <div class="admin-grid side">
          <div class="stack">
            <Overview p={state.data} />
            <History p={state.data} />
          </div>
          <Actions p={state.data} staff={staff} reload={state.reload} />
        </div>
      )}
    </DataState>
  );
}

export default function PlayerPage() {
  return <AdminGate>{(staff) => <Player staff={staff} />}</AdminGate>;
}
