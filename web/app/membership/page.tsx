"use client";

import { useCallback, useEffect, useState } from "react";
import Link from "next/link";
import { formatVnd } from "@/lib/format";
import { useAuth } from "@/store/auth";
import { loyaltyApi, type CustomerDto, type LoyaltyDto, type RewardDto, type LoyaltyTransactionDto } from "@/lib/api";

export default function MembershipPage() {
  const { accessToken } = useAuth();

  const [customer, setCustomer] = useState<CustomerDto | null>(null);
  const [loyalty, setLoyalty] = useState<LoyaltyDto | null>(null);
  const [rewards, setRewards] = useState<RewardDto[]>([]);
  const [transactions, setTransactions] = useState<LoyaltyTransactionDto[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState("");
  const [message, setMessage] = useState("");

  const load = useCallback(async (token: string) => {
    setLoading(true);
    setError("");
    try {
      const me = await loyaltyApi.me(token);
      setCustomer(me);

      let account: LoyaltyDto | null = null;
      try {
        account = await loyaltyApi.get(me.id);
      } catch {
        account = null; // chưa có tài khoản tích điểm
      }
      setLoyalty(account);

      setRewards(await loyaltyApi.rewards());
      setTransactions(await loyaltyApi.transactions(me.id));
    } catch {
      setError("Không tải được thông tin thành viên.");
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    if (accessToken) void load(accessToken);
  }, [accessToken, load]);

  async function redeem(reward: RewardDto) {
    if (!accessToken || !customer) return;
    if (!window.confirm(`Đổi quà "${reward.name}" với ${reward.pointsCost} điểm?`)) return;
    setError("");
    setMessage("");
    try {
      const updated = await loyaltyApi.redeem(customer.id, reward.id);
      setLoyalty(updated);
      setMessage("Đã đổi quà thành công!");
      setTransactions(await loyaltyApi.transactions(customer.id));
    } catch (err) {
      setError(err instanceof Error ? err.message : "Không đổi được quà.");
    }
  }
  if (!accessToken) {
    return (
      <div className="container-page max-w-xl py-20 text-center">
        <p className="text-5xl">⭐</p>
        <h1 className="mt-4 text-2xl font-bold">Tài khoản tích điểm Harness</h1>
        <p className="mt-2 text-neutral-600">
          Đăng nhập bằng số điện thoại (OTP) để xem điểm, hạng thành viên và đổi quà.
        </p>
        <Link href="/login?redirect=/membership" className="btn-primary mt-6">Đăng nhập</Link>
      </div>
    );
  }

  if (loading) {
    return <div className="container-page py-20 text-center text-neutral-500">Đang tải...</div>;
  }

  const tierMeta: Record<string, { label: string; color: string }> = {
    Silver: { label: "Thành viên Bạc", color: "bg-neutral-200 text-neutral-700" },
    Gold: { label: "Thành viên Vàng", color: "bg-yellow-100 text-yellow-700" },
    Platinum: { label: "Thành viên Bạch kim", color: "bg-neutral-300 text-neutral-800" },
    Diamond: { label: "Thành viên Kim cương", color: "bg-indigo-100 text-indigo-700" },
  };
  const tier = (loyalty?.tier ?? "Silver") as keyof typeof tierMeta;
  const tierInfo = tierMeta[tier] ?? tierMeta.Silver;

  return (
    <div className="container-page max-w-4xl py-10">
      <h1 className="text-2xl font-bold">Chương trình thành viên</h1>
      <p className="mt-1 text-neutral-500">
        Chào {customer?.fullName} — mỗi 10.000đ chi tiêu tích 1 điểm, dùng để đổi quà.
      </p>

      {error && <p className="mt-4 rounded-lg bg-red-50 p-3 text-sm text-red-600">{error}</p>}
      {message && <p className="mt-4 rounded-lg bg-green-50 p-3 text-sm text-green-700">{message}</p>}

      <div className="mt-6 flex flex-col gap-3 rounded-xl border bg-gradient-to-r from-brand-600 to-brand-500 p-6 text-white sm:flex-row sm:items-center sm:justify-between">
        <div>
          <div className="flex items-center gap-2">
            <span className={`rounded-full px-3 py-1 text-xs font-bold ${tierInfo.color}`}>
              {tierInfo.label}
            </span>
          </div>
          <p className="mt-2 text-4xl font-extrabold">{loyalty?.points ?? 0} <span className="text-xl font-semibold text-white/80">điểm</span></p>
        </div>
        <div className="text-right text-sm text-white/90">
          <p>Đã chi tiêu: {formatVnd(loyalty?.lifetimeSpend ?? 0)}</p>
          <p className="mt-1">Điểm hết hạn sau 12 tháng kể từ lần giao dịch cuối.</p>
        </div>
      </div>

      <h2 className="mb-3 mt-8 text-lg font-bold">🎁 Kho quà (đổi điểm)</h2>
      {rewards.length === 0 ? (
        <p className="rounded-xl border border-dashed p-8 text-center text-neutral-500">Chưa có quà nào.</p>
      ) : (
        <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
          {rewards.map((r) => {
            const canRedeem = (loyalty?.points ?? 0) >= r.pointsCost;
            return (
              <div key={r.id} className="flex flex-col rounded-xl border bg-white p-5">
                <p className="font-semibold">{r.name}</p>
                <p className="mt-1 flex-1 text-sm text-neutral-500">{r.description}</p>
                <div className="mt-3 flex items-center justify-between">
                  <div className="text-sm">
                    <span className="font-bold text-brand-600">{r.pointsCost} điểm</span>
                    <span className="ml-2 text-neutral-400">{formatVnd(r.value)}</span>
                  </div>
                  <button
                    onClick={() => redeem(r)}
                    disabled={!canRedeem}
                    className="rounded-lg bg-brand-600 px-4 py-1.5 text-sm font-semibold text-white hover:bg-brand-700 disabled:opacity-40"
                  >
                    Đổi
                  </button>
                </div>
              </div>
            );
          })}
        </div>
      )}

      <h2 className="mb-3 mt-8 text-lg font-bold">📜 Lịch sử điểm</h2>
      {transactions.length === 0 ? (
        <p className="rounded-xl border border-dashed p-8 text-center text-neutral-500">Chưa có giao dịch điểm nào.</p>
      ) : (
        <ul className="divide-y rounded-xl border bg-white">
          {transactions.map((t) => (
            <li key={t.id} className="flex items-center justify-between gap-3 px-5 py-3 text-sm">
              <div className="min-w-0">
                <p className="font-medium">{t.reference}</p>
                <p className="truncate text-xs text-neutral-500">
                  {t.note} · {new Date(t.createdAt).toLocaleString("vi-VN")}
                </p>
              </div>
              <span className={`shrink-0 font-bold ${t.pointsDelta > 0 ? "text-green-600" : "text-red-600"}`}>
                {t.pointsDelta > 0 ? "+" : ""}{t.pointsDelta}
              </span>
            </li>
          ))}
        </ul>
      )}
    </div>
  );
}

