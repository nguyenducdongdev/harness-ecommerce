import { useEffect, useState } from "react";
import { Alert, Image, Pressable, ScrollView, StyleSheet, Text, View } from "react-native";
import { useLocalSearchParams } from "expo-router";
import { apiGet, type PagedResult } from "@/lib/api";
import { useCart } from "@/store/cart";
import { vnd } from "@/lib/format";
import type { Review } from "@/lib/types";

interface ProductDetail {
  id: number;
  name: string;
  slug: string;
  categoryName: string | null;
  brandName: string | null;
  price: number;
  salePrice: number | null;
  displayPrice: number;
  imageUrls: string[];
  shortDescription: string | null;
  description: string | null;
}

interface RatingSummary {
  averageRating: number;
  totalCount: number;
}

export default function ProductDetailScreen() {
  const { slug } = useLocalSearchParams<{ slug: string }>();
  const add = useCart((s) => s.add);
  const [product, setProduct] = useState<ProductDetail | null>(null);
  const [rating, setRating] = useState<RatingSummary | null>(null);
  const [reviews, setReviews] = useState<Review[]>([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    if (!slug) return;
    setLoading(true);
    apiGet<ProductDetail>(`/api/v1/products/${slug}`)
      .then((p) => {
        setProduct(p);
        apiGet<RatingSummary>(`/api/v1/reviews/product/${p.id}/rating`).then(setRating).catch(() => setRating(null));
        apiGet<PagedResult<Review>>(`/api/v1/reviews/product/${p.id}?page=1`).then((r) => setReviews(r.items)).catch(() => setReviews([]));
      })
      .catch(() => setProduct(null))
      .finally(() => setLoading(false));
  }, [slug]);

  function handleAdd() {
    if (!product) return;
    add({ productId: product.id, name: product.name, slug: product.slug, price: product.displayPrice, imageUrl: product.imageUrls?.[0] ?? null });
    Alert.alert("Giỏ hàng", "Đã thêm sản phẩm vào giỏ.");
  }

  if (loading) return <View style={styles.center}><Text>Đang tải...</Text></View>;
  if (!product) return <View style={styles.center}><Text>Không tìm thấy sản phẩm.</Text></View>;

  const cover = product.imageUrls?.[0] ?? null;

  return (
    <View style={{ flex: 1 }}>
      <ScrollView contentContainerStyle={styles.container}>
        {cover ? (
          <Image source={{ uri: cover }} style={styles.image} resizeMode="cover" />
        ) : (
          <View style={[styles.image, { backgroundColor: "#eee" }]} />
        )}
        <Text style={styles.name}>{product.name}</Text>
        <Text style={styles.meta}>{[product.brandName, product.categoryName].filter(Boolean).join(" • ")}</Text>
        <View style={styles.priceRow}>
          <Text style={styles.price}>{vnd(product.displayPrice)}</Text>
          {product.salePrice != null && <Text style={styles.oldPrice}>{vnd(product.price)}</Text>}
        </View>
        {rating && rating.totalCount > 0 && (
          <Text style={styles.rating}>★ {rating.averageRating} ({rating.totalCount} đánh giá)</Text>
        )}
        {product.shortDescription ? <Text style={styles.desc}>{product.shortDescription}</Text> : null}
        {product.description ? <Text style={styles.desc}>{product.description}</Text> : null}

        <Text style={styles.sectionTitle}>Đánh giá</Text>
        {reviews.length === 0 ? (
          <Text style={styles.emptyReview}>Chưa có đánh giá nào.</Text>
        ) : (
          reviews.map((r) => (
            <View key={r.id} style={styles.reviewCard}>
              <Text style={styles.reviewHead}>
                {r.customerName} — {"★".repeat(r.rating)}
              </Text>
              <Text style={styles.reviewContent}>{r.content}</Text>
            </View>
          ))
        )}
      </ScrollView>

      <Pressable style={styles.addBtn} onPress={handleAdd}>
        <Text style={styles.addText}>Thêm vào giỏ — {vnd(product.displayPrice)}</Text>
      </Pressable>
    </View>
  );
}

const styles = StyleSheet.create({
  container: { padding: 16, backgroundColor: "#fff" },
  center: { flex: 1, alignItems: "center", justifyContent: "center" },
  image: { width: "100%", height: 260, borderRadius: 12, backgroundColor: "#f0f0f0" },
  name: { fontSize: 20, fontWeight: "700", color: "#222", marginTop: 14 },
  meta: { color: "#888", marginTop: 4 },
  priceRow: { flexDirection: "row", alignItems: "center", gap: 10, marginTop: 10 },
  price: { fontSize: 22, fontWeight: "800", color: "#c9372c" },
  oldPrice: { fontSize: 14, color: "#999", textDecorationLine: "line-through" },
  rating: { marginTop: 8, color: "#f59e0b", fontWeight: "600" },
  desc: { marginTop: 12, color: "#444", lineHeight: 22 },
  sectionTitle: { fontSize: 16, fontWeight: "700", color: "#222", marginTop: 20, marginBottom: 8 },
  emptyReview: { color: "#999" },
  reviewCard: { backgroundColor: "#fafafa", borderRadius: 10, padding: 12, marginBottom: 8 },
  reviewHead: { fontWeight: "600", color: "#333" },
  reviewContent: { marginTop: 4, color: "#555" },
  addBtn: { backgroundColor: "#c9372c", margin: 12, borderRadius: 10, padding: 16, alignItems: "center" },
  addText: { color: "#fff", fontWeight: "700", fontSize: 16 },
});
