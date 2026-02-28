import { createClient } from "@/app/lib/supabase/server";
import { redirect } from "next/navigation";
import { logout } from "@/app/actions/auth";
import { getUserDocuments } from "@/app/lib/api";
import { Document } from "@/app/types/document";
import DashboardClient from "@/app/components/documents/DashboardClient";

export const dynamic = "force-dynamic";

export default async function DashboardPage() {
  const supabase = createClient();
  const { data: { user } } = await supabase.auth.getUser();

  if (!user) redirect("/login");

  let documents: Document[] = [];
  try {
    documents = await getUserDocuments(user.id);
  } catch {
    documents = [];
  }

  return (
    <div className="min-h-screen bg-gray-50">
      <nav className="bg-white border-b border-gray-200 px-6 h-16 flex items-center justify-between">
        <span className="text-lg font-bold text-blue-600">DocChat</span>
        <div className="flex items-center gap-4">
          <span className="text-sm text-gray-500">{user.email}</span>
          <form action={logout}>
            <button type="submit" className="text-sm text-red-500 hover:text-red-700 transition-colors">
              Log out
            </button>
          </form>
        </div>
      </nav>

      <main className="max-w-4xl mx-auto px-6 py-10">
        {/* Server component handles auth and initial data fetching.
            DashboardClient takes over for anything interactive. */}
        <DashboardClient userId={user.id} initialDocuments={documents} />
      </main>
    </div>
  );
}