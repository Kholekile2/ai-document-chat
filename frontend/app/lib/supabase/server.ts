// PURPOSE: Supabase client for use on the SERVER (server components, server actions, route handlers).
// USE THIS when rendering pages on the server or running server actions.
// WHEN TO USE IN OTHER PROJECTS: Any time your server-side code needs
// to query Supabase — fetching user data, protected DB queries, etc.
// The key difference from client.ts is that this manually handles cookies
// because the server has no access to browser APIs.

import { createServerClient } from "@supabase/ssr";
import { cookies } from "next/headers";

export function createClient() {
  // cookies() is a Next.js server utility that reads the incoming
  // request's cookies. Supabase stores the user session in a cookie,
  // so this is how the server knows who is logged in.
  const cookieStore = cookies();

  return createServerClient(
    process.env.NEXT_PUBLIC_SUPABASE_URL!,
    process.env.NEXT_PUBLIC_SUPABASE_ANON_KEY!,
    {
      cookies: {
        // getAll: teaches Supabase how to READ cookies on the server
        getAll() {
          return cookieStore.getAll();
        },
        // setAll: teaches Supabase how to WRITE cookies on the server.
        // The try/catch is necessary because Server Components cannot set
        // cookies — only middleware and Route Handlers can. If this is
        // called from a Server Component, we silently ignore the error
        // because the middleware will handle the session refresh instead.
        setAll(cookiesToSet) {
          try {
            cookiesToSet.forEach(({ name, value, options }) =>
              cookieStore.set(name, value, options)
            );
          } catch {
            // Called from a Server Component — middleware handles session refresh
          }
        },
      },
    }
  );
}