"use client";

interface LedgerFiltersProps {
  sku: string;
  onSkuChange: (value: string) => void;
  locationId: string;
  onLocationChange: (value: string) => void;
  startDate: string;
  onStartDateChange: (value: string) => void;
  endDate: string;
  onEndDateChange: (value: string) => void;
}

export function LedgerFilters({
  sku,
  onSkuChange,
  locationId,
  onLocationChange,
  startDate,
  onStartDateChange,
  endDate,
  onEndDateChange,
}: LedgerFiltersProps) {
  return (
    <div className="flex items-center gap-2 flex-wrap">
      <input
        type="text"
        placeholder="Search SKU..."
        value={sku}
        onChange={(e) => onSkuChange(e.target.value)}
        className="h-8 rounded-md border border-border bg-background px-3 text-xs focus:outline-none focus:ring-2 focus:ring-[#9CAB84] w-32"
      />
      <input
        type="date"
        value={startDate}
        onChange={(e) => onStartDateChange(e.target.value)}
        className="h-8 rounded-md border border-border bg-background px-2 text-xs focus:outline-none focus:ring-2 focus:ring-[#9CAB84]"
      />
      <input
        type="date"
        value={endDate}
        onChange={(e) => onEndDateChange(e.target.value)}
        className="h-8 rounded-md border border-border bg-background px-2 text-xs focus:outline-none focus:ring-2 focus:ring-[#9CAB84]"
      />
      <select
        value={locationId}
        onChange={(e) => onLocationChange(e.target.value)}
        className="h-8 rounded-md border border-border bg-background px-2 text-xs focus:outline-none focus:ring-2 focus:ring-[#9CAB84]"
      >
        <option value="">All Locations</option>
        <option value="WH-MAIN">WH-MAIN</option>
        <option value="SUPPLIER-TRANSIT">SUPPLIER-TRANSIT</option>
      </select>
    </div>
  );
}
