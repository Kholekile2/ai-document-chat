import AuthForm from "@/app/components/auth/AuthForm";
import { signup } from "@/app/actions/auth";

export default function SignupPage() {
  return <AuthForm mode="signup" action={signup} />;
}