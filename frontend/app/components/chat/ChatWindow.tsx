"use client";

import { useState, useRef, useEffect } from "react";
import { streamChat, getConversations, getConversationMessages } from "@/app/lib/api";
import { ChatMessage, Conversation, MessageDto } from "@/app/types/document";
import { cn } from "@/app/lib/utils";

interface ChatWindowProps {
  documentId: string;
  documentName: string;
  userId: string;
}

export default function ChatWindow({ documentId, documentName, userId }: ChatWindowProps) {
  const [conversations, setConversations] = useState<Conversation[]>([]);
  const [activeConversationId, setActiveConversationId] = useState<string | null>(null);
  const [messages, setMessages] = useState<ChatMessage[]>([]);
  const [question, setQuestion] = useState("");
  const [loading, setLoading] = useState(false);
  const [loadingConversations, setLoadingConversations] = useState(true);
  const bottomRef = useRef<HTMLDivElement>(null);
  const textareaRef = useRef<HTMLTextAreaElement>(null);

  // Load conversations when the component mounts
  useEffect(() => {
    loadConversations();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [documentId]);

  useEffect(() => {
    bottomRef.current?.scrollIntoView({ behavior: "smooth" });
  }, [messages]);

  useEffect(() => {
    const textarea = textareaRef.current;
    if (!textarea) return;
    textarea.style.height = "auto";
    textarea.style.height = `${Math.min(textarea.scrollHeight, 120)}px`;
  }, [question]);

  async function loadConversations() {
    setLoadingConversations(true);
    try {
      const convs = await getConversations(documentId, userId);
      setConversations(convs);

      // Auto-load the most recent conversation if one exists
      if (convs.length > 0) {
        await selectConversation(convs[0].id);
      }
    } finally {
      setLoadingConversations(false);
    }
  }

  async function selectConversation(conversationId: string) {
    setActiveConversationId(conversationId);
    const msgs = await getConversationMessages(conversationId, userId);
    setMessages(msgs.map((m: MessageDto) => ({ role: m.role, content: m.content })));
  }

  function startNewConversation() {
    setActiveConversationId(null);
    setMessages([]);
  }

  async function handleSubmit() {
    if (!question.trim() || loading) return;

    const userMessage: ChatMessage = { role: "user", content: question };
    setMessages(prev => [...prev, userMessage, { role: "assistant", content: "" }]);
    setQuestion("");
    setLoading(true);

    try {
      await streamChat(
        question,
        documentId,
        userId,
        activeConversationId,
        (token) => {
          setMessages(prev => {
            const updated = [...prev];
            updated[updated.length - 1] = {
              role: "assistant",
              content: updated[updated.length - 1].content + token
            };
            return updated;
          });
        },
        (newConversationId) => {
          // When the backend creates a new conversation, update our state
          // and refresh the conversation list
          setActiveConversationId(newConversationId);
          loadConversations();
        }
      );
    } catch {
      setMessages(prev => {
        const updated = [...prev];
        updated[updated.length - 1] = {
          role: "assistant",
          content: "Something went wrong. Please try again."
        };
        return updated;
      });
    } finally {
      setLoading(false);
    }
  }

  function handleKeyDown(e: React.KeyboardEvent) {
    if (e.key === "Enter" && !e.shiftKey) {
      e.preventDefault();
      handleSubmit();
    }
  }

  return (
    <div className="flex h-full">

      {/* Sidebar — conversation history */}
      <div className="w-64 border-r border-gray-200 bg-white flex flex-col shrink-0">
        <div className="p-4 border-b border-gray-200">
          <p className="text-xs text-gray-400 uppercase tracking-wide mb-3">Chatting with</p>
          <p className="text-xs font-medium text-gray-700 truncate mb-3">{documentName}</p>
          <button
            onClick={startNewConversation}
            className="w-full text-sm bg-blue-600 text-white px-3 py-2 rounded-lg hover:bg-blue-700 transition-colors font-medium"
          >
            + New Conversation
          </button>
        </div>

        <div className="flex-1 overflow-y-auto p-2">
          {loadingConversations ? (
            <p className="text-xs text-gray-400 text-center py-4">Loading...</p>
          ) : conversations.length === 0 ? (
            <p className="text-xs text-gray-400 text-center py-4">No conversations yet</p>
          ) : (
            conversations.map(conv => (
              <button
                key={conv.id}
                onClick={() => selectConversation(conv.id)}
                className={cn(
                  "w-full text-left px-3 py-2 rounded-lg text-xs mb-1 transition-colors truncate",
                  activeConversationId === conv.id
                    ? "bg-blue-50 text-blue-700 font-medium"
                    : "text-gray-600 hover:bg-gray-100"
                )}
              >
                {conv.title}
              </button>
            ))
          )}
        </div>
      </div>

      {/* Main chat area */}
      <div className="flex-1 flex flex-col min-w-0">
        <div className="flex-1 overflow-y-auto px-6 py-6 space-y-4 min-h-0">
          {messages.length === 0 && (
            <div className="text-center py-20 text-gray-400">
              <p className="text-lg font-medium">Ask a question about this document</p>
              <p className="text-sm mt-1">The AI will answer using only the document&apos;s content</p>
            </div>
          )}

          {messages.map((msg, i) => (
            <div key={i} className={cn("flex", msg.role === "user" ? "justify-end" : "justify-start")}>
              <div className={cn(
                "max-w-[75%] rounded-2xl px-4 py-3 text-sm leading-relaxed break-words whitespace-pre-wrap",
                msg.role === "user"
                  ? "bg-blue-600 text-white rounded-br-sm"
                  : "bg-white border border-gray-200 text-gray-800 rounded-bl-sm shadow-sm"
              )}>
                {msg.content || (msg.role === "assistant" && loading && i === messages.length - 1
                  ? <span className="inline-block w-2 h-4 bg-gray-400 animate-pulse rounded" />
                  : ""
                )}
              </div>
            </div>
          ))}

          <div ref={bottomRef} />
        </div>

        <div className="px-6 py-4 border-t border-gray-200 bg-white shrink-0">
          <div className="flex gap-3 items-end">
            <textarea
              ref={textareaRef}
              value={question}
              onChange={e => setQuestion(e.target.value)}
              onKeyDown={handleKeyDown}
              placeholder="Ask a question... (Enter to send)"
              rows={1}
              disabled={loading}
              className="flex-1 resize-none rounded-xl border border-gray-300 px-4 py-3 text-base text-gray-900 placeholder:text-gray-400 focus:outline-none focus:ring-2 focus:ring-blue-500 disabled:opacity-50 overflow-hidden"
            />
            <button
              onClick={handleSubmit}
              disabled={loading || !question.trim()}
              className={cn(
                "px-4 py-3 rounded-xl text-sm font-medium text-white transition-colors shrink-0",
                loading || !question.trim()
                  ? "bg-gray-300 cursor-not-allowed"
                  : "bg-blue-600 hover:bg-blue-700"
              )}
            >
              {loading ? "..." : "Send"}
            </button>
          </div>
          <p className="text-xs text-gray-400 mt-2">Enter to send · Shift+Enter for new line</p>
        </div>
      </div>
    </div>
  );
}