import { useState } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { Plus, Search, Pencil, Trash2, ChevronLeft, ChevronRight } from 'lucide-react'
import { apiClient, getApiErrorMessage } from '@/api/client'
import { useAuth } from '@/context/AuthContext'
import type { Category, PagedResult, PaymentMethod, Transaction, TransactionType } from '@/types/api'
import { formatCurrency, formatDate, cn } from '@/lib/utils'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Select } from '@/components/ui/select'
import { Badge } from '@/components/ui/badge'
import { Card, CardContent } from '@/components/ui/card'
import { Dialog } from '@/components/ui/dialog'

const PAYMENT_METHODS: PaymentMethod[] = [
  'Cash',
  'CreditCard',
  'DebitCard',
  'BankTransfer',
  'Upi',
  'DigitalWallet',
  'Other',
]

const txSchema = z.object({
  title: z.string().min(1, 'Title is required').max(150),
  amount: z.number().positive('Amount must be greater than 0'),
  type: z.enum(['Income', 'Expense']),
  categoryId: z.string().min(1, 'Category is required'),
  transactionDate: z.string().min(1, 'Date is required'),
  paymentMethod: z.enum(['Cash', 'CreditCard', 'DebitCard', 'BankTransfer', 'Upi', 'DigitalWallet', 'Other']),
  description: z.string().max(500).optional(),
  notes: z.string().max(1000).optional(),
})

type TxForm = z.infer<typeof txSchema>

