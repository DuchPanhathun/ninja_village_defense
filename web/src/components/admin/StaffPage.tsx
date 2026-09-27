import { useState } from 'preact/hooks';
import { collection, getDocs, orderBy, query } from 'firebase/firestore/lite';
import { ROLE_LABELS, isRole } from '@shared/roles';
import { ROLES, type Role, type StaffDoc } from '@shared/types';
import { call } from '../../lib/api';
import { db } from '../../lib/db';
import { formatDate } from '../../lib/format';
import AdminGate, { type Staff } from './AdminGate';
import { DataState, Section, useAction, useAsync } from './ui';

function StaffList({ staff }: { staff: Staff }) {
  const action = useAction();
  const [email, setEmail] = useState('');
  const [role, setRole] = useState<Role | ''>('support');
  const state = useAsync(async () => {
    const snap = await getDocs(query(collection(db(), 'staff'), orderBy('email')));
    return snap.docs.map((d) => ({ uid: d.id, ...(d.data() as StaffDoc) }));
  }, []);

  async function save(targetEmail: string, newRole: Role | null) {
    const ok = await action.run(() => call('adminSetRole', { email: targetEmail, role: newRole }),
      newRole ? `${targetEmail} is now ${newRole}. They must sign out and back in.` : `${targetEmail} is no longer staff (signed out everywhere).`,
      newRole ? undefined : `Remove ${targetEmail} from staff?`);
    if (ok) {
      setEmail('');
      state.reload();
    }
  }

  return (
    <div class="admin-grid side">
      <Section title="Staff accounts">
        {action.feedback}
        <DataState state={state} empty={state.data?.length === 0}>
          <ul class="list">{state.data?.map((s) => (
            <li key={s.uid}>
              <div>
                <b>{s.email}</b> <span class="badge info">{s.role}</span>
                <div class="small muted">set by {s.updatedBy} · {formatDate(s.updatedAt)}</div>
              </div>
              {s.uid !== staff.uid && s.email && (
                <button class="btn btn-small" disabled={action.busy} onClick={() => save(s.email!, null)}>Remove</button>
              )}
            </li>
          ))}</ul>
        </DataState>
      </Section>
      <Section title="Add or change">
        <form class="stack" onSubmit={(e) => { e.preventDefault(); void save(email.trim(), isRole(role) ? role : null); }}>
          <label class="field">
            <span>Email</span>
            <input type="email" value={email} required onInput={(e) => setEmail(e.currentTarget.value)} />
            <span class="hint">They need an account first: sign in once on the website (or link the email in the game).</span>
          </label>
          <label class="field">
            <span>Role</span>
            <select value={role} onChange={(e) => setRole(e.currentTarget.value as Role)}>
              {ROLES.map((r) => <option value={r}>{ROLE_LABELS[r]}</option>)}
            </select>
          </label>
          <p class="hint">Admins must turn on 2-step sign-in before they can do anything. Use the smallest role that does the job.</p>
          <button class="btn btn-primary" disabled={action.busy}>Save role</button>
        </form>
      </Section>
    </div>
  );
}

export default function StaffPage() {
  return <AdminGate permission="staff.write">{(staff) => <StaffList staff={staff} />}</AdminGate>;
}
