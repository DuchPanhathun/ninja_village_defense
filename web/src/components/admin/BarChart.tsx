import { useState } from 'preact/hooks';

export interface Bar {
  key: string;
  /** Short axis label, e.g. "Sep 27". */
  label: string;
  value: number;
}

interface Props {
  title: string;
  bars: Bar[];
  /** Mark color (validated against the admin panel surface: #c4862a amber, #2fa89d teal). */
  color: string;
  format: (value: number) => string;
}

const W = 640;
const H = 220;
const PAD = { top: 22, right: 8, bottom: 26, left: 48 };

/** 0 → max rounded up to a clean step, with 4 gridlines. */
function niceTicks(max: number): number[] {
  if (max <= 0) return [0, 1];
  const rough = max / 4;
  const pow = 10 ** Math.floor(Math.log10(rough));
  const step = [1, 2, 2.5, 5, 10].map((m) => m * pow).find((s) => s >= rough) ?? rough;
  return Array.from({ length: Math.ceil(max / step) + 1 }, (_, i) => i * step);
}

/**
 * One series of daily values as thin columns (<= 24px, 4px rounded cap, square base) on hairline gridlines, with a
 * per-column hover/focus tooltip and a table view. Single series, so the title names it and there is no legend.
 */
export default function BarChart({ title, bars, color, format }: Props) {
  const [active, setActive] = useState<number | null>(null);
  const ticks = niceTicks(Math.max(0, ...bars.map((b) => b.value)));
  const top = ticks[ticks.length - 1];
  const plotW = W - PAD.left - PAD.right;
  const plotH = H - PAD.top - PAD.bottom;
  const slot = plotW / Math.max(1, bars.length);
  const barW = Math.min(24, slot - 2);
  const y = (v: number) => PAD.top + plotH - (v / top) * plotH;
  const maxIndex = bars.reduce((best, b, i) => (b.value > (bars[best]?.value ?? -1) ? i : best), 0);
  const labelEvery = Math.ceil(bars.length / 6);

  const column = (value: number, x: number) => {
    const h = (value / top) * plotH;
    if (h <= 0) return '';
    const r = Math.min(4, h, barW / 2);
    const base = PAD.top + plotH;
    return `M${x},${base} V${base - h + r} Q${x},${base - h} ${x + r},${base - h} H${x + barW - r} Q${x + barW},${base - h} ${x + barW},${base - h + r} V${base} Z`;
  };

  const hovered = active === null ? null : bars[active];
  return (
    <figure class="stack" style="margin: 0; gap: 0.5rem">
      <figcaption><h3 style="margin: 0">{title}</h3></figcaption>
      <div style="position: relative">
        <svg class="chart" viewBox={`0 0 ${W} ${H}`} role="img" aria-label={`${title}. Column chart; values are in the table below.`}>
          {ticks.map((t) => (
            <g key={t}>
              <line x1={PAD.left} x2={W - PAD.right} y1={y(t)} y2={y(t)} stroke="#2b3f50" stroke-width="1" />
              <text x={PAD.left - 8} y={y(t) + 4} text-anchor="end" style="font-variant-numeric: tabular-nums">{format(t)}</text>
            </g>
          ))}
          {bars.map((b, i) => {
            const x = PAD.left + i * slot + (slot - barW) / 2;
            return (
              <g key={b.key}>
                <path d={column(b.value, x)} fill={color} opacity={active === null || active === i ? 1 : 0.55} />
                {i === maxIndex && b.value > 0 && (
                  <text x={x + barW / 2} y={y(b.value) - 6} text-anchor="middle" style="fill: var(--paper)">{format(b.value)}</text>
                )}
                {i % labelEvery === 0 && <text x={x + barW / 2} y={H - 8} text-anchor="middle">{b.label}</text>}
                {/* Full-height hit target, bigger than the mark; keyboard-focusable. */}
                <rect x={PAD.left + i * slot} y={PAD.top} width={slot} height={plotH} fill="transparent" tabIndex={0}
                  aria-label={`${b.label}: ${format(b.value)}`}
                  onPointerEnter={() => setActive(i)} onPointerLeave={() => setActive(null)}
                  onFocus={() => setActive(i)} onBlur={() => setActive(null)} />
              </g>
            );
          })}
        </svg>
        {hovered && active !== null && (
          <div class="chart-tip" style={{ left: `${((PAD.left + (active + 0.5) * slot) / W) * 100}%` }} aria-hidden="true">
            <b>{format(hovered.value)}</b>
            <span>{hovered.label}</span>
          </div>
        )}
      </div>
      <details class="json">
        <summary>Table view</summary>
        <div class="table-wrap"><table>
          <thead><tr><th>Day</th><th class="num">{title}</th></tr></thead>
          <tbody>{bars.map((b) => <tr key={b.key}><td>{b.key}</td><td class="num">{format(b.value)}</td></tr>)}</tbody>
        </table></div>
      </details>
    </figure>
  );
}
