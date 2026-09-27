import type {
  CodeKind,
  DeletionStatus,
  GrantKind,
  OrderStatus,
  Reward,
  Role,
} from './types.js';

/**
 * Every callable function with its request and response. The functions implement these signatures and the
 * website calls them through a typed wrapper, so both sides break at compile time if they drift apart.
 * Times are epoch milliseconds.
 */
export interface Api {
  // ---------------------------------------------------------------- players
  redeemCode: { req: { code: string }; res: { code: string; rewards: Reward[]; message: string } };
  requestAccountDeletion: { req: { confirm: 'DELETE' }; res: { status: DeletionStatus } };
  cancelAccountDeletion: { req: Record<string, never>; res: { status: DeletionStatus } };
  /** Turns active global mail into grants for the caller (the game calls this on sign-in and resume). */
  collectMail: { req: Record<string, never>; res: { created: number } };
  createCheckout: { req: { productId: string }; res: { orderId: string; url: string } };
  /** Test payments only (PAYMENTS_PROVIDER=sandbox): completes the caller's own pending order. */
  sandboxPay: { req: { orderId: string; outcome: 'paid' | 'canceled' }; res: { status: OrderStatus } };

  // ---------------------------------------------------------------- staff
  /** Player counts for the dashboard ("active" = cloud save uploaded in the period). */
  adminCounts: { req: Record<string, never>; res: { players: number; activeDay: number; activeWeek: number; activeMonth: number; banned: number } };
  adminSearchPlayers: { req: { query: string }; res: { players: PlayerSummary[] } };
  adminGetPlayer: { req: { uid: string }; res: PlayerDetail };
  adminSendGift: { req: { uid: string; rewards: Reward[]; title: string; message: string }; res: { grantId: string } };
  adminSetBan: { req: { uid: string; banned: boolean; reason: string; days: number | null }; res: Ok };
  adminRenamePlayer: { req: { uid: string; name: string }; res: Ok };
  adminModerate: { req: { uid: string; leaderboardHidden?: boolean; villageHidden?: boolean; reason: string }; res: Ok };
  adminClearReview: { req: { uid: string }; res: Ok };
  adminProcessDeletion: { req: { uid: string; action: 'delete' | 'reject'; note: string }; res: Ok };
  adminCreateCodes: { req: CreateCodesInput; res: { codes: string[]; batchId: string | null } };
  adminSetCodeActive: { req: { code: string; active: boolean }; res: Ok };
  adminSaveProduct: { req: ProductInput; res: { id: string } };
  adminSendMail: { req: MailInput; res: { id: string } };
  adminSetMailActive: { req: { id: string; active: boolean }; res: Ok };
  adminSetRole: { req: { email: string; role: Role | null }; res: { uid: string } };
}

export type ApiName = keyof Api;
export type Ok = { ok: true };

export interface CreateCodesInput {
  /** One custom code (e.g. "SAKURA2026") — or leave empty and set `count` to generate random ones. */
  code: string;
  count: number;
  prefix: string;
  kind: CodeKind;
  /** Only for campaign codes. */
  maxUses: number | null;
  rewards: Reward[];
  message: string;
  startsAt: number | null;
  expiresAt: number | null;
  note: string;
}

export interface ProductInput {
  /** Lower-case id, e.g. "gems_550". An existing id updates that product. */
  id: string;
  name: string;
  description: string;
  image: string;
  rewards: Reward[];
  price: number;
  currency: string;
  salePrice: number | null;
  active: boolean;
  oncePerPlayer: boolean;
  appProductId: string | null;
  badge: string;
  sortOrder: number;
}

export interface MailInput {
  kind: 'gift' | 'compensation';
  title: string;
  message: string;
  rewards: Reward[];
  startsAt: number;
  expiresAt: number;
  newPlayersToo: boolean;
}

// ---------------------------------------------------------------- views returned to the admin website

export interface PlayerSummary {
  uid: string;
  displayName: string;
  email: string | null;
  anonymous: boolean;
  highestWave: number;
  lastActive: number | null;
  banned: boolean;
}

export interface PlayerDetail {
  uid: string;
  auth: {
    email: string | null;
    emailVerified: boolean;
    anonymous: boolean;
    providers: string[];
    createdAt: number | null;
    lastSignInAt: number | null;
    disabled: boolean;
  } | null;
  profile: {
    displayName: string;
    totalRuns: number;
    highestWave: number;
    buildingLevels: number;
    coins: number;
    gems: number;
    updatedAt: number | null;
  } | null;
  village: {
    castleLevel: number;
    highestWave: number;
    chaptersCleared: number;
    achievementTiers: number;
    likes: number;
    updatedAt: number | null;
  } | null;
  leaderboard: { bestWave: number; bestKills: number } | null;
  /** Read from the cloud save: what was bought in the app. */
  appStore: {
    starterPackPurchased: boolean;
    adsRemoved: boolean;
    purchasedProductIds: string[];
    premiumPassSeasons: string[];
    transactions: number;
  } | null;
  moderation: {
    banned: boolean;
    bannedUntil: number | null;
    banReason: string;
    nameOverride: string | null;
    leaderboardHidden: boolean;
    villageHidden: boolean;
    reviewFlag: { reason: string; orderId: string | null; at: number } | null;
  };
  grants: GrantView[];
  redemptions: { code: string; rewards: Reward[]; at: number }[];
  orders: OrderView[];
  moderationLog: { at: number; by: string; action: string; reason: string; until: number | null }[];
  deletion: { status: DeletionStatus; createdAt: number; note: string } | null;
}

export interface GrantView {
  id: string;
  kind: GrantKind;
  title: string;
  rewards: Reward[];
  createdAt: number;
  claimedAt: number | null;
  revokedAt: number | null;
}

export interface OrderView {
  id: string;
  productName: string;
  amount: number;
  currency: string;
  status: OrderStatus;
  provider: string;
  createdAt: number;
  paidAt: number | null;
}
