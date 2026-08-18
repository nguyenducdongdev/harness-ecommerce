import { useState } from "react";
import { Alert, Pressable, ScrollView, StyleSheet, Text, TextInput, View } from "react-native";
import { useRouter } from "expo-router";
import * as WebBrowser from "expo-web-browser";
import { apiGet, apiPost } from "@/lib/api";
import { cartTotal, useCart } from "@/store/cart";
import { useAuth } from "@/store/auth";
import { vnd } from "@/lib/format";

interface CreatedOrder {
  id: string;
  orderNumber: string;
  totalAmount: number;
}

interface VnPayInit {
  paymentUrl: string;
}

export default function CheckoutScreen() {
  const router = useRouter();
  const { items, clear } = useCart();
  const { customerName, phone: authPhone, accessToken } = useAuth();
  const [name, setName] = useState(customerName ?? "");
  const [phone, setPhone] = useState(authPhone ?? "");
  const [address, setAddress] = useState("");
  const [payment, setPayment] = useState<"Cod" | "VnPay">("Cod");
  const [voucherCode, setVoucherCode] = useState("");
  const [discount, setDiscount] = useState(0);
  const [voucherMsg, setVoucherMsg] = useState("");
  const [submitting, setSubmitting] = useState(false);
  const total = cartTotal(items);

  async function applyVoucher() {
    if (!voucherCode.trim()) return;
    try {
      const r = await apiGet<{ isValid: boolean; discountAmount: number; message: string | null }>(
        `/api/v1/vouchers/validate?code=${encodeURIComponent(voucherCode.trim())}&orderAmount=${total}`,
      );
      if (!r.isValid) {
        setDiscount(0);
        setVoucherMsg(r.message ?? "Không áp dụng được.");
        return;
      }
      setDiscount(r.discountAmount);
      setVoucherMsg(`Giảm ${vnd(r.discountAmount)}`);
    } catch {
      setVoucherMsg("Không kiểm tra được mã.");
    }
  }

  async function handleSubmit() {
    if (!/^0\d{9,10}$/.test(phone)) {
      Alert.alert("Lỗi", "Số điện thoại không hợp lệ.");
      return;
    }
    if (!name.trim() || items.length === 0) {
      Alert.alert("Lỗi", "Nhập họ tên và có sản phẩm trong giỏ.");
      return;
    }
    setSubmitting(true);
    try {
      const order = await apiPost<CreatedOrder>(
        "/api/v1/orders",
        {
          customerName: name,
          customerPhone: phone,
          shippingAddress: address.trim() || "Nhận tại showroom",
          deliveryMethod: "Standard",
          paymentMethod: payment,
          discountAmount: discount,
          items: items.map((i) => ({
            productId: i.productId,
            variantSku: `SKU-${i.productId}`,
            productName: i.name,
            unitPrice: i.price,
            quantity: i.quantity,
          })),
        },
        accessToken,
      );

      if (payment === "VnPay") {
        try {
          const vp = await apiPost<VnPayInit>(
            "/api/v1/payments/vnpay/create",
            { orderId: order.id, amount: order.totalAmount, orderInfo: `Thanh toan don ${order.orderNumber}`, clientIp: "127.0.0.1" },
            accessToken,
          );
          await WebBrowser.openBrowserAsync(vp.paymentUrl);
        } catch {
          Alert.alert("VNPay", "Không mở được cổng thanh toán, vui lòng thử lại.");
        }
      }

      clear();
      Alert.alert("Đặt hàng thành công", `Mã đơn: ${order.orderNumber}`, [
        { text: "Tra cứu", onPress: () => router.push({ pathname: "/track", params: { order: order.orderNumber } } as never) },
        { text: "OK", onPress: () => router.replace("/") },
      ]);
    } catch (e: any) {
      Alert.alert("Lỗi", e.message ?? "Đặt hàng thất bại.");
    } finally {
      setSubmitting(false);
    }
  }

  return (
    <ScrollView contentContainerStyle={styles.container}>
      <Text style={styles.label}>Họ tên *</Text>
      <TextInput style={styles.input} value={name} onChangeText={setName} placeholder="Nguyễn Văn A" />
      <Text style={styles.label}>Số điện thoại *</Text>
      <TextInput style={styles.input} value={phone} onChangeText={setPhone} keyboardType="phone-pad" placeholder="0912345678" />
      <Text style={styles.label}>Địa chỉ giao hàng</Text>
      <TextInput style={styles.input} value={address} onChangeText={setAddress} placeholder="Bỏ trống = nhận tại showroom" />

      <Text style={styles.label}>Thanh toán</Text>
      <View style={styles.payRow}>
        <Pressable style={[styles.payBtn, payment === "Cod" && styles.payActive]} onPress={() => setPayment("Cod")}>
          <Text style={[styles.payText, payment === "Cod" && styles.payTextActive]}>COD</Text>
        </Pressable>
        <Pressable style={[styles.payBtn, payment === "VnPay" && styles.payActive]} onPress={() => setPayment("VnPay")}>
          <Text style={[styles.payText, payment === "VnPay" && styles.payTextActive]}>VNPay</Text>
        </Pressable>
      </View>

      <Text style={styles.label}>Mã giảm giá</Text>
      <View style={styles.voucherRow}>
        <TextInput style={[styles.input, { flex: 1, marginBottom: 0 }]} value={voucherCode} onChangeText={setVoucherCode} placeholder="VD: SALE20" />
        <Pressable style={styles.voucherBtn} onPress={applyVoucher}>
          <Text style={styles.voucherBtnText}>Áp dụng</Text>
        </Pressable>
      </View>
      {voucherMsg ? <Text style={styles.voucherMsg}>{voucherMsg}</Text> : null}

      <View style={styles.summary}>
        <Text>Tạm tính: {vnd(total)}</Text>
        <Text>Giảm giá: −{vnd(discount)}</Text>
        <Text style={styles.total}>Tổng cộng: {vnd(total - discount)}</Text>
      </View>

      <Pressable style={styles.submitBtn} onPress={handleSubmit} disabled={submitting}>
        <Text style={styles.submitText}>
          {submitting ? "Đang đặt..." : payment === "VnPay" ? "Đặt hàng & thanh toán" : "Đặt hàng"}
        </Text>
      </Pressable>
    </ScrollView>
  );
}

