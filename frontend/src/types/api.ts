export type TransactionType = 'Income' | 'Expense'

export type PaymentMethod =
  | 'Cash'
  | 'CreditCard'
  | 'DebitCard'
  | 'BankTransfer'
  | 'Upi'
  | 'DigitalWallet'
  | 'Other'

export interface UserProfile {
  id: string
  email: string
  firstName: string
  lastName: string
  currencyCode: string
  monthlySavingsTarget: number | null
  roles: string[]
  createdAtUtc: string
}

export interface AuthResponse {
  accessToken: string
  accessTokenExpiresAtUtc: string
  refreshToken: string
  refreshTokenExpiresAtUtc: string
  user: UserProfile
}

export interface Category {
  id: string
  name: string
  type: TransactionType
  icon: string
  color: string
  isDefault: boolean
  transactionCount: number
}

export interface Transaction {
  id: string
  title: string
  amount: number
  type: TransactionType
  categoryId: string
  categoryName: string
  categoryColor: string
  categoryIcon: string
  description: string | null
  transactionDate: string
  paymentMethod: PaymentMethod
  notes: string | null
  createdAtUtc: string
  updatedAtUtc: string
}

export interface PagedResult<T> {
  items: T[]
  page: number
  pageSize: number
  totalCount: number
  totalPages: number
  hasPreviousPage: boolean
  hasNextPage: boolean
}

export interface Budget {
  id: string
  categoryId: string
  categoryName: string
  categoryColor: string
  categoryIcon: string
  budgetAmount: number
  spentAmount: number
  remainingAmount: number
  utilizationPercentage: number
  year: number
  month: number
}

export interface SavingsGoal {
  id: string
  name: string
  targetAmount: number
  currentAmount: number
  progressPercentage: number
  targetDate: string | null
  notes: string | null
  isCompleted: boolean
  createdAtUtc: string
}

export interface DashboardOverview {
  year: number
  month: number
  totalIncome: number
  totalExpenses: number
  currentBalance: number
  savingsAmount: number
  monthlySavingsRate: number
  comparisonWithPreviousMonth: {
    previousMonthIncome: number
    previousMonthExpenses: number
    previousMonthSavings: number
    incomeChangePercentage: number | null
    expenseChangePercentage: number | null
    savingsChangePercentage: number | null
  }
  spendingByCategory: {
    categoryId: string
    categoryName: string
    color: string
    icon: string
    amount: number
    percentage: number
  }[]
  monthlyCashflow: {
    year: number
    month: number
    label: string
    income: number
    expenses: number
    savings: number
  }[]
  budgetUtilization: {
    budgetId: string
    categoryId: string
    categoryName: string
    categoryColor: string
    budgetAmount: number
    spentAmount: number
    utilizationPercentage: number
    isOverBudget: boolean
  }[]
  recentTransactions: Transaction[]
}
