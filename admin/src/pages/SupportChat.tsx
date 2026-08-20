import { useState, useEffect } from "react";
import { Card, Input, Button, Tag } from "antd";
import { UserOutlined, SendOutlined, CheckCircleOutlined, CloseCircleOutlined, SyncOutlined } from "@ant-design/icons";

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

export default function SupportChat() {
  const [sessions, setSessions] = useState<Session[]>([]);
  const [selectedId, setSelectedId] = useState<string | null>(null);
  const [inputText, setInputText] = useState("");
  const [connStatus] = useState<"Disconnected" | "Connecting" | "Connected" | "Reconnecting">("Disconnected");

  const fetchSessions = async () => {
    try {
      const res = await fetch("http://localhost:5000/api/v1/support/chat/sessions");
      if (res.ok) {
        const json = await res.json();
        const list: Session[] = json.data || json;
        setSessions(list);
        if (list.length > 0 && !selectedId) setSelectedId(list[0].id);
      }
    } catch {
      // ignore
    }
  };

  useEffect(() => {
    fetchSessions();
  }, []);

  const selectedSession = sessions.find((s) => s.id === selectedId);

  const getTagStatus = () => {
    switch (connStatus) {
      case "Connected":
        return <Tag icon={<CheckCircleOutlined />} color="success">Trực tuyến</Tag>;
      case "Connecting":
        return <Tag icon={<SyncOutlined spin />} color="processing">Đang kết nối...</Tag>;
      case "Reconnecting":
        return <Tag icon={<SyncOutlined spin />} color="warning">Đang kết nối lại...</Tag>;
      default:
        return <Tag icon={<CloseCircleOutlined />} color="error">Ngoại tuyến</Tag>;
    }
  };

  return (
    <div className="p-6">
      <div className="flex justify-between items-center mb-6">
        <div>
          <h1 className="text-2xl font-bold text-gray-800">Hỗ Trợ Khách Hàng (Live Support Chat)</h1>
        </div>
        <div>{getTagStatus()}</div>
      </div>

      <div className="grid grid-cols-1 md:grid-cols-3 gap-6 h-[680px]">
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
                  <Tag color={s.status === "Active" ? "green" : "default"}>{s.status}</Tag>
                </div>
              </div>
            ))}
          </div>
        </Card>

        <Card title="Khung chat" className="md:col-span-2 flex flex-col h-full">
          {selectedSession ? (
            <div className="flex flex-col justify-between h-[580px]">
              <div className="overflow-y-auto p-4 space-y-3 flex-1 bg-gray-50 rounded-lg">
                {selectedSession.messages?.map((m) => (
                  <div key={m.id} className={`flex flex-col ${m.senderType === "Agent" ? "items-end" : "items-start"}`}>
                    <span className="text-xs text-gray-400 mb-1">{m.senderName}</span>
                    <div className={`max-w-[75%] px-4 py-2 rounded-2xl text-sm ${m.senderType === "Agent" ? "bg-amber-500 text-white" : "bg-white text-gray-800 border"}`}>
                      {m.messageText}
                    </div>
                  </div>
                ))}
              </div>

              <div className="pt-3 border-t">
                <div className="flex gap-2">
                  <Input placeholder="Nhập tin nhắn..." value={inputText} onChange={(e) => setInputText(e.target.value)} />
                  <Button type="primary" icon={<SendOutlined />}>
                    Gửi
                  </Button>
                </div>
              </div>
            </div>
          ) : (
            <div className="flex items-center justify-center h-full text-gray-400">Chọn một phiên chat bên trái</div>
          )}
        </Card>
      </div>
    </div>
  );
}
