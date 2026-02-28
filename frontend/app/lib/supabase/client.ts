// PURPOSE: Supabase client for use in BROWSER (client) components.
// USE THIS when you need to access Supabase from interactive UI components
// that run in the browser (anything with "use client" at the top).
// WHEN TO USE IN OTHER PROJECTS: Any time you have a client component
// that needs to read/write from Supabase (e.g. real-time subscriptions,
// user-triggered queries).

import { createBrowserClient } from "@supabase/ssr";

export function createClient() {
  // createBrowserClient is specifically designed for browser environments.
  // It handles session storage via cookies automatically.
  // The environment variables are prefixed with NEXT_PUBLIC_ so Next.js
  // knows it's safe to expose them to the browser bundle.
  return createBrowserClient(
    process.env.NEXT_PUBLIC_SUPABASE_URL!,
    process.env.NEXT_PUBLIC_SUPABASE_ANON_KEY!
  );
}