export function TransactionsPage() {
  const { user } = useAuth()
  const currency = user?.currencyCode || 'USD'
  const queryClient = useQueryClient()

  const [search, setSearch] = useState('')
  const [typeFilter, setTypeFilter] = useState<string>('')
  const [categoryFilter, setCategoryFilter] = useState<string>('')
  const [startDate, setStartDate] = useState('')
  const [endDate, setEndDate] = useState('')
  const [sortBy, setSortBy] = useState<'date' | 'amount'>('date')
  const [sortDirection, setSortDirection] = useState<'asc' | 'desc'>('desc')
  const [page, setPage] = useState(1)

  const [dialogOpen, setDialogOpen] = useState(false)
  const [editingTx, setEditingTx] = useState<Transaction | null>(null)
  const [formError, setFormError] = useState<string | null>(null)

  const { data: categories = [] } = useQuery({
    queryKey: ['categories'],
    queryFn: async () => (await apiClient.get<Category[]>('/categories')).data,
  })

  const { data: txPage, isLoading } = useQuery({
    queryKey: ['transactions', { search, typeFilter, categoryFilter, startDate, endDate, sortBy, sortDirection, page }],
    queryFn: async () => {
      const res = await apiClient.get<PagedResult<Transaction>>('/transactions', {
        params: {
          search: search || undefined,
          type: typeFilter || undefined,
          categoryId: categoryFilter || undefined,
          startDate: startDate || undefined,
          endDate: endDate || undefined,
          sortBy,
          sortDirection,
          page,
          pageSize: 10,
        },
      })
      return res.data
    },
  })

  const {
    register,
    handleSubmit,
    watch,
    reset,
    formState: { errors, isSubmitting },
  } = useForm<TxForm>({
    resolver: zodResolver(txSchema),
    defaultValues: {
      type: 'Expense',
      paymentMethod: 'CreditCard',
      transactionDate: new Date().toISOString().slice(0, 10),
    },
  })

  const selectedType = watch('type')
  const filteredFormCategories = categories.filter((c) => c.type === selectedType)

  const openCreate = () => {
    setEditingTx(null)
    setFormError(null)
    const defaultExpCat = categories.find((c) => c.type === 'Expense')?.id ?? ''
    reset({
      title: '',
      amount: 0,
      type: 'Expense',
      categoryId: defaultExpCat,
      transactionDate: new Date().toISOString().slice(0, 10),
      paymentMethod: 'CreditCard',
      description: '',
      notes: '',
    })
    setDialogOpen(true)
  }

  const openEdit = (tx: Transaction) => {
    setEditingTx(tx)
    setFormError(null)
    reset({
      title: tx.title,
      amount: tx.amount,
      type: tx.type,
      categoryId: tx.categoryId,
      transactionDate: tx.transactionDate.slice(0, 10),
      paymentMethod: tx.paymentMethod,
      description: tx.description ?? '',
      notes: tx.notes ?? '',
    })
    setDialogOpen(true)
  }

  const saveMutation = useMutation({
    mutationFn: async (data: TxForm) => {
      if (editingTx) {
        await apiClient.put(`/transactions/${editingTx.id}`, data)
      } else {
        await apiClient.post('/transactions', data)
      }
    },
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ['transactions'] })
      void queryClient.invalidateQueries({ queryKey: ['dashboard'] })
      void queryClient.invalidateQueries({ queryKey: ['budgets'] })
      void queryClient.invalidateQueries({ queryKey: ['categories'] })
      setDialogOpen(false)
    },
    onError: (err) => setFormError(getApiErrorMessage(err)),
  })

  const deleteMutation = useMutation({
    mutationFn: async (id: string) => apiClient.delete(`/transactions/${id}`),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ['transactions'] })
      void queryClient.invalidateQueries({ queryKey: ['dashboard'] })
      void queryClient.invalidateQueries({ queryKey: ['budgets'] })
      void queryClient.invalidateQueries({ queryKey: ['categories'] })
    },
  })

  return (
    <div className="space-y-6">
      <div className="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
        <div>
          <h1 className="text-2xl font-bold tracking-tight text-slate-900">Transactions</h1>
          <p className="text-sm text-slate-500">Search, filter, and manage your financial records</p>
        </div>
        <Button onClick={openCreate}>
          <Plus className="h-4 w-4" />
          Add Transaction
        </Button>
      </div>

      {/* Filters */}
      <Card>
        <CardContent className="grid gap-3 p-4 sm:grid-cols-2 lg:grid-cols-6">
          <div className="relative lg:col-span-2">
            <Search className="absolute left-3 top-2.5 h-4 w-4 text-slate-400" />
            <Input
              placeholder="Search title, notes, category..."
              value={search}
              onChange={(e) => {
                setSearch(e.target.value)
                setPage(1)
              }}
              className="pl-9"
            />
          </div>

          <Select
            value={typeFilter}
            onChange={(e) => {
              setTypeFilter(e.target.value as TransactionType | '')
              setPage(1)
            }}
          >
            <option value="">All Types</option>
            <option value="Income">Income</option>
            <option value="Expense">Expense</option>
          </Select>

          <Select
            value={categoryFilter}
            onChange={(e) => {
              setCategoryFilter(e.target.value)
              setPage(1)
            }}
          >
            <option value="">All Categories</option>
            {categories.map((c) => (
              <option key={c.id} value={c.id}>
                {c.name} ({c.type})
              </option>
            ))}
          </Select>

          <Input
            type="date"
            value={startDate}
            onChange={(e) => {
              setStartDate(e.target.value)
              setPage(1)
            }}
            title="Start Date"
          />

          <Input
            type="date"
            value={endDate}
            onChange={(e) => {
              setEndDate(e.target.value)
              setPage(1)
            }}
            title="End Date"
          />
        </CardContent>
      </Card>

      {/* Sort & Table */}
      <Card>
        <div className="flex items-center justify-between border-b border-slate-200 px-4 py-3 text-sm">
          <span className="text-slate-500">
            {txPage ? `${txPage.totalCount} transaction(s) found` : 'Loading...'}
          </span>
          <div className="flex items-center gap-2">
            <Select
              value={sortBy}
              onChange={(e) => setSortBy(e.target.value as 'date' | 'amount')}
              className="w-28"
            >
              <option value="date">Date</option>
              <option value="amount">Amount</option>
            </Select>
            <Select
              value={sortDirection}
              onChange={(e) => setSortDirection(e.target.value as 'asc' | 'desc')}
              className="w-28"
            >
              <option value="desc">Desc</option>
              <option value="asc">Asc</option>
            </Select>
          </div>
        </div>

        <div className="overflow-x-auto">
          <table className="w-full text-left text-sm">
            <thead className="border-b border-slate-200 bg-slate-50 text-xs uppercase text-slate-500">
              <tr>
                <th className="px-4 py-3">Title</th>
                <th className="px-4 py-3">Category</th>
                <th className="px-4 py-3">Date</th>
                <th className="px-4 py-3">Method</th>
                <th className="px-4 py-3 text-right">Amount</th>
                <th className="px-4 py-3 text-right">Actions</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-slate-100">
              {isLoading ? (
                <tr>
                  <td colSpan={6} className="px-4 py-8 text-center text-slate-400">
                    Loading transactions...
                  </td>
                </tr>
              ) : txPage?.items.length === 0 ? (
                <tr>
                  <td colSpan={6} className="px-4 py-8 text-center text-slate-400">
                    No transactions match your filters.
                  </td>
                </tr>
              ) : (
                txPage?.items.map((tx) => (
                  <tr key={tx.id} className="hover:bg-slate-50/70">
                    <td className="px-4 py-3">
                      <p className="font-medium text-slate-900">{tx.title}</p>
                      {tx.description && <p className="text-xs text-slate-500 truncate max-w-xs">{tx.description}</p>}
                    </td>
                    <td className="px-4 py-3">
                      <Badge variant="outline" className="gap-1.5">
                        <span className="h-2 w-2 rounded-full" style={{ backgroundColor: tx.categoryColor }} />
                        {tx.categoryName}
                      </Badge>
                    </td>
                    <td className="px-4 py-3 text-slate-600">{formatDate(tx.transactionDate)}</td>
                    <td className="px-4 py-3 text-slate-600">{tx.paymentMethod}</td>
                    <td
                      className={cn(
                        'px-4 py-3 text-right font-semibold',
                        tx.type === 'Income' ? 'text-emerald-600' : 'text-slate-900',
                      )}
                    >
                      {tx.type === 'Income' ? '+' : '-'}
                      {formatCurrency(tx.amount, currency)}
                    </td>
                    <td className="px-4 py-3 text-right">
                      <div className="inline-flex items-center gap-1">
                        <Button variant="ghost" size="icon" onClick={() => openEdit(tx)}>
                          <Pencil className="h-4 w-4 text-slate-500" />
                        </Button>
                        <Button
                          variant="ghost"
                          size="icon"
                          onClick={() => deleteMutation.mutate(tx.id)}
                          disabled={deleteMutation.isPending}
                        >
                          <Trash2 className="h-4 w-4 text-red-500" />
                        </Button>
                      </div>
                    </td>
                  </tr>
                ))
              )}
            </tbody>
          </table>
        </div>

        {/* Pagination */}
        {txPage && txPage.totalPages > 1 && (
          <div className="flex items-center justify-between border-t border-slate-200 px-4 py-3 text-sm">
            <span className="text-slate-500">
              Page {txPage.page} of {txPage.totalPages}
            </span>
            <div className="flex items-center gap-2">
              <Button
                variant="outline"
                size="sm"
                disabled={!txPage.hasPreviousPage}
                onClick={() => setPage((p) => p - 1)}
              >
                <ChevronLeft className="h-4 w-4" />
                Prev
              </Button>
              <Button
                variant="outline"
                size="sm"
                disabled={!txPage.hasNextPage}
                onClick={() => setPage((p) => p + 1)}
              >
                Next
                <ChevronRight className="h-4 w-4" />
              </Button>
            </div>
          </div>
        )}
      </Card>

      {/* Create / Edit Dialog */}
      <Dialog
        open={dialogOpen}
        onClose={() => setDialogOpen(false)}
        title={editingTx ? 'Edit Transaction' : 'New Transaction'}
      >
        {formError && (
          <div className="mb-4 rounded-lg border border-red-200 bg-red-50 px-3 py-2 text-sm text-red-700">
            {formError}
          </div>
        )}
        <form onSubmit={handleSubmit((d) => saveMutation.mutate(d))} className="space-y-4">
          <div className="grid grid-cols-2 gap-3">
            <div className="space-y-1.5">
              <Label>Type</Label>
              <Select {...register('type')}>
                <option value="Expense">Expense</option>
                <option value="Income">Income</option>
              </Select>
            </div>
            <div className="space-y-1.5">
              <Label>Amount</Label>
              <Input type="number" step="0.01" {...register('amount', { valueAsNumber: true })} />
              {errors.amount && <p className="text-xs text-red-600">{errors.amount.message}</p>}
            </div>
          </div>

          <div className="space-y-1.5">
            <Label>Title</Label>
            <Input {...register('title')} placeholder="e.g., Grocery Shopping" />
            {errors.title && <p className="text-xs text-red-600">{errors.title.message}</p>}
          </div>

          <div className="grid grid-cols-2 gap-3">
            <div className="space-y-1.5">
              <Label>Category</Label>
              <Select {...register('categoryId')}>
                <option value="">Select category</option>
                {filteredFormCategories.map((c) => (
                  <option key={c.id} value={c.id}>
                    {c.name}
                  </option>
                ))}
              </Select>
              {errors.categoryId && <p className="text-xs text-red-600">{errors.categoryId.message}</p>}
            </div>
            <div className="space-y-1.5">
              <Label>Date</Label>
              <Input type="date" {...register('transactionDate')} />
              {errors.transactionDate && <p className="text-xs text-red-600">{errors.transactionDate.message}</p>}
            </div>
          </div>

          <div className="space-y-1.5">
            <Label>Payment Method</Label>
            <Select {...register('paymentMethod')}>
              {PAYMENT_METHODS.map((pm) => (
                <option key={pm} value={pm}>
                  {pm}
                </option>
              ))}
            </Select>
          </div>

          <div className="space-y-1.5">
            <Label>Description (optional)</Label>
            <Input {...register('description')} />
          </div>

          <div className="space-y-1.5">
            <Label>Notes (optional)</Label>
            <Input {...register('notes')} />
          </div>

          <div className="flex justify-end gap-2 pt-2">
            <Button type="button" variant="outline" onClick={() => setDialogOpen(false)}>
              Cancel
            </Button>
            <Button type="submit" disabled={isSubmitting || saveMutation.isPending}>
              {saveMutation.isPending ? 'Saving...' : 'Save Transaction'}
            </Button>
          </div>
        </form>
      </Dialog>
    </div>
  )
}
