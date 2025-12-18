-- Personal Finance Database Schema (PostgreSQL)
-- SIMPLIFIED DESIGN: Transaction table with cumulative_delta
-- 
-- Key Design Decisions:
-- 1. initial_balance stored on User (not separate table)
-- 2. Each transaction stores cumulative_delta (running sum of effects)
-- 3. Actual balance = initial_balance + cumulative_delta
-- 4. No UserBalance or UserBalanceHistory tables needed

-- Drop tables if they exist (in reverse order of dependencies)
DROP TABLE IF EXISTS Transaction;
DROP TABLE IF EXISTS TransactionGroup;
DROP TABLE IF EXISTS Category;
DROP TABLE IF EXISTS "Users";

-- ============================================================
-- USERS TABLE (with initial_balance)
-- ============================================================

CREATE TABLE "Users" (
    id SERIAL PRIMARY KEY,
    name VARCHAR(100) NOT NULL,
    email VARCHAR(255) NOT NULL UNIQUE,
    password_hash VARCHAR(255) NOT NULL,
    initial_balance DECIMAL(12, 2) NOT NULL DEFAULT 0,  -- Starting balance when user began tracking
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX idx_users_email ON "Users"(email);

-- ============================================================
-- CATEGORY TABLE
-- ============================================================

CREATE TABLE Category (
    id SERIAL PRIMARY KEY,
    name VARCHAR(255) NOT NULL,
    description TEXT,
    icon VARCHAR(100),
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- ============================================================
-- TRANSACTION GROUP TABLE (for grouping related transactions)
-- ============================================================

CREATE TABLE TransactionGroup (
    id SERIAL PRIMARY KEY,
    name VARCHAR(255) NOT NULL,
    description TEXT,
    user_id INT NOT NULL,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (user_id) REFERENCES "Users"(id) ON DELETE CASCADE
);

CREATE INDEX idx_transaction_group_user ON TransactionGroup(user_id);

-- ============================================================
-- UNIFIED TRANSACTION TABLE
-- ============================================================

CREATE TABLE Transaction (
    id SERIAL PRIMARY KEY,
    user_id INT NOT NULL,
    
    -- Transaction type
    transaction_type VARCHAR(20) NOT NULL,  -- 'EXPENSE', 'INCOME'
    
    -- Amounts
    amount DECIMAL(12, 2) NOT NULL CHECK (amount > 0),  -- Always positive
    signed_amount DECIMAL(12, 2) NOT NULL,              -- Negative for expense, positive for income
    
    -- CUMULATIVE DELTA: Running sum of all signed_amounts up to this transaction
    -- actual_balance = User.initial_balance + cumulative_delta
    cumulative_delta DECIMAL(12, 2) NOT NULL,
    
    -- Transaction details
    date DATE NOT NULL,
    subject VARCHAR(255) NOT NULL,          -- Brief description: what/why (e.g., "Grocery shopping", "Monthly salary")
    notes TEXT,                             -- Optional longer description
    payment_method VARCHAR(20) NOT NULL,
    
    -- Ordering
    seq BIGSERIAL,  -- Deterministic ordering within a user
    
    -- Optional fields
    category_id INT,              -- Required for EXPENSE, optional for INCOME
    transaction_group_id INT,     -- Optional: group related transactions (trips, projects, etc.)
    income_source VARCHAR(255),   -- Optional, only for INCOME
    
    -- Timestamps
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    
    -- Foreign keys
    FOREIGN KEY (user_id) REFERENCES "Users"(id) ON DELETE CASCADE,
    FOREIGN KEY (category_id) REFERENCES Category(id) ON DELETE RESTRICT,
    FOREIGN KEY (transaction_group_id) REFERENCES TransactionGroup(id) ON DELETE SET NULL,
    
    -- Expenses must have a category
    CONSTRAINT chk_expense_has_category CHECK (
        transaction_type != 'EXPENSE' OR category_id IS NOT NULL
    )
);

-- Critical indexes
CREATE INDEX idx_transaction_user_seq ON Transaction(user_id, seq);
CREATE INDEX idx_transaction_user_date ON Transaction(user_id, date DESC, seq DESC);
CREATE INDEX idx_transaction_type ON Transaction(user_id, transaction_type);
CREATE INDEX idx_transaction_group ON Transaction(transaction_group_id);

-- ============================================================
-- DUMMY DATA
-- ============================================================

-- Users (password is 'password123')
INSERT INTO "Users" (name, email, password_hash, initial_balance) VALUES
('John Doe', 'john.doe@email.com', '$2a$11$yQRQSx3N6m00FZPwo/uQiOhMyxf/pKAtSiijU6EoXKQtrGv5WvNF.', 0),
('Jane Smith', 'jane.smith@email.com', '$2a$11$yQRQSx3N6m00FZPwo/uQiOhMyxf/pKAtSiijU6EoXKQtrGv5WvNF.', 0),
('Mike Wilson', 'mike.wilson@email.com', '$2a$11$yQRQSx3N6m00FZPwo/uQiOhMyxf/pKAtSiijU6EoXKQtrGv5WvNF.', 0);

-- Categories
INSERT INTO Category (name, description, icon) VALUES
('Food & Dining', 'Groceries, restaurants, and food delivery', '🍔'),
('Transportation', 'Gas, public transit, car maintenance', '🚗'),
('Housing', 'Rent, mortgage, utilities, maintenance', '🏠'),
('Entertainment', 'Movies, games, subscriptions, hobbies', '🎮'),
('Healthcare', 'Medical expenses, pharmacy, insurance', '⚕️'),
('Shopping', 'Clothing, electronics, general shopping', '🛍️'),
('Education', 'Courses, books, tuition', '📚'),
('Bills & Utilities', 'Electricity, water, internet, phone', '💡'),
('Salary', 'Regular employment income', '💰'),
('Investment', 'Dividends, capital gains, interest', '📈'),
('Freelance', 'Contract work, side gigs', '💼'),
('Other Income', 'Gifts, refunds, misc income', '🎁');

-- Transaction Groups
INSERT INTO TransactionGroup (name, description, user_id) VALUES
('Europe Trip 2024', 'Summer vacation expenses and travel income', 1),
('Home Renovation', 'Kitchen remodel project', 1),
('Wedding Planning', 'Wedding-related transactions', 2);

-- ============================================================
-- USER 1 (John Doe) TRANSACTIONS
-- initial_balance = 0, cumulative_delta tracks running total
-- ============================================================

-- Nov 1: Salary +3500 → cumulative: +3500
INSERT INTO Transaction (user_id, transaction_type, amount, signed_amount, cumulative_delta, date, subject, payment_method, category_id, income_source) 
VALUES (1, 'INCOME', 3500.00, 3500.00, 3500.00, '2024-11-01', 'Monthly salary', 'BANK_TRANSFER', 9, 'ABC Corporation');

-- Nov 1: Grocery -45.50 → cumulative: +3454.50
INSERT INTO Transaction (user_id, transaction_type, amount, signed_amount, cumulative_delta, date, subject, notes, payment_method, category_id) 
VALUES (1, 'EXPENSE', 45.50, -45.50, 3454.50, '2024-11-01', 'Grocery shopping', 'Weekly groceries at Whole Foods', 'DEBIT_CARD', 1);

-- Nov 2: Gas -60 → cumulative: +3394.50
INSERT INTO Transaction (user_id, transaction_type, amount, signed_amount, cumulative_delta, date, subject, payment_method, category_id) 
VALUES (1, 'EXPENSE', 60.00, -60.00, 3394.50, '2024-11-02', 'Gas station fill-up', 'CREDIT_CARD', 2);

-- Nov 3: Rent -1200 → cumulative: +2194.50
INSERT INTO Transaction (user_id, transaction_type, amount, signed_amount, cumulative_delta, date, subject, payment_method, category_id) 
VALUES (1, 'EXPENSE', 1200.00, -1200.00, 2194.50, '2024-11-03', 'Monthly rent payment', 'BANK_TRANSFER', 3);

-- Nov 5: Freelance +500 → cumulative: +2694.50
INSERT INTO Transaction (user_id, transaction_type, amount, signed_amount, cumulative_delta, date, subject, payment_method, category_id, income_source) 
VALUES (1, 'INCOME', 500.00, 500.00, 2694.50, '2024-11-05', 'Freelance project', 'PAYPAL', 11, 'Client Project');

-- Nov 7: Flight -350 → cumulative: +2344.50 (grouped under Europe Trip)
INSERT INTO Transaction (user_id, transaction_type, amount, signed_amount, cumulative_delta, date, subject, notes, payment_method, category_id, transaction_group_id) 
VALUES (1, 'EXPENSE', 350.00, -350.00, 2344.50, '2024-11-07', 'Flight tickets', 'Round trip to Paris', 'CREDIT_CARD', 2, 1);

-- Nov 10: Bonus +1000 → cumulative: +3344.50
INSERT INTO Transaction (user_id, transaction_type, amount, signed_amount, cumulative_delta, date, subject, payment_method, category_id, income_source) 
VALUES (1, 'INCOME', 1000.00, 1000.00, 3344.50, '2024-11-10', 'Bonus payment', 'BANK_TRANSFER', 9, 'ABC Corporation');

-- ============================================================
-- USER 2 (Jane Smith) TRANSACTIONS
-- ============================================================

-- Nov 1: Salary +4200 → cumulative: +4200
INSERT INTO Transaction (user_id, transaction_type, amount, signed_amount, cumulative_delta, date, subject, payment_method, category_id, income_source) 
VALUES (2, 'INCOME', 4200.00, 4200.00, 4200.00, '2024-11-01', 'Monthly salary', 'BANK_TRANSFER', 9, 'XYZ Tech Inc');

-- Nov 5: Doctor -150 → cumulative: +4050
INSERT INTO Transaction (user_id, transaction_type, amount, signed_amount, cumulative_delta, date, subject, payment_method, category_id) 
VALUES (2, 'EXPENSE', 150.00, -150.00, 4050.00, '2024-11-05', 'Doctor visit copay', 'DEBIT_CARD', 5);

-- Nov 8: Dividend +150 → cumulative: +4200
INSERT INTO Transaction (user_id, transaction_type, amount, signed_amount, cumulative_delta, date, subject, payment_method, category_id, income_source) 
VALUES (2, 'INCOME', 150.00, 150.00, 4200.00, '2024-11-08', 'Stock dividend', 'BANK_TRANSFER', 10, 'Investment Portfolio');

-- Nov 10: Wedding deposit -500 → cumulative: +3700 (grouped under Wedding Planning)
INSERT INTO Transaction (user_id, transaction_type, amount, signed_amount, cumulative_delta, date, subject, notes, payment_method, category_id, transaction_group_id) 
VALUES (2, 'EXPENSE', 500.00, -500.00, 3700.00, '2024-11-10', 'Wedding venue deposit', 'Initial deposit for the reception venue', 'BANK_TRANSFER', 4, 3);

-- ============================================================
-- VERIFICATION QUERIES
-- ============================================================

SELECT 'Schema created successfully!' AS status;

SELECT '=== Users with Initial Balance ===' AS section;
SELECT id, name, email, initial_balance FROM "Users";

SELECT '=== User Balances (computed) ===' AS section;
SELECT 
    u.id,
    u.name,
    u.initial_balance,
    COALESCE(t.cumulative_delta, 0) AS cumulative_delta,
    u.initial_balance + COALESCE(t.cumulative_delta, 0) AS current_balance
FROM "Users" u
LEFT JOIN LATERAL (
    SELECT cumulative_delta 
    FROM Transaction 
    WHERE user_id = u.id 
    ORDER BY seq DESC 
    LIMIT 1
) t ON true;

SELECT '=== User 1 Transaction History ===' AS section;
SELECT 
    transaction_type,
    date,
    subject,
    CASE WHEN signed_amount < 0 THEN signed_amount::text ELSE '+' || signed_amount::text END AS change,
    cumulative_delta,
    0 + cumulative_delta AS actual_balance  -- initial_balance is 0 for user 1
FROM Transaction
WHERE user_id = 1
ORDER BY seq;
