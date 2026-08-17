"use client";

import { useCallback, useEffect, useState } from "react";
import { formatVnd } from "@/lib/format";

interface ShippingQuote {
  volumetricWeight: number;
  chargeableWeight: number;
  zone: string;
  zoneLabel: string;
  estimatedFee: number;
  estimatedDays: string;
  freeShipOrderAmount: number;
}

interface Props {
  defaultWidthCm: number;
  defaultDepthCm: number;
  defaultHeightCm: number;
}

/** Ước tính phí vận chuyển theo thể tích (W×D×H / 6000) — gọi /api/v1/shipping/quotes/quote. */
export default function ShippingEstimator({ defaultWidthCm, defaultDepthCm, defaultHeightCm }: Props) {
  const [widthCm, setWidthCm] = useState(defaultWidthCm);
  const [depthCm, setDepthCm] = useState(defaultDepthCm);
  const [heightCm, setHeightCm] = useState(defaultHeightCm);
  const [weightKg, setWeightKg] = useState(0);
  const [zone, setZone] = useState("noi-thanh");
  const [quote, setQuote] = useState<ShippingQuote | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);

  const estimate = useCallback(async (w: number, d: number, h: number, kg: number, z: string) => {
    setLoading(true);
    setError(null);
    try {
      const qs = new URLSearchParams({
        widthCm: String(w),
        depthCm: String(d),
        heightCm: String(h),
        weightKg: String(kg || 0),
        zone: z,
      });
      const res = await fetch(`/api/v1/shipping/quotes/quote?${qs}`);
      const body = await res.json();
      if (!res.ok || !body.success) throw new Error(body.message || "Không tính được phí ship.");
      setQuote(body.data as ShippingQuote);
    } catch (err) {
      setQuote(null);
      setError(err instanceof Error ? err.message : "Không kết nối được dịch vụ tính phí ship.");
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    estimate(defaultWidthCm, defaultDepthCm, defaultHeightCm, 0, "noi-thanh");
  }, [defaultWidthCm, defaultDepthCm, defaultHeightCm, estimate]);

  return (
    <div className="mt-6 rounded-xl border bg-white p-5">
      <p className="mb-3 font-semibold">Ước tính phí vận chuyển</p>
      <div className="grid grid-cols-2 gap-3 sm:grid-cols-4">
        <label className="text-sm">
          <span className="text-neutral-500">Rộng (cm)</span>
          <input
            type="number"
            min={20}
            value={widthCm}
            onChange={(e) => setWidthCm(Number(e.target.value))}
            className="mt-1 w-full rounded border border-neutral-300 px-2 py-1.5"
          />
        </label>
        <label className="text-sm">
          <span className="text-neutral-500">Sâu (cm)</span>
          <input
            type="number"
            min={10}
            value={depthCm}
            onChange={(e) => setDepthCm(Number(e.target.value))}
            className="mt-1 w-full rounded border border-neutral-300 px-2 py-1.5"
          />
        </label>
        <label className="text-sm">
          <span className="text-neutral-500">Cao (cm)</span>
          <input
            type="number"
            min={10}
            value={heightCm}
            onChange={(e) => setHeightCm(Number(e.target.value))}
            className="mt-1 w-full rounded border border-neutral-300 px-2 py-1.5"
          />
        </label>
        <label className="text-sm">
          <span className="text-neutral-500">Cân nặng (kg)</span>
          <input
            type="number"
            min={0}
            value={weightKg}
            onChange={(e) => setWeightKg(Number(e.target.value))}
            className="mt-1 w-full rounded border border-neutral-300 px-2 py-1.5"
          />
        </label>
      </div>

      <div className="mt-3 flex flex-wrap items-center gap-3">
        <select
          value={zone}
          onChange={(e) => setZone(e.target.value)}
          className="rounded border border-neutral-300 px-2 py-1.5 text-sm"
        >
          <option value="noi-thanh">Nội thành</option>
          <option value="ngoai-thanh">Ngoại thành</option>
          <option value="tinh">Liên tỉnh</option>
        </select>
        <button
          type="button"
          onClick={() => estimate(widthCm, depthCm, heightCm, weightKg, zone)}
          disabled={loading}
          className="rounded-lg bg-brand-600 px-4 py-1.5 text-sm font-semibold text-white hover:bg-brand-700 disabled:opacity-60"
        >
          {loading ? "Đang tính..." : "Tính phí ship"}
        </button>
      </div>

      {error && <p className="mt-3 text-sm text-red-600">{error}</p>}

      {quote && (
        <div className="mt-4 rounded-lg bg-neutral-50 p-4 text-sm">
          <div className="flex items-center justify-between">
            <span className="text-neutral-500">Phí ước tính ({quote.zoneLabel}):</span>
            <span className="text-xl font-bold text-brand-600">{formatVnd(quote.estimatedFee)}</span>
          </div>
          <div className="mt-2 flex items-center justify-between text-neutral-500">
            <span>Thời gian giao:</span>
            <span className="font-medium text-neutral-700">{quote.estimatedDays}</span>
          </div>
          <p className="mt-2 rounded bg-brand-50 px-3 py-2 text-brand-700">
            {quote.estimatedFee >= quote.freeShipOrderAmount
              ? "Đơn đủ điều kiện miễn phí vận chuyển."
              : `Miễn phí ship cho đơn từ ${formatVnd(quote.freeShipOrderAmount)}.`}
          </p>
        </div>
      )}
    </div>
  );
}
