import { useState } from 'react'
import { useQuery } from '@tanstack/react-query'
import {
  TrendingUp,
  TrendingDown,
  Wallet,
  PiggyBank,
  Percent,
  ArrowUpRight,
  ArrowDownRight,
} from 'lucide-react'
import {
  BarChart,
  Bar,
  XAxis,
  YAxis,
  CartesianGrid,
  Tooltip,
  ResponsiveContainer,
  PieChart,
  Pie,
  Cell,
  Legend,
} from 'recharts'
import { apiClient } from '@/api/client'
import { useAuth } from '@/context/AuthContext'
import type { DashboardOverview } from '@/types/api'
import { formatCurrency, formatDate, cn } from '@/lib/utils'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { Select } from '@/components/ui/select'
import { Badge } from '@/components/ui/badge'

const MONTHS = [
  'January', 'February', 'March', 'April', 'May', 'June',
  'July', 'August', 'September', 'October', 'November', 'December',
]

function ChangeBadge({ value, invertColor = false }: { value: number | null; invertColor?: boolean }) {
  if (value === null) return <span className="text-xs text-slate-400">No prior data</span>
  const isPositive = value >= 0
  const isGood = invertColor ? !isPositive : isPositive
  return (
    <span
      className={cn(
        'inline-flex items-center gap-0.5 text-xs font-medium',
        isGood ? 'text-emerald-600' : 'text-red-600',
      )}
    >
      {isPositive ? <ArrowUpRight className="h-3.5 w-3.5" /> : <ArrowDownRight className="h-3.5 w-3.5" />}
      {Math.abs(value).toFixed(1)}% vs last month
    </span>
  )
}

