import * as React from "react";
import { useNavigate } from "react-router-dom";
import { api } from "@/lib/api";
import { useAuth } from "@/state/auth";
import { ThemeToggle } from "@/components/ThemeToggle";
import type {
  Category,
  Expense,
  ExpenseGroup,
  Income,
  PaymentMethod,
  User,
  UserBalance,
} from "@/lib/types";
import { paymentMethods } from "@/lib/types";

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

function fmtMoney(n: number) {
  return new Intl.NumberFormat(undefined, {
    style: "currency",
    currency: "USD",
  }).format(n);
}

function paymentLabel(m: PaymentMethod) {
  return paymentMethods.find((x) => x.value === m)?.label ?? `Method ${m}`;
}

export function DashboardPage() {
  const { userId, logout } = useAuth();
  const navigate = useNavigate();

  const [user, setUser] = React.useState<User | null>(null);
  const [balance, setBalance] = React.useState<UserBalance | null>(null);
  const [balanceError, setBalanceError] = React.useState<string | null>(null);
  const [balanceChecked, setBalanceChecked] = React.useState(false);

  const [categories, setCategories] = React.useState<Category[]>([]);
  const [groups, setGroups] = React.useState<ExpenseGroup[]>([]);
  const [expenses, setExpenses] = React.useState<Expense[]>([]);
  const [incomes, setIncomes] = React.useState<Income[]>([]);

  const [loading, setLoading] = React.useState(false);
  const [error, setError] = React.useState<string | null>(null);

  // Forms
  const [initialBalance, setInitialBalance] = React.useState<string>("0");
  const [balanceAtDate, setBalanceAtDate] = React.useState<string>("");
  const [balanceAtDateResult, setBalanceAtDateResult] = React.useState<
    number | null
  >(null);

  const [expenseAmount, setExpenseAmount] = React.useState("");
  const [expenseDate, setExpenseDate] = React.useState("");
  const [expenseCategoryId, setExpenseCategoryId] = React.useState<string>("");
  const [expensePaymentMethod, setExpensePaymentMethod] =
    React.useState<string>("0");
  const [expenseGroupId, setExpenseGroupId] = React.useState<string>("none");
  const [expenseDescription, setExpenseDescription] = React.useState("");

  const [incomeAmount, setIncomeAmount] = React.useState("");
  const [incomeDate, setIncomeDate] = React.useState("");
  const [incomePaymentMethod, setIncomePaymentMethod] =
    React.useState<string>("3");
  const [incomeSource, setIncomeSource] = React.useState("");
  const [incomeDescription, setIncomeDescription] = React.useState("");

  React.useEffect(() => {
    if (!userId) navigate("/login", { replace: true });
  }, [userId, navigate]);

  // Only show onboarding if we've checked for balance and it doesn't exist
  const needsInitialBalance =
    userId !== null && balanceChecked && balance === null;

  const refreshAll = React.useCallback(async () => {
    if (!userId) return;
    setLoading(true);
    setError(null);
    setBalanceError(null);
    try {
      const [u, cats, grps, exps, incs] = await Promise.all([
        api.getUserById(userId),
        api.getCategories(),
        api.getExpenseGroupsByUserId(userId),
        api.getExpensesByUserId(userId),
        api.getIncomesByUserId(userId),
      ]);
      setUser(u);
      setCategories(cats);
      setGroups(grps);
      setExpenses(exps);
      setIncomes(incs);

      try {
        const b = await api.getUserBalance(userId);
        setBalance(b);
        setBalanceChecked(true);
      } catch (e: unknown) {
        setBalance(null);
        setBalanceChecked(true);
        setBalanceError(
          e instanceof Error ? e.message : "Failed to load balance"
        );
      }
    } catch (e: unknown) {
      setError(e instanceof Error ? e.message : "Failed to load dashboard");
    } finally {
      setLoading(false);
    }
  }, [userId]);

  React.useEffect(() => {
    void refreshAll();
  }, [refreshAll]);

  async function onInitBalance(e: React.FormEvent) {
    e.preventDefault();
    if (!userId) return;
    setError(null);
    setLoading(true);
    try {
      const b = await api.initializeUserBalance({
        userId,
        initialBalance: Number(initialBalance),
      });
      setBalance(b);
      await refreshAll();
    } catch (e: unknown) {
      setError(e instanceof Error ? e.message : "Failed to initialize balance");
    } finally {
      setLoading(false);
    }
  }

  async function onRecalculate() {
    if (!userId) return;
    setError(null);
    setLoading(true);
    try {
      const b = await api.recalculateUserBalance(userId);
      setBalance(b);
      await refreshAll();
    } catch (e: unknown) {
      setError(e instanceof Error ? e.message : "Failed to recalculate");
    } finally {
      setLoading(false);
    }
  }

  async function onBalanceAtDate(e: React.FormEvent) {
    e.preventDefault();
    if (!userId) return;
    setError(null);
    setBalanceAtDateResult(null);
    setLoading(true);
    try {
      // Backend expects DateTime; sending an ISO string is fine.
      // For a date-only input, we’ll treat it as midnight local time.
      const iso = new Date(`${balanceAtDate}T00:00:00`).toISOString();
      const r = await api.getBalanceAtDate(userId, iso);
      setBalanceAtDateResult(r.balance);
    } catch (e: unknown) {
      setError(
        e instanceof Error ? e.message : "Failed to fetch balance at date"
      );
    } finally {
      setLoading(false);
    }
  }

  async function onCreateExpense(e: React.FormEvent) {
    e.preventDefault();
    if (!userId) return;
    setError(null);
    setLoading(true);
    try {
      const created = await api.createExpense({
        amount: Number(expenseAmount),
        date: expenseDate,
        description: expenseDescription || null,
        paymentMethod: Number(expensePaymentMethod) as PaymentMethod,
        categoryId: Number(expenseCategoryId),
        userId,
        expenseGroupId:
          expenseGroupId && expenseGroupId !== "none"
            ? Number(expenseGroupId)
            : null,
      });
      setExpenses((prev) => [created, ...prev]);
      setExpenseAmount("");
      setExpenseDescription("");
      setExpenseGroupId("none");
      await refreshAll();
    } catch (e: unknown) {
      setError(e instanceof Error ? e.message : "Failed to create expense");
    } finally {
      setLoading(false);
    }
  }

  async function onDeleteExpense(id: number) {
    setError(null);
    setLoading(true);
    try {
      await api.deleteExpense(id);
      setExpenses((prev) => prev.filter((x) => x.id !== id));
      await refreshAll();
    } catch (e: unknown) {
      setError(e instanceof Error ? e.message : "Failed to delete expense");
    } finally {
      setLoading(false);
    }
  }

  async function onCreateIncome(e: React.FormEvent) {
    e.preventDefault();
    if (!userId) return;
    setError(null);
    setLoading(true);
    try {
      const created = await api.createIncome({
        amount: Number(incomeAmount),
        date: incomeDate,
        description: incomeDescription || null,
        source: incomeSource || null,
        paymentMethod: Number(incomePaymentMethod) as PaymentMethod,
        userId,
      });
      setIncomes((prev) => [created, ...prev]);
      setIncomeAmount("");
      setIncomeDescription("");
      setIncomeSource("");
      await refreshAll();
    } catch (e: unknown) {
      setError(e instanceof Error ? e.message : "Failed to create income");
    } finally {
      setLoading(false);
    }
  }

  async function onDeleteIncome(id: number) {
    setError(null);
    setLoading(true);
    try {
      await api.deleteIncome(id);
      setIncomes((prev) => prev.filter((x) => x.id !== id));
      await refreshAll();
    } catch (e: unknown) {
      setError(e instanceof Error ? e.message : "Failed to delete income");
    } finally {
      setLoading(false);
    }
  }

  function onLogout() {
    logout();
    navigate("/login", { replace: true });
  }

  // Show loading state while checking balance
  if (userId && !balanceChecked) {
    return (
      <div className="mx-auto flex min-h-dvh w-full max-w-xl items-center justify-center px-4 py-10">
        <Card className="w-full">
          <CardContent className="pt-6">
            <div className="text-center text-muted-foreground">Loading...</div>
          </CardContent>
        </Card>
      </div>
    );
  }

  // Onboarding screen: require initial balance before showing the rest of the dashboard
  if (needsInitialBalance) {
    return (
      <div className="mx-auto flex min-h-dvh w-full max-w-xl items-center justify-center px-4 py-10">
        <Card className="w-full">
          <CardHeader>
            <CardTitle>Set your initial balance</CardTitle>
            <CardDescription>
              This is your starting point. After this, each income/expense will
              update the stored balance.
            </CardDescription>
          </CardHeader>
          <CardContent>
            <form onSubmit={onInitBalance}>
              <FieldGroup>
                <Field>
                  <FieldLabel htmlFor="init-balance-onboarding">
                    Initial balance
                  </FieldLabel>
                  <Input
                    id="init-balance-onboarding"
                    inputMode="decimal"
                    value={initialBalance}
                    onChange={(ev) => setInitialBalance(ev.target.value)}
                    placeholder="0"
                    required
                  />
                </Field>
                <Field orientation="horizontal">
                  <Button type="submit" disabled={loading || !userId}>
                    Continue
                  </Button>
                  <Button type="button" variant="outline" onClick={onLogout}>
                    Logout
                  </Button>
                </Field>
              </FieldGroup>
            </form>
          </CardContent>
          <CardFooter className="justify-between">
            <div className="text-sm text-muted-foreground">
              {balanceError
                ? balanceError
                : "No balance found for this user yet."}
            </div>
            {error ? (
              <div className="text-sm text-destructive">{error}</div>
            ) : null}
          </CardFooter>
        </Card>
      </div>
    );
  }

  return (
    <div className="mx-auto flex min-h-dvh w-full max-w-6xl flex-col gap-6 px-4 py-8">
      <div className="flex items-start justify-between gap-4">
        <div>
          <div className="text-sm text-muted-foreground">Dashboard</div>
          <div className="text-xl font-semibold">{user ? user.name : "…"}</div>
          <div className="text-sm text-muted-foreground">
            {user ? user.email : ""}
          </div>
        </div>
        <div className="flex gap-2">
          <ThemeToggle />
          <Button
            variant="outline"
            onClick={() => void refreshAll()}
            disabled={loading}
          >
            Refresh
          </Button>
          <Button variant="secondary" onClick={onLogout}>
            Logout
          </Button>
        </div>
      </div>

      {error ? <div className="text-sm text-destructive">{error}</div> : null}

      <div className="grid gap-6 lg:grid-cols-3">
        <Card className="lg:col-span-2">
          <CardHeader>
            <CardTitle>Current balance</CardTitle>
            <CardDescription>
              Stored in the database and updated after each income/expense.
            </CardDescription>
          </CardHeader>
          <CardContent className="grid gap-4">
            <div className="text-3xl font-semibold">
              {balance ? fmtMoney(balance.currentBalance) : "—"}
            </div>
            <div className="text-sm text-muted-foreground">
              {balance
                ? `Last updated: ${new Date(
                    balance.lastUpdated
                  ).toLocaleString()}`
                : balanceError
                ? balanceError
                : "No balance initialized yet."}
            </div>
            <div className="flex flex-wrap gap-2">
              <Button onClick={onRecalculate} disabled={loading || !userId}>
                Recalculate
              </Button>
            </div>
            <Separator />
            <form onSubmit={onInitBalance}>
              <FieldGroup>
                <Field>
                  <FieldLabel htmlFor="init-balance">
                    Initialize / reset starting balance
                  </FieldLabel>
                  <Input
                    id="init-balance"
                    inputMode="decimal"
                    value={initialBalance}
                    onChange={(ev) => setInitialBalance(ev.target.value)}
                    placeholder="0"
                  />
                </Field>
                <Field orientation="horizontal">
                  <Button
                    type="submit"
                    variant="secondary"
                    disabled={loading || !userId}
                  >
                    Initialize
                  </Button>
                </Field>
              </FieldGroup>
            </form>
          </CardContent>
        </Card>

        <Card>
          <CardHeader>
            <CardTitle>Balance at a date</CardTitle>
            <CardDescription>Lookup from balance history.</CardDescription>
          </CardHeader>
          <CardContent>
            <form onSubmit={onBalanceAtDate}>
              <FieldGroup>
                <Field>
                  <FieldLabel htmlFor="balance-date">Date</FieldLabel>
                  <Input
                    id="balance-date"
                    type="date"
                    value={balanceAtDate}
                    onChange={(ev) => setBalanceAtDate(ev.target.value)}
                    required
                  />
                </Field>
                <Field orientation="horizontal">
                  <Button
                    type="submit"
                    variant="secondary"
                    disabled={loading || !userId}
                  >
                    Lookup
                  </Button>
                </Field>
              </FieldGroup>
            </form>
          </CardContent>
          <CardFooter className="justify-between">
            <div className="text-sm text-muted-foreground">Result</div>
            <div className="text-sm font-medium">
              {balanceAtDateResult === null
                ? "—"
                : fmtMoney(balanceAtDateResult)}
            </div>
          </CardFooter>
        </Card>
      </div>

      <div className="grid gap-6 lg:grid-cols-2">
        <Card>
          <CardHeader>
            <CardTitle>Add expense</CardTitle>
            <CardDescription>
              Creates an expense and appends a balance snapshot.
            </CardDescription>
          </CardHeader>
          <CardContent>
            <form onSubmit={onCreateExpense}>
              <FieldGroup>
                <div className="grid gap-4 md:grid-cols-2">
                  <Field>
                    <FieldLabel htmlFor="exp-amount">Amount</FieldLabel>
                    <Input
                      id="exp-amount"
                      inputMode="decimal"
                      value={expenseAmount}
                      onChange={(ev) => setExpenseAmount(ev.target.value)}
                      required
                    />
                  </Field>
                  <Field>
                    <FieldLabel htmlFor="exp-date">Date</FieldLabel>
                    <Input
                      id="exp-date"
                      type="date"
                      value={expenseDate}
                      onChange={(ev) => setExpenseDate(ev.target.value)}
                      required
                    />
                  </Field>
                </div>
                <div className="grid gap-4 md:grid-cols-2">
                  <Field>
                    <FieldLabel>Category</FieldLabel>
                    <Select
                      value={expenseCategoryId}
                      onValueChange={setExpenseCategoryId}
                    >
                      <SelectTrigger>
                        <SelectValue placeholder="Select category" />
                      </SelectTrigger>
                      <SelectContent>
                        <SelectGroup>
                          {categories.map((c) => (
                            <SelectItem key={c.id} value={String(c.id)}>
                              {c.icon ? `${c.icon} ` : ""}
                              {c.name}
                            </SelectItem>
                          ))}
                        </SelectGroup>
                      </SelectContent>
                    </Select>
                  </Field>
                  <Field>
                    <FieldLabel>Payment method</FieldLabel>
                    <Select
                      value={expensePaymentMethod}
                      onValueChange={setExpensePaymentMethod}
                    >
                      <SelectTrigger>
                        <SelectValue placeholder="Select method" />
                      </SelectTrigger>
                      <SelectContent>
                        <SelectGroup>
                          {paymentMethods.map((m) => (
                            <SelectItem key={m.value} value={String(m.value)}>
                              {m.label}
                            </SelectItem>
                          ))}
                        </SelectGroup>
                      </SelectContent>
                    </Select>
                  </Field>
                </div>
                <Field>
                  <FieldLabel>Expense group (optional)</FieldLabel>
                  <Select
                    value={expenseGroupId}
                    onValueChange={setExpenseGroupId}
                  >
                    <SelectTrigger>
                      <SelectValue placeholder="None" />
                    </SelectTrigger>
                    <SelectContent>
                      <SelectGroup>
                        <SelectItem value="none">None</SelectItem>
                        {groups.map((g) => (
                          <SelectItem key={g.id} value={String(g.id)}>
                            {g.name}
                          </SelectItem>
                        ))}
                      </SelectGroup>
                    </SelectContent>
                  </Select>
                </Field>
                <Field>
                  <FieldLabel htmlFor="exp-desc">
                    Description (optional)
                  </FieldLabel>
                  <Input
                    id="exp-desc"
                    value={expenseDescription}
                    onChange={(ev) => setExpenseDescription(ev.target.value)}
                    placeholder="What was this for?"
                  />
                </Field>
                <Field orientation="horizontal">
                  <Button
                    type="submit"
                    disabled={loading || !expenseCategoryId}
                  >
                    Add expense
                  </Button>
                </Field>
              </FieldGroup>
            </form>
          </CardContent>
        </Card>

        <Card>
          <CardHeader>
            <CardTitle>Add income</CardTitle>
            <CardDescription>
              Creates an income and appends a balance snapshot.
            </CardDescription>
          </CardHeader>
          <CardContent>
            <form onSubmit={onCreateIncome}>
              <FieldGroup>
                <div className="grid gap-4 md:grid-cols-2">
                  <Field>
                    <FieldLabel htmlFor="inc-amount">Amount</FieldLabel>
                    <Input
                      id="inc-amount"
                      inputMode="decimal"
                      value={incomeAmount}
                      onChange={(ev) => setIncomeAmount(ev.target.value)}
                      required
                    />
                  </Field>
                  <Field>
                    <FieldLabel htmlFor="inc-date">Date</FieldLabel>
                    <Input
                      id="inc-date"
                      type="date"
                      value={incomeDate}
                      onChange={(ev) => setIncomeDate(ev.target.value)}
                      required
                    />
                  </Field>
                </div>
                <div className="grid gap-4 md:grid-cols-2">
                  <Field>
                    <FieldLabel>Payment method</FieldLabel>
                    <Select
                      value={incomePaymentMethod}
                      onValueChange={setIncomePaymentMethod}
                    >
                      <SelectTrigger>
                        <SelectValue placeholder="Select method" />
                      </SelectTrigger>
                      <SelectContent>
                        <SelectGroup>
                          {paymentMethods.map((m) => (
                            <SelectItem key={m.value} value={String(m.value)}>
                              {m.label}
                            </SelectItem>
                          ))}
                        </SelectGroup>
                      </SelectContent>
                    </Select>
                  </Field>
                  <Field>
                    <FieldLabel htmlFor="inc-source">
                      Source (optional)
                    </FieldLabel>
                    <Input
                      id="inc-source"
                      value={incomeSource}
                      onChange={(ev) => setIncomeSource(ev.target.value)}
                      placeholder="Employer / client"
                    />
                  </Field>
                </div>
                <Field>
                  <FieldLabel htmlFor="inc-desc">
                    Description (optional)
                  </FieldLabel>
                  <Input
                    id="inc-desc"
                    value={incomeDescription}
                    onChange={(ev) => setIncomeDescription(ev.target.value)}
                    placeholder="e.g. Salary"
                  />
                </Field>
                <Field orientation="horizontal">
                  <Button type="submit" disabled={loading}>
                    Add income
                  </Button>
                </Field>
              </FieldGroup>
            </form>
          </CardContent>
        </Card>
      </div>

      <div className="grid gap-6 lg:grid-cols-2">
        <Card>
          <CardHeader>
            <CardTitle>Recent expenses</CardTitle>
            <CardDescription>{expenses.length} item(s)</CardDescription>
          </CardHeader>
          <CardContent className="grid gap-3">
            {expenses.slice(0, 10).map((e) => (
              <div key={e.id} className="grid gap-2">
                <div className="flex items-center justify-between gap-2">
                  <div className="font-medium">{fmtMoney(e.amount)}</div>
                  <div className="text-sm text-muted-foreground">{e.date}</div>
                </div>
                <div className="text-sm text-muted-foreground">
                  {paymentLabel(e.paymentMethod)} • Category #{e.categoryId}
                  {e.description ? ` • ${e.description}` : ""}
                </div>
                <div className="flex justify-end">
                  <Button
                    variant="outline"
                    size="sm"
                    onClick={() => void onDeleteExpense(e.id)}
                    disabled={loading}
                  >
                    Delete
                  </Button>
                </div>
                <Separator />
              </div>
            ))}
            {expenses.length === 0 ? (
              <div className="text-sm text-muted-foreground">
                No expenses yet.
              </div>
            ) : null}
          </CardContent>
        </Card>

        <Card>
          <CardHeader>
            <CardTitle>Recent incomes</CardTitle>
            <CardDescription>{incomes.length} item(s)</CardDescription>
          </CardHeader>
          <CardContent className="grid gap-3">
            {incomes.slice(0, 10).map((i) => (
              <div key={i.id} className="grid gap-2">
                <div className="flex items-center justify-between gap-2">
                  <div className="font-medium">{fmtMoney(i.amount)}</div>
                  <div className="text-sm text-muted-foreground">{i.date}</div>
                </div>
                <div className="text-sm text-muted-foreground">
                  {paymentLabel(i.paymentMethod)}
                  {i.source ? ` • ${i.source}` : ""}
                  {i.description ? ` • ${i.description}` : ""}
                </div>
                <div className="flex justify-end">
                  <Button
                    variant="outline"
                    size="sm"
                    onClick={() => void onDeleteIncome(i.id)}
                    disabled={loading}
                  >
                    Delete
                  </Button>
                </div>
                <Separator />
              </div>
            ))}
            {incomes.length === 0 ? (
              <div className="text-sm text-muted-foreground">
                No incomes yet.
              </div>
            ) : null}
          </CardContent>
        </Card>
      </div>
    </div>
  );
}
