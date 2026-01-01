"use client";

import * as React from "react";
import {
  Area,
  AreaChart,
  CartesianGrid,
  ReferenceLine,
  XAxis,
  YAxis,
} from "recharts";

import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from "@/components/ui/card";
import {
  ChartContainer,
  ChartTooltip,
  ChartTooltipContent,
  type ChartConfig,
} from "@/components/ui/chart";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import { Textarea } from "@/components/ui/textarea";
import { api } from "@/lib/api";
import type { Transaction } from "@/lib/types";

const chartConfig = {
  balance: {
    label: "Balance",
    color: "var(--chart-1)",
  },
  balanceNegative: {
    label: "Balance (Negative)",
    color: "hsl(0, 84.2%, 60.2%)", // Red color for negative values
  },
} satisfies ChartConfig;

export function BalanceChart() {
  const [loading, setLoading] = React.useState(true);
  const [error, setError] = React.useState<string | null>(null);
  const [transactions, setTransactions] = React.useState<Transaction[]>([]);

  const [formData, setFormData] = React.useState({
    transactionType: "EXPENSE" as "EXPENSE" | "INCOME",
    amount: "",
    date: new Date().toISOString().split("T")[0],
    subject: "",
    notes: "",
    paymentMethod: 0,
    categoryId: "",
    transactionGroupId: "",
    incomeSource: "",
  });

  const [submitting, setSubmitting] = React.useState(false);

  const userId = 1;

  const fetchData = React.useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      const txsResponse = await api.getTransactionsByUserId(userId);
      setTransactions(txsResponse.transactions);
    } catch (e: unknown) {
      setError(e instanceof Error ? e.message : "Failed to load data");
    } finally {
      setLoading(false);
    }
  }, [userId]);

  React.useEffect(() => {
    void fetchData();
  }, [fetchData]);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setSubmitting(true);
    setError(null);

    try {
      const result = await api.createTransaction({
        userId,
        transactionType: formData.transactionType,
        amount: parseFloat(formData.amount),
        date: new Date(formData.date).toISOString(),
        subject: formData.subject,
        notes: formData.notes || null,
        paymentMethod: formData.paymentMethod,
        categoryId: formData.categoryId ? parseInt(formData.categoryId) : null,
        transactionGroupId: formData.transactionGroupId
          ? parseInt(formData.transactionGroupId)
          : null,
        incomeSource: formData.incomeSource || null,
      });

      console.log("Created transaction:", result);

      // Reset form
      setFormData({
        transactionType: "EXPENSE",
        amount: "",
        date: new Date().toISOString().split("T")[0],
        subject: "",
        notes: "",
        paymentMethod: 0,
        categoryId: "",
        transactionGroupId: "",
        incomeSource: "",
      });

      // Refresh data - add a small delay to ensure backend has processed
      await new Promise((resolve) => setTimeout(resolve, 200));
      setLoading(true);
      await fetchData();
    } catch (e: unknown) {
      console.error("Error creating transaction:", e);
      setError(e instanceof Error ? e.message : "Failed to create transaction");
    } finally {
      setSubmitting(false);
    }
  };

  // Calculate cumulative delta over time for November 2024
  const chartData = React.useMemo(() => {
    if (transactions.length === 0) {
      console.log("No transactions available");
      return [];
    }

    console.log("Total transactions:", transactions.length);
    const startDate = new Date("2024-11-01T00:00:00Z");
    const endDate = new Date("2024-11-30T23:59:59Z");

    // Filter transactions for November 2024
    const monthTransactions = transactions
      .filter((tx) => {
        const txDate = new Date(tx.date);
        // Normalize dates to midnight UTC for comparison
        const txDateNormalized = new Date(
          Date.UTC(
            txDate.getUTCFullYear(),
            txDate.getUTCMonth(),
            txDate.getUTCDate()
          )
        );
        const startDateNormalized = new Date(
          Date.UTC(
            startDate.getUTCFullYear(),
            startDate.getUTCMonth(),
            startDate.getUTCDate()
          )
        );
        const endDateNormalized = new Date(
          Date.UTC(
            endDate.getUTCFullYear(),
            endDate.getUTCMonth(),
            endDate.getUTCDate()
          )
        );
        return (
          txDateNormalized >= startDateNormalized &&
          txDateNormalized <= endDateNormalized
        );
      })
      .sort((a, b) => {
        const dateA = new Date(a.date).getTime();
        const dateB = new Date(b.date).getTime();
        if (dateA === dateB) {
          // For same date, order by createdAt ascending so the last one we see is the latest
          const createdAtA = new Date(a.createdAt).getTime();
          const createdAtB = new Date(b.createdAt).getTime();
          return createdAtA - createdAtB; // Ascending order
        }
        return dateA - dateB; // Ascending order by date
      });

    // Group transactions by date and use the last transaction's cumulativeDelta for each day
    // Since transactions are sorted by date (asc) then createdAt (asc), the last transaction
    // for each date will have the latest createdAt and thus the final cumulativeDelta for that day
    const transactionsByDate = new Map<string, number>();

    // Process transactions in order - since they're sorted by date ascending then createdAt ascending,
    // the last transaction we see for each date will be the one with the latest createdAt (final cumulativeDelta)
    monthTransactions.forEach((tx) => {
      // Handle both camelCase (after backend config) and PascalCase (fallback)
      const cumulativeDelta =
        tx.cumulativeDelta ?? (tx as any).CumulativeDelta ?? 0;
      // Extract date string directly from ISO format to avoid timezone shifts
      // The backend returns dates in ISO format (e.g., "2024-11-10T00:00:00" or "2024-11-10")
      // We extract just the YYYY-MM-DD part to preserve the original date
      let dateStr: string;
      if (tx.date.includes("T")) {
        // ISO format with time: extract date part before 'T'
        dateStr = tx.date.split("T")[0];
      } else if (tx.date.match(/^\d{4}-\d{2}-\d{2}/)) {
        // Already in YYYY-MM-DD format
        dateStr = tx.date.substring(0, 10);
      } else {
        // Fallback: try to parse and use local date components
        const txDate = new Date(tx.date);
        if (isNaN(txDate.getTime())) {
          // If still invalid, use the original string
          dateStr = tx.date;
        } else {
          // Use local date components (not UTC) to match what the user entered
          const year = txDate.getFullYear();
          const month = String(txDate.getMonth() + 1).padStart(2, "0");
          const day = String(txDate.getDate()).padStart(2, "0");
          dateStr = `${year}-${month}-${day}`;
        }
      }

      // Since transactions are sorted by date then ID ascending, we can simply
      // overwrite the value for each date - the last one will be the final value
      transactionsByDate.set(dateStr, cumulativeDelta);
    });

    // Get the last cumulativeDelta before November (for days without transactions)
    // Sort descending by date, then descending by createdAt to get the most recent transaction
    const transactionsBeforeMonth = transactions
      .filter((tx) => new Date(tx.date) < startDate)
      .sort((a, b) => {
        const dateA = new Date(a.date).getTime();
        const dateB = new Date(b.date).getTime();
        if (dateA === dateB) {
          // For same date, order by createdAt descending to get the latest
          const createdAtA = new Date(a.createdAt).getTime();
          const createdAtB = new Date(b.createdAt).getTime();
          return createdAtB - createdAtA; // Descending order
        }
        return dateB - dateA; // Descending order by date
      });

    let initialCumulativeDelta = 0;
    if (transactionsBeforeMonth.length > 0) {
      initialCumulativeDelta =
        transactionsBeforeMonth[0].cumulativeDelta ??
        (transactionsBeforeMonth[0] as any).CumulativeDelta ??
        0;
    }

    // Fill in all days of November (1-30)
    // For each day, find the last transaction on or before that day
    const chartDataPoints: Array<{ date: string; balance: number }> = [];
    const currentDate = new Date(startDate);

    // Create a sorted list of all dates with transactions (including before November)
    const allTransactionDates: Array<{
      dateStr: string;
      cumulativeDelta: number;
    }> = [];

    // Add transactions before November
    if (transactionsBeforeMonth.length > 0) {
      // Extract date string directly to avoid timezone shifts
      const beforeDateStr = transactionsBeforeMonth[0].date;
      let dateStr: string;
      if (beforeDateStr.includes("T")) {
        dateStr = beforeDateStr.split("T")[0];
      } else if (beforeDateStr.match(/^\d{4}-\d{2}-\d{2}/)) {
        dateStr = beforeDateStr.substring(0, 10);
      } else {
        // Fallback to parsing
        const beforeDate = new Date(beforeDateStr);
        const year = beforeDate.getFullYear();
        const month = String(beforeDate.getMonth() + 1).padStart(2, "0");
        const day = String(beforeDate.getDate()).padStart(2, "0");
        dateStr = `${year}-${month}-${day}`;
      }
      allTransactionDates.push({
        dateStr,
        cumulativeDelta:
          transactionsBeforeMonth[0].cumulativeDelta ??
          (transactionsBeforeMonth[0] as any).CumulativeDelta ??
          0,
      });
    }

    // Add transactions in November
    transactionsByDate.forEach((cumulativeDelta, dateStr) => {
      allTransactionDates.push({ dateStr, cumulativeDelta });
    });

    // Sort by date ascending
    allTransactionDates.sort((a, b) => a.dateStr.localeCompare(b.dateStr));

    while (currentDate <= endDate) {
      // Use UTC date components to match the date string format from transactions
      const year = currentDate.getUTCFullYear();
      const month = String(currentDate.getUTCMonth() + 1).padStart(2, "0");
      const day = String(currentDate.getUTCDate()).padStart(2, "0");
      const dateStr = `${year}-${month}-${day}`;

      // Check if this day has a transaction
      const cumulativeDelta = transactionsByDate.get(dateStr);

      if (cumulativeDelta !== undefined) {
        // This day has a transaction - use its cumulativeDelta
        chartDataPoints.push({
          date: dateStr,
          balance: Number(Number(cumulativeDelta).toFixed(2)),
        });
      } else {
        // For days without transactions, find the last transaction on or before this day
        // by looking backwards through the sorted transaction dates
        let lastCumulativeDelta = initialCumulativeDelta;
        for (let i = allTransactionDates.length - 1; i >= 0; i--) {
          if (allTransactionDates[i].dateStr <= dateStr) {
            lastCumulativeDelta = allTransactionDates[i].cumulativeDelta;
            break;
          }
        }
        chartDataPoints.push({
          date: dateStr,
          balance: Number(Number(lastCumulativeDelta).toFixed(2)),
        });
      }

      // Increment date using UTC to avoid timezone issues
      currentDate.setUTCDate(currentDate.getUTCDate() + 1);
    }

    return chartDataPoints;
  }, [transactions]);

  // Transform data to separate positive and negative values
  // Use NaN for values that shouldn't be displayed so Recharts doesn't render them
  const transformedChartData = React.useMemo(() => {
    return chartData.map((point) => ({
      ...point,
      balancePositive: point.balance >= 0 ? point.balance : NaN,
      balanceNegative: point.balance < 0 ? point.balance : NaN,
    }));
  }, [chartData]);

  // Calculate Y axis domain with rounded upper bound to thousands
  const yAxisDomain = React.useMemo(() => {
    if (chartData.length === 0) return [0, 1000];

    const maxValue = Math.max(...chartData.map((d) => d.balance));
    const minValue = Math.min(...chartData.map((d) => d.balance));

    // Round upper bound to nearest thousand (always round up)
    const roundedMax = Math.ceil(maxValue / 1000) * 1000;

    // Round lower bound to nearest thousand (always round down)
    const roundedMin = Math.floor(minValue / 1000) * 1000;

    return [roundedMin, roundedMax];
  }, [chartData]);

  if (loading && transactions.length === 0) {
    return (
      <Card>
        <CardContent className="pt-6">
          <div className="text-center text-muted-foreground">Loading...</div>
        </CardContent>
      </Card>
    );
  }

  if (error && transactions.length === 0) {
    return (
      <Card>
        <CardContent className="pt-6">
          <div className="text-center text-destructive">{error}</div>
        </CardContent>
      </Card>
    );
  }

  return (
    <div className="space-y-6">
      <Card>
        <CardHeader>
          <CardTitle>Create Transaction</CardTitle>
          <CardDescription>
            Add a new expense or income transaction
          </CardDescription>
        </CardHeader>
        <CardContent>
          <form onSubmit={handleSubmit} className="space-y-4">
            <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
              <div className="space-y-2">
                <Label htmlFor="transactionType">Transaction Type</Label>
                <Select
                  value={formData.transactionType}
                  onValueChange={(value) =>
                    setFormData({
                      ...formData,
                      transactionType: value as "EXPENSE" | "INCOME",
                    })
                  }
                >
                  <SelectTrigger id="transactionType">
                    <SelectValue />
                  </SelectTrigger>
                  <SelectContent>
                    <SelectItem value="EXPENSE">Expense</SelectItem>
                    <SelectItem value="INCOME">Income</SelectItem>
                  </SelectContent>
                </Select>
              </div>

              <div className="space-y-2">
                <Label htmlFor="amount">Amount</Label>
                <Input
                  id="amount"
                  type="number"
                  step="0.01"
                  min="0.01"
                  required
                  value={formData.amount}
                  onChange={(e) =>
                    setFormData({ ...formData, amount: e.target.value })
                  }
                  placeholder="0.00"
                />
              </div>

              <div className="space-y-2">
                <Label htmlFor="date">Date</Label>
                <Input
                  id="date"
                  type="date"
                  required
                  value={formData.date}
                  onChange={(e) =>
                    setFormData({ ...formData, date: e.target.value })
                  }
                />
              </div>

              <div className="space-y-2">
                <Label htmlFor="paymentMethod">Payment Method</Label>
                <Select
                  value={formData.paymentMethod.toString()}
                  onValueChange={(value) =>
                    setFormData({ ...formData, paymentMethod: parseInt(value) })
                  }
                >
                  <SelectTrigger id="paymentMethod">
                    <SelectValue />
                  </SelectTrigger>
                  <SelectContent>
                    <SelectItem value="0">Cash</SelectItem>
                    <SelectItem value="1">Debit Card</SelectItem>
                    <SelectItem value="2">Credit Card</SelectItem>
                    <SelectItem value="3">Bank Transfer</SelectItem>
                    <SelectItem value="4">Mobile Payment</SelectItem>
                    <SelectItem value="5">PayPal</SelectItem>
                    <SelectItem value="6">Crypto</SelectItem>
                    <SelectItem value="7">Other</SelectItem>
                  </SelectContent>
                </Select>
              </div>

              <div className="space-y-2">
                <Label htmlFor="subject">Subject</Label>
                <Input
                  id="subject"
                  required
                  value={formData.subject}
                  onChange={(e) =>
                    setFormData({ ...formData, subject: e.target.value })
                  }
                  placeholder="Brief description"
                />
              </div>

              <div className="space-y-2">
                <Label htmlFor="categoryId">Category ID</Label>
                <Input
                  id="categoryId"
                  type="number"
                  value={formData.categoryId}
                  onChange={(e) =>
                    setFormData({ ...formData, categoryId: e.target.value })
                  }
                  placeholder="Optional"
                />
              </div>

              <div className="space-y-2">
                <Label htmlFor="transactionGroupId">Transaction Group ID</Label>
                <Input
                  id="transactionGroupId"
                  type="number"
                  value={formData.transactionGroupId}
                  onChange={(e) =>
                    setFormData({
                      ...formData,
                      transactionGroupId: e.target.value,
                    })
                  }
                  placeholder="Optional"
                />
              </div>

              {formData.transactionType === "INCOME" && (
                <div className="space-y-2">
                  <Label htmlFor="incomeSource">Income Source</Label>
                  <Input
                    id="incomeSource"
                    value={formData.incomeSource}
                    onChange={(e) =>
                      setFormData({ ...formData, incomeSource: e.target.value })
                    }
                    placeholder="Optional"
                  />
                </div>
              )}
            </div>

            <div className="space-y-2">
              <Label htmlFor="notes">Notes</Label>
              <Textarea
                id="notes"
                value={formData.notes}
                onChange={(e) =>
                  setFormData({ ...formData, notes: e.target.value })
                }
                placeholder="Optional additional details"
                rows={3}
              />
            </div>

            {error && <div className="text-sm text-destructive">{error}</div>}

            <Button type="submit" disabled={submitting}>
              {submitting ? "Creating..." : "Create Transaction"}
            </Button>
          </form>
        </CardContent>
      </Card>

      <Card className="pt-0">
        <CardHeader className="flex items-center gap-2 space-y-0 border-b py-5 sm:flex-row">
          <div className="grid flex-1 gap-1">
            <CardTitle>Cumulative Delta Over November 2024</CardTitle>
            <CardDescription>
              Showing cumulative delta from transactions in November 2024
            </CardDescription>
          </div>
        </CardHeader>
        <CardContent className="px-2 pt-4 sm:px-6 sm:pt-6">
          <ChartContainer
            config={chartConfig}
            className="aspect-auto h-[250px] w-full"
          >
            <AreaChart data={transformedChartData}>
              <defs>
                <linearGradient id="fillBalance" x1="0" y1="0" x2="0" y2="1">
                  <stop
                    offset="5%"
                    stopColor="var(--color-balance)"
                    stopOpacity={0.8}
                  />
                  <stop
                    offset="95%"
                    stopColor="var(--color-balance)"
                    stopOpacity={0.1}
                  />
                </linearGradient>
                <linearGradient
                  id="fillBalanceNegative"
                  x1="0"
                  y1="0"
                  x2="0"
                  y2="1"
                >
                  <stop
                    offset="5%"
                    stopColor="var(--color-balanceNegative)"
                    stopOpacity={0.8}
                  />
                  <stop
                    offset="95%"
                    stopColor="var(--color-balanceNegative)"
                    stopOpacity={0.1}
                  />
                </linearGradient>
              </defs>
              <CartesianGrid vertical={false} />
              <XAxis
                dataKey="date"
                tickLine={false}
                axisLine={false}
                tickMargin={8}
                minTickGap={32}
                tickFormatter={(value) => {
                  if (!value) return "";
                  const date = new Date(value);
                  if (isNaN(date.getTime())) return String(value);
                  return date.toLocaleDateString("en-US", {
                    month: "short",
                    day: "numeric",
                  });
                }}
              />
              <YAxis
                domain={yAxisDomain}
                tickLine={false}
                axisLine={false}
                tickMargin={8}
                tickFormatter={(value) => {
                  return new Intl.NumberFormat(undefined, {
                    style: "currency",
                    currency: "USD",
                    minimumFractionDigits: 0,
                    maximumFractionDigits: 0,
                  }).format(value);
                }}
              />
              <ReferenceLine
                y={0}
                stroke="var(--border)"
                strokeDasharray="3 3"
              />
              <ChartTooltip
                cursor={false}
                content={
                  <ChartTooltipContent
                    labelFormatter={(value) => {
                      return new Date(value).toLocaleDateString("en-US", {
                        month: "short",
                        day: "numeric",
                      });
                    }}
                    indicator="dot"
                  />
                }
              />
              {/* Area for positive values */}
              <Area
                dataKey="balancePositive"
                type="linear"
                fill="url(#fillBalance)"
                stroke="var(--color-balance)"
                fillOpacity={0.6}
                baseLine={0}
              />
              {/* Area for negative values */}
              <Area
                dataKey="balanceNegative"
                type="linear"
                fill="url(#fillBalanceNegative)"
                stroke="var(--color-balanceNegative)"
                fillOpacity={0.6}
                baseLine={0}
              />
            </AreaChart>
          </ChartContainer>
        </CardContent>
      </Card>
    </div>
  );
}
