"use client";

import React, { createContext, useContext, useState, useEffect } from "react";

export type Language = "vi" | "en";

const translations: Record<Language, Record<string, string>> = {
  vi: {
    "nav.flashSale": "Flash sale",
    "nav.points": "Tích điểm",
    "nav.booking": "Đặt lịch",
    "nav.track": "Tra cứu đơn",
    "nav.search": "Tìm sản phẩm...",
    "cart.title": "Giỏ hàng",
    "cart.checkout": "Thanh toán ngay",
    "checkout.title": "Thanh toán đơn hàng",
    "export.badge": "Hỗ trợ xuất khẩu quốc tế",
    "showroom.allocation": "Tự động phân bổ kho gần nhất",
    "common.currency": "Tiền tệ",
    "common.language": "Ngôn ngữ",
  },
  en: {
    "nav.flashSale": "Flash Sale",
    "nav.points": "Loyalty Points",
    "nav.booking": "Book Appointment",
    "nav.track": "Track Order",
    "nav.search": "Search products...",
    "cart.title": "Shopping Cart",
    "cart.checkout": "Checkout Now",
    "checkout.title": "Order Checkout",
    "export.badge": "International Export Ready",
    "showroom.allocation": "Auto-allocated nearest showroom warehouse",
    "common.currency": "Currency",
    "common.language": "Language",
  },
};

interface I18nContextType {
  lang: Language;
  setLang: (lang: Language) => void;
  t: (key: string) => string;
}

const I18nContext = createContext<I18nContextType | undefined>(undefined);

export function I18nProvider({ children }: { children: React.ReactNode }) {
  const [lang, setLangState] = useState<Language>("vi");

  useEffect(() => {
    const saved = localStorage.getItem("harness_lang") as Language;
    if (saved && (saved === "vi" || saved === "en")) {
      setLangState(saved);
    }
  }, []);

  const setLang = (l: Language) => {
    setLangState(l);
    localStorage.setItem("harness_lang", l);
  };

  const t = (key: string): string => {
    const dict = translations[lang] || translations.vi;
    return dict[key] || translations.vi[key] || key;
  };

  return (
    <I18nContext.Provider value={{ lang, setLang, t }}>
      {children}
    </I18nContext.Provider>
  );
}

export function useTranslation() {
  const context = useContext(I18nContext);
  if (!context) {
    return {
      lang: "vi" as Language,
      setLang: () => {},
      t: (key: string) => key,
    };
  }
  return context;
}
