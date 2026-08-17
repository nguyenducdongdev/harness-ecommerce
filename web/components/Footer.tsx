import Link from "next/link";

export function Footer() {
  return (
    <footer className="mt-16 border-t bg-neutral-900 text-neutral-300">
      <div className="container-page grid gap-8 py-12 md:grid-cols-4">
        <div>
          <p className="text-lg font-bold text-white">Harness Nội Thất</p>
          <p className="mt-2 text-sm">Chuỗi showroom nội thất ứng dụng chính hãng — tư vấn thiết kế tận tâm, lắp đặt tận nơi.</p>
          <p className="mt-4 text-sm">☎️ Hotline: 1900 0000</p>
        </div>
        <div>
          <p className="mb-3 font-semibold text-white">Sản phẩm</p>
          <ul className="space-y-2 text-sm">
            <li><Link href="/products?category=sofa" className="hover:text-white">Sofa &amp; Ghế thư giãn</Link></li>
            <li><Link href="/products?category=giuong-phong-ngu" className="hover:text-white">Giường &amp; Phòng ngủ</Link></li>
            <li><Link href="/products?category=tu-ke" className="hover:text-white">Tủ &amp; Kệ</Link></li>
            <li><Link href="/products?category=ban-an" className="hover:text-white">Bàn ghế ăn</Link></li>
          </ul>
        </div>
        <div>
          <p className="mb-3 font-semibold text-white">Hỗ trợ</p>
          <ul className="space-y-2 text-sm">
            <li><Link href="/track" className="hover:text-white">Tra cứu đơn hàng</Link></li>
            <li><Link href="/pages/chinh-sach-doi-tra" className="hover:text-white">Chính sách đổi trả</Link></li>
            <li><Link href="/pages/chinh-sach-bao-hanh" className="hover:text-white">Chính sách bảo hành</Link></li>
          </ul>
        </div>
        <div>
          <p className="mb-3 font-semibold text-white">Showroom</p>
          <ul className="space-y-2 text-sm">
            <li>123 Nguyễn Huệ, Q1, TP.HCM</li>
            <li>45 Xuân Thủy, Cầu Giấy, Hà Nội</li>
          </ul>
        </div>
      </div>
      <div className="border-t border-neutral-800 py-4 text-center text-xs">
        © 2026 Harness Ecommerce. Được xây dựng trên nền tảng open-source.
      </div>
    </footer>
  );
}
