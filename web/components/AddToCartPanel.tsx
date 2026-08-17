"use client";

import { useState } from "react";
import type { Product } from "@/lib/api";
import { formatVnd } from "@/lib/format";
import { useCart } from "@/store/cart";

export function AddToCartPanel({ product }: { product: Product }) {
  const variants = product.variants.length > 0 ? product.variants : null;
  const [selectedSku, setSelectedSku] = useState(variants?.[0]?.sku ?? "");
  const [added, setAdded] = useState(false);

  const addItem = useCart((s) => s.addItem);

  const selected = variants?.find((v) => v.sku === selectedSku);
  const price = selected?.priceOverride ?? product.displayPrice;

  function handleAdd() {
    addItem({
      productId: product.id,
      variantSku: selectedSku || product.sku,
      productName: product.name,
      sizeName: selected?.sizeName ?? "Mặc định",
      unitPrice: price,
    });
    setAdded(true);
    setTimeout(() => setAdded(false), 2000);
  }

  return (
    <div className="mt-6">
      {variants && (
        <div className="mb-4">
          <p className="mb-2 text-sm font-medium">Kích thước</p>
          <div className="flex flex-wrap gap-2">
            {variants.map((v) => (
              <button
                key={v.sku}
                onClick={() => setSelectedSku(v.sku)}
                className={`rounded-lg border px-4 py-2 text-sm transition ${
                  selectedSku === v.sku
                    ? "border-brand-600 bg-brand-50 font-medium text-brand-700"
                    : "hover:border-brand-400"
                }`}
              >
                {v.sizeName}
              </button>
            ))}
          </div>
        </div>
      )}

      <div className="flex items-center gap-3">
        <button onClick={handleAdd} className="btn-primary text-base">
          {added ? "✓ Đã thêm vào giỏ" : "Thêm vào giỏ"}
        </button>
        <span className="text-sm text-neutral-500">
          Giá kích thước đã chọn: <strong className="text-neutral-900">{formatVnd(price)}</strong>
        </span>
      </div>
    </div>
  );
}
