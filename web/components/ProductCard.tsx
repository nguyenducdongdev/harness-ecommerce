import Link from "next/link";
import type { Product } from "@/lib/api";
import { formatVnd } from "@/lib/format";

export function ProductCard({ product }: { product: Product }) {
  return (
    <Link
      href={`/products/${product.slug}`}
      className="group overflow-hidden rounded-xl border bg-white transition hover:shadow-lg"
    >
      <div className="flex aspect-[4/3] items-center justify-center bg-gradient-to-br from-neutral-100 to-neutral-200 text-6xl">
        🛋️
      </div>
      <div className="p-4">
        <p className="text-xs text-neutral-500">{product.brandName}</p>
        <h3 className="mt-1 line-clamp-2 font-medium group-hover:text-brand-600">{product.name}</h3>

        <div className="mt-2 flex items-baseline gap-2">
          <span className="text-lg font-bold text-brand-600">{formatVnd(product.displayPrice)}</span>
          {product.discountPercent > 0 && (
            <>
              <span className="text-sm text-neutral-400 line-through">{formatVnd(product.price)}</span>
              <span className="rounded bg-red-100 px-1.5 py-0.5 text-xs font-bold text-red-600">
                -{product.discountPercent}%
              </span>
            </>
          )}
        </div>

        {product.attributes["phong-cach"] && (
          <p className="mt-2 text-xs text-neutral-500">Phong cách: {product.attributes["phong-cach"]}</p>
        )}
        {product.warrantyMonths > 0 && (
          <p className="text-xs text-neutral-500">Bảo hành {product.warrantyMonths} tháng</p>
        )}
      </div>
    </Link>
  );
}
