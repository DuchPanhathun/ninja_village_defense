// Gives (or removes) a staff role on the real project — use it once to make the first admin; after that, admins
// manage roles on the admin website (Staff page). Needs Google credentials for the project, e.g.:
//   gcloud auth application-default login
//   node functions/scripts/set-role.mjs --project bookknhom --email you@example.com --role admin
// Roles: admin | support | viewer | none. The person must sign out and back in afterwards.
import { parseArgs } from 'node:util';
import { initializeApp } from 'firebase-admin/app';
import { getAuth } from 'firebase-admin/auth';
import { Timestamp, getFirestore } from 'firebase-admin/firestore';

const { values } = parseArgs({ options: { project: { type: 'string' }, email: { type: 'string' }, role: { type: 'string' } } });
const roles = ['admin', 'support', 'viewer', 'none'];
if (!values.project || !values.email || !roles.includes(values.role ?? '')) {
  console.error('Usage: node set-role.mjs --project <id> --email <email> --role admin|support|viewer|none');
  process.exit(1);
}

initializeApp({ projectId: values.project });
const auth = getAuth();
const db = getFirestore();

const user = await auth.getUserByEmail(values.email.toLowerCase());
const claims = { ...(user.customClaims ?? {}) };
const staffRef = db.collection('staff').doc(user.uid);
if (values.role === 'none') {
  delete claims.role;
  await auth.setCustomUserClaims(user.uid, claims);
  await auth.revokeRefreshTokens(user.uid);
  await staffRef.delete();
} else {
  claims.role = values.role;
  await auth.setCustomUserClaims(user.uid, claims);
  await staffRef.set({ email: user.email, role: values.role, updatedAt: Timestamp.now(), updatedBy: 'set-role script' });
}
await db.collection('audit').add({
  at: Timestamp.now(), actorUid: 'set-role-script', actorEmail: null, role: 'admin',
  action: values.role === 'none' ? 'staff.remove' : 'staff.set-role', targetUid: user.uid, details: { email: user.email, role: values.role },
});
console.log(`${user.email} (${user.uid}) → ${values.role}. They must sign out and back in.`);
process.exit(0);
