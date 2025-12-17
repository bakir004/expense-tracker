export type User = {
  id: number;
  name: string;
  email: string;
  createdAt: string;
  updatedAt: string;
};

export type Category = {
  id: number;
  name: string;
  description?: string | null;
  icon?: string | null;
};

export type ExpenseGroup = {
  id: number;
  name: string;
  description?: string | null;
  userId: number;
};

// Backend likely serializes enums as numbers (default System.Text.Json behavior)
export type PaymentMethod = 0 | 1 | 2 | 3 | 4 | 5 | 6 | 7;

export const paymentMethods: Array<{ value: PaymentMethod; label: string }> = [
  { value: 0, label: "Cash" },
  { value: 1, label: "Debit Card" },
  { value: 2, label: "Credit Card" },
  { value: 3, label: "Bank Transfer" },
  { value: 4, label: "Mobile Payment" },
  { value: 5, label: "PayPal" },
  { value: 6, label: "Crypto" },
  { value: 7, label: "Other" },
];

export type Expense = {
  id: number;
  amount: number;
  date: string; // ISO-ish (from API); can be "YYYY-MM-DD"
  description?: string | null;
  paymentMethod: PaymentMethod;
  categoryId: number;
  userId: number;
  expenseGroupId?: number | null;
};

export type Income = {
  id: number;
  amount: number;
  description?: string | null;
  source?: string | null;
  paymentMethod: PaymentMethod;
  userId: number;
  date: string;
};

export type UserBalance = {
  id: number;
  userId: number;
  currentBalance: number;
  initialBalance: number;
  lastUpdated: string;
};
