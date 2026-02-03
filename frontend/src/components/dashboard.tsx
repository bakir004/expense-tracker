"use client";

import { BalanceChart } from "@/components/balance-chart";
import { DashboardStats } from "@/components/dashboard-stats";
import { CreateTransactionDialog } from "@/components/create-transaction-dialog";
import { RecentTransactions } from "@/components/recent-transactions";
import { Card, CardContent } from "@/components/ui/card";
import { Skeleton } from "@/components/ui/skeleton";
import { useQuery } from "@tanstack/react-query";
import { api } from "@/lib/api";
import type { Transaction } from "@/lib/types";

export function Dashboard() {
  const userId = 1;

  const { data, isLoading, isError, error } = useQuery<
    { transactions: Transaction[] },
    Error
  >({
    queryKey: ["transactions", userId],
    queryFn: () => api.getTransactionsByUserId(userId),
  });

  const transactions = data?.transactions ?? [];

  return (
    <div className="min-h-screen bg-background text-foreground">
      <div className="mx-auto flex min-h-screen max-w-6xl flex-col gap-8 px-4 py-8 sm:px-6 lg:px-10">
        <header className="flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
          <div>
            <p className="text-xs font-semibold uppercase tracking-[0.2em] text-primary/80">
              Dashboard
            </p>
            <h1 className="mt-1 text-2xl font-semibold sm:text-3xl">
              Expense overview
            </h1>
            <p className="mt-2 max-w-xl text-sm text-muted-foreground">
              Track your balance, add new transactions, and explore how your
              spending evolves over time.
            </p>
          </div>

          <div className="flex items-center gap-2 rounded-full border bg-card/70 px-3 py-1.5 text-xs text-muted-foreground shadow-sm backdrop-blur">
            <span className="inline-flex h-2 w-2 rounded-full bg-emerald-400 shadow-[0_0_0_4px_rgba(16,185,129,0.25)]" />
            <span className="font-medium">Live data</span>
            <span className="hidden text-[0.7rem] text-muted-foreground/80 sm:inline">
              Connected to Expense Tracker API
            </span>
          </div>
        </header>

        <main className="flex-1 space-y-6">
          {isLoading && transactions.length === 0 ? (
            <Card>
              <CardContent className="pt-6">
                <div className="space-y-4">
                  <div className="flex gap-4">
                    <Skeleton className="h-24 flex-1 rounded-lg" />
                    <Skeleton className="h-24 flex-1 rounded-lg" />
                    <Skeleton className="h-24 flex-1 rounded-lg" />
                  </div>
                  <Skeleton className="h-[280px] w-full rounded-lg" />
                  <Skeleton className="h-[320px] w-full rounded-lg" />
                </div>
              </CardContent>
            </Card>
          ) : isError && transactions.length === 0 ? (
            <Card>
              <CardContent className="pt-6">
                <div className="text-center text-destructive">
                  {error?.message}
                </div>
              </CardContent>
            </Card>
          ) : (
            <>
              <DashboardStats transactions={transactions} />
              <BalanceChart userId={userId} />
              <RecentTransactions userId={userId} />
            </>
          )}
        </main>

        <footer className="pt-2 text-xs text-muted-foreground/80">
          Tip: click any point on the chart to see that day&apos;s
          transactions.
        </footer>
      </div>
    </div>
  );
}

