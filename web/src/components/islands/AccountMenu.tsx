import { signInUrl, signOutEverywhere, useSession } from '../../lib/session';
import { t } from '../../i18n';

/** Header corner: "Sign in", or the signed-in player's menu (Account, Admin for staff, Sign out). */
export default function AccountMenu() {
  const session = useSession();

  if (session.status === 'loading') return <span class="menu-placeholder" aria-hidden="true" />;
  if (session.status === 'signed-out') {
    return <a class="btn btn-primary btn-small" href={signInUrl('/account/')}>{t.nav.signIn}</a>;
  }

  const email = session.user.email ?? '';
  return (
    <details class="account-menu">
      <summary class="btn btn-small" aria-label={t.nav.account}>
        <span class="avatar" aria-hidden="true">{(email[0] ?? 'N').toUpperCase()}</span>
        <span class="who">{email}</span>
      </summary>
      <div class="menu panel panel-tight">
        <a href="/account/">{t.nav.account}</a>
        {session.role && <a href="/admin/">{t.nav.admin}</a>}
        <button type="button" class="link-button" onClick={() => signOutEverywhere()}>{t.nav.signOut}</button>
      </div>
    </details>
  );
}
