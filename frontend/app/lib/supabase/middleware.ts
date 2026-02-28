// PURPOSE: Runs on every request to keep the user session alive and
// enforce route protection (who can go where).
// PATTERN: This is the standard Supabase SSR session management pattern.
// WHEN TO USE IN OTHER PROJECTS: Any Next.js app using Supabase Auth
// needs this exact setup. Copy this file as-is and adjust the route
// names to match your project's protected and auth routes.

import { createServerClient } from "@supabase/ssr";
import { NextResponse, type NextRequest } from "next/server";

export async function updateSession(request: NextRequest) {
  // Start with a default "pass through" response.
  // We may modify this response to attach updated cookies before returning it.
  let supabaseResponse = NextResponse.next({ request });

  const supabase = createServerClient(
    process.env.NEXT_PUBLIC_SUPABASE_URL!,
    process.env.NEXT_PUBLIC_SUPABASE_ANON_KEY!,
    {
      cookies: {
        getAll() {
          return request.cookies.getAll();
        },
        // When Supabase refreshes the session token, it calls setAll
        // to write the new token back. We apply it to both the request
        // and the response so the updated token flows through correctly.
        setAll(cookiesToSet) {
          cookiesToSet.forEach(({ name, value }) =>
            request.cookies.set(name, value)
          );
          supabaseResponse = NextResponse.next({ request });
          cookiesToSet.forEach(({ name, value, options }) =>
            supabaseResponse.cookies.set(name, value, options)
          );
        },
      },
    }
  );

  // IMPORTANT: Always use getUser() here, never getSession().
  // getUser() makes a network call to Supabase to cryptographically
  // verify the token is still valid. getSession() only reads the local
  // cookie which could be tampered with — never use it for auth checks.
  const {
    data: { user },
  } = await supabase.auth.getUser();

  // Define which routes require authentication
  const isProtectedRoute = request.nextUrl.pathname.startsWith("/dashboard");

  // Define which routes are only for unauthenticated users
  const isAuthRoute =
    request.nextUrl.pathname.startsWith("/login") ||
    request.nextUrl.pathname.startsWith("/signup");

  // If the user is NOT logged in and tries to access a protected route,
  // redirect them to login. This is your "auth guard".
  if (!user && isProtectedRoute) {
    const url = request.nextUrl.clone();
    url.pathname = "/login";
    return NextResponse.redirect(url);
  }

  // If the user IS logged in and tries to visit login/signup,
  // redirect them to the dashboard — no point showing auth pages
  // to someone who is already authenticated.
  if (user && isAuthRoute) {
    const url = request.nextUrl.clone();
    url.pathname = "/dashboard";
    return NextResponse.redirect(url);
  }

  // Return the response with the refreshed session cookie attached
  return supabaseResponse;
}