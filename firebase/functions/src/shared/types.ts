/**
 * The web platform's data contract: Firestore documents and callable payloads shared by the Cloud Functions,
 * the website (imported there as `@shared/...`) and — for grants — the game (WEB 1 in task_web.text).
 *
 * Keep everything in `shared/` free of runtime dependencies: the website bundles these files too.
 * Times inside Firestore documents are Firestore Timestamps; times inside callable payloads are epoch
 * milliseconds (JSON has no Timestamp).
 */

/** What a grant, code, product or gift can give. The game maps each type to its own services. */
export const REWARD_TYPES = ['gems', 'coins', 'hero', 'pet', 'skin', 'crate', 'entitlement'] as const;
export type RewardType = (typeof REWARD_TYPES)[number];

/** One reward line, e.g. `{ type: 'gems', id: 'gems', amount: 550 }` or `{ type: 'hero', id: 'beast_ninja', amount: 1 }`. */
export interface Reward {
  type: RewardType;
  id: string;
  amount: number;
}

/** Structural stand-in for the Admin SDK's and the web SDK's Timestamp classes. */
export interface TimestampLike {
  toMillis(): number;
}

export const ROLES = ['admin', 'support', 'viewer'] as const;
export type Role = (typeof ROLES)[number];

// ------------------------------------------------------------------ grants (users/{uid}/grants/{grantId})

export type GrantKind = 'purchase' | 'redeem' | 'gift' | 'compensation';

/**
 * A reward waiting in the player's in-game inbox. Only server code creates grants; the owner may only set
 * `claimedAt` (once). The game records the grant id in its save before marking it claimed, so a grant can never be
 * applied twice, even offline. The game skips grants with `revokedAt` (refunded before they were collected).
 */
export interface GrantDoc<T = TimestampLike> {
  kind: GrantKind;
  rewards: Reward[];
  /** Shown in the inbox, e.g. "Thanks for your purchase!". */
  title: string;
  message: string;
  createdAt: T;
  claimedAt: T | null;
  revokedAt: T | null;
  /** Where it came from: order id, code, global mail id or the staff member who sent it. */
  source: { orderId?: string; code?: string; mailId?: string; by?: string };
}

// ------------------------------------------------------------------ users/{uid} (fields owned by the server)

/**
 * Fields of `users/{uid}` that only the server may write (Firestore rules reject client changes to them).
 * The rest of the document is the game's cloud save and progress summary.
 */
export const SERVER_USER_FIELDS = [
  'banned',
  'bannedUntil',
  'banReason',
  'nameOverride',
  'leaderboardHidden',
  'villageHidden',
  'reviewFlag',
] as const;

/** Progress fields the game writes next to its save (see FirebaseBackendProvider.WriteSaveAsync). */
export interface UserProgressFields {
  displayName?: string;
  totalRuns?: number;
  highestWave?: number;
  buildingLevels?: number;
  coins?: number;
  gems?: number;
}

export interface UserModerationFields<T = TimestampLike> {
  banned?: boolean;
  /** null or missing = permanent while `banned` is true. */
  bannedUntil?: T | null;
  banReason?: string;
  /** Set by staff after a rename; the game must publish this name (rules enforce it on the leaderboard and village). */
  nameOverride?: string | null;
  leaderboardHidden?: boolean;
  villageHidden?: boolean;
  /** Set when a refund or chargeback arrives after the gems were already collected. */
  reviewFlag?: { reason: string; orderId?: string; at: T } | null;
}

/** users/{uid}/redemptions/{code} — the player's redeem history. */
export interface MyRedemptionDoc<T = TimestampLike> {
  code: string;
  rewards: Reward[];
  grantId: string;
  at: T;
}

// ------------------------------------------------------------------ codes/{CODE}

/** single = one player ever; campaign = shared code with a total use limit; open = any number of players. */
export type CodeKind = 'single' | 'campaign' | 'open';

export interface CodeDoc<T = TimestampLike> {
  code: string;
  kind: CodeKind;
  rewards: Reward[];
  message: string;
  /** null = unlimited. Every player can redeem a code at most once, whatever the kind. */
  maxUses: number | null;
  uses: number;
  active: boolean;
  startsAt: T | null;
  expiresAt: T | null;
  /** Codes generated together share a batch id (for the CSV and the list filter). */
  batchId: string | null;
  note: string;
  createdAt: T;
  createdBy: string;
}

