"use client";

import React, { useState } from "react";

interface ARModelViewerProps {
  productName: string;
  model3dUrl?: string | null;
  isOpen: boolean;
  onClose: () => void;
}

export function ARModelViewer({ productName, model3dUrl, isOpen, onClose }: ARModelViewerProps) {
  const [rotation, setRotation] = useState(45);
  const [activeColor, setActiveColor] = useState("#8b5cf6"); // default theme color
  const [arActive, setArActive] = useState(false);

  if (!isOpen) return null;

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/70 backdrop-blur-sm p-4 animate-fade-in">
      <div className="relative w-full max-w-2xl overflow-hidden rounded-2xl bg-neutral-900 text-white shadow-2xl border border-neutral-800">
        {/* Header */}
        <div className="flex items-center justify-between border-b border-neutral-800 px-6 py-4">
          <div className="flex items-center gap-2">
            <span className="text-xl">🧊</span>
            <h3 className="font-semibold text-lg">{productName} — Xem 3D & Thực tế ảo AR</h3>
          </div>
          <button
            onClick={onClose}
            className="rounded-lg p-1.5 text-neutral-400 hover:bg-neutral-800 hover:text-white transition"
          >
            ✕
          </button>
        </div>

        {/* 3D Canvas Container */}
        <div className="relative h-80 w-full bg-gradient-to-b from-neutral-800 via-neutral-900 to-black flex items-center justify-center overflow-hidden">
          {arActive ? (
            <div className="flex flex-col items-center justify-center text-center p-6 bg-neutral-950/90 inset-0 absolute z-10">
              <span className="text-4xl mb-2 animate-bounce">📱</span>
              <h4 className="font-semibold text-base">Chế độ Thực tế ảo AR (WebXR)</h4>
              <p className="text-xs text-neutral-400 max-w-md mt-1">
                Quét mã QR hoặc truy cập ứng dụng Mobile Harness để trải nghiệm đặt mô hình 3D tỉ lệ 1:1 trực tiếp vào không gian căn phòng.
              </p>
              <div className="mt-3 rounded-lg bg-white p-2 shadow">
                <div className="w-24 h-24 bg-neutral-900 rounded flex items-center justify-center text-[10px] text-neutral-400 border border-neutral-700">
                  [AR QR Code]
                </div>
              </div>
              <button
                onClick={() => setArActive(false)}
                className="mt-3 text-xs text-indigo-400 hover:underline"
              >
                ← Quay lại xem mô hình 3D
              </button>
            </div>
          ) : (
            <div className="relative flex flex-col items-center justify-center w-full h-full">
              <div
                className="w-44 h-44 rounded-2xl flex items-center justify-center shadow-2xl transition-transform duration-300 border-2 border-white/20 cursor-grab active:cursor-grabbing"
                style={{
                  transform: `rotateY(${rotation}deg) rotateX(10deg)`,
                  backgroundColor: activeColor,
                  boxShadow: `0 20px 50px ${activeColor}50`,
                }}
              >
                <div className="text-center p-4">
                  <span className="text-5xl block mb-1">🛋️</span>
                  <span className="text-[10px] font-mono opacity-80 uppercase tracking-widest">3D Model GLTF</span>
                </div>
              </div>

              <div className="absolute bottom-3 left-4 text-[11px] text-neutral-400 flex items-center gap-2">
                <span>🔄 Góc xoay: {rotation}°</span>
              </div>
            </div>
          )}
        </div>

        {/* Interactive Controls */}
        <div className="p-6 bg-neutral-900 border-t border-neutral-800 space-y-4">
          <div className="flex items-center justify-between">
            <label className="text-xs font-medium text-neutral-400">Góc xoay 360°:</label>
            <input
              type="range"
              min="0"
              max="360"
              value={rotation}
              onChange={(e) => setRotation(Number(e.target.value))}
              className="w-2/3 accent-indigo-500 cursor-pointer"
            />
          </div>

          <div className="flex items-center justify-between">
            <span className="text-xs font-medium text-neutral-400">Tùy chọn chất liệu & màu sắc:</span>
            <div className="flex gap-2">
              {[
                { name: "Tím Đô thị", color: "#8b5cf6" },
                { name: "Xám Xi-măng", color: "#64748b" },
                { name: "Nâu Gỗ Óc Chó", color: "#78350f" },
                { name: "Xanh Emerald", color: "#065f46" },
              ].map((item) => (
                <button
                  key={item.color}
                  onClick={() => setActiveColor(item.color)}
                  title={item.name}
                  className={`w-6 h-6 rounded-full border-2 transition ${
                    activeColor === item.color ? "border-white scale-110" : "border-transparent opacity-70"
                  }`}
                  style={{ backgroundColor: item.color }}
                />
              ))}
            </div>
          </div>

          <div className="flex gap-3 pt-2">
            <button
              onClick={() => setArActive(true)}
              className="flex-1 py-2.5 rounded-xl bg-gradient-to-r from-indigo-500 to-purple-600 text-white font-medium text-sm flex items-center justify-center gap-2 hover:opacity-90 transition shadow-lg shadow-indigo-500/25"
            >
              <span>📱 Xem bằng AR trong không gian thực</span>
            </button>
            <button
              onClick={onClose}
              className="px-5 py-2.5 rounded-xl border border-neutral-700 text-neutral-300 hover:bg-neutral-800 font-medium text-sm transition"
            >
              Đóng
            </button>
          </div>
        </div>
      </div>
    </div>
  );
}