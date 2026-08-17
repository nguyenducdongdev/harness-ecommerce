import Link from "next/link";
import { ProductCard } from "@/components/ProductCard";
import { fetchFromServer, type Category, type PagedResult, type Product } from "@/lib/api";

export const revalidate = 120; // ISR: regenerate mỗi 2 phút

export default async function HomePage() {
  const [featured, categories] = await Promise.all([
    fetchFromServer<Product[]>("/api/v1/products/featured?take=8"),
    fetchFromServer<Category[]>("/api/v1/categories"),
  ]);

  return (
    <div className="container-page space-y-12 py-8">
      {/* Hero */}
      <section className="rounded-2xl bg-gradient-to-r from-brand-600 to-brand-500 px-8 py-14 text-white">
        <h1 className="max-w-2xl text-3xl font-bold md:text-5xl">
          Không gian sống đẹp bắt đầu từ nội thất đúng
        </h1>
        <p className="mt-4 max-w-xl text-white/90">
          Sofa, giường, tủ, bàn ghế, nội thất thông minh chính hãng — tư vấn thiết kế
          theo diện tích và phong cách của bạn. Miễn phí lắp đặt tận nhà.
        </p>
        <div className="mt-6 flex gap-3">
          <Link href="/products" className="rounded-lg bg-white px-6 py-3 font-medium text-brand-700 hover:bg-neutral-100">
            Mua sắm ngay
          </Link>
          <Link href="/quiz" className="rounded-lg border border-white/60 px-6 py-3 font-medium hover:bg-white/10">
            Tư vấn nội thất (sắp ra mắt)
          </Link>
        </div>
      </section>

      {/* Danh mục */}
      {categories && categories.length > 0 && (
        <section>
          <h2 className="mb-4 text-xl font-bold">Danh mục</h2>
          <div className="grid grid-cols-2 gap-4 md:grid-cols-4 lg:grid-cols-8">
            {categories.map((c) => (
              <Link
                key={c.id}
                href={`/products?category=${c.slug}`}
                className="rounded-xl border bg-white p-4 text-center text-sm font-medium transition hover:border-brand-500 hover:text-brand-600"
              >
                {c.name}
              </Link>
            ))}
          </div>
        </section>
      )}

      {/* Sản phẩm nổi bật */}
      <section>
        <div className="mb-4 flex items-center justify-between">
          <h2 className="text-xl font-bold">Ưu đãi nổi bật</h2>
          <Link href="/products" className="text-sm text-brand-600 hover:underline">
            Xem tất cả →
          </Link>
        </div>
        {featured && featured.length > 0 ? (
          <div className="grid grid-cols-2 gap-4 md:grid-cols-4">
            {featured.map((p) => (
              <ProductCard key={p.id} product={p} />
            ))}
          </div>
        ) : (
          <p className="rounded-xl border border-dashed p-8 text-center text-neutral-500">
            Chưa có sản phẩm — khởi động backend (.NET API) để thấy dữ liệu mẫu.
          </p>
        )}
      </section>

      {/* Cam kết */}
      <section className="grid gap-4 md:grid-cols-4">
        {[
          ["🚚", "Giao hàng & lắp đặt", "Miễn phí nội thành"],
          ["🛠️", "Bảo hành dài hạn", "Tới 60 tháng"],
          ["🔄", "Đổi trả 30 ngày", "Không hài lòng đổi mới"],
          ["📐", "Tư vấn đo đạc", "Miễn phí tại nhà"],
        ].map(([icon, title, desc]) => (
          <div key={title} className="rounded-xl border bg-white p-5">
            <div className="text-2xl">{icon}</div>
            <p className="mt-2 font-semibold">{title}</p>
            <p className="text-sm text-neutral-500">{desc}</p>
          </div>
        ))}
      </section>
    </div>
  );
}
