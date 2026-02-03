import type {
  GetCategoriesResponse,
  GetTransactionsResponse,
  Transaction,
  TransactionGroup,
  TransactionQueryParams,
  UserBalanceResponse,
} from "./types";

const DEFAULT_BASE_URL = "http://localhost:5000";
const API_VERSION = "v1";

function baseUrl() {
  const env = import.meta.env?.VITE_API_BASE_URL as string | undefined;
  return (env?.trim() || DEFAULT_BASE_URL).replace(/\/$/, "");
}
const API_BASE_URL = `${baseUrl()}/api/${API_VERSION}`;

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
      errors?: Record<string, string[] | string>;
    };

    let errorMessage: string | null = null;

    // Prefer detailed validation errors if present
    if (pd.errors && typeof pd.errors === "object") {
      const parts: string[] = [];
      for (const [field, messages] of Object.entries(pd.errors)) {
        const msgs = Array.isArray(messages) ? messages : [messages];
        const label =
          field === "" || field.toLowerCase() === "general"
            ? "Error"
            : field;
        parts.push(`${label}: ${msgs.join(", ")}`);
      }
      if (parts.length > 0) {
        errorMessage = parts.join(" | ");
      }
    }

    // Fall back to ProblemDetails title/detail
    if (!errorMessage) {
      errorMessage =
        pd.detail ||
        (pd.title &&
          (pd.title === "One or more validation errors occurred."
            ? "Validation failed for the request."
            : pd.title)) ||
        null;
    }

    // Final generic fallback
    if (!errorMessage) {
      errorMessage = `Request failed (${res.status} ${res.statusText})`;
    }

    throw new Error(errorMessage);
  }

  return data;
}

async function get<T>(path: string): Promise<T> {
  const res = await fetch(`${API_BASE_URL}${path}`, {
    headers: { accept: "application/json" },
  });
  return (await readJsonOrThrow(res)) as T;
}

async function post<T>(path: string, body?: unknown): Promise<T> {
  const res = await fetch(`${API_BASE_URL}${path}`, {
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
    userId: number,
    params?: TransactionQueryParams
  ): Promise<GetTransactionsResponse> {
    const search = new URLSearchParams();
    if (params?.subject != null && params.subject !== "")
      search.set("Subject", params.subject);
    if (params?.categoryIds?.length)
      params.categoryIds.forEach((id) => search.append("CategoryIds", String(id)));
    if (params?.paymentMethods?.length)
      params.paymentMethods.forEach((p) => search.append("PaymentMethods", String(p)));
    if (params?.transactionType)
      search.set("TransactionType", params.transactionType);
    if (params?.dateFrom) search.set("DateFrom", params.dateFrom);
    if (params?.dateTo) search.set("DateTo", params.dateTo);
    if (params?.sortBy) search.set("SortBy", params.sortBy);
    if (params?.sortDirection) search.set("SortDirection", params.sortDirection);
    const qs = search.toString();
    const url = qs
      ? `/transactions/user/${userId}?${qs}`
      : `/transactions/user/${userId}`;
    return await get<GetTransactionsResponse>(url);
  },

  async getTransactionsByUserIdAndDateRange(
    userId: number,
    from: string,
    to: string
  ): Promise<GetTransactionsResponse> {
    // Format dates as dd-MM-yyyy for the API using UTC to avoid timezone shifts
    const formatDate = (dateStr: string): string => {
      const date = new Date(dateStr);
      const day = String(date.getUTCDate()).padStart(2, "0");
      const month = String(date.getUTCMonth() + 1).padStart(2, "0");
      const year = date.getUTCFullYear();
      return `${day}-${month}-${year}`;
    };

    const fromFormatted = formatDate(from);
    const toFormatted = formatDate(to);

    return await get<GetTransactionsResponse>(
      `/transactions/user/${userId}/range?from=${fromFormatted}&to=${toFormatted}`
    );
  },

  async getUserBalance(userId: number): Promise<UserBalanceResponse> {
    return await get<UserBalanceResponse>(`/users/${userId}/balance`);
  },

  async getCategories(): Promise<GetCategoriesResponse> {
    return await get<GetCategoriesResponse>(`/categories`);
  },

  async getTransactionGroupsByUserId(
    userId: number
  ): Promise<TransactionGroup[]> {
    return await get<TransactionGroup[]>(`/transaction-groups/user/${userId}`);
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
    return await post<Transaction>(`/transactions`, input);
  },
};
