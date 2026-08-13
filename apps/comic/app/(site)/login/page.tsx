import { GoogleSignInButton } from "@/components/GoogleSignInButton";

export default function LoginPage({
  searchParams,
}: {
  searchParams: Promise<{ error?: string; return?: string }>;
}) {
  return (
    <LoginInner searchParams={searchParams} />
  );
}

async function LoginInner({
  searchParams,
}: {
  searchParams: Promise<{ error?: string; return?: string }>;
}) {
  const sp = await searchParams;
  const ret = sp.return && sp.return.startsWith("/") ? sp.return : "/";
  const href = `/auth/sign-in/google?return=${encodeURIComponent(ret)}`;
  return (
    <div className="shell">
      <div className="login-card">
        <h1>Sign in</h1>
        <p className="muted">Google login is required to browse and manage the library.</p>
        {sp.error ? <p className="error">Sign-in failed ({sp.error}). Try again.</p> : null}
        <GoogleSignInButton href={href} />
      </div>
    </div>
  );
}
