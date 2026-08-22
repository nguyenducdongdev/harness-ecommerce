"use client";

import React, { createContext, useContext, useState, useEffect } from "react";

export type Currency = "VND" | "USD" | "EUR";

interface CurrencyContextType {
  currency: Currency;
  setCurrency: (c: Currency) => void;
  formatPrice: (vndAmount: number) => string;
  convertPrice: (vndAmount: number) => number;
  rates: Record<Currency, number>;
}

const DEFAULT_RATES: Record<Currency, number> = {
  VND: 1,
  USD: 1 / 25400,
  EUR: 1 / 27500,
};

const CurrencyContext = createContext<CurrencyContextType | undefined>(undefined);

export function CurrencyProvider({ children }: { children: React.ReactNode }) {
  const [currency, setCurrencyState] = useState<Currency>("VND");
  const [rates] = useState<Record<Currency, number>>(DEFAULT_RATES);

  useEffect(() => {
    const saved = localStorage.getItem("harness_currency") as Currency;
    if (saved && (saved === "VND" || saved === "USD" || saved === "EUR")) {
      setCurrencyState(saved);
    }
  }, []);

  const setCurrency = (c: Currency) => {
    setCurrencyState(c);
    localStorage.setItem("harness_currency", c);
  };

  const convertPrice = (vndAmount: number): number => {
    if (currency === "VND") return vndAmount;
    const rate = rates[currency] || DEFAULT_RATES[currency];
    return Number((vndAmount * rate).toFixed(2));
  };

  const formatPrice = (vndAmount: number): string => {
    if (currency === "USD") {
      const usd = convertPrice(vndAmount);
      return `$${usd.toLocaleString("en-US", { minimumFractionDigits: 2, maximumFractionDigits: 2 })}`;
    }
    if (currency === "EUR") {
      const eur = convertPrice(vndAmount);
      return `€${eur.toLocaleString("de-DE", { minimumFractionDigits: 2, maximumFractionDigits: 2 })}`;
    }
    return `${vndAmount.toLocaleString("vi-VN")} ₫`;
  };

  return (
    <CurrencyContext.Provider value={{ currency, setCurrency, formatPrice, convertPrice, rates }}>
      {children}
    </CurrencyContext.Provider>
  );
}

export function useCurrency() {
  const context = useContext(CurrencyContext);
  if (!context) {
    // Return fallback for non-provider contexts
    return {
      currency: "VND" as Currency,
      setCurrency: () => {},
      formatPrice: (v: number) => `${v.toLocaleString("vi-VN")} ₫`,
      convertPrice: (v: number) => v,
      rates: DEFAULT_RATES,
    };
  }
  return context;
}
