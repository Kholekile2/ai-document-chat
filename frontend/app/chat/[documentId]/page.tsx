import { createClient } from "@/app/lib/supabase/server";
import { redirect } from "next/navigation";
import { getUserDocuments } from "@/app/lib/api";
import { Document } from "@/app/types/document";
import ChatWindow from "@/app/components/chat/ChatWindow";
import Link from "next/link";

interface ChatPageProps {
  params: Promise<{ documentId: string }>;
}

export default async function ChatPage({ params }: ChatPageProps) {
  const { documentId } = await params;

  const supabase = createClient();
  const { data: { user } } = await supabase.auth.getUser();

  if (!user) redirect("/login");

  let document: Document | null = null;
  try {
    const documents = await getUserDocuments(user.id);
    document = documents.find((d: Document) => d.id === documentId) ?? null;
  } catch (err) {
    console.log("Fetch error:", err);
    redirect("/dashboard");
  }

  if (!document) {
    console.log("Document not found, redirecting");
    redirect("/dashboard");
  }

  if (document.status !== "ready") {
    console.log("Document not ready, status:", document.status);
    redirect("/dashboard");
  }

  return (
    <div className="flex flex-col h-screen bg-gray-50">
      <nav className="bg-white border-b border-gray-200 px-6 h-16 flex items-center justify-between shrink-0">
        <div className="flex items-center gap-4">
          <Link href="/dashboard" className="text-sm text-gray-500 hover:text-gray-700 transition-colors">
            ← Back
          </Link>
          <span className="text-lg font-bold text-blue-600">DocChat</span>
        </div>
        <span className="text-sm text-gray-500">{user.email}</span>
      </nav>

      <div className="flex-1 overflow-hidden max-w-4xl w-full mx-auto">
        <ChatWindow
          documentId={documentId}
          documentName={document.fileName}
          userId={user.id}
        />
      </div>
    </div>
  );
}