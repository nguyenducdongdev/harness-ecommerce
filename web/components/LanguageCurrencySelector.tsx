"use client";

import React from "react";
import { useCurrency, Currency } from "@/context/CurrencyContext";
import { useTranslation, Language } from "@/context/I18nContext";

export default function LanguageCurrencySelector() {
  const { currency, setCurrency } = useCurrency();
  const { lang, setLang } = useTranslation();

  return (
    <div className="flex items-center gap-2 text-xs text-neutral-600">
      <select
        value={lang}
        onChange={(e) => setLang(e.target.value as Language)}
        className="rounded border border-neutral-300 bg-white px-1.5 py-1 text-xs focus:border-brand-500 focus:outline-none"
        aria-label="Select Language"
      >
        <option value="vi">🇻🇳 VI</option>
        <option value="en">🇬🇧 EN</option>
      </select>

      <select
        value={currency}
        onChange={(e) => setCurrency(e.target.value as Currency)}
        className="rounded border border-neutral-300 bg-white px-1.5 py-1 text-xs font-semibold focus:border-brand-500 focus:outline-none"
        aria-label="Select Currency"
      >
        <option value="VND">₫ VND</option>
        <option value="USD">$ USD</option>
        <option value="EUR">€ EUR</option>
      </select>
    </div>
  );
}
