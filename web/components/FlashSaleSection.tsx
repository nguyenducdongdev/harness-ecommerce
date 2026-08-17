"use client";

import { useEffect, useState } from "react";
import Link from "next/link";
import { formatVnd } from "@/lib/format";
import { promotionApi, type FlashSaleDto } from "@/lib/api";

/**
 * Hiển thị flash sale đang diễn ra. Gọi GET /api/v1/flash-sales/active (backend đã kèm thông tin sản phẩm).
 * Nếu không có khuyến mãi → render rỗng an toàn.
 */
export function FlashSaleSection({ compact = false }: { compact?: boolean }) {
  const [sales, setSales] = useState<FlashSaleDto[]>([]);
  const [loaded, setLoaded] = useState(false);

  useEffect(() => {
    let active = true;
    promotionApi
      .activeFlashSales()
      .then((data) => {
        if (active) setSales(data);
      })
      .catch(() => {
        /* backend chưa chạy hoặc không có flash sale — bỏ qua */
      })
      .finally(() => {
        if (active) setLoaded(true);
      });
    return () => {
      active = false;
    };
  }, []);

  // Dồn tất cả sản phẩm trong các flash sale đang chạy
  const items = sales.flatMap((s) =>
    s.items.filter((i) => !i.isSoldOut).map((i) => ({ ...i, saleName: s.name })),
  );

  if (!loaded || items.length === 0) return null;

  const shown = compact ? items.slice(0, 4) : items;

  return (
    <section>
      <div className="mb-4 flex items-center justify-between">
        <div className="flex items-center gap-2">
          <h2 className="text-xl font-bold text-brand-700">⚡ Flash Sale</h2>
          <span className="rounded-full bg-red-600 px-3 py-0.5 text-xs font-bold text-white">ĐANG DIỄN RA</span>
        </div>
        {compact && items.length > 4 && (
          <Link href="/flash-sale" className="text-sm text-brand-600 hover:underline">
            Xem tất cả →
          </Link>
        )}
      </div>

      <div className="grid grid-cols-2 gap-4 md:grid-cols-4">
        {shown.map((item) => {
          const discountPct =
            item.productPrice && item.productPrice > item.salePrice
              ? Math.round(((item.productPrice - item.salePrice) / item.productPrice) * 100)
              : 0;
          const href = item.productSlug ? `/products/${item.productSlug}` : "/products";
          return (
            <Link
              key={item.id}
              href={href}
              className="group rounded-xl border bg-white p-4 transition hover:border-brand-400 hover:shadow-sm"
            >
              <div className="relative aspect-square overflow-hidden rounded-lg bg-neutral-100">
                {item.imageUrl ? (
                  <img src={item.imageUrl} alt={item.productName} className="h-full w-full object-cover" />
                ) : (
                  <div className="flex h-full w-full items-center justify-center text-3xl">🛋️</div>
                )}
                {discountPct > 0 && (
                  <span className="absolute left-2 top-2 rounded bg-red-600 px-2 py-0.5 text-xs font-bold text-white">
                    -{discountPct}%
                  </span>
                )}
              </div>

              <p className="mt-3 line-clamp-2 text-sm font-medium text-neutral-800 group-hover:text-brand-600">
                {item.productName}
              </p>
              <div className="mt-2 flex items-baseline gap-2">
                <span className="text-base font-bold text-red-600">{formatVnd(item.salePrice)}</span>
                {item.productPrice && item.productPrice > item.salePrice && (
                  <span className="text-xs text-neutral-500 line-through">{formatVnd(item.productPrice)}</span>
                )}
              </div>
              <div className="mt-2">
                <div className="h-1.5 w-full overflow-hidden rounded-full bg-neutral-200">
                  <div
                    className="h-full rounded-full bg-brand-600"
                    style={{ width: `${Math.min(100, (item.quantitySold / item.quantityLimit) * 100)}%` }}
                  />
                </div>
                <p className="mt-1 text-[11px] text-neutral-500">
                  Đã bán {item.quantitySold}/{item.quantityLimit}
                </p>
              </div>
            </Link>
          );
        })}
      </div>
    </section>
  );
}
