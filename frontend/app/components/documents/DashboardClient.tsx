"use client";

import { useState } from "react";
import UploadButton from "./UploadButton";
import DocumentList from "./DocumentList";
import { Document } from "@/app/types/document";
import { getUserDocuments } from "@/app/lib/api";

interface DashboardClientProps {
  userId: string;
  initialDocuments: Document[];
}

export default function DashboardClient({ userId, initialDocuments }: DashboardClientProps) {
  const [documents, setDocuments] = useState<Document[]>(initialDocuments);
  const [isRefreshing, setIsRefreshing] = useState(false);

  async function refreshDocuments() {
    setIsRefreshing(true);
    try {
      const updated = await getUserDocuments(userId);
      setDocuments(updated);
    } catch {
      // Silently fail — existing list stays visible
    } finally {
      setIsRefreshing(false);
    }
  }

  return (
    <div>
      <div className="flex items-center justify-between mb-6">
        <div>
          <h1 className="text-2xl font-bold text-gray-900">Your Documents</h1>
          <p className="text-sm text-gray-500 mt-1">
            {documents.length} document{documents.length !== 1 ? "s" : ""} uploaded
          </p>
        </div>
        <UploadButton userId={userId} onUploadComplete={refreshDocuments} />
      </div>

      {/* Subtle refresh indicator — shows when the list is being reloaded
          after an upload or delete without blocking the entire UI */}
      {isRefreshing && (
        <div className="flex items-center gap-2 text-xs text-gray-400 mb-3">
          <div className="w-3 h-3 border-2 border-gray-300 border-t-blue-500 rounded-full animate-spin" />
          Refreshing...
        </div>
      )}

      <DocumentList
        documents={documents}
        userId={userId}
        onDocumentDeleted={refreshDocuments}
      />
    </div>
  );
}