"use client";

import { useState } from "react";
import { formatVnd } from "@/lib/format";

interface OrderResult {
  orderNumber: string;
  status: string;
  customerName: string;
  totalAmount: number;
  items: { productName: string; quantity: number; unitPrice: number }[];
}

const STATUS_LABELS: Record<string, string> = {
  PendingConfirmation: "Chờ xác nhận",
  Processing: "Đang xử lý",
  Shipping: "Đang giao",
  Delivered: "Đã giao",
  Completed: "Hoàn thành",
  Cancelled: "Đã hủy",
  Refunded: "Đã hoàn tiền",
};

export default function TrackPage() {
  const [orderNumber, setOrderNumber] = useState("");
  const [order, setOrder] = useState<OrderResult | null>(null);
  const [error, setError] = useState("");
  const [loading, setLoading] = useState(false);

  async function handleSearch(e: React.FormEvent) {
    e.preventDefault();
    setError("");
    setOrder(null);
    setLoading(true);
    try {
      const res = await fetch(`/api/v1/orders/${encodeURIComponent(orderNumber.trim())}`);
      const body = await res.json();
      if (!res.ok || !body.success) {
        setError(body.message || "Không tìm thấy đơn hàng.");
      } else {
        setOrder(body.data);
      }
    } catch {
      setError("Không kết nối được hệ thống.");
    } finally {
      setLoading(false);
    }
  }

  return (
    <div className="container-page max-w-2xl py-12">
      <h1 className="text-2xl font-bold">Tra cứu đơn hàng</h1>
      <p className="mt-2 text-neutral-500">Nhập mã đơn (VD: HD260816-ABC123) để xem trạng thái.</p>

      <form onSubmit={handleSearch} className="mt-6 flex gap-2">
        <input
          required
          value={orderNumber}
          onChange={(e) => setOrderNumber(e.target.value)}
          placeholder="Mã đơn hàng"
          className="flex-1 rounded-lg border px-4 py-2.5 focus:border-brand-500 focus:outline-none"
        />
        <button type="submit" disabled={loading} className="btn-primary">
          {loading ? "..." : "Tra cứu"}
        </button>
      </form>

      {error && <p className="mt-4 rounded-lg bg-red-50 p-4 text-red-600">{error}</p>}

      {order && (
        <div className="mt-6 rounded-xl border bg-white p-6">
          <div className="flex items-center justify-between">
            <div>
              <p className="font-bold">{order.orderNumber}</p>
              <p className="text-sm text-neutral-500">Khách: {order.customerName}</p>
            </div>
            <span className="rounded-full bg-brand-50 px-3 py-1 text-sm font-medium text-brand-600">
              {STATUS_LABELS[order.status] ?? order.status}
            </span>
          </div>

          <ul className="mt-4 space-y-2 border-t pt-4 text-sm">
            {order.items.map((item, i) => (
              <li key={i} className="flex justify-between">
                <span>{item.productName} × {item.quantity}</span>
                <span>{formatVnd(item.unitPrice * item.quantity)}</span>
              </li>
            ))}
          </ul>

          <p className="mt-4 flex justify-between border-t pt-4 font-bold">
            <span>Tổng cộng</span>
            <span className="text-brand-600">{formatVnd(order.totalAmount)}</span>
          </p>
        </div>
      )}
    </div>
  );
}
