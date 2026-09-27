// Copies the game art the website uses from the Unity project into public/art/ (run: npm run art).
// The copies are committed so the site builds without the Unity project; re-run after the art changes.
import { copyFileSync, existsSync, mkdirSync } from 'node:fs';
import { dirname, join } from 'node:path';
import { fileURLToPath } from 'node:url';

const root = join(dirname(fileURLToPath(import.meta.url)), '..');
const sprites = join(root, '../game/Assets/_Project/Art/Sprites');
const out = join(root, 'public/art');
mkdirSync(out, { recursive: true });

const icons = [
  'logo_emblem', 'app_icon', 'store_gems_100', 'store_gems_550', 'store_gems_1200', 'store_starter_pack',
  'store_remove_ads', 'store_battle_pass_premium', 'store_offer_sakura_bundle', 'store_offer_lantern_pack',
  'store_offer_oni_kit', 'store_offer_blossom_coins', 'store_offer_autumn_training', 'chest_wood_closed',
  'chest_silver_closed', 'chest_surprise_closed', 'chest_surprise_open', 'icon_item_scroll', 'icon_item_money',
  'item_gold_bar', 'icon_achievement_champion', 'icon_weapon_katana', 'icon_weapon_shuriken',
];
const files = {
  'pickup_coin.png': 'Pickups/pickup_coin.png',
  'bg_mainmenu.png': 'Environment/Backgrounds/bg_mainmenu.png',
};
for (const name of icons) files[`${name}.png`] = `UI/Icons/${name}.png`;

// Heroes, skins and pets: the front-facing frame when there is one, else the first idle frame.
const characters = {
  hero_samurai: 'Heroes/hero_samurai', hero_assassin: 'Heroes/hero_assassin', hero_monk: 'Heroes/hero_monk',
  hero_mage_ninja: 'Heroes/hero_mageninja', hero_beast_ninja: 'Heroes/hero_beastninja',
  skin_samurai_gold: 'Heroes/skin_samurai_gold', skin_samurai_oni: 'Heroes/skin_samurai_oni',
  skin_assassin_sakura: 'Heroes/skin_assassin_sakura', skin_assassin_crimson: 'Heroes/skin_assassin_crimson',
  skin_monk_jade: 'Heroes/skin_monk_jade', skin_mage_frost: 'Heroes/skin_mage_frost',
  skin_beast_shadow: 'Heroes/skin_beast_shadow',
  pet_wolf: 'Pets/pet_wolf', pet_monkey: 'Pets/pet_monkey', pet_fox: 'Pets/pet_fox', pet_hawk: 'Pets/pet_hawk',
  pet_dragon: 'Pets/pet_babydragon',
};
for (const [name, base] of Object.entries(characters)) {
  const front = `Characters/${base}_front_0.png`;
  files[`${name}.png`] = existsSync(join(sprites, front)) ? front : `Characters/${base}_idle_0.png`;
}

let missing = 0;
for (const [target, source] of Object.entries(files)) {
  const from = join(sprites, source);
  if (!existsSync(from)) {
    console.error(`missing: ${source}`);
    missing++;
    continue;
  }
  copyFileSync(from, join(out, target));
}
console.log(`Copied ${Object.keys(files).length - missing} files to public/art/`);
if (missing) process.exit(1);
