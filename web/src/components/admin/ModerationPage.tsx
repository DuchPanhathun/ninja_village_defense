import { useState } from 'preact/hooks';
import { collection, getDocs, limit, orderBy, query } from 'firebase/firestore/lite';
import { call } from '../../lib/api';
import { db } from '../../lib/db';
import { formatNumber, relativeTime } from '../../lib/format';
import AdminGate, { type Staff } from './AdminGate';
import { DataState, PlayerLink, Section, useAction, useAsync } from './ui';

interface BoardRow { uid: string; displayName: string; bestWave: number; bestKills: number; updatedAt: number | null }
interface VillageRow { uid: string; displayName: string; castleLevel: number; highestWave: number; likes: number; updatedAt: number | null }

function Leaderboard({ staff }: { staff: Staff }) {
  const action = useAction();
  const state = useAsync(async () => {
    const snap = await getDocs(query(collection(db(), 'leaderboard'), orderBy('bestWave', 'desc'), orderBy('bestKills', 'desc'), limit(100)));
    return snap.docs.map((d): BoardRow => ({ uid: d.id, displayName: d.data().displayName, bestWave: d.data().bestWave, bestKills: d.data().bestKills, updatedAt: d.data().updatedAt?.toMillis() ?? null }));
  }, []);

  async function remove(row: BoardRow) {
    const reason = window.prompt(`Remove ${row.displayName} from the leaderboard? They can't publish scores again until allowed.\nReason:`);
    if (reason === null) return;
    if (await action.run(() => call('adminModerate', { uid: row.uid, leaderboardHidden: true, reason }), `${row.displayName} removed.`)) state.reload();
  }

  return (
    <Section title="Leaderboard — top 100">
      <p class="hint">Scores are reported by the game. Implausible waves or kills next to a short play history usually mean an edited save.</p>
      {action.feedback}
      <DataState state={state} empty={state.data?.length === 0}>
        <div class="table-wrap"><table>
          <thead><tr><th class="num">#</th><th>Name</th><th class="num">Wave</th><th class="num">Kills</th><th>Updated</th><th /></tr></thead>
          <tbody>{state.data?.map((r, i) => (
            <tr key={r.uid}>
              <td class="num">{i + 1}</td>
              <td><PlayerLink uid={r.uid} name={r.displayName} /></td>
              <td class="num">{formatNumber(r.bestWave)}</td>
              <td class="num">{formatNumber(r.bestKills)}</td>
              <td>{relativeTime(r.updatedAt)}</td>
              <td>{staff.can('players.moderate') && <button class="btn btn-small" disabled={action.busy} onClick={() => remove(r)}>Remove</button>}</td>
            </tr>
          ))}</tbody>
        </table></div>
      </DataState>
    </Section>
  );
}

function Villages({ staff }: { staff: Staff }) {
  const action = useAction();
  const state = useAsync(async () => {
    const snap = await getDocs(query(collection(db(), 'villages'), orderBy('updatedAt', 'desc'), limit(60)));
    return snap.docs.map((d): VillageRow => ({ uid: d.id, displayName: d.data().displayName, castleLevel: d.data().castleLevel, highestWave: d.data().highestWave, likes: d.data().likes ?? 0, updatedAt: d.data().updatedAt?.toMillis() ?? null }));
  }, []);

  async function hide(row: VillageRow) {
    const reason = window.prompt(`Hide ${row.displayName}'s public village? It stays hidden until allowed again.\nReason:`);
    if (reason === null) return;
    if (await action.run(() => call('adminModerate', { uid: row.uid, villageHidden: true, reason }), `${row.displayName}'s village hidden.`)) state.reload();
  }

  return (
    <Section title="Public villages — recently updated">
      <p class="hint">Look for offensive names. Open a player to see their full profile before acting.</p>
      {action.feedback}
      <DataState state={state} empty={state.data?.length === 0}>
        <div class="table-wrap"><table>
          <thead><tr><th>Name</th><th class="num">Castle</th><th class="num">Wave</th><th class="num">Likes</th><th>Updated</th><th /></tr></thead>
          <tbody>{state.data?.map((r) => (
            <tr key={r.uid}>
              <td><PlayerLink uid={r.uid} name={r.displayName} /></td>
              <td class="num">{r.castleLevel}</td>
              <td class="num">{formatNumber(r.highestWave)}</td>
              <td class="num">{formatNumber(r.likes)}</td>
              <td>{relativeTime(r.updatedAt)}</td>
              <td>{staff.can('players.moderate') && <button class="btn btn-small" disabled={action.busy} onClick={() => hide(r)}>Hide</button>}</td>
            </tr>
          ))}</tbody>
        </table></div>
      </DataState>
    </Section>
  );
}

function Moderation({ staff }: { staff: Staff }) {
  const [tab, setTab] = useState<'board' | 'villages'>('board');
  return (
    <div class="stack">
      <div class="tabs" role="tablist">
        <button role="tab" aria-selected={tab === 'board'} onClick={() => setTab('board')}>Leaderboard</button>
        <button role="tab" aria-selected={tab === 'villages'} onClick={() => setTab('villages')}>Villages</button>
      </div>
      {tab === 'board' ? <Leaderboard staff={staff} /> : <Villages staff={staff} />}
    </div>
  );
}

export default function ModerationPage() {
  return <AdminGate>{(staff) => <Moderation staff={staff} />}</AdminGate>;
}
