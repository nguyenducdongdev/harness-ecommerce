import { Stack } from "expo-router";
import { StatusBar } from "expo-status-bar";

export default function RootLayout() {
  return (
    <>
      <StatusBar style="light" />
      <Stack
        screenOptions={{
          headerStyle: { backgroundColor: "#c9372c" },
          headerTintColor: "#fff",
          headerTitleStyle: { fontWeight: "700" },
        }}
      >
        <Stack.Screen name="index" options={{ title: "Harness Nội Thất" }} />
        <Stack.Screen name="login" options={{ title: "Đăng nhập" }} />
        <Stack.Screen name="cart" options={{ title: "Giỏ hàng" }} />
        <Stack.Screen name="checkout" options={{ title: "Thanh toán" }} />
        <Stack.Screen name="track" options={{ title: "Tra cứu đơn" }} />
        <Stack.Screen name="booking" options={{ title: "Đặt lịch lắp đặt" }} />
        <Stack.Screen name="membership" options={{ title: "Thành viên" }} />
        <Stack.Screen name="products/[slug]" options={{ title: "Chi tiết sản phẩm" }} />
        <Stack.Screen name="categories/[slug]" options={{ title: "Danh mục" }} />
      </Stack>
    </>
  );
}
