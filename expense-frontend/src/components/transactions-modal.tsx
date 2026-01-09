"use client";

import * as React from "react";
import {
  AlertDialog,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogHeader,
  AlertDialogTitle,
  AlertDialogTrigger,
} from "@/components/ui/alert-dialog";
import { Card } from "@/components/ui/card";
import { X } from "lucide-react";
import type { Transaction } from "@/lib/types";

interface TransactionsModalProps {
  date: string;
  transactions: Transaction[];
  children: React.ReactNode;
}

export function TransactionsModal({
  date,
  transactions,
  children,
}: TransactionsModalProps) {
  const [open, setOpen] = React.useState(false);

  const transactionsForDate = React.useMemo(() => {
    return transactions.filter((tx) => {
      const txDate = tx.date.includes("T")
        ? tx.date.split("T")[0]
        : tx.date.substring(0, 10);
      return txDate === date;
    });
  }, [transactions, date]);

  // Get the balance at the end of the day (from the last transaction)
  const endOfDayBalance = React.useMemo(() => {
    if (transactionsForDate.length === 0) return 0;
    // Sort by createdAt descending to get the latest transaction
    const sorted = [...transactionsForDate].sort((a, b) => {
      const dateA = new Date(a.createdAt).getTime();
      const dateB = new Date(b.createdAt).getTime();
      return dateB - dateA;
    });
    const lastTx = sorted[0];
    return lastTx.cumulativeDelta ?? (lastTx as any).CumulativeDelta ?? 0;
  }, [transactionsForDate]);

  return (
    <AlertDialog open={open} onOpenChange={setOpen}>
      <AlertDialogTrigger asChild>{children}</AlertDialogTrigger>
      <AlertDialogContent className="dark max-w-2xl max-h-[80vh] overflow-y-auto">
        <AlertDialogHeader className="relative">
          <X
            className="absolute right-0 top-0 h-4 w-4 cursor-pointer dark:text-zinc-300 dark:hover:text-red-400 transition"
            onClick={() => setOpen(false)}
          />
          <AlertDialogTitle className="dark:text-zinc-100">
            Transactions on{" "}
            {new Date(date).toLocaleDateString("en-US", {
              month: "long",
              day: "numeric",
              year: "numeric",
            })}
          </AlertDialogTitle>
          <AlertDialogDescription className="dark:text-zinc-400">
            {transactionsForDate.length === 0 &&
              "No transactions found for this date."}
          </AlertDialogDescription>
        </AlertDialogHeader>
        <div className="space-y-2">
          {transactionsForDate.map((tx) => (
            <Card key={tx.id} className="p-4 dark:bg-zinc-800 dark:border-zinc-700">
              <div className="flex items-start justify-between">
                <div className="space-y-1">
                  <div className="font-medium dark:text-zinc-100">{tx.subject}</div>
                  {tx.notes && (
                    <div className="text-sm text-muted-foreground dark:text-zinc-400">
                      {tx.notes}
                    </div>
                  )}
                </div>
                <div className="text-right">
                  <div
                    className={`font-semibold ${
                      tx.transactionType === "EXPENSE"
                        ? "text-destructive dark:text-red-400"
                        : "text-green-600 dark:text-green-400"
                    }`}
                  >
                    {tx.transactionType === "EXPENSE" ? "-" : "+"}
                    {new Intl.NumberFormat(undefined, {
                      style: "currency",
                      currency: "USD",
                    }).format(tx.amount)}
                  </div>
                </div>
              </div>
            </Card>
          ))}
        </div>
        {transactionsForDate.length > 0 && (
          <div className="mt-4 pt-4 border-t dark:border-zinc-700">
            <div className="flex justify-between items-center">
              <span className="text-sm font-medium dark:text-zinc-300">
                Balance at end of day:
              </span>
              <span className="text-lg font-semibold dark:text-zinc-100">
                {new Intl.NumberFormat(undefined, {
                  style: "currency",
                  currency: "USD",
                }).format(endOfDayBalance)}
              </span>
            </div>
          </div>
        )}
      </AlertDialogContent>
    </AlertDialog>
  );
}

