// PURPOSE: The entry point Next.js looks for to run code before every request.
// Next.js has a convention — it always looks for middleware.ts at the
// project root. We keep this file thin and delegate the real logic to
// our supabase/middleware.ts file to keep concerns separated.
// WHEN TO USE IN OTHER PROJECTS: You'll always need this file in any
// Next.js project that uses middleware. Keep it minimal — just import
// and call your actual middleware logic from a separate file.

import { type NextRequest } from "next/server";
import { updateSession } from "./app/lib/supabase/middleware";

export async function middleware(request: NextRequest) {
  return await updateSession(request);
}

// The matcher tells Next.js which routes to run this middleware on.
// This regex means: run on EVERY route EXCEPT Next.js internals
// (_next/static, _next/image) and static files (images, icons, fonts).
// This prevents unnecessary auth checks on assets that don't need them,
// which keeps your app fast.
export const config = {
  matcher: [
    "/((?!_next/static|_next/image|favicon.ico|.*\\.(?:svg|png|jpg|jpeg|gif|webp)$).*)",
  ],
};