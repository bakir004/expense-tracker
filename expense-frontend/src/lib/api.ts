import type {
  GetTransactionsResponse,
  Transaction,
  UserBalanceResponse,
} from "./types";

const DEFAULT_BASE_URL = "http://localhost:5000";

function baseUrl() {
  const env = import.meta.env?.VITE_API_BASE_URL as string | undefined;
  return (env?.trim() || DEFAULT_BASE_URL).replace(/\/$/, "");
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
    const pd = (data ?? {}) as {
      title?: string;
      detail?: string;
      status?: number;
    };
    const msg =
      pd.title ||
      pd.detail ||
      `Request failed (${res.status} ${res.statusText})`;
    throw new Error(msg);
  }
  return data;
}

async function get<T>(path: string): Promise<T> {
  const res = await fetch(`${baseUrl()}${path}`, {
    headers: { accept: "application/json" },
  });
  return (await readJsonOrThrow(res)) as T;
}

async function post<T>(path: string, body?: unknown): Promise<T> {
  const res = await fetch(`${baseUrl()}${path}`, {
    method: "POST",
    headers: {
      accept: "application/json",
      "content-type": "application/json",
    },
    body: body === undefined ? undefined : JSON.stringify(body),
  });
  return (await readJsonOrThrow(res)) as T;
}

export const api = {
  async getTransactionsByUserId(
    userId: number
  ): Promise<GetTransactionsResponse> {
    return await get<GetTransactionsResponse>(
      `/api/v1/transactions/user/${userId}`
    );
  },

  async getUserBalance(userId: number): Promise<UserBalanceResponse> {
    return await get<UserBalanceResponse>(`/api/v1/users/${userId}/balance`);
  },

  async createTransaction(input: {
    userId: number;
    transactionType: "EXPENSE" | "INCOME";
    amount: number;
    date: string;
    subject: string;
    notes?: string | null;
    paymentMethod: number;
    categoryId?: number | null;
    transactionGroupId?: number | null;
    incomeSource?: string | null;
  }): Promise<Transaction> {
    return await post<Transaction>("/api/v1/transactions", input);
  },
};
