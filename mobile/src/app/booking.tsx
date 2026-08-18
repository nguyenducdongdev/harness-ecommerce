import { useState } from "react";
import { Alert, Pressable, ScrollView, StyleSheet, Text, TextInput, View } from "react-native";
import { useRouter } from "expo-router";
import { apiPost } from "@/lib/api";
import { useAuth } from "@/store/auth";

export default function BookingScreen() {
  const router = useRouter();
  const { phone: authPhone, customerName } = useAuth();
  const [type, setType] = useState<"Installation" | "Measurement">("Installation");
  const [name, setName] = useState(customerName ?? "");
  const [phone, setPhone] = useState(authPhone ?? "");
  const [address, setAddress] = useState("");
  const [date, setDate] = useState("");
  const [slot, setSlot] = useState("buoi-sang");
  const [note, setNote] = useState("");
  const [submitting, setSubmitting] = useState(false);

  async function submit() {
    if (!/^0\d{9,10}$/.test(phone)) {
      Alert.alert("Lỗi", "Số điện thoại không hợp lệ.");
      return;
    }
    if (!name.trim() || !address.trim() || !date) {
      Alert.alert("Lỗi", "Nhập đủ họ tên, địa chỉ và ngày hẹn.");
      return;
    }
    setSubmitting(true);
    try {
      await apiPost("/api/v1/bookings", {
        customerPhone: phone,
        customerName: name,
        receiverName: name,
        receiverPhone: phone,
        address,
        appointmentType: type,
        desiredDate: date,
        timeSlot: slot,
        note: note || null,
      });
      Alert.alert("Đã tiếp nhận", "Chúng tôi sẽ liên hệ xác nhận lịch hẹn.", [
        { text: "OK", onPress: () => router.replace("/") },
      ]);
    } catch (e: any) {
      Alert.alert("Lỗi", e.message ?? "Đặt lịch thất bại.");
    } finally {
      setSubmitting(false);
    }
  }

  return (
    <ScrollView contentContainerStyle={styles.container}>
      <Text style={styles.label}>Loại dịch vụ</Text>
      <View style={styles.payRow}>
        <Pressable style={[styles.payBtn, type === "Installation" && styles.payActive]} onPress={() => setType("Installation")}>
          <Text style={[styles.payText, type === "Installation" && styles.payTextActive]}>Lắp đặt tại nhà</Text>
        </Pressable>
        <Pressable style={[styles.payBtn, type === "Measurement" && styles.payActive]} onPress={() => setType("Measurement")}>
          <Text style={[styles.payText, type === "Measurement" && styles.payTextActive]}>Đo đạc riêng</Text>
        </Pressable>
      </View>

      <Text style={styles.label}>Họ tên *</Text>
      <TextInput style={styles.input} value={name} onChangeText={setName} placeholder="Nguyễn Văn A" />
      <Text style={styles.label}>Số điện thoại *</Text>
      <TextInput style={styles.input} value={phone} onChangeText={setPhone} keyboardType="phone-pad" placeholder="0912345678" />
      <Text style={styles.label}>Địa chỉ *</Text>
      <TextInput style={styles.input} value={address} onChangeText={setAddress} placeholder="123 Lê Lợi, Q1, TP.HCM" />
      <Text style={styles.label}>Ngày hẹn (yyyy-mm-dd) *</Text>
      <TextInput style={styles.input} value={date} onChangeText={setDate} placeholder="2026-09-01" />
      <Text style={styles.label}>Khung giờ</Text>
      <View style={styles.payRow}>
        <Pressable style={[styles.payBtn, slot === "buoi-sang" && styles.payActive]} onPress={() => setSlot("buoi-sang")}>
          <Text style={[styles.payText, slot === "buoi-sang" && styles.payTextActive]}>Buổi sáng</Text>
        </Pressable>
        <Pressable style={[styles.payBtn, slot === "buoi-chieu" && styles.payActive]} onPress={() => setSlot("buoi-chieu")}>
          <Text style={[styles.payText, slot === "buoi-chieu" && styles.payTextActive]}>Buổi chiều</Text>
        </Pressable>
      </View>
      <Text style={styles.label}>Ghi chú</Text>
      <TextInput style={styles.input} value={note} onChangeText={setNote} placeholder="VD: đo tủ bếp chữ L" multiline />

      <Pressable style={styles.submitBtn} onPress={submit} disabled={submitting}>
        <Text style={styles.submitText}>{submitting ? "Đang gửi..." : "Đặt lịch"}</Text>
      </Pressable>
    </ScrollView>
  );
}

const styles = StyleSheet.create({
  container: { padding: 16, backgroundColor: "#fff" },
  label: { fontSize: 13, fontWeight: "600", color: "#555", marginTop: 12, marginBottom: 6 },
  input: { borderWidth: 1, borderColor: "#ddd", borderRadius: 8, padding: 12, fontSize: 15 },
  payRow: { flexDirection: "row", gap: 10 },
  payBtn: { flex: 1, borderWidth: 1.5, borderColor: "#ddd", borderRadius: 8, padding: 12, alignItems: "center" },
  payActive: { borderColor: "#c9372c", backgroundColor: "#fff1f0" },
  payText: { fontWeight: "600", color: "#555", fontSize: 13 },
  payTextActive: { color: "#c9372c" },
  submitBtn: { backgroundColor: "#c9372c", borderRadius: 10, padding: 16, alignItems: "center", marginTop: 20 },
  submitText: { color: "#fff", fontWeight: "700", fontSize: 16 },
});
