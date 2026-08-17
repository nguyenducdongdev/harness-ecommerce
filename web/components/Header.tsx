import Link from "next/link";
import type { Category } from "@/lib/api";
import { fetchFromServer } from "@/lib/api";
import { CartBadge } from "./CartBadge";
import { HeaderUserMenu } from "./HeaderUserMenu";

export async function Header() {
  const categories = (await fetchFromServer<Category[]>("/api/v1/categories")) ?? [];

  return (
    <header className="sticky top-0 z-50 border-b bg-white shadow-sm" style={{ height: "var(--header-height)" }}>
      <div className="container-page flex h-full items-center gap-6">
        <Link href="/" className="text-xl font-bold text-brand-600">
          Harness<span className="text-neutral-900">NộiThất</span>
        </Link>

        <nav className="hidden flex-1 items-center gap-4 lg:flex">
          {categories.slice(0, 6).map((c) => (
            <Link
              key={c.id}
              href={`/products?category=${c.slug}`}
              className="text-sm text-neutral-600 transition hover:text-brand-600"
            >
              {c.name}
            </Link>
          ))}
        </nav>

        <div className="flex items-center gap-3">
          <Link href="/flash-sale" className="hidden text-sm font-medium text-red-600 hover:text-red-700 md:block">
            ⚡ Flash sale
          </Link>
          <Link href="/booking" className="hidden text-sm text-neutral-600 hover:text-brand-600 md:block">
            Đặt lịch
          </Link>
          <Link href="/track" className="hidden text-sm text-neutral-600 hover:text-brand-600 md:block">
            Tra cứu đơn
          </Link>
          <HeaderUserMenu />
          <CartBadge />
        </div>
      </div>
    </header>
  );
}