const styles = StyleSheet.create({
  container: { padding: 16, backgroundColor: "#fff" },
  label: { fontSize: 13, fontWeight: "600", color: "#555", marginTop: 12, marginBottom: 6 },
  input: { borderWidth: 1, borderColor: "#ddd", borderRadius: 8, padding: 12, fontSize: 15, marginBottom: 4 },
  payRow: { flexDirection: "row", gap: 10 },
  payBtn: { flex: 1, borderWidth: 1.5, borderColor: "#ddd", borderRadius: 8, padding: 12, alignItems: "center" },
  payActive: { borderColor: "#c9372c", backgroundColor: "#fff1f0" },
  payText: { fontWeight: "600", color: "#555" },
  payTextActive: { color: "#c9372c" },
  voucherRow: { flexDirection: "row", gap: 8 },
  voucherBtn: { backgroundColor: "#c9372c", borderRadius: 8, paddingHorizontal: 16, justifyContent: "center" },
  voucherBtnText: { color: "#fff", fontWeight: "700" },
  voucherMsg: { marginTop: 6, fontSize: 13, color: "#c9372c" },
  summary: { marginTop: 16, backgroundColor: "#fafafa", borderRadius: 10, padding: 12, gap: 4 },
  total: { fontWeight: "700", fontSize: 16, marginTop: 4 },
  submitBtn: { backgroundColor: "#c9372c", borderRadius: 10, padding: 16, alignItems: "center", marginTop: 20 },
  submitText: { color: "#fff", fontWeight: "700", fontSize: 16 },
});

