"use client";

import * as React from "react";
import { format } from "date-fns";
import { CalendarIcon, Plus } from "lucide-react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import {
  AlertDialog,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogHeader,
  AlertDialogTitle,
  AlertDialogTrigger,
} from "@/components/ui/alert-dialog";
import { Button } from "@/components/ui/button";
import { Calendar } from "@/components/ui/calendar";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import {
  Popover,
  PopoverContent,
  PopoverTrigger,
} from "@/components/ui/popover";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import { Textarea } from "@/components/ui/textarea";
import { cn } from "@/lib/utils";
import { api } from "@/lib/api";
import type { Category } from "@/lib/types";

const paymentMethods = [
  { value: "0", label: "Cash" },
  { value: "1", label: "Debit Card" },
  { value: "2", label: "Credit Card" },
  { value: "3", label: "Bank Transfer" },
  { value: "4", label: "Mobile Payment" },
  { value: "5", label: "PayPal" },
  { value: "6", label: "Crypto" },
  { value: "7", label: "Other" },
];

export function CreateTransactionDialog() {
  const [open, setOpen] = React.useState(false);
  const [date, setDate] = React.useState<Date>(new Date());
  const [formData, setFormData] = React.useState({
    transactionType: "EXPENSE" as "EXPENSE" | "INCOME",
    amount: "",
    subject: "",
    notes: "",
    paymentMethod: "0",
    incomeSource: "",
    categoryId: "",
  });

  const queryClient = useQueryClient();

  const { data: categoriesData, isLoading: categoriesLoading } = useQuery<
    { categories: Category[]; totalCount: number },
    Error
  >({
    queryKey: ["categories"],
    queryFn: () => api.getCategories(),
  });

  const categories = categoriesData?.categories ?? [];

  const mutation = useMutation({
    mutationFn: async () => {
      // Format date as ISO string at midnight UTC for the selected date
      // This avoids timezone shifts when converting local date to UTC
      const year = date.getFullYear();
      const month = String(date.getMonth() + 1).padStart(2, "0");
      const day = String(date.getDate()).padStart(2, "0");
      const dateString = `${year}-${month}-${day}T00:00:00.000Z`;

      return api.createTransaction({
        userId: 1, // hardcoded for now
        transactionType: formData.transactionType,
        amount: parseFloat(formData.amount),
        date: dateString,
        subject: formData.subject,
        notes: formData.notes || null,
        paymentMethod: parseInt(formData.paymentMethod),
        categoryId: formData.categoryId
          ? parseInt(formData.categoryId, 10)
          : null,
        transactionGroupId: null,
        incomeSource: formData.incomeSource || null,
      });
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["transactions"] });
      queryClient.invalidateQueries({ queryKey: ["transactions-range"] });
      setOpen(false);
      resetForm();
    },
  });

  const resetForm = () => {
    setDate(new Date());
    setFormData({
      transactionType: "EXPENSE",
      amount: "",
      subject: "",
      notes: "",
      paymentMethod: "0",
      incomeSource: "",
      categoryId: "",
    });
  };

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    mutation.mutate();
  };

  const isValid =
    formData.amount &&
    parseFloat(formData.amount) > 0 &&
    date &&
    formData.subject.trim();

  return (
    <AlertDialog open={open} onOpenChange={setOpen}>
      <AlertDialogTrigger asChild>
        <Button>
          <Plus className="mr-2 h-4 w-4" />
          Add Transaction
        </Button>
      </AlertDialogTrigger>
      <AlertDialogContent
        className="max-w-lg sm:max-w-2xl"
        onOverlayClick={() => setOpen(false)}
      >
        <AlertDialogHeader>
          <AlertDialogTitle className="text-lg">
            Create Transaction
          </AlertDialogTitle>
          <AlertDialogDescription>
            Add a new expense or income transaction to your account.
          </AlertDialogDescription>
        </AlertDialogHeader>

        <form onSubmit={handleSubmit} className="space-y-4">
          {/* Transaction Type */}
          <div className="space-y-2">
            <Label htmlFor="transactionType">Type</Label>
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

          {/* Amount and Date Row */}
          <div className="grid grid-cols-2 gap-4">
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
              <Label>Date</Label>
              <Popover>
                <PopoverTrigger asChild>
                  <Button
                    variant="outline"
                    className={cn(
                      "w-full justify-start text-left font-normal",
                      !date && "text-muted-foreground"
                    )}
                  >
                    <CalendarIcon className="mr-2 h-4 w-4" />
                    {date ? format(date, "PPP") : <span>Pick a date</span>}
                  </Button>
                </PopoverTrigger>
                <PopoverContent className="w-auto p-0" align="start">
                  <Calendar
                    mode="single"
                    selected={date}
                    onSelect={(newDate) => newDate && setDate(newDate)}
                    initialFocus
                  />
                </PopoverContent>
              </Popover>
            </div>
          </div>

          {/* Subject */}
          <div className="space-y-2">
            <Label htmlFor="subject">Subject</Label>
            <Input
              id="subject"
              required
              value={formData.subject}
              onChange={(e) =>
                setFormData({ ...formData, subject: e.target.value })
              }
              placeholder="What was this transaction for?"
            />
          </div>

          {/* Payment Method & Category */}
          <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
            <div className="space-y-2">
              <Label htmlFor="paymentMethod">Payment Method</Label>
              <Select
                value={formData.paymentMethod}
                onValueChange={(value) =>
                  setFormData({ ...formData, paymentMethod: value })
                }
              >
                <SelectTrigger id="paymentMethod">
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  {paymentMethods.map((method) => (
                    <SelectItem key={method.value} value={method.value}>
                      {method.label}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>

            <div className="space-y-2">
              <Label htmlFor="categoryId">Category (optional)</Label>
              <Select
                value={formData.categoryId}
                onValueChange={(value) =>
                  setFormData({ ...formData, categoryId: value })
                }
                disabled={categoriesLoading || categories.length === 0}
              >
                <SelectTrigger id="categoryId">
                  <SelectValue
                    placeholder={
                      categoriesLoading
                        ? "Loading categories..."
                        : "Select a category"
                    }
                  />
                </SelectTrigger>
                <SelectContent>
                  {categories.map((category) => (
                    <SelectItem key={category.id} value={category.id.toString()}>
                      {category.icon ? `${category.icon} ` : ""}
                      {category.name}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>
          </div>

          {/* Income Source (only for income) */}
          {formData.transactionType === "INCOME" && (
            <div className="space-y-2">
              <Label htmlFor="incomeSource">Income Source</Label>
              <Input
                id="incomeSource"
                value={formData.incomeSource}
                onChange={(e) =>
                  setFormData({ ...formData, incomeSource: e.target.value })
                }
                placeholder="e.g. Salary, Freelance, etc."
              />
            </div>
          )}

          {/* Notes */}
          <div className="space-y-2">
            <Label htmlFor="notes">Notes (optional)</Label>
            <Textarea
              id="notes"
              value={formData.notes}
              onChange={(e) =>
                setFormData({ ...formData, notes: e.target.value })
              }
              placeholder="Additional details..."
              rows={2}
            />
          </div>

          {/* Error Message */}
          {mutation.isError && (
            <div className="text-sm text-destructive">
              {mutation.error instanceof Error
                ? mutation.error.message
                : "Failed to create transaction"}
            </div>
          )}

          {/* Actions */}
          <div className="flex justify-end gap-2 pt-2">
            <Button
              type="button"
              variant="outline"
              onClick={() => setOpen(false)}
            >
              Cancel
            </Button>
            <Button type="submit" disabled={!isValid || mutation.isPending}>
              {mutation.isPending ? "Creating..." : "Create"}
            </Button>
          </div>
        </form>
      </AlertDialogContent>
    </AlertDialog>
  );
}
