import type { Metadata } from "next";
import { Providers } from "./providers";
import { Header } from "@/components/Header";
import { Footer } from "@/components/Footer";
import "./globals.css";

export const metadata: Metadata = {
  title: {
    default: "Harness — Nội thất ứng dụng chính hãng, giá tốt chuỗi showroom",
    template: "%s | Harness",
  },
  description:
    "Mua sofa, giường, tủ, bàn ghế, nội thất thông minh chính hãng. Tư vấn thiết kế theo không gian, miễn phí lắp đặt, giao hàng nhanh, nhận tại showroom.",
};

export default function RootLayout({ children }: { children: React.ReactNode }) {
  return (
    <html lang="vi">
      <body>
        <Providers>
          <Header />
          <main className="min-h-[calc(100vh-var(--header-height))]">{children}</main>
          <Footer />
        </Providers>
      </body>
    </html>
  );
}
