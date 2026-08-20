import React, { useState, useEffect, useRef } from "react";
import { Card, Input, Button, Tag, message as antMessage } from "antd";
import { UserOutlined, SendOutlined, CheckCircleOutlined, CloseCircleOutlined, SyncOutlined } from "@ant-design/icons";
import * as signalR from "@microsoft/signalr";

interface Message {
  id: string;
  senderType: string;
  senderName: string;
  messageText: string;
}

interface Session {
  id: string;
  customerName: string;
  customerPhone: string;
  status: string;
  messages: Message[];
}

const QUICK = [
  "Dạ chào anh/chị, em có thể hỗ trợ gì ạ?",
  "Bộ sofa này làm bằng gỗ sồi Nga tự nhiên ạ.",
  "Bên em miễn phí vận chuyển và lắp đặt tận nhà ạ.",
];

export default function SupportChat() {
  const [sessions, setSessions] = useState<Session[]>([]);
  const [selectedId, setSelectedId] = useState<string | null>(null);
  const [inputText, setInputText] = useState("");
  const [connStatus, setConnStatus] = useState<"Disconnected" | "Connecting" | "Connected" | "Reconnecting">("Disconnected");

  const hubRef = useRef<signalR.HubConnection | null>(null);
  const API_BASE = "http://localhost:5000/api/v1";
  const HUB_URL = "http://localhost:5000/hubs/chat";

  const fetchSessions = async () => {
    try {
      const res = await fetch(`${API_BASE}/support/chat/sessions`);
      if (res.ok) {
        const json = await res.json();
        const list: Session[] = json.data || json;
        setSessions(list);
        if (list.length > 0 && !selectedId) setSelectedId(list[0].id);
      }
    } catch {
      if (sessions.length === 0) {
        setSessions([
          {
            id: "demo-1",
            customerName: "Nguyễn Văn Anh",
            customerPhone: "0901234567",
            status: "Active",
            messages: [{ id: "m1", senderType: "Customer", senderName: "Nguyễn Văn Anh", messageText: "Sofa gỗ sồi có sẵn giao trong ngày không shop?" }],
          },
        ]);
        if (!selectedId) setSelectedId("demo-1");
      }
    }
  };

  useEffect(() => {
    fetchSessions();
    const interval = setInterval(fetchSessions, 5000);
    return () => clearInterval(interval);
  }, []);

  // SignalR Auto-reconnect continuous retry loop
  useEffect(() => {
    let isMounted = true;
    let timer: any = null;

    const startConn = async () => {
      if (hubRef.current) {
        try { await hubRef.current.stop(); } catch {}
      }
      const conn = new signalR.HubConnectionBuilder()
        .withUrl(HUB_URL)
        .withAutomaticReconnect({
          nextRetryDelayInMilliseconds: () => 3000
        })
        .configureLogging(signalR.LogLevel.Warning)
        .build();

      conn.onreconnecting(() => isMounted && setConnStatus("Reconnecting"));
      conn.onreconnected(() => isMounted && setConnStatus("Connected"));
      conn.onclose(() => {
        if (isMounted) {
          setConnStatus("Disconnected");
          timer = setTimeout(startConn, 3000);
        }
      });

      conn.on("ReceiveMessage", () => {
        if (isMounted) fetchSessions();
      });

      try {
        setConnStatus("Connecting");
        await conn.start();
        if (isMounted) setConnStatus("Connected");
        hubRef.current = conn;
      } catch {
        if (isMounted) {
          setConnStatus("Disconnected");
          timer = setTimeout(startConn, 3000);
        }
      }
    };

    startConn();
    return () => {
      isMounted = false;
      if (timer) clearTimeout(timer);
      if (hubRef.current) {
        hubRef.current.stop();
        hubRef.current = null;
      }
    };
  }, []);

  const selectedSession = sessions.find((s) => s.id === selectedId);

  const handleSend = async (contentToSend?: string) => {
    const text = contentToSend || inputText;
    if (!text.trim() || !selectedId) return;

    const msgText = text.trim();
    if (!contentToSend) setInputText("");

    if (hubRef.current && connStatus === "Connected") {
      try {
        await hubRef.current.invoke("SendMessage", selectedId, "Agent", "Tư Vấn Viên", msgText, null);
        fetchSessions();
        return;
      } catch {}
    }

    try {
      const res = await fetch(`${API_BASE}/support/chat/sessions/${selectedId}/messages`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ senderType: "Agent", senderName: "Tư Vấn Viên", messageText: msgText }),
      });
      if (res.ok) fetchSessions();
    } catch {
      setSessions((prev) =>
        prev.map((s) =>
          s.id === selectedId
            ? {
                ...s,
                messages: [
                  ...s.messages,
                  { id: "m-" + Date.now(), senderType: "Agent", senderName: "Tư Vấn Viên", messageText: msgText },
                ],
              }
            : s
        )
      );
    }
  };

  const getTagStatus = () => {
    switch (connStatus) {
  return (
    <div className="p-6">
      <div className="flex justify-between items-center mb-6">
        <div>
          <h1 className="text-2xl font-bold text-gray-800">Hỗ Trợ Khách Hàng (Live Support Chat)</h1>
          <p className="text-gray-500 text-sm">Quản lý các phiên tư vấn trực tuyến real-time với khách hàng</p>
        </div>
        <div>{getTagStatus()}</div>
      </div>

      <div className="grid grid-cols-1 md:grid-cols-3 gap-6 h-[680px]">
        {/* Danh sách phiên chat */}
        <Card title="Danh sách phiên chat" className="md:col-span-1 flex flex-col h-full overflow-hidden">
          <div className="overflow-y-auto max-h-[580px] space-y-2 pr-1">
            {sessions.map((s) => (
              <div
                key={s.id}
                onClick={() => setSelectedId(s.id)}
                className={`p-3 rounded-lg border cursor-pointer transition ${
                  selectedId === s.id ? "bg-amber-50 border-amber-500 shadow-sm" : "hover:bg-gray-50 border-gray-200"
                }`}
              >
                <div className="flex justify-between items-start">
                  <div className="font-semibold text-gray-800 flex items-center gap-1.5">
                    <UserOutlined className="text-amber-600" />
                    {s.customerName}
                  </div>
                  <Tag color={s.status === "Active" ? "green" : "default"} className="mr-0 text-[11px]">
                    {s.status}
                  </Tag>
                </div>
                <div className="text-xs text-gray-500 mt-1">SĐT: {s.customerPhone || "N/A"}</div>
                <div className="text-xs text-gray-600 mt-2 truncate italic bg-white p-1.5 rounded border border-gray-100">
                  {s.messages && s.messages.length > 0 ? s.messages[s.messages.length - 1].messageText : "Chưa có tin nhắn"}
                </div>
              </div>
            ))}
          </div>
        </Card>

        {/* Nội dung hội thoại */}
        <Card title={selectedSession ? `Trò chuyện với: ${selectedSession.customerName}` : "Nội dung hội thoại"} className="md:col-span-2 flex flex-col h-full">
          {selectedSession ? (
            <div className="flex flex-col h-[580px] justify-between">
              {/* Messages */}
              <div className="flex-1 overflow-y-auto p-4 bg-gray-50 rounded-lg space-y-3 mb-3 border">
                {selectedSession.messages.map((m, idx) => (
                  <div key={m.id || idx} className={`flex flex-col ${m.senderType === "Agent" ? "items-end" : "items-start"}`}>
                    <span className="text-[11px] text-gray-400 mb-0.5">{m.senderName || m.senderType}</span>
                    <div
                      className={`max-w-[75%] rounded-2xl px-4 py-2 text-sm shadow-sm ${
                        m.senderType === "Agent"
                          ? "bg-amber-600 text-white rounded-br-none"
                          : m.senderType === "System"
                          ? "bg-gray-200 text-gray-700 rounded-bl-none text-xs italic text-center w-full max-w-full"
                          : "bg-white text-gray-800 border rounded-bl-none"
                      }`}
                    >
                      {m.messageText}
                    </div>
                  </div>
                ))}
              </div>

              {/* Quick Replies */}
              <div className="flex flex-wrap gap-2 mb-3">
                <span className="text-xs text-gray-500 font-medium py-1">Trả lời nhanh:</span>
                {QUICK.map((q, idx) => (
                  <button
                    key={idx}
                    onClick={() => handleSend(q)}
                    className="text-xs bg-amber-50 text-amber-800 border border-amber-200 hover:bg-amber-100 px-2.5 py-1 rounded-full transition"
                  >
                    {q}
                  </button>
                ))}
              </div>

              {/* Input box */}
              <div className="flex gap-2">
                <Input
                  value={inputText}
                  onChange={(e) => setInputText(e.target.value)}
                  onPressEnter={() => handleSend()}
                  placeholder="Nhập tin nhắn trả lời..."
                  size="large"
                />
                <Button type="primary" icon={<SendOutlined />} onClick={() => handleSend()} size="large" className="bg-amber-600 hover:bg-amber-500">
                  Gửi
                </Button>
              </div>
            </div>
          ) : (
            <div className="flex items-center justify-center h-full text-gray-400">Chọn một phiên chat bên trái để bắt đầu</div>
          )}
        </Card>
      </div>
    </div>
  );
}

      case "Connected":
        return <Tag icon={<CheckCircleOutlined />} color="success">Trực tuyến (Live Hub)</Tag>;
      case "Connecting":
        return <Tag icon={<SyncOutlined spin />} color="processing">Đang kết nối...</Tag>;
      case "Reconnecting":
        return <Tag icon={<SyncOutlined spin />} color="warning">Đang tự động kết nối lại...</Tag>;
      default:
        return <Tag icon={<CloseCircleOutlined />} color="error">Ngoại tuyến (Đang retry...)</Tag>;
    }
  };
