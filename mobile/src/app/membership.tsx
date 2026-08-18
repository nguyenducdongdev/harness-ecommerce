import { useCallback, useState } from "react";
import { Pressable, ScrollView, StyleSheet, Text, View } from "react-native";
import { useFocusEffect, useRouter } from "expo-router";
import { apiGet, apiPost } from "@/lib/api";
import { useAuth } from "@/store/auth";
import { vnd } from "@/lib/format";

interface Loyalty {
  customerId: string;
  points: number;
  tier: string;
  lifetimeSpend: number;
}

interface Reward {
  id: number;
  name: string;
  description: string | null;
  pointsCost: number;
  value: number;
}

interface Txn {
  id: string;
  type: string;
  points: number;
  createdAt: string;
}

export default function MembershipScreen() {
  const router = useRouter();
  const { accessToken, customerId, customerName } = useAuth();
  const [loyalty, setLoyalty] = useState<Loyalty | null>(null);
  const [rewards, setRewards] = useState<Reward[]>([]);
  const [txns, setTxns] = useState<Txn[]>([]);
  const [message, setMessage] = useState("");

  const load = useCallback(() => {
    apiGet<Reward[]>("/api/v1/loyalty/rewards").then(setRewards).catch(() => setRewards([]));
    if (!customerId) return;
    apiGet<Loyalty>(`/api/v1/loyalty/${customerId}`).then(setLoyalty).catch(() => setLoyalty(null));
    apiGet<Txn[]>(`/api/v1/loyalty/${customerId}/transactions`).then(setTxns).catch(() => setTxns([]));
  }, [customerId]);

  useFocusEffect(load);

  async function redeem(rewardId: number) {
    if (!customerId) {
      router.push("/login");
      return;
    }
    try {
      const updated = await apiPost<Loyalty>("/api/v1/loyalty/redeem-reward", { customerId, rewardId });
      setLoyalty(updated);
      setMessage("Đã đổi quà thành công! 🎉");
      load();
    } catch (e: any) {
      setMessage(e.message ?? "Đổi quà thất bại.");
    }
  }

  if (!customerId) {
    return (
      <View style={styles.center}>
        <Text style={styles.emptyText}>Đăng nhập để xem điểm thành viên.</Text>
        <Pressable style={styles.btn} onPress={() => router.push("/login")}>
          <Text style={styles.btnText}>Đăng nhập</Text>
        </Pressable>
      </View>
    );
  }

  return (
    <ScrollView contentContainerStyle={styles.container}>
      <View style={styles.pointsCard}>
        <Text style={styles.points}>{loyalty?.points ?? 0}</Text>
        <Text style={styles.pointsLabel}>điểm — hạng {loyalty?.tier ?? "Đồng"}</Text>
        <Text style={styles.name}>{customerName ?? ""}</Text>
        <Text style={styles.spend}>Chi tiêu tích lũy: {vnd(loyalty?.lifetimeSpend ?? 0)}</Text>
      </View>

      {message ? <Text style={styles.message}>{message}</Text> : null}

      <Text style={styles.sectionTitle}>Kho quà</Text>
      {rewards.map((r) => {
        const affordable = (loyalty?.points ?? 0) >= r.pointsCost;
        return (
          <View key={r.id} style={styles.rewardRow}>
            <View style={{ flex: 1 }}>
              <Text style={styles.rewardName}>{r.name}</Text>
              <Text style={styles.rewardCost}>{r.pointsCost} điểm — trị giá {vnd(r.value)}</Text>
            </View>
            <Pressable
              style={[styles.redeemBtn, !affordable && styles.redeemDisabled]}
              onPress={() => redeem(r.id)}
              disabled={!affordable}
            >
              <Text style={styles.redeemText}>{affordable ? "Đổi" : "Thiếu điểm"}</Text>
            </Pressable>
          </View>
        );
      })}

      <Text style={styles.sectionTitle}>Lịch sử điểm</Text>
      {txns.slice(0, 15).map((t) => (
        <View key={t.id} style={styles.txnRow}>
          <Text style={{ flex: 1 }}>{t.type}</Text>
          <Text style={{ color: t.points >= 0 ? "#3f8600" : "#cf1322", fontWeight: "700" }}>
            {t.points >= 0 ? "+" : ""}{t.points}
          </Text>
        </View>
      ))}
    </ScrollView>
  );
}

const styles = StyleSheet.create({
  container: { padding: 16, backgroundColor: "#f7f7f7" },
  center: { flex: 1, alignItems: "center", justifyContent: "center", gap: 16 },
  emptyText: { fontSize: 15, color: "#666" },
  btn: { backgroundColor: "#c9372c", paddingHorizontal: 24, paddingVertical: 12, borderRadius: 8 },
  btnText: { color: "#fff", fontWeight: "700" },
  pointsCard: { backgroundColor: "#c9372c", borderRadius: 14, padding: 20, alignItems: "center" },
  points: { fontSize: 40, fontWeight: "800", color: "#fff" },
  pointsLabel: { color: "#ffd6d3", fontWeight: "600" },
  name: { color: "#fff", marginTop: 8, fontSize: 14 },
  spend: { color: "#ffd6d3", fontSize: 12, marginTop: 4 },
  message: { marginTop: 12, color: "#c9372c", fontWeight: "600", textAlign: "center" },
  sectionTitle: { fontSize: 16, fontWeight: "700", color: "#222", marginTop: 20, marginBottom: 8 },
  rewardRow: { flexDirection: "row", alignItems: "center", backgroundColor: "#fff", borderRadius: 10, padding: 12, marginBottom: 8 },
  rewardName: { fontWeight: "600", color: "#222" },
  rewardCost: { fontSize: 12, color: "#888", marginTop: 2 },
  redeemBtn: { backgroundColor: "#c9372c", borderRadius: 8, paddingHorizontal: 16, paddingVertical: 8 },
  redeemDisabled: { backgroundColor: "#ccc" },
  redeemText: { color: "#fff", fontWeight: "700" },
  txnRow: { flexDirection: "row", backgroundColor: "#fff", borderRadius: 8, padding: 10, marginBottom: 6 },
});
