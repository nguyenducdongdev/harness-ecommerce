"use client";

import { create } from "zustand";
import { persist } from "zustand/middleware";

interface AuthState {
  accessToken: string | null;
  phone: string | null;
  customerName: string | null;
  signIn: (session: { accessToken: string; phone: string; customerName: string | null }) => void;
  signOut: () => void;
}

export const useAuth = create<AuthState>()(
  persist(
    (set) => ({
      accessToken: null,
      phone: null,
      customerName: null,
      signIn: (session) =>
        set({
          accessToken: session.accessToken,
          phone: session.phone,
          customerName: session.customerName ?? session.phone,
        }),
      signOut: () => set({ accessToken: null, phone: null, customerName: null }),
    }),
    { name: "harness-auth" },
  ),
);
