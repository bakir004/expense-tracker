import * as React from "react"
import { api } from "@/lib/api"
import { navigate } from "@/router"
import { useAuth } from "@/state/auth"

import { Button } from "@/components/ui/button"
import { Card, CardContent, CardDescription, CardFooter, CardHeader, CardTitle } from "@/components/ui/card"
import { Field, FieldGroup, FieldLabel } from "@/components/ui/field"
import { Input } from "@/components/ui/input"

export function RegisterPage() {
  const { loginAs } = useAuth()

  const [name, setName] = React.useState("")
  const [email, setEmail] = React.useState("")
  const [password, setPassword] = React.useState("")
  const [loading, setLoading] = React.useState(false)
  const [error, setError] = React.useState<string | null>(null)

  async function onSubmit(e: React.FormEvent) {
    e.preventDefault()
    setError(null)
    setLoading(true)
    try {
      const user = await api.createUser({
        name: name.trim(),
        email: email.trim(),
        password,
      })
      loginAs(user.id)
      navigate("/dashboard")
    } catch (e: unknown) {
      setError(e instanceof Error ? e.message : "Registration failed")
    } finally {
      setLoading(false)
    }
  }

  return (
    <div className="mx-auto flex min-h-dvh w-full max-w-xl items-center justify-center px-4 py-10">
      <Card className="w-full">
        <CardHeader>
          <CardTitle>Create account</CardTitle>
          <CardDescription>Register a new user.</CardDescription>
        </CardHeader>
        <CardContent>
          <form onSubmit={onSubmit}>
            <FieldGroup>
              <Field>
                <FieldLabel htmlFor="reg-name">Name</FieldLabel>
                <Input
                  id="reg-name"
                  value={name}
                  onChange={(ev) => setName(ev.target.value)}
                  placeholder="John Doe"
                  required
                />
              </Field>
              <Field>
                <FieldLabel htmlFor="reg-email">Email</FieldLabel>
                <Input
                  id="reg-email"
                  value={email}
                  onChange={(ev) => setEmail(ev.target.value)}
                  placeholder="john.doe@email.com"
                  autoComplete="email"
                  required
                />
              </Field>
              <Field>
                <FieldLabel htmlFor="reg-password">Password</FieldLabel>
                <Input
                  id="reg-password"
                  type="password"
                  value={password}
                  onChange={(ev) => setPassword(ev.target.value)}
                  placeholder="••••••••"
                  autoComplete="new-password"
                  required
                />
              </Field>
              <Field orientation="horizontal">
                <Button type="submit" disabled={loading}>
                  Create account
                </Button>
                <Button type="button" variant="outline" onClick={() => navigate("/login")}>
                  Back to login
                </Button>
              </Field>
            </FieldGroup>
          </form>
        </CardContent>
        <CardFooter className="justify-end">
          {error ? <div className="text-sm text-destructive">{error}</div> : null}
        </CardFooter>
      </Card>
    </div>
  )
}


