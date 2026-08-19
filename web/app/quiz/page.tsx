"use client";

import React, { useState } from "react";
import Link from "next/link";
import { api, QuizRecommendationResult, Product } from "@/lib/api";
import { useCart } from "@/store/cart";
import { ARModelViewer } from "@/components/ARModelViewer";

const ROOM_TYPES = [
  { id: "phong-khach", name: "Phòng Khách", icon: "🛋️", desc: "Sofa, Bàn trà, Kệ tivi" },
  { id: "phong-ngu", name: "Phòng Ngủ", icon: "🛏️", desc: "Giường ngủ, Tủ quần áo" },
  { id: "phong-an", name: "Phòng Bếp & Ăn", icon: "🍽️", desc: "Bàn ghế ăn, Tủ bếp" },
  { id: "van-phong", name: "Văn Phòng", icon: "💼", desc: "Bàn làm việc, Ghế xoay" },
];

const ROOM_AREAS = [
  { value: 15, label: "Nhỏ (< 20 m²)", desc: "Tối ưu diện tích linh hoạt" },
  { value: 25, label: "Vừa (20 - 30 m²)", desc: "Cân đối & thông thoáng" },
  { value: 40, label: "Lớn (30 - 50 m²)", desc: "Sang trọng, thoải mái" },
  { value: 60, label: "Rộng (> 50 m²)", desc: "Phối cảnh cao cấp trọn bộ" },
];

const STYLES = [
  { id: "Modern", name: "Hiện Đại", tag: "Tối giản, tinh tế" },
  { id: "Scandinavian", name: "Bắc Âu", tag: "Gỗ sáng màu, ấm cúng" },
  { id: "Neoclassic", name: "Tân Cổ Điển", tag: "Sang trọng, quý phái" },
  { id: "Indochine", name: "Đông Dương", tag: "Bản sắc Á Đông lãng mạn" },
];

const BUDGETS = [
  { min: 0, max: 20000000, label: "Dưới 20 triệu", desc: "Tiết kiệm thông minh" },
  { min: 20000000, max: 40000000, label: "20 - 40 triệu", desc: "Tiêu chuẩn cao cấp" },
  { min: 40000000, max: 70000000, label: "40 - 70 triệu", desc: "Sang trọng trọn gói" },
  { min: 70000000, max: 150000000, label: "Trên 70 triệu", desc: "Bespoke thiết kế riêng" },
];

