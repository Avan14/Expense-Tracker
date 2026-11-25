import { useState } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { Plus, Pencil, Trash2 } from 'lucide-react'
import { apiClient, getApiErrorMessage } from '@/api/client'
import { useAuth } from '@/context/AuthContext'
import type { Budget, Category } from '@/types/api'
import { formatCurrency, cn } from '@/lib/utils'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Select } from '@/components/ui/select'
import { Badge } from '@/components/ui/badge'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { Dialog } from '@/components/ui/dialog'

const MONTHS = [
  'January', 'February', 'March', 'April', 'May', 'June',
  'July', 'August', 'September', 'October', 'November', 'December',
]

const budgetSchema = z.object({
  categoryId: z.string().min(1, 'Category is required'),
  amount: z.number().positive('Budget amount must be greater than 0'),
})

type BudgetForm = z.infer<typeof budgetSchema>

export function BudgetsPage() {
  const { user } = useAuth()
  const currency = user?.currencyCode || 'USD'
  const queryClient = useQueryClient()
  const now = new Date()

  const [year, setYear] = useState(now.getUTCFullYear())
  const [month, setMonth] = useState(now.getUTCMonth() + 1)
  const [dialogOpen, setDialogOpen] = useState(false)
  const [editing, setEditing] = useState<Budget | null>(null)
  const [errorMsg, setErrorMsg] = useState<string | null>(null)

  const { data: budgets = [], isLoading } = useQuery({
    queryKey: ['budgets', year, month],
    queryFn: async () =>
      (await apiClient.get<Budget[]>('/budgets', { params: { year, month } })).data,
  })

  const { data: categories = [] } = useQuery({
    queryKey: ['categories', 'Expense'],
    queryFn: async () =>
      (await apiClient.get<Category[]>('/categories', { params: { type: 'Expense' } })).data,
  })

  const {
    register,
    handleSubmit,
    reset,
    formState: { errors },
  } = useForm<BudgetForm>({
    resolver: zodResolver(budgetSchema),
  })

  const openCreate = () => {
    setEditing(null)
    setErrorMsg(null)
    reset({ categoryId: categories[0]?.id ?? '', amount: 500 })
    setDialogOpen(true)
  }

  const openEdit = (b: Budget) => {
    setEditing(b)
    setErrorMsg(null)
    reset({ categoryId: b.categoryId, amount: b.budgetAmount })
    setDialogOpen(true)
  }

  const saveMutation = useMutation({
    mutationFn: async (data: BudgetForm) => {
      if (editing) {
        await apiClient.put(`/budgets/${editing.id}`, { amount: data.amount })
      } else {
        await apiClient.post('/budgets', { ...data, year, month })
      }
    },
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ['budgets'] })
      void queryClient.invalidateQueries({ queryKey: ['dashboard'] })
      setDialogOpen(false)
    },
    onError: (err) => setErrorMsg(getApiErrorMessage(err)),
  })

  const deleteMutation = useMutation({
    mutationFn: async (id: string) => apiClient.delete(`/budgets/${id}`),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ['budgets'] })
      void queryClient.invalidateQueries({ queryKey: ['dashboard'] })
    },
  })

  const years = Array.from({ length: 5 }, (_, i) => now.getUTCFullYear() - 2 + i)

  return (
    <div className="space-y-6">
      <div className="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
        <div>
          <h1 className="text-2xl font-bold tracking-tight text-slate-900">Monthly Budgets</h1>
          <p className="text-sm text-slate-500">Set category spending limits and track utilization</p>
        </div>
        <div className="flex items-center gap-2">
          <Select value={month} onChange={(e) => setMonth(Number(e.target.value))} className="w-32">
            {MONTHS.map((m, idx) => (
              <option key={m} value={idx + 1}>
                {m}
              </option>
            ))}
          </Select>
          <Select value={year} onChange={(e) => setYear(Number(e.target.value))} className="w-24">
            {years.map((y) => (
              <option key={y} value={y}>
                {y}
              </option>
            ))}
          </Select>
          <Button onClick={openCreate}>
            <Plus className="h-4 w-4" />
            Add Budget
          </Button>
        </div>
      </div>

      {isLoading ? (
        <p className="text-sm text-slate-400">Loading budgets...</p>
      ) : budgets.length === 0 ? (
        <Card>
          <CardContent className="py-12 text-center text-sm text-slate-500">
            No budgets configured for {MONTHS[month - 1]} {year}. Click &quot;Add Budget&quot; to create one.
          </CardContent>
        </Card>
      ) : (
        <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
          {budgets.map((b) => {
            const isOver = b.spentAmount > b.budgetAmount
            return (
              <Card key={b.id}>
                <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
                  <div className="flex items-center gap-2">
                    <span className="h-3 w-3 rounded-full" style={{ backgroundColor: b.categoryColor }} />
                    <CardTitle className="text-base">{b.categoryName}</CardTitle>
                  </div>
                  <div className="flex items-center gap-1">
                    <Button variant="ghost" size="icon" onClick={() => openEdit(b)}>
                      <Pencil className="h-4 w-4 text-slate-500" />
                    </Button>
                    <Button variant="ghost" size="icon" onClick={() => deleteMutation.mutate(b.id)}>
                      <Trash2 className="h-4 w-4 text-red-500" />
                    </Button>
                  </div>
                </CardHeader>
                <CardContent className="space-y-3">
                  <div className="flex items-baseline justify-between">
                    <span className="text-2xl font-bold text-slate-900">
                      {formatCurrency(b.spentAmount, currency)}
                    </span>
                    <span className="text-sm text-slate-500">
                      of {formatCurrency(b.budgetAmount, currency)}
                    </span>
                  </div>
                  <div className="h-2.5 w-full overflow-hidden rounded-full bg-slate-100">
                    <div
                      className={cn('h-full rounded-full', isOver ? 'bg-red-500' : 'bg-indigo-600')}
                      style={{ width: `${Math.min(100, b.utilizationPercentage)}%` }}
                    />
                  </div>
                  <div className="flex items-center justify-between text-xs">
                    <Badge variant={isOver ? 'destructive' : 'secondary'}>
                      {b.utilizationPercentage.toFixed(1)}% used
                    </Badge>
                    <span className={isOver ? 'font-medium text-red-600' : 'text-slate-500'}>
                      {isOver
                        ? `${formatCurrency(Math.abs(b.remainingAmount), currency)} over budget`
                        : `${formatCurrency(b.remainingAmount, currency)} remaining`}
                    </span>
                  </div>
                </CardContent>
              </Card>
            )
          })}
        </div>
      )}

      <Dialog
        open={dialogOpen}
        onClose={() => setDialogOpen(false)}
        title={editing ? `Edit ${editing.categoryName} Budget` : 'Create Monthly Budget'}
      >
        {errorMsg && (
          <div className="mb-4 rounded-lg border border-red-200 bg-red-50 px-3 py-2 text-sm text-red-700">
            {errorMsg}
          </div>
        )}
        <form onSubmit={handleSubmit((d) => saveMutation.mutate(d))} className="space-y-4">
          {!editing && (
            <div className="space-y-1.5">
              <Label>Expense Category</Label>
              <Select {...register('categoryId')}>
                {categories.map((c) => (
                  <option key={c.id} value={c.id}>
                    {c.name}
                  </option>
                ))}
              </Select>
              {errors.categoryId && <p className="text-xs text-red-600">{errors.categoryId.message}</p>}
            </div>
          )}

          <div className="space-y-1.5">
            <Label>Monthly Limit</Label>
            <Input type="number" step="0.01" {...register('amount', { valueAsNumber: true })} />
            {errors.amount && <p className="text-xs text-red-600">{errors.amount.message}</p>}
          </div>

          <div className="flex justify-end gap-2 pt-2">
            <Button type="button" variant="outline" onClick={() => setDialogOpen(false)}>
              Cancel
            </Button>
            <Button type="submit" disabled={saveMutation.isPending}>
              {saveMutation.isPending ? 'Saving...' : 'Save Budget'}
            </Button>
          </div>
        </form>
      </Dialog>
    </div>
  )
}
