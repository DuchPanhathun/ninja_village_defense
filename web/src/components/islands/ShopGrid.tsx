import { useEffect, useState } from 'preact/hooks';
import { collection, getDocs, limit, orderBy, query, where } from 'firebase/firestore/lite';
import { formatMoney } from '@shared/money';
import type { OrderDoc, ProductDoc } from '@shared/types';
import { call, errorMessage } from '../../lib/api';
import { db } from '../../lib/db';
import { queryParam } from '../../lib/format';
import { signInUrl, useSession } from '../../lib/session';
import { site } from '../../config';
import { t } from '../../i18n';
import RewardChips from '../RewardChips';

type Product = ProductDoc & { id: string };

/** Web-shop products (catalog is server-only; the price charged is the catalog's, set by createCheckout). */
export default function ShopGrid() {
  const session = useSession();
  const [products, setProducts] = useState<Product[] | null>(null);
  const [owned, setOwned] = useState<Set<string>>(new Set());
  const [buying, setBuying] = useState('');
  const [error, setError] = useState('');
  const uid = session.status === 'signed-in' ? session.user.uid : null;

  useEffect(() => {
    const database = db();
    getDocs(query(collection(database, 'products'), where('active', '==', true), orderBy('sortOrder')))
      .then((snap) => setProducts(snap.docs.map((d) => ({ id: d.id, ...(d.data() as ProductDoc) }))))
      .catch(() => setProducts([]));
  }, []);

  useEffect(() => {
    if (!uid) return setOwned(new Set());
    getDocs(query(collection(db(), 'orders'), where('uid', '==', uid), orderBy('createdAt', 'desc'), limit(100)))
      .then((snap) => setOwned(new Set(snap.docs.map((d) => d.data() as OrderDoc).filter((o) => o.status === 'paid').map((o) => o.productId))))
      .catch(() => undefined);
  }, [uid]);

  async function buy(product: Product) {
    setBuying(product.id);
    setError('');
    try {
      const { url } = await call('createCheckout', { productId: product.id });
      window.location.assign(url);
    } catch (err) {
      setError(errorMessage(err));
      setBuying('');
    }
  }

  return (
    <div class="stack-lg">
      {site.paymentsTestMode && <p class="notice warn"><img src="/art/icon_item_money.png" alt="" /><span>{t.shop.testMode}</span></p>}
      {queryParam('canceled') && <p class="notice" role="status">{t.shop.canceled}</p>}
      {error && <p class="notice error" role="alert">{error}</p>}

      {products === null ? (
        <div class="shop-grid" aria-busy="true">
          {[0, 1, 2].map((i) => <div class="panel product" key={i}><div class="skeleton" style="height: 120px" /><div class="skeleton" /><div class="skeleton" style="width: 60%" /></div>)}
        </div>
      ) : products.length === 0 ? (
        <div class="panel center stack">
          <img class="pixel" src="/art/chest_wood_closed.png" alt="" width="96" height="96" />
          <p>{t.shop.empty}</p>
        </div>
      ) : (
        <div class="shop-grid">
          {products.map((product) => {
            const isOwned = product.oncePerPlayer && owned.has(product.id);
            const price = product.salePrice ?? product.price;
            return (
              <article class="panel product" key={product.id}>
                {product.badge && <span class="ribbon">{product.badge}</span>}
                <div class="product-art"><img class="pixel" src={`/art/${product.image}`} alt="" width="112" height="112" loading="lazy" /></div>
                <h2>{product.name}</h2>
                {product.description && <p class="muted small">{product.description}</p>}
                <div class="includes">
                  <span class="visually-hidden">{t.shop.includes}</span>
                  <RewardChips rewards={product.rewards} />
                </div>
                {product.oncePerPlayer && <p class="hint">{t.shop.oncePerPlayer}</p>}
                <div class="buy-row">
                  <p class="price">
                    {product.salePrice !== null && <s class="muted">{formatMoney(product.price, product.currency)}</s>}
                    <b>{formatMoney(price, product.currency)}</b>
                  </p>
                  {session.status !== 'signed-in' ? (
                    <a class="btn btn-primary" href={signInUrl('/shop/')}>{t.shop.signInToBuy}</a>
                  ) : isOwned ? (
                    <button class="btn" disabled>{t.shop.owned}</button>
                  ) : (
                    <button class="btn btn-primary" disabled={!!buying} onClick={() => buy(product)}>
                      {buying === product.id ? t.shop.redirecting : t.shop.buy}
                    </button>
                  )}
                </div>
              </article>
            );
          })}
        </div>
      )}
    </div>
  );
}
