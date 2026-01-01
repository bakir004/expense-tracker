export type Transaction = {
  id: number;
  userId: number;
  transactionType: "EXPENSE" | "INCOME";
  amount: number;
  signedAmount: number;
  cumulativeDelta: number;
  date: string; // ISO date string
  subject: string;
  notes?: string | null;
  paymentMethod: string;
  categoryId?: number | null;
  transactionGroupId?: number | null;
  incomeSource?: string | null;
  createdAt: string;
  updatedAt: string;
};

export type UserBalanceResponse = {
  userId: number;
  initialBalance: number;
  cumulativeDelta: number;
  currentBalance: number;
};

export type GetTransactionsResponse = {
  transactions: Transaction[];
  totalCount: number;
  summary?: {
    totalIncome: number;
    totalExpenses: number;
    netChange: number;
    incomeCount: number;
    expenseCount: number;
  } | null;
};
