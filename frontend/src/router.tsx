import * as React from "react";

type RouteDef = {
  path: string;
  element: React.ReactNode;
};

type RouterContextValue = {
  path: string;
  navigate: (to: string) => void;
};

const RouterContext = React.createContext<RouterContextValue | null>(null);

function normalizePath(p: string): string {
  if (!p) return "/";
  if (!p.startsWith("/")) return `/${p}`;
  return p;
}

function readHashPath(): string {
  const raw = window.location.hash.replace(/^#/, "");
  return normalizePath(raw || "/");
}

export function navigate(to: string) {
  window.location.hash = normalizePath(to);
}

export function Router({ children }: { children: React.ReactNode }) {
  const [path, setPath] = React.useState<string>(() => readHashPath());

  React.useEffect(() => {
    const onHashChange = () => setPath(readHashPath());
    window.addEventListener("hashchange", onHashChange);
    return () => window.removeEventListener("hashchange", onHashChange);
  }, []);

  const value = React.useMemo<RouterContextValue>(
    () => ({ path, navigate }),
    [path]
  );

  return (
    <RouterContext.Provider value={value}>{children}</RouterContext.Provider>
  );
}

export function useRouter() {
  const ctx = React.useContext(RouterContext);
  if (!ctx) throw new Error("useRouter must be used within <Router>");
  return ctx;
}

function matchPath(current: string, route: string): boolean {
  if (route === "*") return true;
  return normalizePath(current) === normalizePath(route);
}

export function Route({ path, element }: RouteDef) {
  const { path: current } = useRouter();
  if (!matchPath(current, path)) return null;
  return <>{element}</>;
}
