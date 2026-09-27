import { useState } from 'preact/hooks';
import type { PlayerSummary } from '@shared/api';
import { call, errorMessage } from '../../lib/api';
import { formatNumber, relativeTime } from '../../lib/format';
import AdminGate from './AdminGate';
import { PlayerLink } from './ui';

function Search() {
  const [query, setQuery] = useState('');
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState('');
  const [players, setPlayers] = useState<PlayerSummary[] | null>(null);

  async function search(event: Event) {
    event.preventDefault();
    setBusy(true);
    setError('');
    try {
      setPlayers((await call('adminSearchPlayers', { query: query.trim() })).players);
    } catch (err) {
      setError(errorMessage(err));
    } finally {
      setBusy(false);
    }
  }

  return (
    <div class="stack">
      <form class="panel toolbar" onSubmit={search}>
        <label class="field">
          <span>Account ID, email, or the start of a display name</span>
          <input value={query} onInput={(e) => setQuery(e.currentTarget.value)} placeholder="e.g. Kx9f… · ninja@example.com · Shadow" required autoFocus />
        </label>
        <button class="btn btn-primary" disabled={busy}>{busy ? 'Searching…' : 'Search'}</button>
      </form>
      <p class="hint">Names are matched from the start and are case-sensitive (as the game shows them).</p>
      {error && <p class="notice error">{error}</p>}
      {players && (
        <section class="panel">
          {players.length === 0 ? <p class="muted">No players found.</p> : (
            <div class="table-wrap">
              <table>
                <thead><tr><th>Name</th><th>Email</th><th>Account ID</th><th class="num">Best wave</th><th>Last active</th><th /></tr></thead>
                <tbody>
                  {players.map((p) => (
                    <tr key={p.uid}>
                      <td><PlayerLink uid={p.uid} name={p.displayName} /></td>
                      <td>{p.email ?? <span class="muted">{p.anonymous ? 'not linked' : '—'}</span>}</td>
                      <td class="mono small">{p.uid}</td>
                      <td class="num">{formatNumber(p.highestWave)}</td>
                      <td>{relativeTime(p.lastActive)}</td>
                      <td>{p.banned && <span class="badge bad">banned</span>}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
        </section>
      )}
    </div>
  );
}

export default function PlayersPage() {
  return <AdminGate>{() => <Search />}</AdminGate>;
}
