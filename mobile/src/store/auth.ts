import { create } from "zustand";
import { createJSONStorage, persist } from "zustand/middleware";
import AsyncStorage from "@react-native-async-storage/async-storage";

interface AuthState {
  accessToken: string | null;
  phone: string | null;
  customerId: string | null;
  customerName: string | null;
  signIn: (s: { accessToken: string; phone: string; customerId: string; customerName: string | null }) => void;
  setName: (name: string) => void;
  signOut: () => void;
}

export const useAuth = create<AuthState>()(
  persist(
    (set) => ({
      accessToken: null,
      phone: null,
      customerId: null,
      customerName: null,
      signIn: (s) =>
        set({ accessToken: s.accessToken, phone: s.phone, customerId: s.customerId, customerName: s.customerName ?? s.phone }),
      setName: (name) => set({ customerName: name }),
      signOut: () => set({ accessToken: null, phone: null, customerId: null, customerName: null }),
    }),
    { name: "harness-auth", storage: createJSONStorage(() => AsyncStorage) },
  ),
);
