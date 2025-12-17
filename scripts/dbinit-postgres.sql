-- Personal Finance Database Schema (PostgreSQL)
-- Drop tables if they exist (in reverse order of dependencies)
DROP TABLE IF EXISTS Income;
DROP TABLE IF EXISTS Expense;
DROP TABLE IF EXISTS ExpenseGroup;
DROP TABLE IF EXISTS UserBalanceHistory;
DROP TABLE IF EXISTS UserBalance;
DROP TABLE IF EXISTS Category;
DROP TABLE IF EXISTS "Users";

-- Drop types if they exist
DROP TYPE IF EXISTS transaction_type_enum;
DROP TYPE IF EXISTS payment_method_enum;

-- Create Users table (Pascal case, matching the application code expectations)
CREATE TABLE "Users" (
    id SERIAL PRIMARY KEY,
    name VARCHAR(100) NOT NULL,
    email VARCHAR(255) NOT NULL UNIQUE,
    password_hash VARCHAR(255) NOT NULL,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- Create index on email for faster lookups
CREATE INDEX IF NOT EXISTS idx_users_email ON "Users"(email);

-- Create UserBalance table
CREATE TABLE UserBalance (
    id SERIAL PRIMARY KEY,
    user_id INT NOT NULL UNIQUE,
    current_balance DECIMAL(10, 2) NOT NULL DEFAULT 0,
    initial_balance DECIMAL(10, 2) NOT NULL DEFAULT 0,
    last_updated TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (user_id) REFERENCES "Users"(id) ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS idx_user_balance_user_id ON UserBalance(user_id);

-- Create transaction type enum for UserBalanceHistory
CREATE TYPE transaction_type_enum AS ENUM ('INCOME', 'EXPENSE', 'INITIAL', 'RECALCULATE');

-- Create UserBalanceHistory table to store balance snapshots over time
CREATE TABLE UserBalanceHistory (
    id SERIAL PRIMARY KEY,
    user_id INT NOT NULL,
    balance DECIMAL(10, 2) NOT NULL,
    transaction_date TIMESTAMP NOT NULL,
    transaction_type transaction_type_enum NOT NULL,
    transaction_id INT, -- Optional: reference to Income.id or Expense.id
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (user_id) REFERENCES "Users"(id) ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS idx_user_balance_history_user_date ON UserBalanceHistory(user_id, transaction_date DESC);
CREATE INDEX IF NOT EXISTS idx_user_balance_history_user_id ON UserBalanceHistory(user_id);

-- Create Category table
CREATE TABLE Category (
    id SERIAL PRIMARY KEY,
    name VARCHAR(255) NOT NULL,
    description TEXT,
    icon VARCHAR(100)
);

-- Create ExpenseGroup table
CREATE TABLE ExpenseGroup (
    id SERIAL PRIMARY KEY,
    name VARCHAR(255) NOT NULL,
    description TEXT,
    user_id INT NOT NULL,
    FOREIGN KEY (user_id) REFERENCES "Users"(id) ON DELETE CASCADE
);

-- Create custom type for payment method
CREATE TYPE payment_method_enum AS ENUM (
    'CASH', 'DEBIT_CARD', 'CREDIT_CARD', 'BANK_TRANSFER', 
    'MOBILE_PAYMENT', 'PAYPAL', 'CRYPTO', 'OTHER'
);

-- Create Expense table
CREATE TABLE Expense (
    id SERIAL PRIMARY KEY,
    amount DECIMAL(10, 2) NOT NULL,
    date DATE NOT NULL,
    description TEXT,
    payment_method payment_method_enum NOT NULL,
    category_id INT NOT NULL,
    user_id INT NOT NULL,
    expense_group_id INT,
    FOREIGN KEY (category_id) REFERENCES Category(id) ON DELETE RESTRICT,
    FOREIGN KEY (user_id) REFERENCES "Users"(id) ON DELETE CASCADE,
    FOREIGN KEY (expense_group_id) REFERENCES ExpenseGroup(id) ON DELETE SET NULL
);

-- Create Income table
CREATE TABLE Income (
    id SERIAL PRIMARY KEY,
    amount DECIMAL(10, 2) NOT NULL,
    description TEXT,
    source VARCHAR(255),
    payment_method payment_method_enum NOT NULL,
    user_id INT NOT NULL,
    date DATE NOT NULL,
    FOREIGN KEY (user_id) REFERENCES "Users"(id) ON DELETE CASCADE
);

-- Insert dummy data

-- Users (matching the application code schema: name, email, password_hash)
INSERT INTO "Users" (name, email, password_hash, created_at, updated_at) VALUES
('John Doe', 'john.doe@email.com', '$2a$11$hashed_password_123', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
('Jane Smith', 'jane.smith@email.com', '$2a$11$hashed_password_456', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
('Mike Wilson', 'mike.wilson@email.com', '$2a$11$hashed_password_789', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP);

-- Categories
INSERT INTO Category (name, description, icon) VALUES
('Food & Dining', 'Groceries, restaurants, and food delivery', '🍔'),
('Transportation', 'Gas, public transit, car maintenance', '🚗'),
('Housing', 'Rent, mortgage, utilities, maintenance', '🏠'),
('Entertainment', 'Movies, games, subscriptions, hobbies', '🎮'),
('Healthcare', 'Medical expenses, pharmacy, insurance', '⚕️'),
('Shopping', 'Clothing, electronics, general shopping', '🛍️'),
('Education', 'Courses, books, tuition', '📚'),
('Bills & Utilities', 'Electricity, water, internet, phone', '💡');

-- Expense Groups
INSERT INTO ExpenseGroup (name, description, user_id) VALUES
('Europe Trip 2024', 'Summer vacation expenses', 1),
('Home Renovation', 'Kitchen remodel project', 1),
('Wedding Planning', 'Wedding-related expenses', 2),
('Monthly Subscriptions', 'Recurring subscription services', 3);

-- Expenses
INSERT INTO Expense (amount, date, description, payment_method, category_id, user_id, expense_group_id) VALUES
(45.50, '2024-11-01', 'Grocery shopping at Whole Foods', 'DEBIT_CARD', 1, 1, NULL),
(60.00, '2024-11-02', 'Gas station fill-up', 'CREDIT_CARD', 2, 1, NULL),
(1200.00, '2024-11-03', 'Monthly rent payment', 'BANK_TRANSFER', 3, 1, NULL),
(25.99, '2024-11-04', 'Netflix subscription', 'CREDIT_CARD', 4, 1, 4),
(150.00, '2024-11-05', 'Doctor visit copay', 'DEBIT_CARD', 5, 2, NULL),
(89.99, '2024-11-06', 'New running shoes', 'CREDIT_CARD', 6, 2, NULL),
(350.00, '2024-11-07', 'Flight tickets', 'CREDIT_CARD', 2, 1, 1),
(120.00, '2024-11-08', 'Electricity bill', 'BANK_TRANSFER', 8, 1, NULL),
(42.30, '2024-11-09', 'Restaurant dinner', 'CASH', 1, 2, NULL),
(500.00, '2024-11-10', 'Wedding venue deposit', 'BANK_TRANSFER', 4, 2, 3),
(75.00, '2024-11-11', 'Online course enrollment', 'PAYPAL', 7, 3, NULL),
(200.00, '2024-11-12', 'Hardware store - renovation supplies', 'DEBIT_CARD', 3, 1, 2);

-- Income
INSERT INTO Income (amount, description, source, payment_method, user_id, date) VALUES
(3500.00, 'Monthly salary', 'ABC Corporation', 'BANK_TRANSFER', 1, '2024-11-01'),
(4200.00, 'Monthly salary', 'XYZ Tech Inc', 'BANK_TRANSFER', 2, '2024-11-01'),
(2800.00, 'Monthly salary', 'Local Business LLC', 'BANK_TRANSFER', 3, '2024-11-01'),
(500.00, 'Freelance project payment', 'Client Project', 'PAYPAL', 1, '2024-11-05'),
(150.00, 'Stock dividend', 'Investment Portfolio', 'BANK_TRANSFER', 2, '2024-11-08'),
(1000.00, 'Bonus payment', 'ABC Corporation', 'BANK_TRANSFER', 1, '2024-11-10'),
(300.00, 'Side gig earnings', 'Consulting Work', 'MOBILE_PAYMENT', 3, '2024-11-12');

-- Display summary
SELECT 'Database schema created successfully!' AS Status;
SELECT COUNT(*) AS TotalUsers FROM "Users";
SELECT COUNT(*) AS TotalCategories FROM Category;
SELECT COUNT(*) AS TotalExpenses FROM Expense;
SELECT COUNT(*) AS TotalIncome FROM Income;
SELECT COUNT(*) AS TotalExpenseGroups FROM ExpenseGroup;

