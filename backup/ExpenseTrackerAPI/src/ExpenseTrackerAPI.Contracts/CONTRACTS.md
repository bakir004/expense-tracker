# Contracts – API boundary pattern

The **Contracts** project is the single place that defines and handles everything at the **API boundary**: what the consumer sends in and what the API returns.

## Responsibilities

### 1. Incoming (API consumer → application)

- **Request/query DTOs**
  Types that ASP.NET Core binds from the HTTP request:
  - Body: `CreateXxxRequest`, `UpdateXxxRequest`
  - Query: `XxxQueryParameters`, query/route strings

- **Parsing and format validation**
  All parsing from strings (dates, enums, query options) lives here and returns `ErrorOr<T>`:
  - Query/route: `TransactionParsers.ParseTransactionType(string)`, `TransactionParsers.ParseDateRange(from, to)`, `TransactionParsers.BuildQueryOptions(TransactionQueryParameters?)`
  - Body: `CreateTransactionRequest.Parse()` → `ErrorOr<ParsedCreateTransactionInput>`, etc.

- **Parsed types**
  Strongly-typed results of parsing (e.g. `TransactionQueryOptions`, `ParsedCreateTransactionInput`) so the application layer receives domain-friendly types, not raw strings.

The **controller** only:
- Binds `[FromBody]` / `[FromQuery]` to contract DTOs
- Calls contract parsers and gets `ErrorOr<ParsedType>`
- On success, calls the application service with parsed input
- Never does `Enum.Parse`, `DateOnly.ParseExact`, or query-building logic

### 2. Outgoing (application → API consumer)

- **Response DTOs**
  Types that describe the JSON shape returned to the client: `TransactionResponse`, `GetTransactionsResponse`, etc.

- **Mapping (optional)**
  Domain → response mapping can live in Application (e.g. `TransactionMappings.ToResponse()`) or in Contracts; the important part is that the **response shape** is defined only in Contracts.

## Layer rules

- **Contracts** may reference **Domain** (for enums, entities, shared errors) and **ErrorOr**. It does **not** reference Application or WebApi.
- **WebApi** references Contracts and Application; controllers use contract DTOs and parsers, then call application services.
- **Application** may reference Contracts (e.g. for `TransactionQueryOptions`, request DTOs) and Domain; it performs **business** validation (e.g. “date not in future”), not binding/parsing.

## Summary

| Concern              | Owned by   | Example                                      |
|----------------------|------------|----------------------------------------------|
| Request/query shape  | Contracts  | `CreateTransactionRequest`, `TransactionQueryParameters` |
| String → typed parse | Contracts  | `TransactionParsers.*`, `request.Parse()`    |
| Parsed query/input   | Contracts  | `TransactionQueryOptions`, `ParsedCreateTransactionInput` |
| Response shape      | Contracts  | `TransactionResponse`, `GetTransactionsResponse` |
| Business rules      | Application| “Amount > 0”, “date not in future”           |
