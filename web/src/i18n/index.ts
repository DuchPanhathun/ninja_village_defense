import { en, type Strings } from './en';

/** Languages the website speaks — the same as the game (English only for now). */
export const locales = { en } satisfies Record<string, Strings>;
export type Locale = keyof typeof locales;
export const defaultLocale: Locale = 'en';

/** The strings for the current language. */
export const t: Strings = locales[defaultLocale];
