"use client";

import Link from "next/link";
import { useRouter } from "next/navigation";
import { useAuth } from "@/store/auth";

export function HeaderUserMenu() {
  const router = useRouter();
  const { accessToken, customerName, signOut } = useAuth();

  if (!accessToken) {
    return (
      <Link href="/login" className="text-sm text-neutral-600 transition hover:text-brand-600">
        Đăng nhập
      </Link>
    );
  }

  return (
    <div className="flex items-center gap-2">
      <span className="hidden text-sm font-medium text-neutral-700 sm:block">{customerName}</span>
      <button
        onClick={() => {
          signOut();
          router.refresh();
        }}
        className="text-sm text-neutral-500 hover:text-red-500"
      >
        Thoát
      </button>
    </div>
  );
}
