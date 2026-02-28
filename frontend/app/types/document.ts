// Shared TypeScript types used across the frontend.
// Keeping types in one place avoids duplication and keeps
// the frontend in sync with what the backend returns.

export interface Document {
  id: string;
  fileName: string;
  fileSize: number;
  status: "uploaded" | "processing" | "ready" | "failed";
  createdAt: string;
}

export interface ChatMessage {
  role: "user" | "assistant";
  content: string;
}

export interface Conversation {
  id: string;
  title: string;
  createdAt: string;
  updatedAt: string;
}

export interface Message {
  id: string;
  role: "user" | "assistant";
  content: string;
  createdAt: string;
}