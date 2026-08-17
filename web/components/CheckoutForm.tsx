"use client";

import { useState } from "react";
import { formatVnd } from "@/lib/format";
import { useCart } from "@/store/cart";
import { promotionApi } from "@/lib/api";

interface Props {
  totalAmount: number;
}

const DELIVERY_OPTIONS = [
  { value: "Standard", label: "Giao tiêu chuẩn (3-5 ngày)" },
  { value: "Express", label: "Giao nhanh (24h)" },
  { value: "PickupAtStore", label: "Nhận tại showroom" },
];

const PAYMENT_OPTIONS = [
  { value: "Cod", label: "Tiền mặt khi nhận hàng (COD)" },
  { value: "BankTransfer", label: "Chuyển khoản / QR ngân hàng" },
  { value: "VnPay", label: "VNPay (thẻ/ví)" },
  { value: "MoMo", label: "Ví MoMo" },
];

interface CreatedOrder {
  id: string;
  orderNumber: string;
  totalAmount: number;
}

interface VnPayInit {
  paymentUrl: string;
}

export function CheckoutForm({ totalAmount }: Props) {
  const items = useCart((s) => s.items);
  const clear = useCart((s) => s.clear);

  const [name, setName] = useState("");
  const [phone, setPhone] = useState("");
  const [address, setAddress] = useState("");
  const [delivery, setDelivery] = useState("Standard");
  const [payment, setPayment] = useState("Cod");
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState("");
  const [orderNumber, setOrderNumber] = useState("");
  const [voucherCode, setVoucherCode] = useState("");
  const [discountAmount, setDiscountAmount] = useState(0);
  const [voucherMessage, setVoucherMessage] = useState("");
  const [voucherError, setVoucherError] = useState("");
  const [applyingVoucher, setApplyingVoucher] = useState(false);

  async function applyVoucher(e: React.MouseEvent) {
    e.preventDefault();
    setVoucherError("");
    setVoucherMessage("");
    const code = voucherCode.trim();
    if (!code) return;
    setApplyingVoucher(true);
    try {
      const result = await promotionApi.validateVoucher(code, totalAmount);
      if (!result.isValid) {
        setDiscountAmount(0);
        setVoucherError(result.message || "Mã giảm giá không áp dụng được.");
        return;
      }
      setDiscountAmount(result.discountAmount);
      setVoucherMessage(`Giảm ${formatVnd(result.discountAmount)} — ${result.message}`);
    } catch {
      setVoucherError("Không kiểm tra được mã giảm giá.");
    } finally {
      setApplyingVoucher(false);
    }
  }

  async function redirectToVnPay(order: CreatedOrder, clientIp: string) {
    // Cất mã đơn để auto-tra cứu khi VNPay đưa khách về /track
    sessionStorage.setItem("harness-last-order", order.orderNumber);

    const res = await fetch("/api/v1/payments/vnpay/create", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({
        orderId: order.id,
        amount: order.totalAmount,
        orderInfo: `Thanh toan don ${order.orderNumber}`,
        clientIp,
      }),
    });
    const body = await res.json();
    if (!res.ok || !body.success) {
      throw new Error(body.message || "Không tạo được URL thanh toán VNPay.");
    }
    const data = body.data as VnPayInit;
    window.location.href = data.paymentUrl;
  }

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    setError("");
    if (!/^0\d{9,10}$/.test(phone)) {
      setError("Số điện thoại không hợp lệ (VD: 0912345678).");
      return;
    }

    setSubmitting(true);
    try {
      const res = await fetch("/api/v1/orders", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({
          customerName: name,
          customerPhone: phone,
          shippingAddress: address || "Nhận tại showroom",
          deliveryMethod: delivery,
          paymentMethod: payment,
          discountAmount,
          items: items.map((i) => ({
            productId: i.productId,
            variantSku: i.variantSku,
            productName: i.productName,
            unitPrice: i.unitPrice,
            quantity: i.quantity,
          })),
        }),
      });
      const body = await res.json();

      if (!res.ok || !body.success) {
        setError(body.message || "Đặt hàng thất bại, vui lòng thử lại.");
        return;
      }

      const order = body.data as CreatedOrder;
      clear();

      // Nếu khách chọn VNPay → chuyển sang cổng thanh toán (quay về /track sau khi xong)
      if (payment === "VnPay") {
        setError("");
        await redirectToVnPay(order, "127.0.0.1");
        return;
      }

      setOrderNumber(order.orderNumber);
      sessionStorage.setItem("harness-last-order", order.orderNumber);
    } catch (err) {
      setError(
        err instanceof Error && err.message ? err.message : "Không kết nối được hệ thống. Backend đã chạy chưa?",
      );
    } finally {
      setSubmitting(false);
    }
  }

  if (orderNumber) {
    return (
      <div className="rounded-xl border bg-white p-6 text-center">
        <p className="text-4xl">✅</p>
        <h2 className="mt-3 font-bold">Đặt hàng thành công!</h2>
        <p className="mt-2 text-sm text-neutral-600">
          Mã đơn của bạn: <strong className="text-brand-600">{orderNumber}</strong>
        </p>
        <p className="mt-1 text-sm text-neutral-500">Chúng tôi sẽ gọi xác nhận trong thời gian sớm nhất.</p>
      </div>
    );
  }

  return (
    <form onSubmit={handleSubmit} className="space-y-4 rounded-xl border bg-white p-6">
      <h2 className="font-bold">Thanh toán</h2>

      <input
        required
        value={name}
        onChange={(e) => setName(e.target.value)}
        placeholder="Họ và tên *"
        className="w-full rounded-lg border px-3 py-2 text-sm focus:border-brand-500 focus:outline-none"
      />
      <input
        required
        value={phone}
        onChange={(e) => setPhone(e.target.value)}
        placeholder="Số điện thoại * (09xxxxxxxx)"
        className="w-full rounded-lg border px-3 py-2 text-sm focus:border-brand-500 focus:outline-none"
      />
      <input
        value={address}
        onChange={(e) => setAddress(e.target.value)}
        placeholder="Địa chỉ giao hàng"
        className="w-full rounded-lg border px-3 py-2 text-sm focus:border-brand-500 focus:outline-none"
      />

      <select
        value={delivery}
        onChange={(e) => setDelivery(e.target.value)}
        className="w-full rounded-lg border px-3 py-2 text-sm"
      >
        {DELIVERY_OPTIONS.map((o) => (
          <option key={o.value} value={o.value}>{o.label}</option>
        ))}
      </select>

      <select
        value={payment}
        onChange={(e) => setPayment(e.target.value)}
        className="w-full rounded-lg border px-3 py-2 text-sm"
      >
        {PAYMENT_OPTIONS.map((o) => (
          <option key={o.value} value={o.value}>{o.label}</option>
        ))}
      </select>

      {payment === "VnPay" && (
        <p className="rounded-lg bg-brand-50 p-3 text-xs text-brand-700">
          Bạn sẽ được chuyển sang cổng thanh toán VNPay (sandbox) để hoàn tất thanh toán.
        </p>
      )}

      <div className="rounded-lg border border-dashed border-neutral-300 p-3">
        <label className="mb-1 block text-xs font-medium text-neutral-600">Mã giảm giá (voucher)</label>
        <div className="flex gap-2">
          <input
            value={voucherCode}
            onChange={(e) => {
              setVoucherCode(e.target.value);
              setVoucherError("");
              setVoucherMessage("");
            }}
            placeholder="Nhập mã (VD: WELCOME10)"
            className="flex-1 rounded-lg border px-3 py-2 text-sm uppercase focus:border-brand-500 focus:outline-none"
          />
          <button
            type="button"
            onClick={applyVoucher}
            disabled={applyingVoucher || !voucherCode.trim()}
            className="rounded-lg bg-neutral-900 px-4 py-2 text-sm font-semibold text-white disabled:opacity-40"
          >
            {applyingVoucher ? "..." : "Áp dụng"}
          </button>
        </div>
        {voucherMessage && <p className="mt-2 text-xs font-medium text-green-600">{voucherMessage}</p>}
        {voucherError && <p className="mt-2 text-xs font-medium text-red-600">{voucherError}</p>}
        {discountAmount > 0 && (
          <button
            type="button"
            onClick={() => {
              setDiscountAmount(0);
              setVoucherCode("");
              setVoucherMessage("");
            }}
            className="mt-1 text-xs text-brand-600 hover:underline"
          >
            Bỏ mã giảm giá
          </button>
        )}
      </div>

      <div className="space-y-1 border-t pt-3 text-sm">
        <div className="flex justify-between text-neutral-600">
          <span>Tạm tính</span>
          <span>{formatVnd(totalAmount)}</span>
        </div>
        {discountAmount > 0 && (
          <div className="flex justify-between text-green-600">
            <span>Giảm giá</span>
            <span>-{formatVnd(discountAmount)}</span>
          </div>
        )}
        <div className="flex justify-between border-t pt-2 text-lg font-bold">
          <span>Tổng cộng</span>
          <span className="text-brand-600">{formatVnd(Math.max(0, totalAmount - discountAmount))}</span>
        </div>
      </div>

      {error && <p className="rounded-lg bg-red-50 p-3 text-sm text-red-600">{error}</p>}

      <button type="submit" disabled={submitting} className="btn-primary w-full text-base">
        {submitting ? "Đang xử lý..." : payment === "VnPay" ? "Đặt hàng & thanh toán" : "Đặt hàng"}
      </button>
      <p className="text-center text-xs text-neutral-400">
        Bằng việc đặt hàng, bạn đồng ý với chính sách đổi trả và bảo hành của Harness.
      </p>
    </form>
  );
}
