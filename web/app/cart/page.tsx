"use client";

import Link from "next/link";
import { formatVnd } from "@/lib/format";
import { useCart } from "@/store/cart";
import { CheckoutForm } from "@/components/CheckoutForm";

export default function CartPage() {
  const items = useCart((s) => s.items);
  const setQuantity = useCart((s) => s.setQuantity);
  const removeItem = useCart((s) => s.removeItem);
  const totalAmount = useCart((s) => s.totalAmount());

  if (items.length === 0) {
    return (
      <div className="container-page py-20 text-center">
        <p className="text-5xl">🛒</p>
        <h1 className="mt-4 text-xl font-bold">Giỏ hàng trống</h1>
        <p className="mt-2 text-neutral-500">Hãy chọn cho không gian sống của bạn những món nội thất ưng ý nhé.</p>
        <Link href="/products" className="btn-primary mt-6">
          Mua sắm ngay
        </Link>
      </div>
    );
  }

  return (
    <div className="container-page grid gap-8 py-8 lg:grid-cols-[1fr_380px]">
      <section>
        <h1 className="mb-4 text-xl font-bold">Giỏ hàng ({items.length} sản phẩm)</h1>
        <div className="space-y-3">
          {items.map((item) => (
            <div key={item.variantSku} className="flex items-center gap-4 rounded-xl border bg-white p-4">
              <div className="flex h-16 w-16 items-center justify-center rounded-lg bg-neutral-100 text-2xl">🛋️</div>
              <div className="flex-1">
                <p className="font-medium">{item.productName}</p>
                <p className="text-sm text-neutral-500">Kích thước: {item.sizeName}</p>
                <p className="text-sm font-semibold text-brand-600">{formatVnd(item.unitPrice)}</p>
              </div>
              <div className="flex items-center gap-2">
                <button
                  onClick={() => setQuantity(item.variantSku, item.quantity - 1)}
                  className="h-8 w-8 rounded-lg border hover:border-brand-500"
                >
                  −
                </button>
                <span className="w-8 text-center">{item.quantity}</span>
                <button
                  onClick={() => setQuantity(item.variantSku, item.quantity + 1)}
                  className="h-8 w-8 rounded-lg border hover:border-brand-500"
                >
                  +
                </button>
              </div>
              <div className="w-28 text-right font-semibold">
                {formatVnd(item.unitPrice * item.quantity)}
              </div>
              <button
                onClick={() => removeItem(item.variantSku)}
                className="text-neutral-400 hover:text-red-500"
                aria-label="Xóa"
              >
                ✕
              </button>
            </div>
          ))}
        </div>
      </section>

      <aside>
        <CheckoutForm totalAmount={totalAmount} />
      </aside>
    </div>
  );
}
