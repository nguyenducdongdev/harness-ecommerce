"use client";

import { useState } from "react";
import { useRouter } from "next/navigation";
import Link from "next/link";
import { useAuth } from "@/store/auth";

interface OtpRequestResult {
  phone: string;
  otpCode: string | null;
  expiryMinutes: number;
}

export default function LoginPage() {
  const router = useRouter();
  const signIn = useAuth((s) => s.signIn);

  const [phone, setPhone] = useState("");
  const [code, setCode] = useState("");
  const [name, setName] = useState("");
  const [step, setStep] = useState<"phone" | "code">("phone");
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState("");
  const [otpHint, setOtpHint] = useState<string | null>(null);

  async function requestOtp(e: React.FormEvent) {
    e.preventDefault();
    setError("");
    setOtpHint(null);
    if (!/^0\d{9,10}$/.test(phone)) {
      setError("Số điện thoại không hợp lệ (VD: 0912345678).");
      return;
    }
    setLoading(true);
    try {
      const res = await fetch("/api/v1/customers/otp/request", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ phone }),
      });
      const body = await res.json();
      if (!res.ok || !body.success) {
        setError(body.message || "Không gửi được OTP.");
        return;
      }
      const data = body.data as OtpRequestResult;
      if (data.otpCode) setOtpHint(`[Sandbox] Mã OTP của bạn: ${data.otpCode}`);
      setStep("code");
    } catch {
      setError("Không kết nối được hệ thống.");
    } finally {
      setLoading(false);
    }
  }

  async function verifyOtp(e: React.FormEvent) {
    e.preventDefault();
    setError("");
    setLoading(true);
    try {
      const res = await fetch("/api/v1/customers/otp/verify", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ phone, code, name: name || undefined }),
      });
      const body = await res.json();
      if (!res.ok || !body.success) {
        setError(body.message || "Mã OTP không đúng.");
        return;
      }
      signIn({
        accessToken: body.data.accessToken,
        phone: body.data.phone,
        customerName: null,
      });
      const redirectTo = new URLSearchParams(window.location.search).get("redirect") || "/";
      router.push(redirectTo);
      router.refresh();
    } catch {
      setError("Không kết nối được hệ thống.");
    } finally {
      setLoading(false);
    }
  }

  return (
    <div className="container-page flex justify-center py-12">
      <div className="w-full max-w-md rounded-xl border bg-white p-6">
        <h1 className="text-xl font-bold">Đăng nhập / Đăng ký</h1>
        <p className="mt-1 text-sm text-neutral-500">
          Dùng số điện thoại để nhận mã OTP. Tài khoản mới sẽ được tạo tự động.
        </p>

        {step === "phone" ? (
          <form onSubmit={requestOtp} className="mt-6 space-y-4">
            <input
              required
              value={phone}
              onChange={(e) => setPhone(e.target.value)}
              placeholder="Số điện thoại * (09xxxxxxxx)"
              className="w-full rounded-lg border px-3 py-2.5 text-sm focus:border-brand-500 focus:outline-none"
            />
            {error && <p className="rounded-lg bg-red-50 p-3 text-sm text-red-600">{error}</p>}
            <button type="submit" disabled={loading} className="btn-primary w-full text-base">
              {loading ? "Đang gửi..." : "Gửi mã OTP"}
            </button>
          </form>
        ) : (
          <form onSubmit={verifyOtp} className="mt-6 space-y-4">
            <p className="text-sm text-neutral-600">
              Đã gửi mã đến <strong>{phone}</strong>.{" "}
              <button type="button" className="text-brand-600 hover:underline" onClick={() => setStep("phone")}>
                Đổi số
              </button>
            </p>
            {otpHint && (
              <p className="rounded-lg bg-brand-50 p-3 text-sm text-brand-700">{otpHint}</p>
            )}
            <input
              value={name}
              onChange={(e) => setName(e.target.value)}
              placeholder="Họ tên (không bắt buộc)"
              className="w-full rounded-lg border px-3 py-2.5 text-sm focus:border-brand-500 focus:outline-none"
            />
            <input
              required
              value={code}
              onChange={(e) => setCode(e.target.value)}
              placeholder="Mã OTP"
              inputMode="numeric"
              className="w-full rounded-lg border px-3 py-2.5 text-sm focus:border-brand-500 focus:outline-none"
            />
            {error && <p className="rounded-lg bg-red-50 p-3 text-sm text-red-600">{error}</p>}
            <button type="submit" disabled={loading} className="btn-primary w-full text-base">
              {loading ? "Đang xác thực..." : "Xác thực"}
            </button>
          </form>
        )}

        <p className="mt-6 text-center text-xs text-neutral-400">
          Hoặc <Link href="/" className="text-brand-600 hover:underline">về trang chủ</Link>
        </p>
      </div>
    </div>
  );
}
