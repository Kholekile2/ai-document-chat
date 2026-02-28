// PURPOSE: A reusable form component for both login and signup.
// PATTERN: Instead of building two separate form components that are
// nearly identical, we build one component that adapts based on props.
// This is called the "single component, multiple uses" pattern.
// WHEN TO USE IN OTHER PROJECTS: Any time you have two very similar
// UI forms (login/signup, create/edit), consider building one component
// that accepts a mode prop to control the differences.

"use client";

// "use client" is required here because we use React hooks (useState)
// which only work in the browser. Server components cannot use hooks.
// RULE OF THUMB: Add "use client" only when you need interactivity,
// hooks, or browser APIs. Keep as many components as possible as
// server components for better performance.

import { useState } from "react";
import Link from "next/link";
import { cn } from "@/app/lib/utils";

interface AuthFormProps {
  // mode controls which labels, text, and links to show
  mode: "login" | "signup";
  // action is the server action to call on form submit.
  // It returns either an error object or nothing (void) on success.
  action: (formData: FormData) => Promise<{ error: string } | void>;
}

export default function AuthForm({ mode, action }: AuthFormProps) {
  // Local state for the error message — null means no error
  const [error, setError] = useState<string | null>(null);

  // Local state to track if we're waiting for the server action to complete.
  // Used to disable the button and show "Please wait..." text.
  const [loading, setLoading] = useState(false);

  const isLogin = mode === "login";

  async function handleSubmit(formData: FormData) {
    // Clear any previous error before each new attempt
    setError(null);
    setLoading(true);

    // Call the server action (login or signup) passed in via props.
    // This runs on the server — we just await the result here.
    const result = await action(formData);

    // If the server action returned an error, display it.
    // If it succeeded, it would have called redirect() on the server,
    // so this code never runs on success.
    if (result?.error) {
      setError(result.error);
      setLoading(false);
    }
  }

  return (
    <div className="min-h-screen flex items-center justify-center bg-gray-50 px-4">
      <div className="w-full max-w-md">
        <div className="text-center mb-8">
          <Link href="/" className="text-2xl font-bold text-blue-600">
            DocChat
          </Link>
          <p className="mt-2 text-gray-500 text-sm">
            {isLogin ? "Welcome back" : "Create your account"}
          </p>
        </div>

        <div className="bg-white rounded-2xl border border-gray-200 shadow-sm p-8">
          {/* form action accepts a function in Next.js — this is how
              Server Actions are connected to forms. When submitted,
              Next.js calls handleSubmit with a FormData object containing
              all the form field values. */}
          <form action={handleSubmit} className="flex flex-col gap-5">
            <div className="flex flex-col gap-1.5">
              <label htmlFor="email" className="text-sm font-medium text-gray-700">
                Email
              </label>
              {/* The name attribute is how FormData identifies each field.
                  formData.get("email") in the server action matches name="email" here. */}
              <input
                id="email"
                name="email"
                type="email"
                required
                autoComplete="email"
                placeholder="you@example.com"
                className="w-full border border-gray-300 rounded-lg px-3 py-2.5 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-transparent transition"
              />
            </div>

            <div className="flex flex-col gap-1.5">
              <label htmlFor="password" className="text-sm font-medium text-gray-700">
                Password
              </label>
              <input
                id="password"
                name="password"
                type="password"
                required
                // autoComplete hints to the browser which saved password to suggest
                autoComplete={isLogin ? "current-password" : "new-password"}
                placeholder="••••••••"
                className="w-full border border-gray-300 rounded-lg px-3 py-2.5 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-transparent transition"
              />
            </div>

            {/* Conditionally render the error box only when there is an error.
                This is the standard pattern for displaying server-side errors in forms. */}
            {error && (
              <div className="text-sm text-red-600 bg-red-50 border border-red-200 rounded-lg px-3 py-2">
                {error}
              </div>
            )}

            <button
              type="submit"
              disabled={loading}
              // cn() applies base classes always, and conditionally applies
              // different classes depending on the loading state
              className={cn(
                "w-full bg-blue-600 text-white py-2.5 rounded-lg text-sm font-medium transition-colors",
                loading ? "opacity-60 cursor-not-allowed" : "hover:bg-blue-700"
              )}
            >
              {loading ? "Please wait..." : isLogin ? "Log in" : "Create account"}
            </button>
          </form>

          <p className="mt-6 text-center text-sm text-gray-500">
            {isLogin ? "Don't have an account?" : "Already have an account?"}{" "}
            <Link
              href={isLogin ? "/signup" : "/login"}
              className="text-blue-600 font-medium hover:underline"
            >
              {isLogin ? "Sign up" : "Log in"}
            </Link>
          </p>
        </div>
      </div>
    </div>
  );
}