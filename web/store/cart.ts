"use client";

import { create } from "zustand";
import { persist } from "zustand/middleware";

export interface CartItem {
  productId: number;
  variantSku: string;
  productName: string;
  sizeName: string;
  unitPrice: number;
  quantity: number;
}

interface CartState {
  items: CartItem[];
  addItem: (item: Omit<CartItem, "quantity">, quantity?: number) => void;
  removeItem: (variantSku: string) => void;
  setQuantity: (variantSku: string, quantity: number) => void;
  clear: () => void;
  totalItems: () => number;
  totalAmount: () => number;
}

export const useCart = create<CartState>()(
  persist(
    (set, get) => ({
      items: [],
      addItem: (item, quantity = 1) =>
        set((state) => {
          const existing = state.items.find((i) => i.variantSku === item.variantSku);
          if (existing) {
            return {
              items: state.items.map((i) =>
                i.variantSku === item.variantSku ? { ...i, quantity: i.quantity + quantity } : i,
              ),
            };
          }
          return { items: [...state.items, { ...item, quantity }] };
        }),
      removeItem: (variantSku) =>
        set((state) => ({ items: state.items.filter((i) => i.variantSku !== variantSku) })),
      setQuantity: (variantSku, quantity) =>
        set((state) => ({
          items: state.items.map((i) =>
            i.variantSku === variantSku ? { ...i, quantity: Math.max(1, Math.min(50, quantity)) } : i,
          ),
        })),
      clear: () => set({ items: [] }),
      totalItems: () => get().items.reduce((sum, i) => sum + i.quantity, 0),
      totalAmount: () => get().items.reduce((sum, i) => sum + i.unitPrice * i.quantity, 0),
    }),
    { name: "harness-cart" },
  ),
);
