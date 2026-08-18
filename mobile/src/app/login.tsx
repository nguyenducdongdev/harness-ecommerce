import { useState } from "react";
import { Alert, Pressable, ScrollView, StyleSheet, Text, TextInput, View } from "react-native";
import { useRouter } from "expo-router";
import { apiGet, apiPost } from "@/lib/api";
import { useAuth } from "@/store/auth";
import type { CustomerProfile, OtpSession } from "@/lib/types";

export default function LoginScreen() {
  const router = useRouter();
  const signIn = useAuth((s) => s.signIn);
  const [step, setStep] = useState<1 | 2>(1);
  const [phone, setPhone] = useState("");
  const [otp, setOtp] = useState("");
  const [sandboxCode, setSandboxCode] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);

  async function requestOtp() {
    if (!/^0\d{9,10}$/.test(phone)) {
      Alert.alert("Lỗi", "Số điện thoại VN không hợp lệ.");
      return;
    }
    setLoading(true);
    try {
      const res = await apiPost<{ phone: string; otpCode: string | null; expiryMinutes: number }>(
        "/api/v1/customers/otp/request",
        { phone },
      );
      setSandboxCode(res.otpCode);
      setStep(2);
    } catch (e: any) {
      Alert.alert("Lỗi", e.message);
    } finally {
      setLoading(false);
    }
  }

  async function verifyOtp() {
    if (!otp || otp.length < 4) {
      Alert.alert("Lỗi", "Nhập mã OTP đã gửi.");
      return;
    }
    setLoading(true);
    try {
      const session = await apiPost<OtpSession>("/api/v1/customers/otp/verify", { phone, code: otp });
      let name: string | null = null;
      try {
        const me = await apiGet<CustomerProfile>("/api/v1/customers/me", session.accessToken);
        name = me.name;
      } catch {
        /* không có thì dùng phone */
      }
      signIn({ accessToken: session.accessToken, phone: session.phone, customerId: session.customerId, customerName: name });
      router.replace("/");
    } catch (e: any) {
      Alert.alert("Lỗi", e.message);
    } finally {
      setLoading(false);
    }
  }

  return (
    <ScrollView contentContainerStyle={styles.container}>
      <Text style={styles.title}>{step === 1 ? "Đăng nhập bằng số điện thoại" : "Nhập mã OTP"}</Text>

      {step === 1 ? (
        <>
          <TextInput
            style={styles.input}
            placeholder="Số điện thoại (VD: 0912345678)"
            keyboardType="phone-pad"
            value={phone}
            onChangeText={setPhone}
          />
          <Pressable style={styles.btn} onPress={requestOtp} disabled={loading}>
            <Text style={styles.btnText}>{loading ? "Đang gửi..." : "Gửi mã OTP"}</Text>
          </Pressable>
        </>
      ) : (
        <>
          {sandboxCode && (
            <View style={styles.sandbox}>
              <Text style={styles.sandboxText}>Mã sandbox: {sandboxCode}</Text>
            </View>
          )}
          <TextInput
            style={styles.input}
            placeholder="Mã OTP"
            keyboardType="numeric"
            value={otp}
            onChangeText={setOtp}
          />
          <Pressable style={styles.btn} onPress={verifyOtp} disabled={loading}>
            <Text style={styles.btnText}>{loading ? "Đang xác thực..." : "Xác thực"}</Text>
          </Pressable>
          <Pressable onPress={() => setStep(1)}>
            <Text style={styles.back}>← Đổi số điện thoại</Text>
          </Pressable>
        </>
      )}
    </ScrollView>
  );
}

const styles = StyleSheet.create({
  container: { padding: 24, backgroundColor: "#fff", flexGrow: 1 },
  title: { fontSize: 20, fontWeight: "700", color: "#222", marginBottom: 20 },
  input: {
    borderWidth: 1, borderColor: "#ddd", borderRadius: 10, padding: 14, fontSize: 16, marginBottom: 16,
  },
  btn: { backgroundColor: "#c9372c", borderRadius: 10, padding: 16, alignItems: "center" },
  btnText: { color: "#fff", fontWeight: "700", fontSize: 16 },
  back: { color: "#c9372c", marginTop: 16, textAlign: "center" },
  sandbox: { backgroundColor: "#fff7e6", borderLeftWidth: 4, borderLeftColor: "#faad14", padding: 10, marginBottom: 12, borderRadius: 6 },
  sandboxText: { color: "#874d00", fontSize: 13 },
});
