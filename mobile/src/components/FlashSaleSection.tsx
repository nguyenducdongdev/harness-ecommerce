import { Image, ScrollView, StyleSheet, Text, View } from "react-native";
import type { FlashSale } from "@/lib/types";
import { vnd } from "@/lib/format";

export default function FlashSaleSection({ sale }: { sale: FlashSale }) {
  return (
    <View style={styles.container}>
      <Text style={styles.title}>⚡ {sale.name}</Text>
      <ScrollView horizontal showsHorizontalScrollIndicator={false}>
        {sale.items.map((item) => (
          <View key={item.id} style={styles.item}>
            {item.imageUrl ? (
              <Image source={{ uri: item.imageUrl }} style={styles.image} resizeMode="cover" />
            ) : (
              <View style={[styles.image, { backgroundColor: "#eee" }]} />
            )}
            <View style={styles.info}>
              <Text numberOfLines={1} style={styles.name}>
                {item.productName}
              </Text>
              <Text style={styles.salePrice}>{vnd(item.salePrice)}</Text>
              <Text style={styles.sold}>
                Đã bán {item.quantitySold}/{item.quantityLimit}
              </Text>
            </View>
          </View>
        ))}
      </ScrollView>
    </View>
  );
}

const styles = StyleSheet.create({
  container: { marginVertical: 8 },
  title: { fontSize: 16, fontWeight: "700", color: "#c9372c", marginHorizontal: 12, marginVertical: 6 },
  item: { width: 140, marginHorizontal: 6, backgroundColor: "#fff", borderRadius: 10, overflow: "hidden", elevation: 2 },
  image: { width: "100%", height: 110, backgroundColor: "#f0f0f0" },
  info: { padding: 6 },
  name: { fontSize: 12, fontWeight: "600", color: "#222" },
  salePrice: { fontSize: 13, fontWeight: "700", color: "#c9372c", marginTop: 2 },
  sold: { fontSize: 10, color: "#888", marginTop: 2 },
});
