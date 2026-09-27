import { useState } from 'preact/hooks';
import { collection, getDocs, orderBy, query } from 'firebase/firestore/lite';
import type { ProductInput } from '@shared/api';
import { formatMoney, minorToInput, parseMoney } from '@shared/money';
import type { ProductDoc } from '@shared/types';
import { call } from '../../lib/api';
import { db } from '../../lib/db';
import RewardChips from '../RewardChips';
import AdminGate, { type Staff } from './AdminGate';
import { DataState, RewardBuilder, Section, useAction, useAsync } from './ui';

/** Pictures the shop can use (files in public/art/, copied from the game by `npm run art`). */
const IMAGES = [
  'store_gems_100.png', 'store_gems_550.png', 'store_gems_1200.png', 'store_starter_pack.png', 'store_remove_ads.png',
  'store_battle_pass_premium.png', 'store_offer_sakura_bundle.png', 'store_offer_lantern_pack.png', 'store_offer_oni_kit.png',
  'store_offer_blossom_coins.png', 'store_offer_autumn_training.png', 'chest_silver_closed.png', 'chest_surprise_closed.png',
];

const APP = 'com.thun.ninjavillagedefense.';
const blank: ProductInput = {
  id: '', name: '', description: '', image: 'store_gems_100.png', rewards: [{ type: 'gems', id: 'gems', amount: 100 }],
  price: 99, currency: 'USD', salePrice: null, active: false, oncePerPlayer: false, appProductId: null, badge: '', sortOrder: 10,
};

/** Starting points that mirror the app's catalog (StoreProducts.cs), so web and app offers stay comparable. */
const TEMPLATES: Record<string, ProductInput> = {
  'Pouch of Gems (100)': { ...blank, id: 'gems_100', name: 'Pouch of Gems', description: '100 gems', image: 'store_gems_100.png', price: 99, sortOrder: 10 },
  'Chest of Gems (550)': { ...blank, id: 'gems_550', name: 'Chest of Gems', description: '550 gems (+10% bonus)', image: 'store_gems_550.png', rewards: [{ type: 'gems', id: 'gems', amount: 550 }], price: 499, sortOrder: 20 },
  'Vault of Gems (1,200)': { ...blank, id: 'gems_1200', name: 'Vault of Gems', description: '1,200 gems (+20% bonus)', image: 'store_gems_1200.png', rewards: [{ type: 'gems', id: 'gems', amount: 1200 }], price: 999, badge: 'Best value', sortOrder: 30 },
  'Starter Pack': {
    ...blank, id: 'starter_pack', name: 'Starter Pack', description: '300 gems, 5,000 coins and the Beast Ninja hero — once only',
    image: 'store_starter_pack.png', price: 299, oncePerPlayer: true, appProductId: `${APP}starter_pack`, badge: 'Once only', sortOrder: 1,
    rewards: [{ type: 'gems', id: 'gems', amount: 300 }, { type: 'coins', id: 'coins', amount: 5000 }, { type: 'hero', id: 'beast_ninja', amount: 1 }, { type: 'entitlement', id: 'starter_pack', amount: 1 }],
  },
  'Remove Ads': {
    ...blank, id: 'remove_ads', name: 'Remove Ads', description: 'No more interstitial ads. Optional reward ads stay available.',
    image: 'store_remove_ads.png', price: 399, oncePerPlayer: true, appProductId: `${APP}remove_ads`, sortOrder: 40,
    rewards: [{ type: 'entitlement', id: 'remove_ads', amount: 1 }],
  },
};

