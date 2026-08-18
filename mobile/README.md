# Harness Mobile (Expo / React Native)

App mua sắm nội thất — Phase 2, mirror tính năng của web Next.js.

## Stack
- **Expo SDK 57** + **Expo Router** (file-based routing trong `src/app/`)
- React Native 0.86, React 19, TypeScript
- Zustand + AsyncStorage (giỏ hàng, phiên OTP)
- `expo-web-browser` cho thanh toán VNPay

## Cấu trúc
```
src/
  app/                # Routes (Expo Router)
    index.tsx         # Trang chủ: banner, danh mục, flash sale, sản phẩm
    login.tsx         # Đăng nhập OTP 2 bước
    cart.tsx          # Giỏ hàng
    checkout.tsx      # Thanh toán COD / VNPay
    track.tsx         # Tra cứu đơn
    booking.tsx       # Đặt lịch lắp đặt / đo đạc
    membership.tsx    # Điểm thành viên + đổi quà
    products/[slug].tsx
    categories/[slug].tsx
  components/         # ProductCard, FlashSaleSection
  lib/                # api.ts, format.ts, types.ts
  store/              # auth.ts, cart.ts (zustand persist)
```

## Cấu hình
Đặt biến môi trường (file `.env`):
```
EXPO_PUBLIC_API_URL=http://localhost:5080
```

## Chạy
```bash
npm install
npm start        # mở Expo Dev Tools / quét QR bằng Expo Go
npm run android  # giả lập Android
npm run ios      # giả lập iOS (macOS)
```

## Kiểm tra build (không cần thiết bị)
```bash
npx tsc --noEmit        # typecheck
npx expo export         # bundle JS (validate Metro + routes)
```

> Ghi chú: đăng nhập OTP sandbox trả mã ngay trong response
> (`Otp:ReturnCodeInResponse=true`) để test mà không cần SMS gateway.
