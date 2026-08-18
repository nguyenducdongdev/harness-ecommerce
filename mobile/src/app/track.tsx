import { useState } from "react";
import { Pressable, ScrollView, StyleSheet, Text, TextInput, View } from "react-native";
import { useLocalSearchParams } from "expo-router";
import { apiGet } from "@/lib/api";
import { vnd } from "@/lib/format";

interface OrderDetail {
  id: string;
  orderNumber: string;
  status: string;
  customerName: string;
  customerPhone: string;
  totalAmount: number;
  paymentMethod: string;
  shippingAddress: string;
  items: { productName: string; quantity: number; unitPrice: number }[];
}

const STATUS_LABELS: Record<string, string> = {
  PendingConfirmation: "Chờ xác nhận",
  Processing: "Đang xử lý",
  Shipping: "Đang giao",
  Delivered: "Đã giao",
  Completed: "Hoàn thành",
  Cancelled: "Đã hủy",
  Refunded: "Đã hoàn tiền",
};

export default function TrackScreen() {
  const params = useLocalSearchParams<{ order?: string }>();
  const [code, setCode] = useState(params.order ?? "");
  const [order, setOrder] = useState<OrderDetail | null>(null);
  const [error, setError] = useState("");
  const [loading, setLoading] = useState(false);

  async function lookup() {
    if (!code.trim()) return;
    setLoading(true);
    setError("");
    setOrder(null);
    try {
      const o = await apiGet<OrderDetail>(`/api/v1/orders/${encodeURIComponent(code.trim())}`);
      setOrder(o);
    } catch (e: any) {
      setError(e.message ?? "Không tìm thấy đơn.");
    } finally {
      setLoading(false);
    }
  }

  return (
    <ScrollView contentContainerStyle={styles.container}>
      <Text style={styles.label}>Mã đơn hàng</Text>
      <View style={styles.row}>
        <TextInput style={[styles.input, { flex: 1 }]} value={code} onChangeText={setCode} placeholder="VD: HD260816-ABC123" />
        <Pressable style={styles.btn} onPress={lookup} disabled={loading}>
          <Text style={styles.btnText}>{loading ? "..." : "Tra"}</Text>
        </Pressable>
      </View>
      {error ? <Text style={styles.error}>{error}</Text> : null}

      {order && (
        <View style={styles.card}>
          <Text style={styles.orderNo}>{order.orderNumber}</Text>
          <Text>Trạng thái: <Text style={styles.status}>{STATUS_LABELS[order.status] ?? order.status}</Text></Text>
          <Text>Khách: {order.customerName} ({order.customerPhone})</Text>
          <Text>Thanh toán: {order.paymentMethod}</Text>
          <Text>Địa chỉ: {order.shippingAddress}</Text>
          <View style={styles.divider} />
          {order.items.map((it, idx) => (
            <Text key={idx} style={styles.item}>
              {it.productName} × {it.quantity} — {vnd(it.unitPrice * it.quantity)}
            </Text>
          ))}
          <View style={styles.divider} />
          <Text style={styles.total}>Tổng: {vnd(order.totalAmount)}</Text>
        </View>
      )}
    </ScrollView>
  );
}

const styles = StyleSheet.create({
  container: { padding: 16, backgroundColor: "#fff" },
  label: { fontSize: 13, fontWeight: "600", color: "#555", marginBottom: 6 },
  row: { flexDirection: "row", gap: 8 },
  input: { borderWidth: 1, borderColor: "#ddd", borderRadius: 8, padding: 12, fontSize: 15 },
  btn: { backgroundColor: "#c9372c", borderRadius: 8, paddingHorizontal: 18, justifyContent: "center" },
  btnText: { color: "#fff", fontWeight: "700" },
  error: { marginTop: 10, color: "#c9372c" },
  card: { marginTop: 16, backgroundColor: "#fafafa", borderRadius: 10, padding: 14, gap: 6 },
  orderNo: { fontSize: 17, fontWeight: "700", color: "#222" },
  status: { color: "#c9372c", fontWeight: "700" },
  divider: { height: 1, backgroundColor: "#eee", marginVertical: 6 },
  item: { fontSize: 13, color: "#333" },
  total: { fontWeight: "700", fontSize: 16 },
});