function Editor({ initial, onSaved, onCancel }: { initial: ProductInput; onSaved: () => void; onCancel: () => void }) {
  const action = useAction();
  const [p, setP] = useState<ProductInput>(initial);
  const [price, setPrice] = useState(minorToInput(initial.price, initial.currency));
  const [sale, setSale] = useState(initial.salePrice === null ? '' : minorToInput(initial.salePrice, initial.currency));
  const isNew = initial.id === '';
  const set = (patch: Partial<ProductInput>) => setP({ ...p, ...patch });

  async function save(event: Event) {
    event.preventDefault();
    const minor = parseMoney(price, p.currency);
    const saleMinor = sale.trim() === '' ? null : parseMoney(sale, p.currency);
    if (minor === null || (sale.trim() !== '' && saleMinor === null)) {
      action.clear();
      window.alert('Prices look like 4.99 (no currency sign).');
      return;
    }
    if (await action.run(() => call('adminSaveProduct', { ...p, price: minor, salePrice: saleMinor }), 'Saved.')) onSaved();
  }

  return (
    <Section title={isNew ? 'New product' : `Edit ${initial.id}`} actions={<button class="btn btn-ghost btn-small" onClick={onCancel}>Close</button>}>
      <form class="stack" onSubmit={save}>
        {isNew && (
          <label class="field">
            <span>Start from</span>
            <select onChange={(e) => {
              const t = TEMPLATES[e.currentTarget.value];
              if (!t) return;
              setP(t);
              setPrice(minorToInput(t.price, t.currency));
              setSale('');
            }}>
              <option value="">Blank</option>
              {Object.keys(TEMPLATES).map((name) => <option value={name}>{name}</option>)}
            </select>
          </label>
        )}
        <div class="form-grid two">
          <label class="field"><span>Product ID</span><input class="mono" value={p.id} disabled={!isNew} required pattern="[a-z0-9_]{2,40}" onInput={(e) => set({ id: e.currentTarget.value.toLowerCase() })} /></label>
          <label class="field"><span>Name</span><input value={p.name} required maxLength={60} onInput={(e) => set({ name: e.currentTarget.value })} /></label>
        </div>
        <label class="field"><span>Description</span><input value={p.description} maxLength={300} onInput={(e) => set({ description: e.currentTarget.value })} /></label>
        <div class="field">
          <span>Picture</span>
          <div class="row">
            <img class="pixel" src={`/art/${p.image}`} alt="" width="56" height="56" />
            <select value={p.image} onChange={(e) => set({ image: e.currentTarget.value })} style="flex: 1">
              {IMAGES.map((img) => <option value={img}>{img}</option>)}
            </select>
          </div>
        </div>
        <RewardBuilder value={p.rewards} onChange={(rewards) => set({ rewards })} />
        <div class="form-grid two">
          <label class="field"><span>Currency</span><input value={p.currency} maxLength={3} required onInput={(e) => set({ currency: e.currentTarget.value.toUpperCase() })} /></label>
          <label class="field"><span>Price</span><input inputMode="decimal" value={price} required placeholder="4.99" onInput={(e) => setPrice(e.currentTarget.value)} /></label>
          <label class="field"><span>Sale price (optional)</span><input inputMode="decimal" value={sale} placeholder="3.99" onInput={(e) => setSale(e.currentTarget.value)} /></label>
          <label class="field"><span>Ribbon (optional)</span><input value={p.badge} maxLength={20} placeholder="Best value" onInput={(e) => set({ badge: e.currentTarget.value })} /></label>
          <label class="field"><span>Sort order</span><input type="number" min={0} max={9999} value={p.sortOrder} onInput={(e) => set({ sortOrder: Number(e.currentTarget.value) })} /></label>
          <label class="field"><span>App product ID (for once-only offers)</span><input class="mono" value={p.appProductId ?? ''} placeholder={`${APP}starter_pack`} onInput={(e) => set({ appProductId: e.currentTarget.value || null })} /></label>
        </div>
        <label class="check"><input type="checkbox" checked={p.oncePerPlayer} onChange={(e) => set({ oncePerPlayer: e.currentTarget.checked })} /> Once per player (checks web orders and the app save)</label>
        <label class="check"><input type="checkbox" checked={p.active} onChange={(e) => set({ active: e.currentTarget.checked })} /> On sale (visible in the shop)</label>
        <p class="hint">Compare with the in-app price before putting a product on sale: web prices shouldn’t undercut the app by surprise.</p>
        {action.feedback}
        <button class="btn btn-primary" disabled={action.busy}>Save product</button>
      </form>
    </Section>
  );
}

type Product = ProductDoc & { id: string };

function toInput(p: Product): ProductInput {
  const { updatedAt: _updated, ...rest } = p;
  return rest;
}

function Shop({ staff }: { staff: Staff }) {
  const state = useAsync(async () => {
    const snap = await getDocs(query(collection(db(), 'products'), orderBy('sortOrder')));
    return snap.docs.map((d) => ({ id: d.id, ...(d.data() as ProductDoc) }));
  }, []);
  const [editing, setEditing] = useState<ProductInput | null>(null);
  const canEdit = staff.can('shop.write');

  return (
    <div class={editing ? 'admin-grid side' : 'stack'}>
      <Section title="Products" actions={canEdit && <button class="btn btn-primary btn-small" onClick={() => setEditing({ ...blank })}>+ New product</button>}>
        <DataState state={state} empty={state.data?.length === 0}>
          <div class="table-wrap"><table>
            <thead><tr><th /><th>Product</th><th>Rewards</th><th class="num">Price</th><th>Status</th><th /></tr></thead>
            <tbody>{state.data?.map((p) => (
              <tr key={p.id}>
                <td><img class="pixel" src={`/art/${p.image}`} alt="" width="40" height="40" /></td>
                <td><b>{p.name}</b><div class="small muted mono">{p.id}</div>{p.badge && <span class="badge warn">{p.badge}</span>}</td>
                <td><RewardChips rewards={p.rewards} showInternal /></td>
                <td class="num">
                  {p.salePrice !== null && <div><s class="muted small">{formatMoney(p.price, p.currency)}</s></div>}
                  {formatMoney(p.salePrice ?? p.price, p.currency)}
                </td>
                <td>
                  {p.active ? <span class="badge good">on sale</span> : <span class="badge">hidden</span>}
                  {p.oncePerPlayer && <div><span class="badge info">once</span></div>}
                </td>
                <td>{canEdit && <button class="btn btn-small" onClick={() => setEditing(toInput(p))}>Edit</button>}</td>
              </tr>
            ))}</tbody>
          </table></div>
        </DataState>
      </Section>
      {editing && <Editor key={editing.id || 'new'} initial={editing} onCancel={() => setEditing(null)} onSaved={() => { setEditing(null); state.reload(); }} />}
    </div>
  );
}

export default function ShopPage() {
  return <AdminGate>{(staff) => <Shop staff={staff} />}</AdminGate>;
}
