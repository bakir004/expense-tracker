import * as React from "react"

type AuthState = {
  userId: number | null
}

type AuthContextValue = AuthState & {
  loginAs: (userId: number) => void
  logout: () => void
}

const STORAGE_KEY = "expense-tracker:userId"

const AuthContext = React.createContext<AuthContextValue | null>(null)

function readUserId(): number | null {
  const raw = localStorage.getItem(STORAGE_KEY)
  if (!raw) return null
  const n = Number(raw)
  return Number.isFinite(n) && n > 0 ? n : null
}

export function AuthProvider({ children }: { children: React.ReactNode }) {
  const [userId, setUserId] = React.useState<number | null>(() => readUserId())

  const loginAs = React.useCallback((id: number) => {
    localStorage.setItem(STORAGE_KEY, String(id))
    setUserId(id)
  }, [])

  const logout = React.useCallback(() => {
    localStorage.removeItem(STORAGE_KEY)
    setUserId(null)
  }, [])

  const value = React.useMemo<AuthContextValue>(
    () => ({ userId, loginAs, logout }),
    [userId, loginAs, logout],
  )

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}

export function useAuth() {
  const ctx = React.useContext(AuthContext)
  if (!ctx) throw new Error("useAuth must be used within <AuthProvider>")
  return ctx
}


