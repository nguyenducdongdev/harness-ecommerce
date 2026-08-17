import { FlashSaleSection } from "@/components/FlashSaleSection";

export const metadata = { title: "Flash Sale" };

export default function FlashSalePage() {
  return (
    <div className="container-page py-8">
      <h1 className="text-2xl font-bold">⚡ Flash Sale hôm nay</h1>
      <p className="mt-1 text-neutral-500">
        Giá ưu đãi số lượng có hạn trong khung giờ — đặt ngay kẻo lỡ.
      </p>
      <div className="mt-6">
        <FlashSaleSection />
      </div>
    </div>
  );
}