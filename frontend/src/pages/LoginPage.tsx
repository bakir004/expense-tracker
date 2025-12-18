import * as React from "react";
import { useNavigate, useLocation } from "react-router-dom";
import { api } from "@/lib/api";
import { useAuth } from "@/state/auth";
import { ThemeToggle } from "@/components/ThemeToggle";
import type { User } from "@/lib/types";

import { Button } from "@/components/ui/button";
import {
  Card,
  CardContent,
  CardDescription,
  CardFooter,
  CardHeader,
  CardTitle,
} from "@/components/ui/card";
import { Field, FieldGroup, FieldLabel } from "@/components/ui/field";
import { Input } from "@/components/ui/input";
import {
  Select,
  SelectContent,
  SelectGroup,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import { Separator } from "@/components/ui/separator";

export function LoginPage() {
  const { userId, loginAs } = useAuth();
  const navigate = useNavigate();
  const location = useLocation();

  const [users, setUsers] = React.useState<User[]>([]);
  const [loading, setLoading] = React.useState(false);
  const [error, setError] = React.useState<string | null>(null);

  const [email, setEmail] = React.useState("");
  const [password, setPassword] = React.useState("");
  const [selectedUserId, setSelectedUserId] = React.useState<string>("");

  React.useEffect(() => {
    // If already logged in, go dashboard
    if (userId && location.pathname !== "/dashboard")
      navigate("/dashboard", { replace: true });
  }, [userId, location.pathname, navigate]);

  React.useEffect(() => {
    let cancelled = false;
    setLoading(true);
    api
      .getUsers()
      .then((u) => {
        if (cancelled) return;
        setUsers(u);
      })
      .catch((e: unknown) => {
        if (cancelled) return;
        setError(e instanceof Error ? e.message : "Failed to load users");
      })
      .finally(() => {
        if (cancelled) return;
        setLoading(false);
      });
    return () => {
      cancelled = true;
    };
  }, []);

  async function onLoginByEmail(e: React.FormEvent) {
    e.preventDefault();
    setError(null);
    setLoading(true);
    try {
      const all = users.length ? users : await api.getUsers();
      const match = all.find(
        (u) => u.email.toLowerCase() === email.trim().toLowerCase()
      );
      if (!match) throw new Error("No user found with that email.");
      // Note: API currently has no login endpoint; this is a simple “select user” login for now.
      // Password is collected for future auth support.
      void password;
      loginAs(match.id);
      navigate("/dashboard", { replace: true });
    } catch (e: unknown) {
      setError(e instanceof Error ? e.message : "Login failed");
    } finally {
      setLoading(false);
    }
  }

  function onLoginByUserId(e: React.FormEvent) {
    e.preventDefault();
    setError(null);
    const id = Number(selectedUserId);
    if (!Number.isFinite(id) || id <= 0) {
      setError("Pick a user to continue.");
      return;
    }
    loginAs(id);
    navigate("/dashboard", { replace: true });
  }

  return (
    <div className="mx-auto flex min-h-dvh w-full max-w-3xl items-center justify-center px-4 py-10">
      <Card className="w-full">
        <CardHeader>
          <CardTitle>Expense Tracker</CardTitle>
          <CardDescription>Login to your dashboard.</CardDescription>
        </CardHeader>
        <CardContent className="grid gap-6 md:grid-cols-2">
          <form onSubmit={onLoginByEmail}>
            <FieldGroup>
              <Field>
                <FieldLabel htmlFor="login-email">Email</FieldLabel>
                <Input
                  id="login-email"
                  value={email}
                  onChange={(ev) => setEmail(ev.target.value)}
                  placeholder="john.doe@email.com"
                  autoComplete="email"
                  required
                />
              </Field>
              <Field>
                <FieldLabel htmlFor="login-password">Password</FieldLabel>
                <Input
                  id="login-password"
                  type="password"
                  value={password}
                  onChange={(ev) => setPassword(ev.target.value)}
                  placeholder="••••••••"
                  autoComplete="current-password"
                />
              </Field>
              <Field orientation="horizontal">
                <Button type="submit" disabled={loading}>
                  Login
                </Button>
                <Button
                  type="button"
                  variant="outline"
                  onClick={() => navigate("/register", { replace: true })}
                >
                  Register
                </Button>
              </Field>
            </FieldGroup>
          </form>

          <div className="flex items-stretch">
            <Separator orientation="vertical" className="hidden md:block" />
            <div className="hidden md:block px-6" />
            <form className="w-full" onSubmit={onLoginByUserId}>
              <FieldGroup>
                <Field>
                  <FieldLabel>User</FieldLabel>
                  <Select
                    value={selectedUserId}
                    onValueChange={setSelectedUserId}
                  >
                    <SelectTrigger>
                      <SelectValue
                        placeholder={loading ? "Loading..." : "Select a user"}
                      />
                    </SelectTrigger>
                    <SelectContent>
                      <SelectGroup>
                        {users.map((u) => (
                          <SelectItem key={u.id} value={String(u.id)}>
                            {u.name} ({u.email})
                          </SelectItem>
                        ))}
                      </SelectGroup>
                    </SelectContent>
                  </Select>
                </Field>
                <Field orientation="horizontal">
                  <Button type="submit" variant="secondary" disabled={loading}>
                    Continue
                  </Button>
                </Field>
              </FieldGroup>
            </form>
          </div>
        </CardContent>
        <CardFooter className="justify-between gap-4">
          <div className="flex items-center gap-2">
            <ThemeToggle />
            <div className="text-muted-foreground text-sm">
              Swagger:{" "}
              <a
                className="underline"
                href="http://localhost:5000/swagger"
                target="_blank"
                rel="noreferrer"
              >
                /swagger
              </a>
            </div>
          </div>
          {error ? (
            <div className="text-sm text-destructive">{error}</div>
          ) : null}
        </CardFooter>
      </Card>
    </div>
  );
}
