import type { ComponentChildren } from 'preact';
import { can, roleNeedsMfa, type Permission } from '@shared/roles';
import type { Role } from '@shared/types';
import { signInUrl, signOutEverywhere, useSession } from '../../lib/session';
import { Loading } from '../SignInPrompt';

export interface Staff {
  uid: string;
  email: string;
  role: Role;
  mfa: boolean;
  can: (permission: Permission) => boolean;
}

interface Props {
  /** The 2-step setup page must open for admins who haven't set it up yet. */
  allowWithoutMfa?: boolean;
  permission?: Permission;
  children: (staff: Staff) => ComponentChildren;
}

/**
 * Wraps every admin page: signed in, with a staff role (Firebase custom claim), and — for admins — signed in with
 * 2-step verification, like the Firestore rules and functions require. This only decides what to show; the
 * server enforces the same checks on every read and change.
 */
export default function AdminGate({ allowWithoutMfa = false, permission = 'read', children }: Props) {
  const session = useSession();

  if (session.status === 'loading') return <Loading />;
  if (session.status === 'signed-out') {
    return (
      <div class="panel stack" style="max-width: 520px">
        <p>Staff only. Sign in with your staff account.</p>
        <a class="btn btn-primary" href={signInUrl()}>Sign in</a>
      </div>
    );
  }

  const { user, role, mfa } = session;
  const bar = (
    <div class="whoami">
      <span>{user.email}</span>
      {role && <span class="badge info">{role}</span>}
      {role && roleNeedsMfa(role) && <span class={`badge ${mfa ? 'good' : 'bad'}`}>{mfa ? '2-step ✓' : 'no 2-step'}</span>}
      <button class="link-button" type="button" onClick={() => signOutEverywhere('/signin/?next=/admin/')}>Sign out</button>
    </div>
  );

  if (!role) {
    return (
      <>
        {bar}
        <div class="panel stack" style="max-width: 560px">
          <h2>Not a staff account</h2>
          <p class="muted">Ask an admin to give <b>{user.email}</b> a role on the Staff page, then sign out and back in.</p>
        </div>
      </>
    );
  }

  if (roleNeedsMfa(role) && !mfa && !allowWithoutMfa) {
    return (
      <>
        {bar}
        <div class="panel stack" style="max-width: 600px">
          <h2>2-step verification needed</h2>
          <p class="muted">
            Admin accounts must sign in with a second step (an authenticator app). Set it up once, then sign out and back in.
          </p>
          <div class="row">
            <a class="btn btn-primary" href="/admin/security/">Set up 2-step sign-in</a>
            <button class="btn btn-ghost" type="button" onClick={() => signOutEverywhere('/signin/?next=/admin/')}>Sign in again</button>
          </div>
        </div>
      </>
    );
  }

  if (!can(role, permission)) {
    return (
      <>
        {bar}
        <p class="notice warn">The {role} role can’t open this page.</p>
      </>
    );
  }

  const staff: Staff = { uid: user.uid, email: user.email ?? '', role, mfa, can: (p) => can(role, p) };
  return (
    <>
      {bar}
      {children(staff)}
    </>
  );
}
