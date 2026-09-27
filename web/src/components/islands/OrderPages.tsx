import { useEffect, useState } from 'preact/hooks';
import { doc, getDoc } from 'firebase/firestore/lite';
import { formatMoney } from '@shared/money';
import type { OrderDoc } from '@shared/types';
import { call, errorMessage } from '../../lib/api';
import { db } from '../../lib/db';
import { fill, queryParam } from '../../lib/format';
import { useSession } from '../../lib/session';
import { t } from '../../i18n';
import RewardChips from '../RewardChips';
import SignInPrompt, { Loading } from '../SignInPrompt';

function OrderSummary({ order }: { order: OrderDoc }) {
  return (
    <div class="stack">
      <div class="row" style="justify-content: space-between">
        <h2 style="margin: 0">{order.productName}</h2>
        <b style="font: 700 1.4rem var(--font-ui); color: var(--lantern)">{formatMoney(order.amount, order.currency)}</b>
      </div>
      <RewardChips rewards={order.rewards} />
    </div>
  );
}

/**
 * Test mode's stand-in for the provider's hosted checkout (/shop/checkout/?order=…). "Pay" goes through the
 * sandboxPay function, which uses the same code path as a real payment webhook.
 */
export function SandboxCheckout() {
  const session = useSession();
  const orderId = queryParam('order');
  const [order, setOrder] = useState<OrderDoc | null | undefined>(undefined);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState('');

  useEffect(() => {
    if (session.status !== 'signed-in' || !orderId) return;
    getDoc(doc(db(), 'orders', orderId))
      .then((snap) => setOrder(snap.exists() ? (snap.data() as OrderDoc) : null))
      .catch(() => setOrder(null));
  }, [session.status, orderId]);

  async function finish(outcome: 'paid' | 'canceled') {
    setBusy(true);
    setError('');
    try {
      await call('sandboxPay', { orderId, outcome });
      window.location.assign(outcome === 'paid' ? `/shop/done/?order=${encodeURIComponent(orderId)}` : `/shop/?canceled=${encodeURIComponent(orderId)}`);
    } catch (err) {
      setError(errorMessage(err));
      setBusy(false);
    }
  }

  if (session.status === 'loading' || (session.status === 'signed-in' && order === undefined && orderId)) return <Loading />;
  if (session.status === 'signed-out') return <SignInPrompt message={t.common.signInFirst} />;
  if (!order) return <p class="notice error">{t.checkout.notFound}</p>;

  return (
    <div class="panel stack">
      <OrderSummary order={order} />
      <hr class="divider" />
      {order.status !== 'pending' ? (
        <p class="notice">{fill(t.checkout.alreadyDone, { status: t.status[order.status].toLowerCase() })}</p>
      ) : (
        <div class="row">
          <button class="btn btn-primary" disabled={busy} onClick={() => finish('paid')}>{t.checkout.pay}</button>
          <button class="btn btn-ghost" disabled={busy} onClick={() => finish('canceled')}>{t.checkout.cancel}</button>
        </div>
      )}
      {error && <p class="notice error" role="alert">{error}</p>}
    </div>
  );
}

const DONE_TEXT: Partial<Record<OrderDoc['status'], string>> = {
  paid: t.done.paid,
  pending: t.done.pending,
  canceled: t.done.canceled,
  failed: t.done.canceled,
  review: t.done.review,
  refunded: t.done.refunded,
  chargeback: t.done.refunded,
};

/** Where the provider sends buyers back (/shop/done/?order=…): follows the order until the webhook lands. */
export function OrderDone() {
  const session = useSession();
  const orderId = queryParam('order');
  const [order, setOrder] = useState<OrderDoc | null | undefined>(undefined);

  useEffect(() => {
    if (session.status !== 'signed-in' || !orderId) return;
    // Poll while the payment webhook hasn't landed yet (Firestore Lite has no live listeners; ~2 minutes max).
    let tries = 0;
    let timer: ReturnType<typeof setTimeout> | undefined;
    const check = async () => {
      const snap = await getDoc(doc(db(), 'orders', orderId)).catch(() => null);
      const next = snap?.exists() ? (snap.data() as OrderDoc) : null;
      setOrder(next);
      if (next?.status === 'pending' && ++tries < 60) timer = setTimeout(check, 2000);
    };
    void check();
    return () => clearTimeout(timer);
  }, [session.status, orderId]);

  if (session.status === 'loading' || (session.status === 'signed-in' && order === undefined && orderId)) return <Loading />;
  if (session.status === 'signed-out') return <SignInPrompt message={t.common.signInFirst} />;
  if (!order) return <p class="notice error">{t.checkout.notFound}</p>;

  const paid = order.status === 'paid';
  return (
    <div class="panel stack center">
      <img class="pixel" src={paid ? '/art/chest_surprise_open.png' : '/art/chest_wood_closed.png'} alt="" width="112" height="112" />
      {paid && <h2>{t.done.title}</h2>}
      <p class={`notice ${paid ? 'success' : order.status === 'pending' ? '' : 'warn'}`} role="status" style="text-align: left">
        {DONE_TEXT[order.status]}
      </p>
      <div class="panel panel-accent" style="text-align: left"><OrderSummary order={order} /></div>
      <div class="row" style="justify-content: center">
        <a class="btn" href="/shop/">{t.done.backToShop}</a>
        <a class="btn btn-ghost" href="/account/">{t.done.toAccount}</a>
      </div>
    </div>
  );
}

