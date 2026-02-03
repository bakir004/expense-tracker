"use client";

import * as React from "react";
import { format } from "date-fns";
import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { Calendar } from "@/components/ui/calendar";
import { Checkbox } from "@/components/ui/checkbox";
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuRadioGroup,
  DropdownMenuRadioItem,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu";
import {
  Popover,
  PopoverContent,
  PopoverTrigger,
} from "@/components/ui/popover";
import { Input } from "@/components/ui/input";
import { Skeleton } from "@/components/ui/skeleton";
import { keepPreviousData, useQuery } from "@tanstack/react-query";
import { api } from "@/lib/api";
import { cn } from "@/lib/utils";
import type { Transaction, TransactionQueryParams } from "@/lib/types";
import {
  ArrowDownNarrowWide,
  ArrowUpNarrowWide,
  ArrowUpDown,
  Filter,
} from "lucide-react";
import { CreateTransactionDialog } from "./create-transaction-dialog";

const DEFAULT_LIMIT = 25;

const PAYMENT_METHODS: { value: number; label: string }[] = [
  { value: 0, label: "Cash" },
  { value: 1, label: "Debit Card" },
  { value: 2, label: "Credit Card" },
  { value: 3, label: "Bank Transfer" },
  { value: 4, label: "Mobile Payment" },
  { value: 5, label: "PayPal" },
  { value: 6, label: "Crypto" },
  { value: 7, label: "Other" },
];

const PAYMENT_METHODS_MAP: Record<string, string> = Object.fromEntries(
  PAYMENT_METHODS.map((p) => [String(p.value), p.label])
);

function formatDateForApi(d: Date): string {
  const day = String(d.getDate()).padStart(2, "0");
  const month = String(d.getMonth() + 1).padStart(2, "0");
  const year = d.getFullYear();
  return `${day}-${month}-${year}`;
}

/** Parse dd-MM-yyyy to Date for Calendar */
function parseApiDate(apiDate: string | undefined): Date | undefined {
  if (!apiDate) return undefined;
  const parts = apiDate.split("-");
  if (parts.length !== 3) return undefined;
  const [d, m, y] = parts;
  return new Date(Number(y), Number(m) - 1, Number(d));
}

type SortByOption = "subject" | "paymentMethod" | "category" | "amount" | null;
type SortDirection = "asc" | "desc";

interface RecentTransactionsProps {
  userId: number;
  limit?: number;
}

type FilterKey = "date" | "subject" | "category" | "payment" | "amount";

