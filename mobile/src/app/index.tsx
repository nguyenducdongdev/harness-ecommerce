import { useEffect, useState } from "react";
import {
  FlatList,
  Image,
  Pressable,
  ScrollView,
  StyleSheet,
  Text,
  View,
} from "react-native";
import { useRouter } from "expo-router";
import { apiGet, type PagedResult } from "@/lib/api";
import type { Banner, Category, FlashSale, Product } from "@/lib/types";
import FlashSaleSection from "@/components/FlashSaleSection";
import ProductCard from "@/components/ProductCard";
import { useAuth } from "@/store/auth";
import { useCart, cartCount } from "@/store/cart";

export default function HomeScreen() {
  const router = useRouter();
  const [banners, setBanners] = useState<Banner[]>([]);
  const [categories, setCategories] = useState<Category[]>([]);
  const [flashSale, setFlashSale] = useState<FlashSale | null>(null);
  const [products, setProducts] = useState<Product[]>([]);
  const { accessToken, phone, signOut } = useAuth();
  const items = useCart((s) => s.items);

  useEffect(() => {
    apiGet<Banner[]>("/api/v1/banners?position=home-hero").then(setBanners).catch(() => setBanners([]));
    apiGet<Category[]>("/api/v1/categories").then(setCategories).catch(() => setCategories([]));
    apiGet<FlashSale[]>("/api/v1/flash-sales/active")
      .then((fs) => setFlashSale(fs[0] ?? null))
      .catch(() => setFlashSale(null));
    apiGet<PagedResult<Product>>("/api/v1/products?pageSize=8")
      .then((r) => setProducts(r.items))
      .catch(() => setProducts([]));
  }, []);

  const quickLinks = [
    { label: "🛒 Giỏ hàng", path: "/cart", badge: cartCount(items) },
    { label: "📦 Tra đơn", path: "/track", badge: 0 },
    { label: "📅 Đặt lịch", path: "/booking", badge: 0 },
    { label: "⭐ Tích điểm", path: "/membership", badge: 0 },
  ];

  return (
    <ScrollView style={styles.container}>
      {/* Banner */}
      {banners.length > 0 && (
        <ScrollView horizontal showsHorizontalScrollIndicator={false} style={styles.bannerWrap}>
          {banners.map((b) => (
            <Image key={b.id} source={{ uri: b.imageUrl }} style={styles.banner} resizeMode="cover" />
          ))}
        </ScrollView>
      )}

      {/* Quick links */}
      <View style={styles.quickRow}>
        {quickLinks.map((q) => (
          <Pressable key={q.path} style={styles.quickItem} onPress={() => router.push(q.path as never)}>
            <Text style={styles.quickLabel}>{q.label}</Text>
            {q.badge > 0 && <View style={styles.badge}><Text style={styles.badgeText}>{q.badge}</Text></View>}
          </Pressable>
        ))}
      </View>

      {/* Auth */}
      <View style={styles.authRow}>
        {accessToken ? (
          <>
            <Text style={styles.authText}>👋 {phone}</Text>
            <Pressable onPress={signOut}>
              <Text style={styles.authLink}>Đăng xuất</Text>
            </Pressable>
          </>
        ) : (
          <Pressable style={styles.loginBtn} onPress={() => router.push("/login")}>
            <Text style={styles.loginBtnText}>Đăng nhập / Đăng ký</Text>
          </Pressable>
        )}
      </View>

      {/* Danh mục */}
      <ScrollView horizontal showsHorizontalScrollIndicator={false} style={styles.catWrap}>
        {categories.map((c) => (
          <Pressable key={c.id} style={styles.catChip} onPress={() => router.push(`/categories/${c.slug}` as never)}>
            <Text style={styles.catText}>{c.name}</Text>
          </Pressable>
        ))}
      </ScrollView>

      {/* Flash sale */}
      {flashSale && <FlashSaleSection sale={flashSale} />}

      {/* Sản phẩm */}
      <Text style={styles.sectionTitle}>Sản phẩm nổi bật</Text>
      <FlatList
        data={products}
        numColumns={2}
        scrollEnabled={false}
        keyExtractor={(p) => String(p.id)}
        renderItem={({ item }) => <ProductCard product={item} />}
        contentContainerStyle={{ alignItems: "center" }}
      />
    </ScrollView>
  );
}

const styles = StyleSheet.create({
  container: { flex: 1, backgroundColor: "#f7f7f7" },
  bannerWrap: { maxHeight: 180 },
  banner: { width: 320, height: 180, margin: 6, borderRadius: 12 },
  quickRow: { flexDirection: "row", flexWrap: "wrap", justifyContent: "space-around", padding: 12 },
  quickItem: {
    backgroundColor: "#fff",
    paddingHorizontal: 14,
    paddingVertical: 10,
    borderRadius: 10,
    margin: 4,
    minWidth: 140,
    alignItems: "center",
    elevation: 1,
    position: "relative",
  },
  quickLabel: { fontSize: 13, fontWeight: "600", color: "#333" },
  badge: {
    position: "absolute", top: -4, right: 6, backgroundColor: "#c9372c",
    borderRadius: 10, minWidth: 18, height: 18, alignItems: "center", justifyContent: "center",
  },
  badgeText: { color: "#fff", fontSize: 11, fontWeight: "700" },
  authRow: { flexDirection: "row", justifyContent: "space-between", alignItems: "center", paddingHorizontal: 16, paddingVertical: 8 },
  authText: { fontSize: 14, fontWeight: "600", color: "#333" },
  authLink: { color: "#c9372c", fontWeight: "600" },
  loginBtn: { backgroundColor: "#c9372c", paddingHorizontal: 20, paddingVertical: 10, borderRadius: 8, alignSelf: "center" },
  loginBtnText: { color: "#fff", fontWeight: "700" },
  sectionTitle: { fontSize: 16, fontWeight: "700", color: "#222", marginHorizontal: 12, marginTop: 12 },
  catWrap: { marginTop: 8 },
  catChip: { backgroundColor: "#fff", paddingHorizontal: 14, paddingVertical: 8, borderRadius: 18, marginHorizontal: 6, borderWidth: 1, borderColor: "#eee" },
  catText: { fontSize: 13, color: "#333", fontWeight: "600" },
});