/** codes/{CODE}/redemptions/{uid} — who used a code. */
export interface CodeRedemptionDoc<T = TimestampLike> {
  uid: string;
  at: T;
}

// ------------------------------------------------------------------ products/{id}

export interface ProductDoc<T = TimestampLike> {
  name: string;
  description: string;
  /** File name under the website's /art/ folder, e.g. "store_gems_550.png". */
  image: string;
  rewards: Reward[];
  /** Price in the currency's minor unit (cents for USD). */
  price: number;
  currency: string;
  /** Replaces `price` while set. */
  salePrice: number | null;
  active: boolean;
  /** Can be bought once per player (across the web shop and — with `appProductId` — the app). */
  oncePerPlayer: boolean;
  /** The matching in-app product id (e.g. "com.thun.ninjavillagedefense.starter_pack"), checked against the save. */
  appProductId: string | null;
  /** Small ribbon on the card, e.g. "Best value". */
  badge: string;
  sortOrder: number;
  updatedAt: T;
}

// ------------------------------------------------------------------ orders/{orderId}

export type OrderStatus = 'pending' | 'paid' | 'canceled' | 'failed' | 'refunded' | 'chargeback' | 'review';

export interface OrderDoc<T = TimestampLike> {
  uid: string;
  email: string | null;
  productId: string;
  productName: string;
  rewards: Reward[];
  /** Minor units, copied from the catalog when the order was created — never from the browser. */
  amount: number;
  currency: string;
  status: OrderStatus;
  provider: string;
  providerRef: string | null;
  paymentId: string | null;
  grantId: string | null;
  createdAt: T;
  paidAt: T | null;
  refundedAt: T | null;
  /** Why an order needs a human: amount mismatch, refund after the gems were collected, ... */
  note: string;
}

// ------------------------------------------------------------------ mail/{mailId} (gift to everyone)

export interface MailDoc<T = TimestampLike> {
  kind: 'gift' | 'compensation';
  title: string;
  message: string;
  rewards: Reward[];
  active: boolean;
  startsAt: T;
  expiresAt: T;
  /** false = only accounts that existed when the mail was sent (typical for compensation). */
  newPlayersToo: boolean;
  createdAt: T;
  createdBy: string;
  /** How many players collected it (approximate, incremented on each collection). */
  collected: number;
}

// ------------------------------------------------------------------ deletionRequests/{uid}

export type DeletionStatus = 'pending' | 'done' | 'rejected' | 'canceled';

export interface DeletionRequestDoc<T = TimestampLike> {
  uid: string;
  email: string | null;
  displayName: string;
  source: 'website' | 'support';
  status: DeletionStatus;
  createdAt: T;
  processedAt: T | null;
  processedBy: string | null;
  note: string;
}

// ------------------------------------------------------------------ audit/{id}, staff/{uid}, stats/{day}

export interface AuditDoc<T = TimestampLike> {
  at: T;
  actorUid: string;
  actorEmail: string | null;
  role: Role;
  action: string;
  targetUid: string | null;
  /** Small JSON-able summary of what changed. */
  details: Record<string, unknown>;
}

export interface StaffDoc<T = TimestampLike> {
  email: string | null;
  role: Role;
  updatedAt: T;
  updatedBy: string;
}

/** stats/{yyyy-mm-dd} (UTC day), incremented by the functions. */
export interface DayStatsDoc {
  newPlayers?: number;
  orders?: number;
  refunds?: number;
  /** Minor units per currency, e.g. `{ USD: 1998 }`. */
  revenue?: Record<string, number>;
  redemptions?: number;
  gifts?: number;
}

/** users/{uid}/moderationLog/{id} — ban history and other staff actions on one player. */
export interface ModerationLogDoc<T = TimestampLike> {
  at: T;
  by: string;
  action: 'ban' | 'unban' | 'rename' | 'hide-leaderboard' | 'show-leaderboard' | 'hide-village' | 'show-village';
  reason: string;
  until: T | null;
}
