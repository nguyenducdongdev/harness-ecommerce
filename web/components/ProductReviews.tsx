"use client";

import { useCallback, useEffect, useState } from "react";
import { reviewApi, type ProductRatingDto, type ReviewDto } from "@/lib/api";
import { useAuth } from "@/store/auth";

function Stars({ value }: { value: number }) {
  return (
    <span className="text-amber-500">
      {"★".repeat(Math.round(value))}
      <span className="text-neutral-300">{"★".repeat(5 - Math.round(value))}</span>
    </span>
  );
}

export function ProductReviews({ productId, productName }: { productId: number; productName: string }) {
  const { phone, customerName } = useAuth();

  const [rating, setRating] = useState<ProductRatingDto | null>(null);
  const [reviews, setReviews] = useState<ReviewDto[]>([]);
  const [loading, setLoading] = useState(true);

  const [name, setName] = useState(customerName ?? "");
  const [rPhone, setRPhone] = useState(phone ?? "");
  const [stars, setStars] = useState(5);
  const [content, setContent] = useState("");
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState("");
  const [success, setSuccess] = useState("");

  const load = useCallback(async () => {
    setLoading(true);
    try {
      setRating(await reviewApi.rating(productId));
      const data = await reviewApi.getByProduct(productId);
      setReviews(data.items);
    } catch {
      /* bỏ qua nếu chưa có */
    } finally {
      setLoading(false);
    }
  }, [productId]);

  useEffect(() => {
    void load();
  }, [load]);

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    setError("");
    setSuccess("");
    if (!/^0\d{9,10}$/.test(rPhone)) {
      setError("Số điện thoại không hợp lệ (VD: 0912345678).");
      return;
    }
    setSubmitting(true);
    try {
      await reviewApi.submit({ productId, customerName: name, customerPhone: rPhone, rating: stars, content });
      setSuccess("Cảm ơn bạn! Đánh giá đang chờ kiểm duyệt.");
      setContent("");
    } catch (err) {
      setError(err instanceof Error ? err.message : "Không gửi được đánh giá.");
    } finally {
      setSubmitting(false);
    }
  }
const totalCount = rating?.totalCount ?? 0;

  return (
    <section className="mt-10">
      <h2 className="text-lg font-bold">Đánh giá {productName}</h2>

      <div className="mt-4 grid gap-6 md:grid-cols-[260px_1fr]">
        <div className="rounded-xl border bg-white p-5 text-center">
          <p className="text-4xl font-extrabold text-brand-600">{rating ? rating.averageRating.toFixed(1) : "—"}</p>
          {rating && <Stars value={rating.averageRating} />}
          <p className="mt-1 text-sm text-neutral-500">{totalCount} đánh giá</p>
          {rating && (
            <ul className="mt-3 space-y-1 text-xs">
              {[...rating.ratings].reverse().map((b) => (
                <li key={b.star} className="flex items-center gap-2">
                  <span className="w-8 text-left">{b.star}★</span>
                  <div className="h-1.5 flex-1 overflow-hidden rounded-full bg-neutral-200">
                    <div
                      className="h-full rounded-full bg-amber-500"
                      style={{ width: `${totalCount ? (b.count / totalCount) * 100 : 0}%` }}
                    />
                  </div>
                  <span className="w-6 text-right text-neutral-400">{b.count}</span>
                </li>
              ))}
            </ul>
          )}
        </div>

        <div>
          {loading ? (
            <p className="text-neutral-500">Đang tải đánh giá...</p>
          ) : reviews.length === 0 ? (
            <p className="rounded-xl border border-dashed p-8 text-center text-neutral-500">
              Chưa có đánh giá nào — hãy là người đầu tiên nhận xét sản phẩm.
            </p>
          ) : (
            <ul className="space-y-3">
              {reviews.map((r) => (
                <li key={r.id} className="rounded-xl border bg-white p-4">
                  <div className="flex items-center gap-2">
                    <span className="font-semibold">{r.customerName}</span>
                    <Stars value={r.rating} />
                    {r.verifiedPurchase && (
                      <span className="rounded bg-green-100 px-2 py-0.5 text-xs font-medium text-green-700">
                        ✓ Đã mua
                      </span>
                    )}
                  </div>
                  <p className="mt-2 text-sm text-neutral-700">{r.content}</p>
                </li>
              ))}
            </ul>
          )}
        </div>
      </div>

      <form onSubmit={handleSubmit} className="mt-6 rounded-xl border bg-white p-5">
        <p className="font-semibold">Viết đánh giá</p>
        {error && <p className="mt-3 rounded-lg bg-red-50 p-3 text-sm text-red-600">{error}</p>}
        {success && <p className="mt-3 rounded-lg bg-green-50 p-3 text-sm text-green-700">{success}</p>}
        <div className="mt-4 grid gap-3 sm:grid-cols-3">
          <input required value={name} onChange={(e) => setName(e.target.value)} placeholder="Tên của bạn *" className="rounded-lg border px-3 py-2 text-sm focus:border-brand-500 focus:outline-none" />
          <input required value={rPhone} onChange={(e) => setRPhone(e.target.value)} placeholder="Số điện thoại * (09xxxxxxxx)" className="rounded-lg border px-3 py-2 text-sm focus:border-brand-500 focus:outline-none" />
          <select value={stars} onChange={(e) => setStars(Number(e.target.value))} className="rounded-lg border px-3 py-2 text-sm">
            {[5, 4, 3, 2, 1].map((s) => (
              <option key={s} value={s}>{s} ★</option>
            ))}
          </select>
        </div>
        <textarea required value={content} onChange={(e) => setContent(e.target.value)} rows={3} placeholder="Chia sẻ trải nghiệm của bạn về sản phẩm..." className="mt-3 w-full rounded-lg border px-3 py-2 text-sm focus:border-brand-500 focus:outline-none" />
        <button type="submit" disabled={submitting} className="btn-primary mt-3 disabled:opacity-50">
          {submitting ? "Đang gửi..." : "Gửi đánh giá"}
        </button>
      </form>
    </section>
  );
}
