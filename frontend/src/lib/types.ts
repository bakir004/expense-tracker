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

export type Category = {
  id: number;
  name: string;
  description?: string | null;
  icon?: string | null;
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

export type GetCategoriesResponse = {
  categories: Category[];
  totalCount: number;
};

export type TransactionGroup = {
  id: number;
  name: string;
  description?: string | null;
  userId: number;
  createdAt: string;
};

/** Query params for GET /transactions/user/:id (filtering and sorting) */
export type TransactionQueryParams = {
  subject?: string;
  categoryIds?: number[];
  paymentMethods?: number[];
  transactionType?: "EXPENSE" | "INCOME";
  dateFrom?: string; // dd-MM-yyyy
  dateTo?: string;
  sortBy?: "subject" | "paymentMethod" | "category" | "amount";
  sortDirection?: "asc" | "desc";
};
