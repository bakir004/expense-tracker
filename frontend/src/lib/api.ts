import type {
  Category,
  Expense,
  ExpenseGroup,
  Income,
  PaymentMethod,
  User,
  UserBalance,
} from "@/lib/types";

type ProblemDetails = {
  title?: string;
  detail?: string;
  status?: number;
  type?: string;
  traceId?: string;
};

const DEFAULT_BASE_URL = "";

function baseUrl() {
  const env = import.meta.env?.VITE_API_BASE_URL as string | undefined;
  return (env?.trim() || DEFAULT_BASE_URL).replace(/\/$/, "");
}

function apiPrefix() {
  // If the user configured an explicit base URL, hit it directly (no prefix).
  // Otherwise, use Vite's /api dev proxy to avoid CORS.
  const env = import.meta.env?.VITE_API_BASE_URL as string | undefined;
  return env?.trim() ? "" : "/api";
}

async function readJsonOrThrow(res: Response) {
  const text = await res.text();
  let data: unknown = null;
  if (text) {
    try {
      data = JSON.parse(text);
    } catch {
      data = null;
    }
  }
  if (!res.ok) {
    const pd = (data ?? {}) as ProblemDetails;
    const msg =
      pd.title ||
      pd.detail ||
      `Request failed (${res.status} ${res.statusText})`;
    throw new Error(msg);
  }
  return data;
}

async function get<T>(path: string): Promise<T> {
  const res = await fetch(`${baseUrl()}${apiPrefix()}${path}`, {
    headers: { accept: "application/json" },
  });
  return (await readJsonOrThrow(res)) as T;
}

async function post<T>(path: string, body?: unknown): Promise<T> {
  const res = await fetch(`${baseUrl()}${apiPrefix()}${path}`, {
    method: "POST",
    headers: {
      accept: "application/json",
      "content-type": "application/json",
    },
    body: body === undefined ? undefined : JSON.stringify(body),
  });
  return (await readJsonOrThrow(res)) as T;
}

async function del(path: string): Promise<void> {
  const res = await fetch(`${baseUrl()}${apiPrefix()}${path}`, {
    method: "DELETE",
    headers: { accept: "application/json" },
  });
  if (res.status === 204) return;
  await readJsonOrThrow(res);
}

export const api = {
  // Users
  async getUsers(): Promise<User[]> {
    const r = await get<{ users: User[] }>("/Users");
    return r.users;
  },
  async getUserById(id: number): Promise<User> {
    return await get<User>(`/Users/${id}`);
  },
  async createUser(input: {
    name: string;
    email: string;
    password: string;
  }): Promise<User> {
    return await post<User>("/Users", input);
  },

  // Categories
  async getCategories(): Promise<Category[]> {
    const r = await get<{ categories: Category[] }>("/Categories");
    return r.categories;
  },
  async createCategory(input: {
    name: string;
    description?: string | null;
    icon?: string | null;
  }): Promise<Category> {
    return await post<Category>("/Categories", input);
  },

  // ExpenseGroups
  async getExpenseGroupsByUserId(userId: number): Promise<ExpenseGroup[]> {
    return await get<ExpenseGroup[]>(`/ExpenseGroups/user/${userId}`);
  },
  async createExpenseGroup(input: {
    name: string;
    description?: string | null;
    userId: number;
  }): Promise<ExpenseGroup> {
    return await post<ExpenseGroup>("/ExpenseGroups", input);
  },

  // Expenses
  async getExpensesByUserId(userId: number): Promise<Expense[]> {
    return await get<Expense[]>(`/Expenses/user/${userId}`);
  },
  async createExpense(input: {
    amount: number;
    date: string;
    description?: string | null;
    paymentMethod: PaymentMethod;
    categoryId: number;
    userId: number;
    expenseGroupId?: number | null;
  }): Promise<Expense> {
    return await post<Expense>("/Expenses", input);
  },
  async deleteExpense(id: number): Promise<void> {
    await del(`/Expenses/${id}`);
  },

  // Incomes
  async getIncomesByUserId(userId: number): Promise<Income[]> {
    return await get<Income[]>(`/Incomes/user/${userId}`);
  },
  async createIncome(input: {
    amount: number;
    description?: string | null;
    source?: string | null;
    paymentMethod: PaymentMethod;
    userId: number;
    date: string;
  }): Promise<Income> {
    return await post<Income>("/Incomes", input);
  },
  async deleteIncome(id: number): Promise<void> {
    await del(`/Incomes/${id}`);
  },

  // UserBalances
  async getUserBalance(userId: number): Promise<UserBalance> {
    return await get<UserBalance>(`/UserBalances/user/${userId}`);
  },
  async initializeUserBalance(input: {
    userId: number;
    initialBalance: number;
  }): Promise<UserBalance> {
    return await post<UserBalance>("/UserBalances/initialize", input);
  },
  async recalculateUserBalance(userId: number): Promise<UserBalance> {
    return await post<UserBalance>(`/UserBalances/user/${userId}/recalculate`);
  },
  async getBalanceAtDate(
    userId: number,
    targetDateIso: string
  ): Promise<{
    userId: number;
    targetDate: string;
    balance: number;
  }> {
    const q = new URLSearchParams({ targetDate: targetDateIso });
    return await get(`/UserBalances/user/${userId}/balance-at-date?${q}`);
  },
};