export function RecentTransactions({
  userId,
  limit = DEFAULT_LIMIT,
}: RecentTransactionsProps) {
  const [queryParams, setQueryParams] = React.useState<TransactionQueryParams>({});
  const [openFilter, setOpenFilter] = React.useState<FilterKey | null>(null);
  const [pendingParams, setPendingParams] = React.useState<TransactionQueryParams>({});
  const [filterSubjectInput, setFilterSubjectInput] = React.useState("");
  const [minLoading, setMinLoading] = React.useState(false);
  const scrollRestoreRef = React.useRef<number | null>(null);
  const subjectDebounceRef = React.useRef<ReturnType<typeof setTimeout> | null>(null);

  const debouncedSetPendingSubject = React.useCallback((value: string) => {
    if (subjectDebounceRef.current) window.clearTimeout(subjectDebounceRef.current);
    subjectDebounceRef.current = window.setTimeout(() => {
      subjectDebounceRef.current = null;
      setPendingParams((prev) => {
        const next = { ...prev, subject: value || undefined };
        if (next.subject === "") delete next.subject;
        return next;
      });
    }, 300);
  }, []);

  React.useEffect(() => () => {
    if (subjectDebounceRef.current) window.clearTimeout(subjectDebounceRef.current);
  }, []);

  React.useEffect(() => {
    if (!minLoading) return;
    const t = window.setTimeout(() => setMinLoading(false), 300);
    return () => window.clearTimeout(t);
  }, [minLoading]);

  const setPending = React.useCallback(
    (updates: Partial<TransactionQueryParams> | ((prev: TransactionQueryParams) => TransactionQueryParams)) => {
      setPendingParams((prev) => {
        const next = typeof updates === "function" ? updates(prev) : { ...prev, ...updates };
        if (next.subject === "") delete next.subject;
        if (next.categoryIds?.length === 0) delete next.categoryIds;
        if (next.paymentMethods?.length === 0) delete next.paymentMethods;
        if (next.transactionType === undefined) delete next.transactionType;
        if (next.dateFrom === undefined || next.dateFrom === "") delete next.dateFrom;
        if (next.dateTo === undefined || next.dateTo === "") delete next.dateTo;
        return next;
      });
    },
    []
  );

  const openFilterPopover = (key: FilterKey) => {
    scrollRestoreRef.current = window.scrollY;
    setPendingParams({ ...queryParams });
    if (key === "subject") setFilterSubjectInput(queryParams.subject ?? "");
    setOpenFilter(key);
  };

  const { data, isLoading, isFetching } = useQuery({
    queryKey: ["transactions", userId, queryParams],
    queryFn: () => api.getTransactionsByUserId(userId, queryParams),
    placeholderData: keepPreviousData,
  });

  const { data: categoriesData } = useQuery({
    queryKey: ["categories"],
    queryFn: () => api.getCategories(),
  });
  const { data: groupsData } = useQuery({
    queryKey: ["transaction-groups", userId],
    queryFn: () => api.getTransactionGroupsByUserId(userId),
  });

  const transactions = data?.transactions ?? [];
  const categories = categoriesData?.categories ?? [];
  const categoryById = React.useMemo(() => {
    const map: Record<number, { name: string; icon?: string | null }> = {};
    categories.forEach((c) => {
      map[c.id] = { name: c.name, icon: c.icon };
    });
    return map;
  }, [categories]);

  const groupById = React.useMemo(() => {
    const map: Record<number, string> = {};
    (groupsData ?? []).forEach((g) => {
      map[g.id] = g.name;
    });
    return map;
  }, [groupsData]);

  const sorted = React.useMemo(() => {
    return [...transactions].slice(0, limit);
  }, [transactions, limit]);

  const dayStripByIndex = React.useMemo(() => {
    const getDayKey = (tx: Transaction) =>
      tx.date.includes("T") ? tx.date.slice(0, 10) : tx.date.substring(0, 10);
    const result: number[] = [];
    let stripe = 0;
    for (let i = 0; i < sorted.length; i++) {
      const dayKey = getDayKey(sorted[i]);
      const prevKey = i > 0 ? getDayKey(sorted[i - 1]) : null;
      if (prevKey != null && dayKey !== prevKey) stripe = 1 - stripe;
      result.push(stripe);
    }
    return result;
  }, [sorted]);

  const updateParams = React.useCallback((updates: Partial<TransactionQueryParams>) => {
    scrollRestoreRef.current = window.scrollY;
    setQueryParams((prev) => {
      const next = { ...prev, ...updates };
      if (updates.subject === "") delete next.subject;
      if (updates.categoryIds?.length === 0) delete next.categoryIds;
      if (updates.paymentMethods?.length === 0) delete next.paymentMethods;
      if (updates.transactionType === undefined) delete next.transactionType;
      if (updates.dateFrom === undefined || updates.dateFrom === "") delete next.dateFrom;
      if (updates.dateTo === undefined || updates.dateTo === "") delete next.dateTo;
      if (updates.sortBy === undefined) delete next.sortBy;
      if (updates.sortDirection === undefined) delete next.sortDirection;
      return next;
    });
  }, []);

  const closeFilterPopover = React.useCallback(
    (overrides?: Partial<TransactionQueryParams>) => {
      if (openFilter !== null) {
        const toApply =
          openFilter === "subject"
            ? { ...pendingParams, subject: filterSubjectInput || undefined }
            : overrides
              ? { ...pendingParams, ...overrides }
              : pendingParams;
        updateParams(toApply);
        setOpenFilter(null);
        setMinLoading(true);
      }
    },
    [openFilter, pendingParams, filterSubjectInput, updateParams]
  );

  React.useEffect(() => {
    if (scrollRestoreRef.current === null) return;
    const y = scrollRestoreRef.current;
    scrollRestoreRef.current = null;
    const restore = () => window.scrollTo({ top: y, left: window.scrollX });
    requestAnimationFrame(restore);
    const t = window.setTimeout(restore, 0);
    const t2 = window.setTimeout(restore, 100);
    return () => {
      window.clearTimeout(t);
      window.clearTimeout(t2);
    };
  }, [queryParams]);

  const setSort = (column: SortByOption, direction: SortDirection | null) => {
    if (direction === null) {
      updateParams({ sortBy: undefined, sortDirection: undefined });
      return;
    }
    updateParams({
      sortBy: column ?? undefined,
      sortDirection: direction,
    });
  };

  const sortBy = queryParams.sortBy ?? null;
  const sortDirection = queryParams.sortDirection ?? "desc";

  const hasActiveFilters =
    (queryParams.subject?.length ?? 0) > 0 ||
    (queryParams.categoryIds?.length ?? 0) > 0 ||
    (queryParams.paymentMethods?.length ?? 0) > 0 ||
    queryParams.transactionType != null ||
    (queryParams.dateFrom?.length ?? 0) > 0 ||
    (queryParams.dateTo?.length ?? 0) > 0;

  const isInitialLoad = Object.keys(queryParams).length === 0;

  const TableHeaderCell = ({
    label,
    column,
    filterKey,
    filterContent,
  }: {
    label: string;
    column: SortByOption;
    filterKey?: FilterKey;
    filterContent?: (
      pending: TransactionQueryParams,
      setPending: (u: Partial<TransactionQueryParams>) => void,
      closeAndApply: (overrides?: Partial<TransactionQueryParams>) => void
    ) => React.ReactNode;
  }) => (
    <th className="px-2 py-3 text-left font-medium text-muted-foreground">
      <div className="flex items-center gap-1">
        <span className="min-w-0 truncate">{label}</span>
        <div className="flex shrink-0 items-center gap-0.5">
          <DropdownMenu>
            <DropdownMenuTrigger asChild>
              <Button
                variant="ghost"
                size="icon-xs"
                className={cn(
                  "h-6 w-6 text-muted-foreground hover:text-foreground",
                  (sortBy === column || (column === null && (sortBy == null))) &&
                    "text-primary"
                )}
                title="Sort"
              >
                <ArrowUpDown className="h-3.5 w-3.5" />
              </Button>
            </DropdownMenuTrigger>
            <DropdownMenuContent align="start" className="min-w-44">
              <DropdownMenuRadioGroup
                value={
                  sortBy === column || (column === null && sortBy == null)
                    ? sortDirection
                    : "none"
                }
                onValueChange={(value) => {
                  if (value === "none") setSort(column, null);
                  else if (value === "asc" || value === "desc") setSort(column, value);
                }}
              >
                <DropdownMenuRadioItem value="none">
                  None
                </DropdownMenuRadioItem>
                <DropdownMenuRadioItem value="asc">
                  <ArrowUpNarrowWide className="mr-1.5 h-3.5 w-3.5" />
                  Ascending
                </DropdownMenuRadioItem>
                <DropdownMenuRadioItem value="desc">
                  <ArrowDownNarrowWide className="mr-1.5 h-3.5 w-3.5" />
                  Descending
                </DropdownMenuRadioItem>
              </DropdownMenuRadioGroup>
            </DropdownMenuContent>
          </DropdownMenu>
          {filterKey != null && filterContent != null && (
            <Popover
              open={openFilter === filterKey}
              onOpenChange={(open) => {
                if (open) openFilterPopover(filterKey);
                else closeFilterPopover();
              }}
            >
              <PopoverTrigger asChild>
                <Button
                  variant="ghost"
                  size="icon-xs"
                  className={cn(
                    "h-6 w-6 text-muted-foreground hover:text-foreground",
                    hasActiveFilters && "text-primary"
                  )}
                  title="Filter"
                >
                  <Filter className="h-3.5 w-3.5" />
                </Button>
              </PopoverTrigger>
              <PopoverContent align="start" className="w-64">
                {filterContent(pendingParams, setPending, closeFilterPopover)}
              </PopoverContent>
            </Popover>
          )}
        </div>
      </div>
    </th>
  );

  const SkeletonTableRows = ({ rows = 8 }: { rows?: number }) => (
    <>
      {Array.from({ length: rows }).map((_, i) => (
        <tr key={i} className="border-b last:border-0">
          <td className="px-4 py-3">
            <Skeleton className="h-4 w-20" />
          </td>
          <td className="px-4 py-3">
            <div className="space-y-1.5">
              <Skeleton className="h-4 w-3/4 max-w-[14rem]" />
              <Skeleton className="h-3 w-1/2 max-w-[10rem]" />
            </div>
          </td>
          <td className="px-4 py-3">
            <Skeleton className="h-4 w-16" />
          </td>
          <td className="px-4 py-3">
            <Skeleton className="h-4 w-14" />
          </td>
          <td className="px-4 py-3">
            <Skeleton className="h-4 w-16" />
          </td>
          <td className="px-4 py-3 text-right">
            <Skeleton className="h-4 w-14 inline-block ml-auto" />
          </td>
        </tr>
      ))}
    </>
  );

  if (isLoading && transactions.length === 0 && isInitialLoad) {
    return (
      <Card>
        <CardHeader className="pb-3">
          <CardTitle className="text-lg">
            <Skeleton className="h-6 w-40" />
          </CardTitle>
          <CardDescription>
            <Skeleton className="mt-1 h-4 w-32" />
          </CardDescription>
        </CardHeader>
        <CardContent>
          <div className="overflow-x-auto rounded-md border">
            <table className="w-full text-sm">
              <thead className="">
                <tr className="border-b bg-muted/50">
                  {Array.from({ length: 6 }).map((_, i) => (
                    <th key={i} className="px-4 py-3 text-left">
                      <Skeleton className="h-4 w-16" />
                    </th>
                  ))}
                </tr>
              </thead>
              <tbody>
                <SkeletonTableRows rows={10} />
              </tbody>
            </table>
          </div>
        </CardContent>
      </Card>
    );
  }

  if (transactions.length === 0 && !hasActiveFilters) {
    return (
      <Card>
        <CardHeader>
          <CardTitle className="text-lg">Recent transactions</CardTitle>
          <CardDescription>Your most recent activity</CardDescription>
        </CardHeader>
        <CardContent>
          <p className="text-sm text-muted-foreground">
            No transactions yet. Create one to see it here.
          </p>
        </CardContent>
      </Card>
    );
  }

  return (
    <Card>
      <CardHeader className="pb-3">
        <div className="flex flex-col gap-1 sm:flex-row sm:items-center sm:justify-between">
          <div className="flex items-center gap-2 justify-between w-full">
            <div className="flex flex-col gap-1">
                <CardTitle className="text-lg">Recent transactions</CardTitle>
                <CardDescription>
                {sorted.length === transactions.length
                    ? `${transactions.length} transaction${transactions.length === 1 ? "" : "s"}`
                    : `Showing ${sorted.length} of ${transactions.length}`}
                {hasActiveFilters && " (filtered)"}
                </CardDescription>
              </div>
            <CreateTransactionDialog />
          </div>
          {hasActiveFilters && (
            <Button
              variant="outline"
              size="sm"
              onClick={() => {
                scrollRestoreRef.current = window.scrollY;
                setQueryParams({});
                setMinLoading(true);
              }}
            >
              Clear filters
            </Button>
          )}
        </div>
      </CardHeader>
      <CardContent>
        <div className="relative overflow-x-auto rounded-md border">
          {(isFetching || minLoading) && (
            <div className="absolute inset-0 z-10 rounded-md bg-background/80 backdrop-blur-[1px]">
              <table className="w-full text-sm">
                <thead>
                  <tr className="border-b bg-muted/50">
                    {Array.from({ length: 6 }).map((_, i) => (
                      <th key={i} className="px-4 py-3 text-left" />
                    ))}
                  </tr>
                </thead>
                <tbody>
                  <SkeletonTableRows rows={Math.max(5, sorted.length)} />
                </tbody>
              </table>
            </div>
          )}
          <table className="w-full text-sm">
            <thead>
              <tr className="border-b bg-muted/50">
                <TableHeaderCell
                  label="Date"
                  column={null}
                  filterKey="date"
                  filterContent={(pending, setPending, closeAndApply) => {
                    const from = parseApiDate(pending.dateFrom);
                    const to = parseApiDate(pending.dateTo);
                    const range: { from: Date; to?: Date } | undefined =
                      from ? { from, to: to ?? undefined } : undefined;
                    return (
                      <div className="space-y-2">
                        <p className="text-xs font-medium text-muted-foreground">
                          Date range
                        </p>
                        <Calendar
                          mode="range"
                          selected={range}
                          onSelect={(r) => {
                            if (!r?.from) {
                              setPending({ dateFrom: undefined, dateTo: undefined });
                              return;
                            }
                            const dateFrom = formatDateForApi(r.from);
                            const dateTo = r.to ? formatDateForApi(r.to) : undefined;
                            setPending({ dateFrom, dateTo });
                            if (r.to) {
                              closeAndApply({ dateFrom, dateTo });
                            }
                          }}
                          numberOfMonths={1}
                          initialFocus
                        />
                        <p className="text-xs text-muted-foreground">
                          {from
                            ? to
                              ? `${format(from, "PP")} – ${format(to, "PP")}`
                              : format(from, "PPP")
                            : "Select start and end date"}
                        </p>
                      </div>
                    );
                  }}
                />
                <TableHeaderCell
                  label="Subject"
                  column="subject"
                  filterKey="subject"
                  filterContent={() => (
                    <div className="space-y-2">
                      <p className="text-xs font-medium text-muted-foreground">
                        Subject contains
                      </p>
                      <Input
                        placeholder="Search…"
                        value={filterSubjectInput}
                        onChange={(e) => {
                          const v = e.target.value;
                          setFilterSubjectInput(v);
                          debouncedSetPendingSubject(v);
                        }}
                      />
                    </div>
                  )}
                />
                <TableHeaderCell
                  label="Category"
                  column="category"
                  filterKey="category"
                  filterContent={(pending, setPending) => (
                    <div className="space-y-2">
                      <p className="text-xs font-medium text-muted-foreground">
                        Categories
                      </p>
                      <div className="scrollbar-thin max-h-48 space-y-1 overflow-y-auto">
                        {categories.map((c) => {
                          const selected =
                            pending.categoryIds?.includes(c.id) ?? false;
                          return (
                            <label
                              key={c.id}
                              className="flex cursor-pointer items-center gap-2 rounded px-2 py-1 hover:bg-muted/50"
                            >
                              <Checkbox
                                checked={selected}
                                onCheckedChange={() => {
                                  const ids = pending.categoryIds ?? [];
                                  const next = selected
                                    ? ids.filter((id) => id !== c.id)
                                    : [...ids, c.id];
                                  setPending({
                                    categoryIds: next.length ? next : undefined,
                                  });
                                }}
                              />
                              {c.icon && (
                                <span className="text-base">{c.icon}</span>
                              )}
                              <span className="text-sm">{c.name}</span>
                            </label>
                          );
                        })}
                      </div>
                    </div>
                  )}
                />
                <th className="px-2 py-3 text-left font-medium text-muted-foreground">
                  Group
                </th>
                <TableHeaderCell
                  label="Payment"
                  column="paymentMethod"
                  filterKey="payment"
                  filterContent={(pending, setPending) => (
                    <div className="space-y-2">
                      <p className="text-xs font-medium text-muted-foreground">
                        Payment methods
                      </p>
                      <div className="scrollbar-thin max-h-48 space-y-1 overflow-y-auto">
                        {PAYMENT_METHODS.map((p) => {
                          const selected =
                            pending.paymentMethods?.includes(p.value) ?? false;
                          return (
                            <label
                              key={p.value}
                              className="flex cursor-pointer items-center gap-2 rounded px-2 py-1 hover:bg-muted/50"
                            >
                              <Checkbox
                                checked={selected}
                                onCheckedChange={() => {
                                  const current =
                                    pending.paymentMethods ?? [];
                                  const next = selected
                                    ? current.filter((v) => v !== p.value)
                                    : [...current, p.value];
                                  setPending({
                                    paymentMethods:
                                      next.length > 0 ? next : undefined,
                                  });
                                }}
                              />
                              <span className="text-sm">{p.label}</span>
                            </label>
                          );
                        })}
                      </div>
                    </div>
                  )}
                />
                <TableHeaderCell
                  label="Amount"
                  column="amount"
                  filterKey="amount"
                  filterContent={(pending, setPending) => (
                    <div className="space-y-2">
                      <p className="text-xs font-medium text-muted-foreground">
                        Type
                      </p>
                      <div className="space-y-1">
                        {(["EXPENSE", "INCOME"] as const).map((type) => {
                          const selected =
                            pending.transactionType === type;
                          return (
                            <label
                              key={type}
                              className="flex cursor-pointer items-center gap-2 rounded px-2 py-1 hover:bg-muted/50"
                            >
                              <Checkbox
                                checked={selected}
                                onCheckedChange={() => {
                                  setPending({
                                    transactionType: selected
                                      ? undefined
                                      : type,
                                  });
                                }}
                              />
                              <span className="text-sm capitalize">{type.toLowerCase()}</span>
                            </label>
                          );
                        })}
                      </div>
                    </div>
                  )}
                />
              </tr>
            </thead>
            <tbody>
              {sorted.map((tx, i) => {
                const isIncome = (tx.signedAmount ?? 0) >= 0;
                const amountColor = isIncome
                  ? "text-emerald-600 dark:text-emerald-400"
                  : "text-destructive";
                const category =
                  tx.categoryId != null ? categoryById[tx.categoryId] : null;
                const groupName =
                  tx.transactionGroupId != null &&
                  groupById[tx.transactionGroupId] != null
                    ? groupById[tx.transactionGroupId]
                    : "—";
                const paymentLabel =
                  PAYMENT_METHODS_MAP[String(tx.paymentMethod)] ??
                  tx.paymentMethod ??
                  "—";
                const dayStripe = dayStripByIndex[i];
                return (
                  <tr
                    key={tx.id}
                    className={cn(
                      "border-b last:border-0 transition-colors hover:bg-muted/40",
                      dayStripe === 0 ? "bg-muted/25" : "bg-muted/5"
                    )}
                  >
                    <td className="px-4 py-3 text-muted-foreground whitespace-nowrap">
                      {new Date(tx.date).toLocaleDateString(undefined, {
                        month: "short",
                        day: "numeric",
                        year: "numeric",
                      })}
                    </td>
                    <td className="px-4 py-3">
                      <span className="font-medium">{tx.subject}</span>
                      {tx.notes && (
                        <span className="block max-w-[200px] truncate text-xs text-muted-foreground">
                          {tx.notes}
                        </span>
                      )}
                    </td>
                    <td className="px-4 py-3 text-muted-foreground">
                      {category ? (
                        <span className="inline-flex items-center gap-1.5">
                          {category.icon && (
                            <span className="text-base leading-none" aria-hidden>
                              {category.icon}
                            </span>
                          )}
                          <span>{category.name}</span>
                        </span>
                      ) : (
                        "—"
                      )}
                    </td>
                    <td className="px-4 py-3 text-muted-foreground">
                      {groupName}
                    </td>
                    <td className="px-4 py-3 text-muted-foreground">
                      {paymentLabel}
                    </td>
                    <td
                      className={cn(
                        "px-4 py-3 text-right font-medium tabular-nums",
                        amountColor
                      )}
                    >
                      {isIncome ? "+" : "−"}
                      {new Intl.NumberFormat(undefined, {
                        style: "currency",
                        currency: "USD",
                      }).format(tx.amount)}
                    </td>
                  </tr>
                );
              })}
            </tbody>
          </table>
        </div>
      </CardContent>
    </Card>
  );
}
