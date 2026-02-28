// Central place for all backend API calls.
// Keeping API calls here means if the backend URL changes,
// we only update it in one place.

const API_URL = process.env.NEXT_PUBLIC_API_URL;

// Helper that adds the user ID header to every request.
// Our backend uses this header to identify who is making the request.
function getHeaders(userId: string) {
  return {
    "X-User-Id": userId,
  };
}

export async function uploadDocument(file: File, userId: string) {
  // FormData is the browser's built-in way to send files over HTTP.
  // We can't use JSON for file uploads — multipart/form-data is required.
  const formData = new FormData();
  formData.append("file", file);

  const response = await fetch(`${API_URL}/api/documents/upload`, {
    method: "POST",
    headers: getHeaders(userId),
    body: formData,
    cache: 'no-store',
  });

  if (!response.ok) {
    const error = await response.json();
    throw new Error(error.error || "Upload failed");
  }

  return response.json();
}

export async function getUserDocuments(userId: string) {
  const response = await fetch(`${API_URL}/api/documents`, {
    headers: getHeaders(userId),
    // Disable Next.js caching so each user always gets their own documents.
    // Without this, Next.js may return a cached response from a different user.
    cache: 'no-store',
  });

  if (!response.ok) {
    throw new Error("Failed to fetch documents");
  }

  return response.json();
}

export async function streamChat(
  question: string,
  documentId: string,
  userId: string,
  conversationId: string | null,
  onToken: (token: string) => void,
  onConversationId: (id: string) => void
): Promise<void> {
  const response = await fetch(`${API_URL}/api/chat`, {
    method: "POST",
    headers: {
      "Content-Type": "application/json",
      "X-User-Id": userId,
    },
    body: JSON.stringify({
      question,
      documentId,
      conversationId: conversationId ?? "00000000-0000-0000-0000-000000000000"
    }),
  });

  if (!response.ok) throw new Error("Chat request failed");

  const reader = response.body!.getReader();
  const decoder = new TextDecoder();

  while (true) {
    const { done, value } = await reader.read();
    if (done) break;

    const chunk = decoder.decode(value, { stream: true });
    const lines = chunk.split("\n");

    for (const line of lines) {
      if (!line.startsWith("data: ")) continue;
      const data = line.slice("data: ".length).trimEnd();
      if (data === "[DONE]") return;

      // The backend sends the conversation ID as the first event
      if (data.startsWith("__CONV_ID__")) {
        onConversationId(data.replace("__CONV_ID__", ""));
        continue;
      }

      if (data) onToken(data);
    }
  }
}

export async function getConversations(documentId: string, userId: string) {
  const response = await fetch(
    `${API_URL}/api/chat/conversations?documentId=${documentId}`,
    { headers: getHeaders(userId) }
  );
  if (!response.ok) return [];
  return response.json();
}

export async function getConversationMessages(conversationId: string, userId: string) {
  const response = await fetch(
    `${API_URL}/api/chat/conversations/${conversationId}/messages`,
    { headers: getHeaders(userId) }
  );
  if (!response.ok) return [];
  return response.json();
}

export async function deleteDocument(documentId: string, userId: string): Promise<void> {
  const response = await fetch(`${API_URL}/api/documents/${documentId}`, {
    method: "DELETE",
    headers: getHeaders(userId),
  });

  if (!response.ok) {
    throw new Error("Failed to delete document");
  }
}