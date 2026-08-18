import { Image, Pressable, ScrollView, StyleSheet, Text, View } from "react-native";
import { useRouter } from "expo-router";
import { cartTotal, useCart } from "@/store/cart";
import { vnd } from "@/lib/format";

export default function CartScreen() {
  const router = useRouter();
  const { items, remove, setQuantity, clear } = useCart();
  const total = cartTotal(items);

  return (
    <View style={styles.container}>
      {items.length === 0 ? (
        <View style={styles.empty}>
          <Text style={styles.emptyText}>Giỏ hàng trống</Text>
          <Pressable style={styles.btn} onPress={() => router.push("/")}>
            <Text style={styles.btnText}>Mua sắm ngay</Text>
          </Pressable>
        </View>
      ) : (
        <>
          <ScrollView style={{ flex: 1 }}>
            {items.map((item) => (
              <View key={item.productId} style={styles.row}>
                {item.imageUrl ? (
                  <Image source={{ uri: item.imageUrl }} style={styles.thumb} resizeMode="cover" />
                ) : (
                  <View style={[styles.thumb, { backgroundColor: "#eee" }]} />
                )}
                <View style={{ flex: 1, marginLeft: 10 }}>
                  <Text numberOfLines={2} style={styles.name}>{item.name}</Text>
                  <Text style={styles.price}>{vnd(item.price)}</Text>
                  <View style={styles.qtyRow}>
                    <Pressable style={styles.qtyBtn} onPress={() => setQuantity(item.productId, item.quantity - 1)}>
                      <Text>−</Text>
                    </Pressable>
                    <Text style={styles.qty}>{item.quantity}</Text>
                    <Pressable style={styles.qtyBtn} onPress={() => setQuantity(item.productId, item.quantity + 1)}>
                      <Text>+</Text>
                    </Pressable>
                    <Pressable onPress={() => remove(item.productId)} style={{ marginLeft: 12 }}>
                      <Text style={styles.remove}>Xóa</Text>
                    </Pressable>
                  </View>
                </View>
              </View>
            ))}
          </ScrollView>
          <View style={styles.footer}>
            <Text style={styles.total}>Tổng: {vnd(total)}</Text>
            <Pressable style={styles.checkoutBtn} onPress={() => router.push("/checkout")}>
              <Text style={styles.btnText}>Thanh toán</Text>
            </Pressable>
          </View>
        </>
      )}
    </View>
  );
}

const styles = StyleSheet.create({
  container: { flex: 1, backgroundColor: "#f7f7f7", padding: 12 },
  empty: { flex: 1, alignItems: "center", justifyContent: "center", gap: 16 },
  emptyText: { fontSize: 16, color: "#888" },
  row: { flexDirection: "row", backgroundColor: "#fff", borderRadius: 10, padding: 10, marginBottom: 10 },
  thumb: { width: 70, height: 70, borderRadius: 8, backgroundColor: "#f0f0f0" },
  name: { fontSize: 13, fontWeight: "600", color: "#222" },
  price: { fontSize: 13, color: "#c9372c", fontWeight: "700", marginTop: 4 },
  qtyRow: { flexDirection: "row", alignItems: "center", marginTop: 8, gap: 8 },
  qtyBtn: { borderWidth: 1, borderColor: "#ddd", borderRadius: 6, width: 26, height: 26, alignItems: "center", justifyContent: "center" },
  qty: { minWidth: 24, textAlign: "center", fontWeight: "600" },
  remove: { color: "#c9372c", fontSize: 13 },
  footer: { flexDirection: "row", justifyContent: "space-between", alignItems: "center", padding: 12, backgroundColor: "#fff", borderRadius: 10 },
  total: { fontSize: 16, fontWeight: "700", color: "#222" },
  checkoutBtn: { backgroundColor: "#c9372c", paddingHorizontal: 24, paddingVertical: 12, borderRadius: 8 },
  btn: { backgroundColor: "#c9372c", paddingHorizontal: 24, paddingVertical: 12, borderRadius: 8 },
  btnText: { color: "#fff", fontWeight: "700" },
});
