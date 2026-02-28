// PURPOSE: Shared utility functions used across the entire frontend.
// PATTERN: It's standard practice in Next.js / React projects to have
// a utils.ts file for small reusable helpers. As the project grows,
// you can add more utility functions here.
// WHEN TO USE IN OTHER PROJECTS: Copy this cn() function into any
// React + Tailwind project — you'll use it constantly.

import { clsx, type ClassValue } from "clsx";
import { twMerge } from "tailwind-merge";

// cn() = "class names" — combines clsx and tailwind-merge.
// clsx handles conditional class logic:
//   cn("base", isError && "text-red-500") → "base text-red-500"
// tailwind-merge resolves Tailwind conflicts:
//   cn("bg-blue-500", "bg-red-500") → "bg-red-500" (last one wins)
// Without tailwind-merge, both classes would be in the string and
// the browser would apply whichever appears last in the CSS file,
// which is unpredictable.
export function cn(...inputs: ClassValue[]) {
  return twMerge(clsx(inputs));
}