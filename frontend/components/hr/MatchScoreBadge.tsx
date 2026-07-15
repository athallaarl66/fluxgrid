"use client";

export function MatchScoreBadge({ score }: { score: number }) {
  const pct = Math.round(score * 100);
  const isHigh = pct >= 85;
  const isMid = pct >= 60;

  const bg = isHigh ? "bg-[#d4e7ab]" : isMid ? "bg-[#e6f6ca]" : "bg-[#ffdad6]";
  const text = isHigh
    ? "text-[#586838]"
    : isMid
      ? "text-[#63714f]"
      : "text-[#93000a]";

  return (
    <span
      className={`inline-flex h-5 min-w-[44px] items-center justify-center rounded-[10px] px-2 py-0.5 text-[11px] font-semibold ${bg} ${text}`}
    >
      {pct}%
    </span>
  );
}
