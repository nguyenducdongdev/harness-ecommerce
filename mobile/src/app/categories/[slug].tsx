import { useEffect, useState } from "react";
import { FlatList, StyleSheet, Text, View } from "react-native";
import { useLocalSearchParams } from "expo-router";
import { apiGet, type PagedResult } from "@/lib/api";
import type { Product } from "@/lib/types";
import ProductCard from "@/components/ProductCard";

export default function CategoryScreen() {
  const { slug } = useLocalSearchParams<{ slug: string }>();
  const [products, setProducts] = useState<Product[]>([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    if (!slug) return;
    setLoading(true);
    apiGet<PagedResult<Product>>(`/api/v1/products?categorySlug=${encodeURIComponent(slug)}&pageSize=20`)
      .then((r) => setProducts(r.items))
      .catch(() => setProducts([]))
      .finally(() => setLoading(false));
  }, [slug]);

  return (
    <View style={styles.container}>
      {loading ? (
        <Text style={styles.hint}>Đang tải...</Text>
      ) : products.length === 0 ? (
        <Text style={styles.hint}>Chưa có sản phẩm trong danh mục này.</Text>
      ) : (
        <FlatList
          data={products}
          numColumns={2}
          keyExtractor={(p) => String(p.id)}
          renderItem={({ item }) => <ProductCard product={item} />}
          contentContainerStyle={{ alignItems: "center" }}
        />
      )}
    </View>
  );
}

const styles = StyleSheet.create({
  container: { flex: 1, backgroundColor: "#f7f7f7", padding: 6 },
  hint: { textAlign: "center", color: "#888", marginTop: 40 },
});
