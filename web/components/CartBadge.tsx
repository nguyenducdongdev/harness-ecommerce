"use client";

import Link from "next/link";
import { useCart } from "@/store/cart";

export function CartBadge() {
  const count = useCart((s) => s.totalItems());

  return (
    <Link href="/cart" className="relative rounded-lg border px-3 py-1.5 text-sm hover:border-brand-500">
      🛒 Giỏ hàng
      {count > 0 && (
        <span className="absolute -right-2 -top-2 flex h-5 w-5 items-center justify-center rounded-full bg-brand-600 text-xs font-bold text-white">
          {count}
        </span>
      )}
    </Link>
  );
}
