"use client";

import * as React from "react";
import {
  Card,
  CardDescription,
  CardHeader,
  CardTitle,
} from "@/components/ui/card";
import { cn } from "@/lib/utils";
import type { Transaction } from "@/lib/types";

interface DashboardStatsProps {
  transactions: Transaction[];
}

export function DashboardStats({ transactions }: DashboardStatsProps) {
  const {
    currentBalance,
    monthIncome,
    monthExpenses,
    monthTransactionsCount,
    monthLabel,
    netPast30,
  } = React.useMemo(() => {
    if (transactions.length === 0) {
      return {
        currentBalance: 0,
        monthIncome: 0,
        monthExpenses: 0,
        monthTransactionsCount: 0,
        monthLabel: "Past month",
        netPast30: 0,
      };
    }

    // Current balance = latest cumulativeDelta across all transactions
    const sortedAll = [...transactions].sort((a, b) => {
      const dateA = new Date(a.date).getTime();
      const dateB = new Date(b.date).getTime();
      if (dateA === dateB) {
        const createdAtA = new Date(a.createdAt).getTime();
        const createdAtB = new Date(b.createdAt).getTime();
        return createdAtB - createdAtA;
      }
      return dateB - dateA;
    });
    const latestTx = sortedAll[0];
    const currentBalanceValue =
      latestTx.cumulativeDelta ?? (latestTx as any).CumulativeDelta ?? 0;

    // Calculate past month (30 days ago to now)
    const now = new Date();
    const endDate = new Date(
      Date.UTC(now.getUTCFullYear(), now.getUTCMonth(), now.getUTCDate(), 23, 59, 59)
    );
    const startDate = new Date(endDate);
    startDate.setUTCDate(startDate.getUTCDate() - 30);
    startDate.setUTCHours(0, 0, 0, 0);

    const inMonth = transactions.filter((tx) => {
      const txDate = new Date(tx.date);
      return txDate >= startDate && txDate <= endDate;
    });

    let income = 0;
    let expenses = 0;
    inMonth.forEach((tx) => {
      if (tx.transactionType === "INCOME") {
        income += tx.amount;
      } else {
        expenses += tx.amount;
      }
    });

    const netPast30 = income - expenses;

    return {
      currentBalance: currentBalanceValue,
      monthIncome: income,
      monthExpenses: expenses,
      monthTransactionsCount: inMonth.length,
      monthLabel: "Past 30 days",
      netPast30,
    };
  }, [transactions]);

  return (
    <div className="grid gap-4 md:grid-cols-3 mb-4">
      <Card className="border-primary/10 bg-gradient-to-tl from-primary/10 via-card to-card">
        <CardHeader className="pb-3">
          <CardDescription className="text-xs font-bold uppercase tracking-[0.18em] ">
            Current balance
          </CardDescription>
          <CardTitle className="text-2xl">
            {new Intl.NumberFormat(undefined, {
              style: "currency",
              currency: "USD",
            }).format(currentBalance)}
          </CardTitle>
          <p
            className={cn(
              "mt-1 text-xs font-medium",
              netPast30 > 0 && "text-emerald-500",
              netPast30 < 0 && "text-destructive",
              netPast30 === 0 && "text-muted-foreground"
            )}
          >
            {netPast30 > 0 && "+"}
            {new Intl.NumberFormat(undefined, {
              style: "currency",
              currency: "USD",
            }).format(netPast30)}{" "}
            past 30 days
          </p>
          <p className="mt-0.5 text-xs text-muted-foreground">
            Based on your latest transaction history.
          </p>
        </CardHeader>
      </Card>

      <Card className="border-emerald-500/20 bg-gradient-to-tr from-emerald-500/10 via-card to-card">
        <CardHeader className="pb-3">
          <CardDescription className="text-xs font-semibold uppercase tracking-[0.18em] text-emerald-500/80">
            {monthLabel} income
          </CardDescription>
          <CardTitle className="text-xl text-emerald-500">
            {new Intl.NumberFormat(undefined, {
              style: "currency",
              currency: "USD",
            }).format(monthIncome)}
          </CardTitle>
          <p className="mt-1 text-xs text-muted-foreground">
            Total money coming in during the past month.
          </p>
        </CardHeader>
      </Card>

      <Card className="border-destructive/20 bg-gradient-to-tl from-destructive/10 via-card to-card">
        <CardHeader className="pb-3">
          <CardDescription className="text-xs font-semibold uppercase tracking-[0.18em] text-destructive/80">
            {monthLabel} expenses
          </CardDescription>
          <CardTitle className="text-xl text-destructive">
            {new Intl.NumberFormat(undefined, {
              style: "currency",
              currency: "USD",
            }).format(monthExpenses)}
          </CardTitle>
          <p className="mt-1 text-xs text-muted-foreground">
            Across {monthTransactionsCount} transaction
            {monthTransactionsCount === 1 ? "" : "s"} in the past month.
          </p>
        </CardHeader>
      </Card>
    </div>
  );
}
