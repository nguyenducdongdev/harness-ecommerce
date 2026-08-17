import Link from "next/link";
import { ProductCard } from "@/components/ProductCard";
import { fetchFromServer, type Category, type PagedResult, type Product } from "@/lib/api";

interface Props {
  searchParams: {
    category?: string;
    q?: string;
    sort?: string;
    page?: string;
    minPrice?: string;
    maxPrice?: string;
  };
}

const SORTS = [
  { value: "newest", label: "Mới nhất" },
  { value: "price-asc", label: "Giá tăng dần" },
  { value: "price-desc", label: "Giá giảm dần" },
  { value: "popular", label: "Phổ biến" },
];

export default async function ProductsPage({ searchParams }: Props) {
  const page = Number(searchParams.page ?? "1");
  const qs = new URLSearchParams();
  if (searchParams.category) qs.set("categorySlug", searchParams.category);
  if (searchParams.q) qs.set("searchTerm", searchParams.q);
  if (searchParams.sort) qs.set("sort", searchParams.sort);
  qs.set("page", String(page));
  qs.set("pageSize", "12");

  const [result, categories] = await Promise.all([
    fetchFromServer<PagedResult<Product>>(`/api/v1/products?${qs.toString()}`),
    fetchFromServer<Category[]>("/api/v1/categories"),
  ]);

  const products = result?.items ?? [];
  const totalPages = result?.totalPages ?? 0;
  const activeCategory = searchParams.category;

  function pageHref(p: number) {
    const params = new URLSearchParams();
    if (activeCategory) params.set("category", activeCategory);
    if (searchParams.q) params.set("q", searchParams.q);
    if (searchParams.sort) params.set("sort", searchParams.sort);
    params.set("page", String(p));
    return `/products?${params.toString()}`;
  }

  return (
    <div className="container-page grid gap-6 py-8 lg:grid-cols-[240px_1fr]">
      {/* Sidebar danh mục + sắp xếp */}
      <aside className="space-y-6">
        <div className="rounded-xl border bg-white p-4">
          <p className="mb-3 font-semibold">Danh mục</p>
          <ul className="space-y-1 text-sm">
            <li>
              <Link
                href="/products"
                className={`block rounded px-2 py-1.5 ${!activeCategory ? "bg-brand-50 font-medium text-brand-600" : "hover:bg-neutral-50"}`}
              >
                Tất cả
              </Link>
            </li>
            {(categories ?? []).map((c) => (
              <li key={c.id}>
                <Link
                  href={`/products?category=${c.slug}`}
                  className={`block rounded px-2 py-1.5 ${
                    activeCategory === c.slug ? "bg-brand-50 font-medium text-brand-600" : "hover:bg-neutral-50"
                  }`}
                >
                  {c.name}
                </Link>
              </li>
            ))}
          </ul>
        </div>

        <div className="rounded-xl border bg-white p-4">
          <p className="mb-3 font-semibold">Sắp xếp</p>
          <ul className="space-y-1 text-sm">
            {SORTS.map((s) => (
              <li key={s.value}>
                <Link
                  href={`/products?${new URLSearchParams({
                    ...(activeCategory ? { category: activeCategory } : {}),
                    sort: s.value,
                  }).toString()}`}
                  className={`block rounded px-2 py-1.5 ${
                    (searchParams.sort ?? "newest") === s.value
                      ? "bg-brand-50 font-medium text-brand-600"
                      : "hover:bg-neutral-50"
                  }`}
                >
                  {s.label}
                </Link>
              </li>
            ))}
          </ul>
        </div>
      </aside>

      {/* Danh sách sản phẩm */}
      <section>
        <div className="mb-4 flex items-baseline justify-between">
          <h1 className="text-xl font-bold">
            {categories?.find((c) => c.slug === activeCategory)?.name ?? "Tất cả sản phẩm"}
          </h1>
          <p className="text-sm text-neutral-500">{result?.totalCount ?? 0} sản phẩm</p>
        </div>

        {products.length > 0 ? (
          <div className="grid grid-cols-2 gap-4 md:grid-cols-3">
            {products.map((p) => (
              <ProductCard key={p.id} product={p} />
            ))}
          </div>
        ) : (
          <p className="rounded-xl border border-dashed p-12 text-center text-neutral-500">
            Không tìm thấy sản phẩm phù hợp.
          </p>
        )}

        {/* Phân trang */}
        {totalPages > 1 && (
          <nav className="mt-8 flex justify-center gap-2">
            {Array.from({ length: totalPages }, (_, i) => i + 1).map((p) => (
              <Link
                key={p}
                href={pageHref(p)}
                className={`rounded-lg border px-3.5 py-1.5 text-sm ${
                  p === page ? "border-brand-600 bg-brand-600 text-white" : "hover:border-brand-500"
                }`}
              >
                {p}
              </Link>
            ))}
          </nav>
        )}
      </section>
    </div>
  );
}
