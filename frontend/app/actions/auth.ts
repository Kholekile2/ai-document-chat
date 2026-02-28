// PURPOSE: Server Actions for authentication — login, signup, and logout.
// WHAT ARE SERVER ACTIONS: A Next.js feature that lets you write server-side
// functions and call them from your UI directly without creating API routes.
// They run exclusively on the server — never exposed to the browser.
// WHEN TO USE IN OTHER PROJECTS: Use Server Actions for any form submission
// or mutation (create, update, delete) that needs to run on the server.
// They replace the need for separate API route files for simple operations.

"use server";

import { revalidatePath } from "next/cache";
import { redirect } from "next/navigation";
import { createClient } from "@/app/lib/supabase/server";

export async function login(formData: FormData) {
  // createClient() uses the SERVER client (reads session from cookies)
  const supabase = createClient();

  // signInWithPassword validates credentials against Supabase Auth.
  // On success, Supabase sets a session cookie automatically.
  // formData.get() extracts values from the submitted HTML form.
  const { error } = await supabase.auth.signInWithPassword({
    email: formData.get("email") as string,
    password: formData.get("password") as string,
  });

  // If login fails (wrong password, unconfirmed email, etc.),
  // return the error message to the client to display in the UI.
  // We return instead of throwing so the form can show the error
  // without a full page crash.
  if (error) {
    return { error: error.message };
  }

  // revalidatePath clears Next.js's cache for the entire layout.
  // This ensures the UI reflects the new auth state immediately
  // rather than showing stale cached data.
  revalidatePath("/", "layout");

  // redirect() is a Next.js server utility that sends the user
  // to a different page after a successful server action.
  redirect("/dashboard");
}

export async function signup(formData: FormData) {
  const supabase = createClient();

  // signUp creates a new user in Supabase Auth.
  // By default Supabase sends a confirmation email — we disabled
  // this in the Supabase dashboard for development convenience.
  // In production you would re-enable email confirmation.
  const { error } = await supabase.auth.signUp({
    email: formData.get("email") as string,
    password: formData.get("password") as string,
  });

  if (error) {
    return { error: error.message };
  }

  revalidatePath("/", "layout");
  redirect("/dashboard");
}

export async function logout() {
  const supabase = createClient();

  // signOut clears the session cookie, effectively logging the user out.
  // After this, any protected route will redirect them to /login.
  await supabase.auth.signOut();

  revalidatePath("/", "layout");
  redirect("/login");
}