export default function QuizPage() {
  const [step, setStep] = useState(1);
  const [roomType, setRoomType] = useState("phong-khach");
  const [roomArea, setRoomArea] = useState(25);
  const [style, setStyle] = useState("Scandinavian");
  const [budget, setBudget] = useState(BUDGETS[1]);

  const [loading, setLoading] = useState(false);
  const [result, setResult] = useState<QuizRecommendationResult | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [selectedProduct3D, setSelectedProduct3D] = useState<Product | null>(null);

  const cart = useCart();

  const handleCalculate = async () => {
    setLoading(true);
    setError(null);
    try {
      const data = await api.getQuizRecommendation({
        roomType,
        roomAreaM2: roomArea,
        style,
        minBudget: budget.min,
        maxBudget: budget.max,
      });
      setResult(data);
      setStep(5);
    } catch (err: any) {
      setError(err?.message || "Không thể tải gợi ý.");
    } finally {
      setLoading(false);
    }
  };

  const handleAddAllToCart = () => {
    if (!result) return;
    let added = 0;
    result.recommendedProducts.forEach((p) => {
      cart.addItem({
        productId: p.id,
        variantSku: p.variants?.[0]?.sku || p.sku || `SKU-${p.id}`,
        productName: p.name,
        sizeName: p.variants?.[0]?.sizeName || "Mặc định",
        unitPrice: p.displayPrice,
      });
      added++;
    });
    alert(`Đã thêm ${added} sản phẩm tư vấn vào giỏ hàng.`);
  };

  return (
    <div className="container-page py-10 max-w-5xl">
      <div className="text-center mb-8">
        <span className="inline-block rounded-full bg-amber-100 px-3 py-1 text-xs font-semibold text-amber-800 mb-2">
          ✨ Tư Vấn Không Gian Nội Thất AI
        </span>
        <h1 className="text-3xl font-extrabold text-neutral-900">Công Cụ Phối Cảnh Thông Minh</h1>
        <p className="mt-2 text-neutral-600 text-sm">Gợi ý nội thất & mô hình 3D AR theo không gian căn hộ</p>
      </div>

      <div className="bg-white rounded-2xl p-6 sm:p-8 shadow-sm border border-neutral-200">
        {step === 1 && (
          <div className="space-y-5">
            <h2 className="text-lg font-bold">Bước 1: Chọn loại không gian</h2>
            <div className="grid grid-cols-2 gap-3">
              {ROOM_TYPES.map((rt) => (
                <div
                  key={rt.id}
                  onClick={() => setRoomType(rt.id)}
                  className={`p-4 rounded-xl border-2 cursor-pointer ${
                    roomType === rt.id ? "border-indigo-600 bg-indigo-50" : "border-neutral-200"
                  }`}
                >
                  <span className="text-2xl block mb-1">{rt.icon}</span>
                  <div className="font-bold text-sm">{rt.name}</div>
                  <div className="text-xs text-neutral-500">{rt.desc}</div>
                </div>
              ))}
            </div>
            <div className="flex justify-end pt-2">
              <button onClick={() => setStep(2)} className="px-5 py-2 bg-indigo-600 text-white rounded-xl text-sm font-medium">
                Tiếp tục: Diện tích →
              </button>
            </div>
          </div>
        )}

        {step === 2 && (
          <div className="space-y-5">
            <h2 className="text-lg font-bold">Bước 2: Diện tích ước tính (m²)</h2>
            <div className="grid grid-cols-2 gap-3">
              {ROOM_AREAS.map((ra) => (
                <div
                  key={ra.value}
                  onClick={() => setRoomArea(ra.value)}
                  className={`p-4 rounded-xl border-2 cursor-pointer ${
                    roomArea === ra.value ? "border-indigo-600 bg-indigo-50" : "border-neutral-200"
                  }`}
                >
                  <div className="font-bold text-sm">{ra.label}</div>
                  <div className="text-xs text-neutral-500">{ra.desc}</div>
                </div>
              ))}
            </div>
            <div className="flex justify-between pt-2">
              <button onClick={() => setStep(1)} className="px-4 py-2 border rounded-xl text-sm">← Quay lại</button>
              <button onClick={() => setStep(3)} className="px-5 py-2 bg-indigo-600 text-white rounded-xl text-sm font-medium">
                Tiếp tục: Phong cách →
              </button>
            </div>
          </div>
        )}

        {step === 3 && (
          <div className="space-y-5">
            <h2 className="text-lg font-bold">Bước 3: Phong cách yêu thích</h2>
            <div className="grid grid-cols-2 gap-3">
              {STYLES.map((st) => (
                <div
                  key={st.id}
                  onClick={() => setStyle(st.id)}
                  className={`p-4 rounded-xl border-2 cursor-pointer ${
                    style === st.id ? "border-indigo-600 bg-indigo-50" : "border-neutral-200"
                  }`}
                >
                  <div className="font-bold text-sm">{st.name}</div>
                  <div className="text-xs text-neutral-500">{st.tag}</div>
                </div>
              ))}
            </div>
            <div className="flex justify-between pt-2">
              <button onClick={() => setStep(2)} className="px-4 py-2 border rounded-xl text-sm">← Quay lại</button>
              <button onClick={() => setStep(4)} className="px-5 py-2 bg-indigo-600 text-white rounded-xl text-sm font-medium">
                Tiếp tục: Ngân sách →
              </button>
            </div>
          </div>
        )}

        {step === 4 && (
          <div className="space-y-5">
            <h2 className="text-lg font-bold">Bước 4: Ngân sách dự kiến</h2>
            <div className="grid grid-cols-2 gap-3">
              {BUDGETS.map((bg) => (
                <div
                  key={bg.label}
                  onClick={() => setBudget(bg)}
                  className={`p-4 rounded-xl border-2 cursor-pointer ${
                    budget.label === bg.label ? "border-indigo-600 bg-indigo-50" : "border-neutral-200"
                  }`}
                >
                  <div className="font-bold text-sm text-indigo-700">{bg.label}</div>
                  <div className="text-xs text-neutral-500">{bg.desc}</div>
                </div>
              ))}
            </div>
            {error && <div className="p-3 bg-red-50 text-red-700 text-xs rounded-lg">{error}</div>}
            <div className="flex justify-between pt-2">
              <button onClick={() => setStep(3)} className="px-4 py-2 border rounded-xl text-sm">← Quay lại</button>
              <button
                onClick={handleCalculate}
                disabled={loading}
                className="px-6 py-2.5 bg-gradient-to-r from-indigo-600 to-purple-600 text-white rounded-xl text-sm font-bold disabled:opacity-50"
              >
                {loading ? "Đang phân tích..." : "🎯 Phối Cảnh AI"}
              </button>
            </div>
          </div>
        )}

        {step === 5 && result && (
          <div className="space-y-6">
            <div className="rounded-xl bg-gradient-to-r from-indigo-900 to-purple-900 p-6 text-white shadow-lg">
              <span className="text-xs font-bold uppercase tracking-widest text-indigo-300">🤖 Tư Vấn Chuyên Gia</span>
              <p className="text-sm mt-2 leading-relaxed">{result.summary}</p>
              <div className="mt-4 flex justify-between items-center border-t border-white/10 pt-3 text-xs">
                <div>
                  <span>Tổng ngân sách: </span>
                  <span className="font-bold text-amber-400 text-base ml-1">{result.totalEstimatedPrice.toLocaleString("vi-VN")} đ</span>
                </div>
                <button onClick={handleAddAllToCart} className="px-4 py-2 rounded-lg bg-amber-500 text-black font-bold">
                  Thêm tất cả vào giỏ
                </button>
              </div>
            </div>

            {result.recommendedCombos.length > 0 && (
              <div>
                <h3 className="text-sm font-bold mb-3">🏛️ Combo Không Gian Trọn Gói</h3>
                <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
                  {result.recommendedCombos.map((cb) => (
                    <div key={cb.id} className="p-4 rounded-xl border bg-neutral-50 flex justify-between items-center">
                      <div>
                        <div className="font-bold text-sm">{cb.name}</div>
                        <div className="text-xs text-indigo-600 font-medium">{cb.saleTotal.toLocaleString("vi-VN")} đ</div>
                      </div>
                      <Link href={`/combos/${cb.slug}`} className="text-xs text-indigo-600 font-bold hover:underline">
                        Chi tiết →
                      </Link>
                    </div>
                  ))}
                </div>
              </div>
            )}

            <div>
              <h3 className="text-sm font-bold mb-3">🛋️ Sản Phẩm Phối Hợp ({result.recommendedProducts.length})</h3>
              <div className="grid grid-cols-1 sm:grid-cols-3 gap-4">
                {result.recommendedProducts.map((p) => (
                  <div key={p.id} className="p-3 rounded-xl border bg-white shadow-sm flex flex-col justify-between">
                    <div>
                      <div className="h-32 bg-neutral-100 rounded-lg flex items-center justify-center relative overflow-hidden mb-2">
                        {p.imageUrls?.[0] ? (
                          <img src={p.imageUrls[0]} alt={p.name} className="w-full h-full object-cover" />
                        ) : (
                          <span className="text-3xl">🪑</span>
                        )}
                        <button
                          onClick={() => setSelectedProduct3D(p)}
                          className="absolute top-2 right-2 px-2 py-1 bg-black/60 text-white text-[10px] rounded backdrop-blur"
                        >
                          🧊 3D/AR
                        </button>
                      </div>
                      <div className="text-xs font-semibold line-clamp-1">{p.name}</div>
                      <div className="text-xs font-bold text-indigo-600 mt-1">{p.displayPrice.toLocaleString("vi-VN")} đ</div>
                    </div>
                    <button
                      onClick={() => {
                        cart.addItem({
                          productId: p.id,
                          variantSku: p.variants?.[0]?.sku || p.sku || `SKU-${p.id}`,
                          productName: p.name,
                          sizeName: p.variants?.[0]?.sizeName || "Mặc định",
                          unitPrice: p.displayPrice,
                        });
                        alert(`Đã thêm "${p.name}" vào giỏ.`);
                      }}
                      className="mt-3 w-full py-1.5 bg-neutral-900 text-white rounded-lg text-xs font-medium"
                    >
                      + Thêm giỏ
                    </button>
                  </div>
                ))}
              </div>
            </div>

            <div className="flex justify-center pt-4">
              <button onClick={() => setStep(1)} className="px-5 py-2 border rounded-xl text-xs font-medium">
                🔄 Thực hiện lại bài tư vấn
              </button>
            </div>
          </div>
        )}
      </div>

      {selectedProduct3D && (
        <ARModelViewer
          productName={selectedProduct3D.name}
          model3dUrl={selectedProduct3D.model3dUrl}
          isOpen={!!selectedProduct3D}
          onClose={() => setSelectedProduct3D(null)}
        />
      )}
    </div>
  );
}
