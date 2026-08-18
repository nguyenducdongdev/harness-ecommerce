import { Image, Pressable, StyleSheet, Text, View } from "react-native";
import { useRouter } from "expo-router";
import type { Product } from "@/lib/types";
import { vnd } from "@/lib/format";

export default function ProductCard({ product }: { product: Product }) {
  const router = useRouter();
  const cover = product.imageUrls?.[0] ?? null;
  return (
    <Pressable style={styles.card} onPress={() => router.push(`/products/${product.slug}`)}>
      {cover ? (
        <Image source={{ uri: cover }} style={styles.image} resizeMode="cover" />
      ) : (
        <View style={[styles.image, styles.placeholder]} />
      )}
      <View style={styles.body}>
        <Text numberOfLines={2} style={styles.name}>
          {product.name}
        </Text>
        <View style={styles.priceRow}>
          <Text style={styles.price}>{vnd(product.displayPrice)}</Text>
          {product.salePrice != null && (
            <Text style={styles.oldPrice}>{vnd(product.price)}</Text>
          )}
        </View>
      </View>
    </Pressable>
  );
}

const styles = StyleSheet.create({
  card: { width: 150, margin: 6, backgroundColor: "#fff", borderRadius: 12, overflow: "hidden", elevation: 2 },
  image: { width: "100%", height: 130, backgroundColor: "#f0f0f0" },
  placeholder: { backgroundColor: "#e8e8e8" },
  body: { padding: 8 },
  name: { fontSize: 13, fontWeight: "600", color: "#222", minHeight: 34 },
  priceRow: { flexDirection: "row", alignItems: "center", gap: 6, marginTop: 4 },
  price: { fontSize: 14, fontWeight: "700", color: "#c9372c" },
  oldPrice: { fontSize: 11, color: "#999", textDecorationLine: "line-through" },
});
