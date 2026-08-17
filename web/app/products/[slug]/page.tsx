import { notFound } from "next/navigation";
import { AddToCartPanel } from "@/components/AddToCartPanel";
import ShippingEstimator from "@/components/ShippingEstimator";
import { fetchFromServer, type Product } from "@/lib/api";
import { formatVnd } from "@/lib/format";

interface Props {
  params: { slug: string };
}

export async function generateMetadata({ params }: Props) {
  const product = await fetchFromServer<Product>(`/api/v1/products/${params.slug}`);
  return {
    title: product?.name ?? "Sản phẩm",
    description: product?.shortDescription ?? undefined,
  };
}

export default async function ProductDetailPage({ params }: Props) {
  const product = await fetchFromServer<Product>(`/api/v1/products/${params.slug}`);
  if (!product) notFound();

  return (
    <div className="container-page py-8">
      <nav className="mb-4 text-sm text-neutral-500">
        <a href="/" className="hover:text-brand-600">Trang chủ</a> /{" "}
        <a href={`/products?category=${product.categoryId}`} className="hover:text-brand-600">
          {product.categoryName}
        </a>{" "}
        / <span className="text-neutral-900">{product.name}</span>
      </nav>

      <div className="grid gap-8 lg:grid-cols-2">
        {/* Ảnh — Phase 2 thay bằng gallery thật từ MinIO */}
        <div className="flex aspect-square items-center justify-center rounded-2xl bg-gradient-to-br from-neutral-100 to-neutral-200 text-9xl">
          🛋️
        </div>

        <div>
          <p className="text-sm text-neutral-500">{product.brandName}</p>
          <h1 className="mt-1 text-2xl font-bold">{product.name}</h1>

          <div className="mt-4 flex items-baseline gap-3">
            <span className="text-3xl font-bold text-brand-600">{formatVnd(product.displayPrice)}</span>
            {product.discountPercent > 0 && (
              <>
                <span className="text-lg text-neutral-400 line-through">{formatVnd(product.price)}</span>
                <span className="rounded bg-red-100 px-2 py-0.5 text-sm font-bold text-red-600">
                  -{product.discountPercent}%
                </span>
              </>
            )}
          </div>

          {product.shortDescription && (
            <p className="mt-4 text-neutral-600">{product.shortDescription}</p>
          )}

          <AddToCartPanel product={product} />

          {/* Ước tính phí vận chuyển theo thể tích (phí ship hàng cồng kềnh) */}
          {product.variants && product.variants.length > 0 ? (
            <ShippingEstimator
              defaultWidthCm={product.variants[0].widthCm}
              defaultDepthCm={product.variants[0].depthCm}
              defaultHeightCm={product.variants[0].heightCm}
            />
          ) : null}

          {/* Thông số */}
          <div className="mt-6 rounded-xl border bg-white p-5">
            <p className="mb-3 font-semibold">Thông số sản phẩm</p>
            <dl className="space-y-2 text-sm">
              {Object.entries({
                "Thương hiệu": product.brandName ?? "—",
                "Phong cách": product.attributes["phong-cach"] ?? "—",
                "Chất liệu": product.attributes["chat-lieu"] ?? "—",
                "Bảo hành": `${product.warrantyMonths} tháng`,
                "Mã SKU": product.sku,
              }).map(([key, value]) => (
                <div key={key} className="flex justify-between border-b border-neutral-100 pb-2">
                  <dt className="text-neutral-500">{key}</dt>
                  <dd className="font-medium">{value}</dd>
                </div>
              ))}
            </dl>
          </div>
        </div>
      </div>

      {product.description && (
        <section className="mt-10 rounded-xl border bg-white p-6">
          <h2 className="mb-3 text-lg font-bold">Mô tả chi tiết</h2>
          <p className="whitespace-pre-line leading-relaxed text-neutral-600">{product.description}</p>
        </section>
      )}
    </div>
  );
}