export function DashboardPage() {
  const { user } = useAuth()
  const currency = user?.currencyCode || 'USD'
  const now = new Date()
  const [year, setYear] = useState(now.getUTCFullYear())
  const [month, setMonth] = useState(now.getUTCMonth() + 1)

  const { data, isLoading } = useQuery({
    queryKey: ['dashboard', year, month],
    queryFn: async () => {
      const res = await apiClient.get<DashboardOverview>('/dashboard/overview', {
        params: { year, month },
      })
      return res.data
    },
  })

  const years = Array.from({ length: 5 }, (_, i) => now.getUTCFullYear() - 2 + i)

  if (isLoading || !data) {
    return (
      <div className="space-y-6">
        <div className="h-8 w-48 animate-pulse rounded-lg bg-slate-200" />
        <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-5">
          {Array.from({ length: 5 }).map((_, i) => (
            <div key={i} className="h-28 animate-pulse rounded-xl bg-slate-200" />
          ))}
        </div>
      </div>
    )
  }

  return (
    <div className="space-y-6">
      {/* Header & Period Selector */}
      <div className="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
        <div>
          <h1 className="text-2xl font-bold tracking-tight text-slate-900">Financial Overview</h1>
          <p className="text-sm text-slate-500">
            Track your cashflow, spending habits, and budget health for {MONTHS[month - 1]} {year}
          </p>
        </div>
        <div className="flex items-center gap-2">
          <Select
            value={month}
            onChange={(e) => setMonth(Number(e.target.value))}
            className="w-36"
          >
            {MONTHS.map((m, idx) => (
              <option key={m} value={idx + 1}>
                {m}
              </option>
            ))}
          </Select>
          <Select
            value={year}
            onChange={(e) => setYear(Number(e.target.value))}
            className="w-24"
          >
            {years.map((y) => (
              <option key={y} value={y}>
                {y}
              </option>
            ))}
          </Select>
        </div>
      </div>

      {/* KPI Cards */}
      <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-5">
        <Card>
          <CardContent className="p-5">
            <div className="flex items-center justify-between">
              <span className="text-xs font-medium text-slate-500">Total Income</span>
              <TrendingUp className="h-4 w-4 text-emerald-600" />
            </div>
            <p className="mt-2 text-2xl font-bold text-slate-900">{formatCurrency(data.totalIncome, currency)}</p>
            <div className="mt-1">
              <ChangeBadge value={data.comparisonWithPreviousMonth.incomeChangePercentage} />
            </div>
          </CardContent>
        </Card>

        <Card>
          <CardContent className="p-5">
            <div className="flex items-center justify-between">
              <span className="text-xs font-medium text-slate-500">Total Expenses</span>
              <TrendingDown className="h-4 w-4 text-red-600" />
            </div>
            <p className="mt-2 text-2xl font-bold text-slate-900">{formatCurrency(data.totalExpenses, currency)}</p>
            <div className="mt-1">
              <ChangeBadge value={data.comparisonWithPreviousMonth.expenseChangePercentage} invertColor />
            </div>
          </CardContent>
        </Card>

        <Card>
          <CardContent className="p-5">
            <div className="flex items-center justify-between">
              <span className="text-xs font-medium text-slate-500">Current Balance</span>
              <Wallet className="h-4 w-4 text-indigo-600" />
            </div>
            <p className="mt-2 text-2xl font-bold text-slate-900">{formatCurrency(data.currentBalance, currency)}</p>
            <p className="mt-1 text-xs text-slate-400">Cumulative net balance</p>
          </CardContent>
        </Card>

        <Card>
          <CardContent className="p-5">
            <div className="flex items-center justify-between">
              <span className="text-xs font-medium text-slate-500">Monthly Savings</span>
              <PiggyBank className="h-4 w-4 text-sky-600" />
            </div>
            <p className={cn('mt-2 text-2xl font-bold', data.savingsAmount >= 0 ? 'text-emerald-600' : 'text-red-600')}>
              {formatCurrency(data.savingsAmount, currency)}
            </p>
            <div className="mt-1">
              <ChangeBadge value={data.comparisonWithPreviousMonth.savingsChangePercentage} />
            </div>
          </CardContent>
        </Card>

        <Card>
          <CardContent className="p-5">
            <div className="flex items-center justify-between">
              <span className="text-xs font-medium text-slate-500">Savings Rate</span>
              <Percent className="h-4 w-4 text-violet-600" />
            </div>
            <p className="mt-2 text-2xl font-bold text-slate-900">{data.monthlySavingsRate.toFixed(1)}%</p>
            <p className="mt-1 text-xs text-slate-400">Of monthly income saved</p>
          </CardContent>
        </Card>
      </div>

      {/* Charts Row */}
      <div className="grid gap-6 lg:grid-cols-7">
        <Card className="lg:col-span-4">
          <CardHeader>
            <CardTitle>Income vs Expenses</CardTitle>
            <CardDescription>6-month cashflow comparison</CardDescription>
          </CardHeader>
          <CardContent>
            <div className="h-72 w-full">
              <ResponsiveContainer width="100%" height="100%">
                <BarChart data={data.monthlyCashflow}>
                  <CartesianGrid strokeDasharray="3 3" stroke="#e2e8f0" />
                  <XAxis dataKey="label" tick={{ fontSize: 12 }} />
                  <YAxis tick={{ fontSize: 12 }} />
                  <Tooltip formatter={(val) => formatCurrency(Number(val), currency)} />
                  <Legend />
                  <Bar dataKey="income" name="Income" fill="#10b981" radius={[4, 4, 0, 0]} />
                  <Bar dataKey="expenses" name="Expenses" fill="#ef4444" radius={[4, 4, 0, 0]} />
                </BarChart>
              </ResponsiveContainer>
            </div>
          </CardContent>
        </Card>

        <Card className="lg:col-span-3">
          <CardHeader>
            <CardTitle>Spending by Category</CardTitle>
            <CardDescription>Expense distribution for {MONTHS[month - 1]}</CardDescription>
          </CardHeader>
          <CardContent>
            {data.spendingByCategory.length === 0 ? (
              <div className="flex h-72 items-center justify-center text-sm text-slate-400">
                No expenses recorded for this period.
              </div>
            ) : (
              <div className="h-72 w-full">
                <ResponsiveContainer width="100%" height="100%">
                  <PieChart>
                    <Pie
                      data={data.spendingByCategory}
                      dataKey="amount"
                      nameKey="categoryName"
                      cx="50%"
                      cy="50%"
                      innerRadius={55}
                      outerRadius={85}
                      paddingAngle={3}
                    >
                      {data.spendingByCategory.map((entry) => (
                        <Cell key={entry.categoryId} fill={entry.color} />
                      ))}
                    </Pie>
                    <Tooltip formatter={(val) => formatCurrency(Number(val), currency)} />
                    <Legend />
                  </PieChart>
                </ResponsiveContainer>
              </div>
            )}
          </CardContent>
        </Card>
      </div>

      {/* Bottom Row: Budget Utilization & Recent Transactions */}
      <div className="grid gap-6 lg:grid-cols-2">
        <Card>
          <CardHeader>
            <CardTitle>Budget Utilization</CardTitle>
            <CardDescription>Monthly spending limits</CardDescription>
          </CardHeader>
          <CardContent>
            {data.budgetUtilization.length === 0 ? (
              <p className="py-8 text-center text-sm text-slate-400">
                No budgets set for {MONTHS[month - 1]} {year}.
              </p>
            ) : (
              <div className="space-y-4">
                {data.budgetUtilization.map((b) => (
                  <div key={b.budgetId} className="space-y-1.5">
                    <div className="flex items-center justify-between text-sm">
                      <span className="font-medium text-slate-800">{b.categoryName}</span>
                      <span className="text-slate-600">
                        {formatCurrency(b.spentAmount, currency)} / {formatCurrency(b.budgetAmount, currency)}{' '}
                        <Badge variant={b.isOverBudget ? 'destructive' : 'secondary'} className="ml-1">
                          {b.utilizationPercentage.toFixed(0)}%
                        </Badge>
                      </span>
                    </div>
                    <div className="h-2 w-full overflow-hidden rounded-full bg-slate-100">
                      <div
                        className={cn(
                          'h-full rounded-full transition-all',
                          b.isOverBudget ? 'bg-red-500' : 'bg-indigo-600',
                        )}
                        style={{ width: `${Math.min(100, b.utilizationPercentage)}%` }}
                      />
                    </div>
                  </div>
                ))}
              </div>
            )}
          </CardContent>
        </Card>

        <Card>
          <CardHeader>
            <CardTitle>Recent Transactions</CardTitle>
            <CardDescription>Latest financial activity</CardDescription>
          </CardHeader>
          <CardContent>
            {data.recentTransactions.length === 0 ? (
              <p className="py-8 text-center text-sm text-slate-400">No transactions yet.</p>
            ) : (
              <div className="divide-y divide-slate-100">
                {data.recentTransactions.map((tx) => (
                  <div key={tx.id} className="flex items-center justify-between py-3 first:pt-0 last:pb-0">
                    <div className="flex items-center gap-3 min-w-0">
                      <span
                        className="h-3 w-3 shrink-0 rounded-full"
                        style={{ backgroundColor: tx.categoryColor }}
                      />
                      <div className="min-w-0">
                        <p className="truncate text-sm font-medium text-slate-900">{tx.title}</p>
                        <p className="text-xs text-slate-500">
                          {tx.categoryName} · {formatDate(tx.transactionDate)}
                        </p>
                      </div>
                    </div>
                    <span
                      className={cn(
                        'text-sm font-semibold shrink-0 ml-3',
                        tx.type === 'Income' ? 'text-emerald-600' : 'text-slate-900',
                      )}
                    >
                      {tx.type === 'Income' ? '+' : '-'}
                      {formatCurrency(tx.amount, currency)}
                    </span>
                  </div>
                ))}
              </div>
            )}
          </CardContent>
        </Card>
      </div>
    </div>
  )
}
