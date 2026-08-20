"use client";
import React, { useState, useEffect, useRef } from "react";
import * as signalR from "@microsoft/signalr";

interface Message { id: string; senderType: "Customer" | "Agent" | "System"; senderName: string; messageText: string; }
interface ChatSession { id: string; customerName: string; messages: Message[]; }

export default function ChatWidget() {
  const [isOpen, setIsOpen] = useState(false);
  const [session, setSession] = useState<ChatSession | null>(null);
  const [name, setName] = useState("");
  const [phone, setPhone] = useState("");
  const [text, setText] = useState("");
  const [connectionStatus, setConnectionStatus] = useState<"Disconnected" | "Connecting" | "Connected" | "Reconnecting">("Disconnected");
  
  const messagesEndRef = useRef<HTMLDivElement>(null);
  const hubRef = useRef<signalR.HubConnection | null>(null);
  const API_BASE = process.env.NEXT_PUBLIC_API_URL || "http://localhost:5000/api/v1";
  const HUB_URL = API_BASE.replace("/api/v1", "") + "/hubs/chat";

  useEffect(() => {
    const savedId = localStorage.getItem("harness_chat_session_id");
    if (savedId) fetchMessages(savedId);
  }, []);

  useEffect(() => {
    if (isOpen) messagesEndRef.current?.scrollIntoView({ behavior: "smooth" });
  }, [session?.messages, isOpen]);

  // SignalR Auto-connect with continuous retry on disconnect
  useEffect(() => {
    if (!session?.id) return;
    let isMounted = true;
    let timer: any = null;

    const startConn = async () => {
      if (hubRef.current) { try { await hubRef.current.stop(); } catch {} }
      const conn = new signalR.HubConnectionBuilder()
        .withUrl(HUB_URL)
        .withAutomaticReconnect({
          nextRetryDelayInMilliseconds: () => 3000
        })
        .configureLogging(signalR.LogLevel.Warning)
        .build();

      conn.onreconnecting(() => isMounted && setConnectionStatus("Reconnecting"));
      conn.onreconnected(() => {
        if (isMounted) {
          setConnectionStatus("Connected");
          conn.invoke("JoinSessionGroup", session.id);
        }
      });
      conn.onclose(() => {
        if (isMounted) {
          setConnectionStatus("Disconnected");
          timer = setTimeout(startConn, 3000);
        }
      });

      conn.on("ReceiveMessage", (msg: Message) => {
        if (!isMounted) return;
        setSession((prev) => {
          if (!prev) return prev;
          if (prev.messages.some(m => m.id === msg.id || (m.messageText === msg.messageText && m.senderType === msg.senderType))) return prev;
          return { ...prev, messages: [...prev.messages, msg] };
        });
      });

      try {
        setConnectionStatus("Connecting");
        await conn.start();
        if (isMounted) {
          setConnectionStatus("Connected");
          await conn.invoke("JoinSessionGroup", session.id);
        }
        hubRef.current = conn;
      } catch {
        if (isMounted) {
          setConnectionStatus("Disconnected");
          timer = setTimeout(startConn, 3000);
        }
      }
    };

    startConn();
    return () => {
      isMounted = false;
      if (timer) clearTimeout(timer);
      if (hubRef.current) { hubRef.current.stop(); hubRef.current = null; }
    };
  }, [session?.id]);

  const fetchMessages = async (id: string) => {
    try {
      const res = await fetch(`${API_BASE}/support/chat/sessions/${id}/messages`);
      if (res.ok) {
        const json = await res.json();
        setSession((prev) => ({ id, customerName: prev?.customerName || "Khách", messages: json.data || json }));
      }
    } catch {}
  };

  const handleStart = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!name || !phone) return;
    try {
      const res = await fetch(`${API_BASE}/support/chat/sessions`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ customerName: name, customerPhone: phone }),
      });
      if (res.ok) {
        const json = await res.json();
        const data = json.data || json;
        setSession(data);
        localStorage.setItem("harness_chat_session_id", data.id);
      } else { createLocal(); }
    } catch { createLocal(); }
  };

  const createLocal = () => {
    const mockId = "session-" + Date.now();
    const newS: ChatSession = {
      id: mockId,
      customerName: name || "Khách hàng",
      messages: [{ id: "m1", senderType: "System", senderName: "Hệ Thống", messageText: `Xin chào ${name}! CSKH Harness sẽ tư vấn ngay.` }],
    };
    setSession(newS);
    localStorage.setItem("harness_chat_session_id", mockId);
  };

  const handleSend = async () => {
    if (!text.trim() || !session) return;
    const content = text.trim();
    setText("");
    const tempMsg: Message = { id: "m-" + Date.now(), senderType: "Customer", senderName: session.customerName, messageText: content };
    setSession((prev) => (prev ? { ...prev, messages: [...prev.messages, tempMsg] } : null));

    if (hubRef.current && connectionStatus === "Connected") {
      try {
        await hubRef.current.invoke("SendMessage", session.id, "Customer", session.customerName, content, null);
        return;
      } catch {}
    }

    try {
      await fetch(`${API_BASE}/support/chat/sessions/${session.id}/messages`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ senderType: "Customer", senderName: session.customerName, messageText: content }),
      });
    } catch {}
  };

  return (
    <div className="fixed bottom-5 right-5 z-50 font-sans">
      {!isOpen ? (
        <button onClick={() => setIsOpen(true)} className="flex items-center gap-2 bg-emerald-700 text-white px-4 py-3 rounded-full shadow-lg hover:bg-emerald-800">
          <span className="font-semibold text-sm">💬 CSKH Live Chat</span>
        </button>
      ) : (
        <div className="w-[350px] h-[460px] bg-white rounded-2xl shadow-2xl border flex flex-col overflow-hidden">
          <div className="bg-emerald-800 text-white p-3 flex justify-between items-center">
            <div className="flex items-center gap-2">
              <h3 className="font-bold text-sm">🎧 CSKH Trực Tuyến Harness</h3>
              {session && (
                <span className={`w-2 h-2 rounded-full ${
                  connectionStatus === "Connected" ? "bg-green-400" :
                  connectionStatus === "Reconnecting" || connectionStatus === "Connecting" ? "bg-yellow-400 animate-ping" : "bg-red-400"
                }`} title={connectionStatus} />
              )}
            </div>
            <button onClick={() => setIsOpen(false)} className="text-white font-bold">✕</button>
          </div>
          {!session ? (
            <form onSubmit={handleStart} className="p-4 flex-1 flex flex-col justify-center gap-3">
              <p className="text-gray-600 text-xs text-center">Bắt đầu trò chuyện với tư vấn viên</p>
              <input type="text" required placeholder="Họ và Tên *" value={name} onChange={(e) => setName(e.target.value)} className="w-full px-3 py-2 text-xs border rounded-lg" />
              <input type="tel" required placeholder="Số điện thoại *" value={phone} onChange={(e) => setPhone(e.target.value)} className="w-full px-3 py-2 text-xs border rounded-lg" />
              <button type="submit" className="w-full bg-emerald-700 text-white py-2 rounded-lg text-xs font-semibold">Bắt Đầu Chat</button>
            </form>
          ) : (
            <div className="flex-1 flex flex-col h-full overflow-hidden bg-gray-50">
              {connectionStatus !== "Connected" && (
                <div className="bg-amber-100 text-amber-800 text-[10px] px-2 py-0.5 text-center font-medium">
                  {connectionStatus === "Reconnecting" || connectionStatus === "Connecting"
                    ? "🔄 Đang tự động kết nối lại..."
                    : "⚠️ Mất kết nối. Đang liên tục tự động thử lại..."}
                </div>
              )}
              <div className="flex-1 p-3 overflow-y-auto space-y-2">
                {session.messages.map((m, idx) => (
                  m.senderType === "System" ? (
                    <div key={idx} className="text-center my-1"><span className="text-[10px] bg-gray-200 text-gray-600 px-2 py-0.5 rounded-full">{m.messageText}</span></div>
                  ) : (
                    <div key={idx} className={`flex flex-col ${m.senderType === "Customer" ? "items-end" : "items-start"}`}>
                      <div className={`max-w-[80%] text-xs px-3 py-2 rounded-xl ${m.senderType === "Customer" ? "bg-emerald-700 text-white" : "bg-white border text-gray-800"}`}>{m.messageText}</div>
                    </div>
                  )
                ))}
                <div ref={messagesEndRef} />
              </div>
              <div className="p-2 bg-white border-t flex gap-2">
                <input type="text" placeholder="Nhập tin nhắn..." value={text} onChange={(e) => setText(e.target.value)} onKeyDown={(e) => e.key === "Enter" && handleSend()} className="flex-1 px-3 py-1.5 text-xs border rounded-full" />
                <button onClick={handleSend} disabled={!text.trim()} className="bg-emerald-700 text-white px-3 py-1 rounded-full text-xs">➔</button>
              </div>
            </div>
          )}
        </div>
      )}
    </div>
  );
}
