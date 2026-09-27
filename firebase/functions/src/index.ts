/**
 * Ninja Village Defense — Cloud Functions for the web platform (task_web.text). Only this server code creates
 * grants, orders and codes; the website and the game are never trusted to.
 */
import { setGlobalOptions } from 'firebase-functions/v2';
import { FUNCTIONS_REGION } from './lib/config.js';

setGlobalOptions({ region: FUNCTIONS_REGION, maxInstances: 10 });

// Players (website and game)
export { redeemCode } from './player/redeem.js';
export { requestAccountDeletion, cancelAccountDeletion } from './player/account.js';
export { collectMail } from './player/mail.js';
export { createCheckout, sandboxPay, paymentWebhook } from './payments/checkout.js';

// Staff (admin website)
export {
  adminCounts,
  adminSearchPlayers,
  adminGetPlayer,
  adminSendGift,
  adminSetBan,
  adminRenamePlayer,
  adminModerate,
  adminClearReview,
} from './admin/players.js';
export { adminProcessDeletion } from './admin/deletions.js';
export { adminCreateCodes, adminSetCodeActive } from './admin/codes.js';
export { adminSaveProduct, adminSendMail, adminSetMailActive, adminSetRole } from './admin/catalog.js';

// Background jobs
export { onAccountCreated, checkOrders } from './triggers/jobs.js';
