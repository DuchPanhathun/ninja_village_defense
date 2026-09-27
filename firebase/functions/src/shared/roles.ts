import { ROLES, type Role } from './types.js';

/**
 * Staff roles live in the Firebase Auth custom claim `role`. What each may do — enforced by the functions (every
 * change) and by the Firestore rules (reads); the admin website only uses it to hide buttons.
 */
export type Permission =
  | 'read'
  | 'players.gift'
  | 'players.ban'
  | 'players.rename'
  | 'players.moderate'
  | 'players.delete'
  | 'codes.write'
  | 'shop.write'
  | 'mail.write'
  | 'staff.write';

const SUPPORT: Permission[] = ['read', 'players.gift', 'players.ban', 'players.rename', 'players.moderate'];

export const ROLE_PERMISSIONS: Record<Role, readonly Permission[]> = {
  viewer: ['read'],
  support: SUPPORT,
  admin: [...SUPPORT, 'players.delete', 'codes.write', 'shop.write', 'mail.write', 'staff.write'],
};

export const ROLE_LABELS: Record<Role, string> = {
  admin: 'Admin — everything',
  support: 'Support — search, gifts, bans, renames',
  viewer: 'Viewer — read-only',
};

export function isRole(value: unknown): value is Role {
  return typeof value === 'string' && (ROLES as readonly string[]).includes(value);
}

export function can(role: Role | null | undefined, permission: Permission): boolean {
  return !!role && ROLE_PERMISSIONS[role].includes(permission);
}

/** Admin accounts must sign in with a second factor (Firestore rules and functions both check it). */
export function roleNeedsMfa(role: Role | null | undefined): boolean {
  return role === 'admin';
}
