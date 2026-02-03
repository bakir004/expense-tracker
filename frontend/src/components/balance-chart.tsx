"use client";

import * as React from "react";
import {
  Area,
  AreaChart,
  CartesianGrid,
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
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import { Skeleton } from "@/components/ui/skeleton";
import { useQuery } from "@tanstack/react-query";
import { api } from "@/lib/api";
import type { Transaction } from "@/lib/types";
import { TransactionsModal } from "@/components/transactions-modal";

const chartConfig = {
  balance: {
    label: "Balance",
    color: "var(--chart-1)",
  },
} satisfies ChartConfig;

type TimeRange = "30d" | "3m" | "6m" | "1y" | "all";

const timeRangeLabels: Record<TimeRange, string> = {
  "30d": "Past 30 days",
  "3m": "Past 3 months",
  "6m": "Past 6 months",
  "1y": "Past year",
  all: "All time",
};

function getDateRange(range: TimeRange): { startDate: Date; endDate: Date } {
  const now = new Date();
  const endDate = new Date(
    Date.UTC(now.getUTCFullYear(), now.getUTCMonth(), now.getUTCDate(), 23, 59, 59)
  );

  let startDate: Date;

  switch (range) {
    case "30d":
      startDate = new Date(endDate);
      startDate.setUTCDate(startDate.getUTCDate() - 30);
      break;
    case "3m":
      startDate = new Date(endDate);
      startDate.setUTCMonth(startDate.getUTCMonth() - 3);
      break;
    case "6m":
      startDate = new Date(endDate);
      startDate.setUTCMonth(startDate.getUTCMonth() - 6);
      break;
    case "1y":
      startDate = new Date(endDate);
      startDate.setUTCFullYear(startDate.getUTCFullYear() - 1);
      break;
    case "all":
    default:
      // For all time, use a very old date
      startDate = new Date("1970-01-01T00:00:00Z");
      break;
  }

  startDate.setUTCHours(0, 0, 0, 0);

  return { startDate, endDate };
}

interface BalanceChartProps {
  userId: number;
}

export function BalanceChart({ userId }: BalanceChartProps) {
  const [timeRange, setTimeRange] = React.useState<TimeRange>("30d");

  // Get date range for the selected time range
  const { startDate, endDate } = getDateRange(timeRange);

  // Fetch transactions for the selected range
  const { data, isLoading: isFetching } = useQuery<
    { transactions: Transaction[] },
    Error
  >({
    queryKey: ["transactions-range", userId, timeRange],
    queryFn: async () => {
      // For "all" range, fetch all transactions without date filtering
      if (timeRange === "all") {
        const response = await api.getTransactionsByUserId(userId);
        return { transactions: response.transactions };
      }

      // For other ranges, use date range endpoint
      const response = await api.getTransactionsByUserIdAndDateRange(
        userId,
        startDate.toISOString(),
        endDate.toISOString()
      );
      return { transactions: response.transactions };
    },
  });

  const transactions = data?.transactions ?? [];

  // Calculate cumulative delta over time for selected range
  const chartData = React.useMemo(() => {
    if (transactions.length === 0) {
      return [];
    }

    const { startDate, endDate } = getDateRange(timeRange);

    // For "all time", find the actual earliest and latest dates from transactions
    let actualStartDate = startDate;
    let actualEndDate = endDate;

    if (timeRange === "all" && transactions.length > 0) {
      const sortedByDate = [...transactions].sort(
        (a, b) => new Date(a.date).getTime() - new Date(b.date).getTime()
      );
      actualStartDate = new Date(sortedByDate[0].date);
      actualStartDate.setUTCHours(0, 0, 0, 0);
      actualEndDate = new Date(sortedByDate[sortedByDate.length - 1].date);
      actualEndDate.setUTCHours(23, 59, 59, 999);
    }

    // Filter and sort transactions for the selected range
    const rangeTransactions = transactions
      .filter((tx) => {
        const txDate = new Date(tx.date);
        const txDateNormalized = new Date(
          Date.UTC(
            txDate.getUTCFullYear(),
            txDate.getUTCMonth(),
            txDate.getUTCDate()
          )
        );
        const startDateNormalized = new Date(
          Date.UTC(
            actualStartDate.getUTCFullYear(),
            actualStartDate.getUTCMonth(),
            actualStartDate.getUTCDate()
          )
        );
        const endDateNormalized = new Date(
          Date.UTC(
            actualEndDate.getUTCFullYear(),
            actualEndDate.getUTCMonth(),
            actualEndDate.getUTCDate()
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
          const createdAtA = new Date(a.createdAt).getTime();
          const createdAtB = new Date(b.createdAt).getTime();
          return createdAtA - createdAtB;
        }
        return dateA - dateB;
      });

    // Group transactions by date
    const transactionsByDate = new Map<string, number>();

    rangeTransactions.forEach((tx) => {
      const cumulativeDelta =
        tx.cumulativeDelta ?? (tx as any).CumulativeDelta ?? 0;
      let dateStr: string;
      if (tx.date.includes("T")) {
        dateStr = tx.date.split("T")[0];
      } else if (tx.date.match(/^\d{4}-\d{2}-\d{2}/)) {
        dateStr = tx.date.substring(0, 10);
      } else {
        const txDate = new Date(tx.date);
        if (isNaN(txDate.getTime())) {
          dateStr = tx.date;
        } else {
          const year = txDate.getFullYear();
          const month = String(txDate.getMonth() + 1).padStart(2, "0");
          const day = String(txDate.getDate()).padStart(2, "0");
          dateStr = `${year}-${month}-${day}`;
        }
      }
      transactionsByDate.set(dateStr, cumulativeDelta);
    });

    // Get the last cumulativeDelta before the range
    const transactionsBeforeRange = transactions
      .filter((tx) => new Date(tx.date) < actualStartDate)
      .sort((a, b) => {
        const dateA = new Date(a.date).getTime();
        const dateB = new Date(b.date).getTime();
        if (dateA === dateB) {
          const createdAtA = new Date(a.createdAt).getTime();
          const createdAtB = new Date(b.createdAt).getTime();
          return createdAtB - createdAtA;
        }
        return dateB - dateA;
      });

    let initialCumulativeDelta = 0;
    if (transactionsBeforeRange.length > 0) {
      initialCumulativeDelta =
        transactionsBeforeRange[0].cumulativeDelta ??
        (transactionsBeforeRange[0] as any).CumulativeDelta ??
        0;
    }

    // Build list of all transaction dates for lookback
    const allTransactionDates: Array<{
      dateStr: string;
      cumulativeDelta: number;
    }> = [];

    if (transactionsBeforeRange.length > 0) {
      const beforeDateStr = transactionsBeforeRange[0].date;
      let dateStr: string;
      if (beforeDateStr.includes("T")) {
        dateStr = beforeDateStr.split("T")[0];
      } else if (beforeDateStr.match(/^\d{4}-\d{2}-\d{2}/)) {
        dateStr = beforeDateStr.substring(0, 10);
      } else {
        const beforeDate = new Date(beforeDateStr);
        const year = beforeDate.getFullYear();
        const month = String(beforeDate.getMonth() + 1).padStart(2, "0");
        const day = String(beforeDate.getDate()).padStart(2, "0");
        dateStr = `${year}-${month}-${day}`;
      }
      allTransactionDates.push({
        dateStr,
        cumulativeDelta:
          transactionsBeforeRange[0].cumulativeDelta ??
          (transactionsBeforeRange[0] as any).CumulativeDelta ??
          0,
      });
    }

    transactionsByDate.forEach((cumulativeDelta, dateStr) => {
      allTransactionDates.push({ dateStr, cumulativeDelta });
    });

    allTransactionDates.sort((a, b) => a.dateStr.localeCompare(b.dateStr));

    // Fill in all days in the range
    const chartDataPoints: Array<{ date: string; balance: number }> = [];
    const currentDate = new Date(actualStartDate);

    while (currentDate <= actualEndDate) {
      const year = currentDate.getUTCFullYear();
      const month = String(currentDate.getUTCMonth() + 1).padStart(2, "0");
      const day = String(currentDate.getUTCDate()).padStart(2, "0");
      const dateStr = `${year}-${month}-${day}`;

      const cumulativeDelta = transactionsByDate.get(dateStr);

      if (cumulativeDelta !== undefined) {
        chartDataPoints.push({
          date: dateStr,
          balance: Number(Number(cumulativeDelta).toFixed(2)),
        });
      } else {
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

      currentDate.setUTCDate(currentDate.getUTCDate() + 1);
    }

    return chartDataPoints;
  }, [transactions, timeRange]);

  // Calculate Y axis domain with rounded bounds to thousands
  const yAxisDomain = React.useMemo(() => {
    if (chartData.length === 0) return [0, 1000];

    const maxValue = Math.max(...chartData.map((d) => d.balance));
    const minValue = Math.min(...chartData.map((d) => d.balance));

    const roundedMax = Math.ceil(maxValue / 1000) * 1000;
    const roundedMin = Math.floor(minValue / 1000) * 1000;

    return [roundedMin, roundedMax];
  }, [chartData]);

  // Custom dot component that's clickable with vertical rectangle
  const CustomDot = React.useCallback(
    (props: any) => {
      const { cx, cy, payload } = props;
      if (!cx || !cy || !payload) {
        return <g />;
      }

      const chartHeight = 250;
      const rectWidth = 40;
      const rectX = cx - rectWidth / 2;

      return (
        <g>
          <foreignObject
            x={rectX}
            y={0}
            width={rectWidth}
            height={chartHeight}
            style={{ overflow: "visible", pointerEvents: "all" }}
          >
            <div
              style={{
                width: "100%",
                height: "100%",
                position: "relative",
                pointerEvents: "all",
              }}
            >
              <TransactionsModal date={payload.date} transactions={transactions}>
                <button
                  type="button"
                  style={{
                    position: "absolute",
                    top: 0,
                    left: 0,
                    width: "100%",
                    height: "100%",
                    border: "none",
                    background: "transparent",
                    cursor: "pointer",
                    padding: 0,
                    margin: 0,
                    outline: "none",
                  }}
                  onMouseDown={(e) => e.preventDefault()}
                  aria-label={`View transactions for ${payload.date}`}
                />
              </TransactionsModal>
            </div>
          </foreignObject>
          <circle
            cx={cx}
            cy={cy}
            r={4}
            fill="var(--color-balance)"
            style={{ pointerEvents: "none" }}
          />
        </g>
      );
    },
    [transactions]
  );

  return (
    <Card className="pt-0 mb-4">
      <CardHeader className="flex items-center gap-2 space-y-0 border-b py-5 sm:flex-row">
        <div className="grid flex-1 gap-1">
          <CardTitle>Balance Over Time</CardTitle>
          <CardDescription>
            Showing cumulative balance for {timeRangeLabels[timeRange].toLowerCase()}
          </CardDescription>
        </div>
        <Select
          value={timeRange}
          onValueChange={(value) => setTimeRange(value as TimeRange)}
        >
          <SelectTrigger className="w-[160px]">
            <SelectValue placeholder="Select range" />
          </SelectTrigger>
          <SelectContent>
            <SelectItem value="30d">Past 30 days</SelectItem>
            <SelectItem value="3m">Past 3 months</SelectItem>
            <SelectItem value="6m">Past 6 months</SelectItem>
            <SelectItem value="1y">Past year</SelectItem>
            <SelectItem value="all">All time</SelectItem>
          </SelectContent>
        </Select>
      </CardHeader>
      <CardContent className="px-2 pt-4 sm:px-6 sm:pt-6">
        {isFetching ? (
          <Skeleton className="h-[250px] w-full rounded-lg" />
        ) : (
          <ChartContainer
            config={chartConfig}
            className="aspect-auto h-[250px] w-full"
          >
            <AreaChart data={chartData}>
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
            <ChartTooltip
              cursor={{ stroke: "var(--color-balance)", strokeWidth: 1 }}
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
            <Area
              dataKey="balance"
              type="linear"
              fill="url(#fillBalance)"
              stroke="var(--color-balance)"
              fillOpacity={0.6}
              dot={CustomDot}
              activeDot={false}
              animationDuration={400}
            />
          </AreaChart>
        </ChartContainer>
        )}
      </CardContent>
    </Card>
  );
}
