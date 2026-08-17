"use client";

import { useState } from "react";
import Link from "next/link";
import { useAuth } from "@/store/auth";

const TIME_SLOTS = [
  { value: "buoi-sang", label: "Buổi sáng (8h - 12h)" },
  { value: "buoi-chieu", label: "Buổi chiều (13h - 17h)" },
  { value: "toi-uu", label: "Theo lịch tối ưu của kỹ thuật viên" },
];

export default function BookingPage() {
  const phone = useAuth((s) => s.phone);

  const [customerPhone, setCustomerPhone] = useState(phone ?? "");
  const [customerName, setCustomerName] = useState("");
  const [receiverName, setReceiverName] = useState("");
  const [receiverPhone, setReceiverPhone] = useState("");
  const [address, setAddress] = useState("");
  const [appointmentType, setAppointmentType] = useState("Installation");
  const [desiredDate, setDesiredDate] = useState("");
  const [timeSlot, setTimeSlot] = useState("buoi-sang");
  const [note, setNote] = useState("");
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState("");
  const [bookingId, setBookingId] = useState("");
  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    setError("");
    if (!/^0\d{9,10}$/.test(customerPhone) || !/^0\d{9,10}$/.test(receiverPhone)) {
      setError("Số điện thoại không hợp lệ (VD: 0912345678).");
      return;
    }
    setSubmitting(true);
    try {
      const res = await fetch("/api/v1/bookings", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({
          customerPhone,
          customerName,
          receiverName,
          receiverPhone,
          address,
          appointmentType,
          desiredDate,
          timeSlot,
          note: note || undefined,
        }),
      });
      const body = await res.json();
      if (!res.ok || !body.success) {
        setError(body.message || "Đặt lịch thất bại, vui lòng thử lại.");
        return;
      }
      setBookingId(body.data.id);
    } catch {
      setError("Không kết nối được hệ thống. Backend đã chạy chưa?");
    } finally {
      setSubmitting(false);
    }
  }

  if (bookingId) {
    return (
      <div className="container-page max-w-2xl py-16 text-center">
        <p className="text-5xl">📅</p>
        <h1 className="mt-4 text-2xl font-bold">Đặt lịch thành công!</h1>
        <p className="mt-2 text-neutral-600">
          Mã lịch hẹn của bạn: <strong className="text-brand-600">{bookingId}</strong>
        </p>
        <p className="mt-1 text-sm text-neutral-500">
          Chúng tôi sẽ gọi xác nhận trước khi kỹ thuật viên đến đúng ngày bạn chọn.
        </p>
        <Link href="/" className="btn-primary mt-6">Về trang chủ</Link>
      </div>
    );
  }

  const inputCls = "w-full rounded-lg border px-3 py-2.5 text-sm focus:border-brand-500 focus:outline-none";
  return (
    <div className="container-page flex justify-center py-12">
      <div className="w-full max-w-xl">
        <h1 className="text-2xl font-bold">Đặt lịch sắp đặt / đo đạc tại nhà</h1>
        <p className="mt-2 text-sm text-neutral-500">
          Dịch vụ tận nơi cho nội thất cồng kềnh: giao + lắp đặt, hoặc đo đạc riêng (tủ bếp, tủ áo).
          Miễn phí trong khu vực nội thành.
        </p>

        <form onSubmit={handleSubmit} className="mt-6 space-y-4 rounded-xl border bg-white p-6">
          {error && <p className="rounded-lg bg-red-50 p-3 text-sm text-red-600">{error}</p>}

          <div className="grid gap-4 sm:grid-cols-2">
            <div>
              <label className="mb-1 block text-xs font-medium text-neutral-600">SĐT khách hàng *</label>
              <input required value={customerPhone} onChange={(e) => setCustomerPhone(e.target.value)} placeholder="09xxxxxxxx" className={inputCls} />
            </div>
            <div>
              <label className="mb-1 block text-xs font-medium text-neutral-600">Tên khách hàng *</label>
              <input required value={customerName} onChange={(e) => setCustomerName(e.target.value)} placeholder="Họ và tên" className={inputCls} />
            </div>
            <div>
              <label className="mb-1 block text-xs font-medium text-neutral-600">SĐT người nhận *</label>
              <input required value={receiverPhone} onChange={(e) => setReceiverPhone(e.target.value)} placeholder="09xxxxxxxx" className={inputCls} />
            </div>
            <div>
              <label className="mb-1 block text-xs font-medium text-neutral-600">Tên người nhận *</label>
              <input required value={receiverName} onChange={(e) => setReceiverName(e.target.value)} placeholder="Họ và tên" className={inputCls} />
            </div>
          </div>

          <div>
            <label className="mb-1 block text-xs font-medium text-neutral-600">Địa chỉ *</label>
            <input required value={address} onChange={(e) => setAddress(e.target.value)} placeholder="Số nhà, đường, phường/xã, quận/huyện, TP" className={inputCls} />
          </div>

          <div className="grid gap-4 sm:grid-cols-2">
            <div>
              <label className="mb-1 block text-xs font-medium text-neutral-600">Loại dịch vụ *</label>
              <select value={appointmentType} onChange={(e) => setAppointmentType(e.target.value)} className={inputCls}>
                <option value="Installation">Lắp đặt tại nhà</option>
                <option value="Measurement">Đo đạc riêng (bespoke)</option>
              </select>
            </div>
            <div>
              <label className="mb-1 block text-xs font-medium text-neutral-600">Ngày mong muốn *</label>
              <input required type="date" value={desiredDate} min={new Date().toISOString().split("T")[0]} onChange={(e) => setDesiredDate(e.target.value)} className={inputCls} />
            </div>
          </div>

          <div>
            <label className="mb-1 block text-xs font-medium text-neutral-600">Khung giờ *</label>
            <select value={timeSlot} onChange={(e) => setTimeSlot(e.target.value)} className={inputCls}>
              {TIME_SLOTS.map((t) => (
                <option key={t.value} value={t.value}>{t.label}</option>
              ))}
            </select>
          </div>

          {appointmentType === "Measurement" && (
            <div>
              <label className="mb-1 block text-xs font-medium text-neutral-600">Ghi chú đo đạc (không gian, mục đích...)</label>
              <textarea value={note} onChange={(e) => setNote(e.target.value)} placeholder="VD: Đo đạc tủ bếp 3m, phòng khách 20m²..." rows={3} className={inputCls} />
            </div>
          )}

          <button type="submit" disabled={submitting} className="btn-primary w-full text-base">
            {submitting ? "Đang đặt lịch..." : "Xác nhận đặt lịch"}
          </button>
        </form>
      </div>
    </div>
  );
}


