"use client";

import Link from "next/link";
import { useState } from "react";
import { Document } from "@/app/types/document";
import { deleteDocument } from "@/app/lib/api";
import { cn } from "@/app/lib/utils";

interface DocumentListProps {
  documents: Document[];
  userId: string;
  onDocumentDeleted: () => void;
}

function formatFileSize(bytes: number): string {
  if (bytes < 1024) return `${bytes} B`;
  if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`;
  return `${(bytes / (1024 * 1024)).toFixed(1)} MB`;
}

function StatusBadge({ status }: { status: Document["status"] }) {
  const styles = {
    uploaded:   "bg-gray-100 text-gray-600",
    processing: "bg-yellow-100 text-yellow-700",
    ready:      "bg-green-100 text-green-700",
    failed:     "bg-red-100 text-red-600",
  };

  return (
    <span className={`text-xs px-2 py-1 rounded-full font-medium ${styles[status]}`}>
      {status}
    </span>
  );
}

export default function DocumentList({ documents, userId, onDocumentDeleted }: DocumentListProps) {
  const [deletingId, setDeletingId] = useState<string | null>(null);
  const [confirmId, setConfirmId] = useState<string | null>(null);

  async function handleDelete(documentId: string) {
    // First click sets confirmId — shows "Are you sure?" state.
    // Second click on the same document actually deletes it.
    // Clicking a different document resets the confirmation.
    if (confirmId !== documentId) {
      setConfirmId(documentId);
      return;
    }

    setDeletingId(documentId);
    setConfirmId(null);

    try {
      await deleteDocument(documentId, userId);
      onDocumentDeleted();
    } catch {
      // If delete fails, just reset state — the document stays in the list
      setDeletingId(null);
    }
  }

  function handleCancelDelete(e: React.MouseEvent) {
    e.stopPropagation();
    setConfirmId(null);
  }

  if (documents.length === 0) {
    return (
      <div className="text-center py-16 text-gray-400">
        <p className="text-lg">No documents yet</p>
        <p className="text-sm mt-1">Upload a PDF to get started</p>
      </div>
    );
  }

  return (
    <div className="divide-y divide-gray-100 rounded-xl border border-gray-200 bg-white">
      {documents.map((doc) => (
        <div
          key={doc.id}
          className="flex items-center justify-between px-5 py-4"
          // Clicking anywhere outside the buttons resets confirm state
          onClick={() => confirmId === doc.id && setConfirmId(null)}
        >
          <div className="flex flex-col gap-1 min-w-0 mr-4">
            <span className="text-sm font-medium text-gray-900 truncate">{doc.fileName}</span>
            <span className="text-xs text-gray-400">
              {formatFileSize(doc.fileSize)} · {new Date(doc.createdAt).toLocaleDateString("en-US", {
                year: "numeric",
                month: "short",
                day: "numeric",
              })}
            </span>
          </div>

          <div className="flex items-center gap-2 shrink-0">
            <StatusBadge status={doc.status} />

            {doc.status === "ready" && (
              <Link
                href={`/chat/${doc.id}`}
                className="text-xs bg-blue-600 text-white px-3 py-1.5 rounded-lg hover:bg-blue-700 transition-colors font-medium"
              >
                Chat
              </Link>
            )}

            {/* Two-step delete: first click shows confirm, second click deletes */}
            {confirmId === doc.id ? (
              <div className="flex items-center gap-1">
                <button
                  onClick={() => handleDelete(doc.id)}
                  disabled={deletingId === doc.id}
                  className="text-xs bg-red-600 text-white px-2 py-1.5 rounded-lg hover:bg-red-700 transition-colors font-medium"
                >
                  Confirm
                </button>
                <button
                  onClick={handleCancelDelete}
                  className="text-xs bg-gray-100 text-gray-600 px-2 py-1.5 rounded-lg hover:bg-gray-200 transition-colors"
                >
                  Cancel
                </button>
              </div>
            ) : (
              <button
                onClick={() => handleDelete(doc.id)}
                disabled={deletingId === doc.id}
                className={cn(
                  "text-xs px-3 py-1.5 rounded-lg transition-colors font-medium",
                  deletingId === doc.id
                    ? "bg-gray-100 text-gray-400 cursor-not-allowed"
                    : "bg-gray-100 text-gray-600 hover:bg-red-50 hover:text-red-600"
                )}
              >
                {deletingId === doc.id ? "Deleting..." : "Delete"}
              </button>
            )}
          </div>
        </div>
      ))}
    </div>
  );
}