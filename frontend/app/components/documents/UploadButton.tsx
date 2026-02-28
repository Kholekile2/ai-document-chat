"use client";

import { useState, useRef } from "react";
import { uploadDocument } from "@/app/lib/api";
import { cn } from "@/app/lib/utils";

interface UploadButtonProps {
  userId: string;
  onUploadComplete: () => void;
}

// The upload pipeline has multiple steps that take different amounts of time.
// Showing the current step gives the user confidence the app is working
// rather than staring at a generic "Uploading..." message for 10-30 seconds.
const UPLOAD_STEPS = [
  { after: 0,     label: "Uploading PDF..."        },
  { after: 2000,  label: "Extracting text..."      },
  { after: 5000,  label: "Generating embeddings..." },
  { after: 12000, label: "Saving to database..."   },
  { after: 18000, label: "Almost done..."           },
];

export default function UploadButton({ userId, onUploadComplete }: UploadButtonProps) {
  const [uploading, setUploading] = useState(false);
  const [stepLabel, setStepLabel] = useState("Uploading PDF...");
  const [error, setError] = useState<string | null>(null);
  const fileInputRef = useRef<HTMLInputElement>(null);
  const timersRef = useRef<NodeJS.Timeout[]>([]);

  function startStepTimers() {
    // Schedule each step label to appear at the right time.
    // These are estimates based on typical processing times —
    // they don't reflect the actual backend state, just keep
    // the user informed that work is happening.
    timersRef.current = UPLOAD_STEPS.map(({ after, label }) =>
      setTimeout(() => setStepLabel(label), after)
    );
  }

  function clearStepTimers() {
    timersRef.current.forEach(clearTimeout);
    timersRef.current = [];
  }

  async function handleFileChange(e: React.ChangeEvent<HTMLInputElement>) {
    const file = e.target.files?.[0];
    if (!file) return;

    setError(null);
    setUploading(true);
    setStepLabel("Uploading PDF...");
    startStepTimers();

    try {
      await uploadDocument(file, userId);
      onUploadComplete();
    } catch (err) {
      setError(err instanceof Error ? err.message : "Upload failed");
    } finally {
      clearStepTimers();
      setUploading(false);
      setStepLabel("Uploading PDF...");
      if (fileInputRef.current) fileInputRef.current.value = "";
    }
  }

  return (
    <div className="flex flex-col items-end gap-2">
      <input
        ref={fileInputRef}
        type="file"
        accept=".pdf"
        className="hidden"
        onChange={handleFileChange}
      />

      <button
        onClick={() => fileInputRef.current?.click()}
        disabled={uploading}
        className={cn(
          "flex items-center gap-2 bg-blue-600 text-white px-4 py-2 rounded-lg text-sm font-medium transition-colors",
          uploading ? "opacity-80 cursor-not-allowed" : "hover:bg-blue-700"
        )}
      >
        {uploading && (
          // Spinning indicator while processing
          <div className="w-3 h-3 border-2 border-white/40 border-t-white rounded-full animate-spin" />
        )}
        {uploading ? stepLabel : "Upload PDF"}
      </button>

      {error && (
        <p className="text-sm text-red-600">{error}</p>
      )}
    </div>
  );
}