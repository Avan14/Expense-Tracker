import { useState } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { Plus, Trash2, ArrowUpCircle } from 'lucide-react'
import { apiClient, getApiErrorMessage } from '@/api/client'
import { useAuth } from '@/context/AuthContext'
import type { SavingsGoal } from '@/types/api'
import { formatCurrency, formatDate } from '@/lib/utils'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Badge } from '@/components/ui/badge'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { Dialog } from '@/components/ui/dialog'

const goalSchema = z.object({
  name: z.string().min(1, 'Goal name is required').max(120),
  targetAmount: z.number().positive('Target must be positive'),
  currentAmount: z.number().min(0),
  targetDate: z.string().optional(),
  notes: z.string().max(1000).optional(),
})

type GoalForm = z.infer<typeof goalSchema>

export function SavingsGoalsPage() {
  const { user } = useAuth()
  const currency = user?.currencyCode || 'USD'
  const queryClient = useQueryClient()

  const [dialogOpen, setDialogOpen] = useState(false)
  const [contributeGoal, setContributeGoal] = useState<SavingsGoal | null>(null)
  const [contributeAmount, setContributeAmount] = useState('100')
  const [errorMsg, setErrorMsg] = useState<string | null>(null)

  const { data: goals = [], isLoading } = useQuery({
    queryKey: ['savings-goals'],
    queryFn: async () => (await apiClient.get<SavingsGoal[]>('/savings-goals')).data,
  })

  const {
    register,
    handleSubmit,
    reset,
    formState: { errors },
  } = useForm<GoalForm>({
    resolver: zodResolver(goalSchema),
    defaultValues: { currentAmount: 0 },
  })

  const createMutation = useMutation({
    mutationFn: async (data: GoalForm) => {
      await apiClient.post('/savings-goals', {
        ...data,
        targetDate: data.targetDate || null,
      })
    },
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ['savings-goals'] })
      setDialogOpen(false)
      reset()
    },
    onError: (err) => setErrorMsg(getApiErrorMessage(err)),
  })

  const contributeMutation = useMutation({
    mutationFn: async ({ id, amount }: { id: string; amount: number }) =>
      apiClient.post(`/savings-goals/${id}/contribute`, { amount }),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ['savings-goals'] })
      setContributeGoal(null)
    },
    onError: (err) => setErrorMsg(getApiErrorMessage(err)),
  })

  const deleteMutation = useMutation({
    mutationFn: async (id: string) => apiClient.delete(`/savings-goals/${id}`),
    onSuccess: () => void queryClient.invalidateQueries({ queryKey: ['savings-goals'] }),
  })

  return (
    <div className="space-y-6">
      <div className="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
        <div>
          <h1 className="text-2xl font-bold tracking-tight text-slate-900">Savings Goals</h1>
          <p className="text-sm text-slate-500">Track progress toward your financial milestones</p>
        </div>
        <Button
          onClick={() => {
            setErrorMsg(null)
            reset({ name: '', targetAmount: 1000, currentAmount: 0, targetDate: '', notes: '' })
            setDialogOpen(true)
          }}
        >
          <Plus className="h-4 w-4" />
          New Goal
        </Button>
      </div>

      {isLoading ? (
        <p className="text-sm text-slate-400">Loading savings goals...</p>
      ) : goals.length === 0 ? (
        <Card>
          <CardContent className="py-12 text-center text-sm text-slate-500">
            No savings goals yet. Create your first goal to start tracking!
          </CardContent>
        </Card>
      ) : (
        <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
          {goals.map((g) => (
            <Card key={g.id}>
              <CardHeader className="flex flex-row items-start justify-between space-y-0 pb-2">
                <div>
                  <CardTitle className="text-base">{g.name}</CardTitle>
                  {g.targetDate && (
                    <p className="mt-1 text-xs text-slate-500">Target: {formatDate(g.targetDate)}</p>
                  )}
                </div>
                <Button variant="ghost" size="icon" onClick={() => deleteMutation.mutate(g.id)}>
                  <Trash2 className="h-4 w-4 text-red-500" />
                </Button>
              </CardHeader>
              <CardContent className="space-y-3">
                <div className="flex items-baseline justify-between">
                  <span className="text-2xl font-bold text-slate-900">
                    {formatCurrency(g.currentAmount, currency)}
                  </span>
                  <span className="text-sm text-slate-500">
                    of {formatCurrency(g.targetAmount, currency)}
                  </span>
                </div>
                <div className="h-2.5 w-full overflow-hidden rounded-full bg-slate-100">
                  <div
                    className="h-full rounded-full bg-emerald-500 transition-all"
                    style={{ width: `${Math.min(100, g.progressPercentage)}%` }}
                  />
                </div>
                <div className="flex items-center justify-between">
                  <Badge variant={g.isCompleted ? 'success' : 'secondary'}>
                    {g.isCompleted ? 'Completed' : `${g.progressPercentage.toFixed(1)}%`}
                  </Badge>
                  <Button
                    size="sm"
                    variant="outline"
                    onClick={() => {
                      setErrorMsg(null)
                      setContributeAmount('100')
                      setContributeGoal(g)
                    }}
                  >
                    <ArrowUpCircle className="h-3.5 w-3.5" />
                    Contribute
                  </Button>
                </div>
              </CardContent>
            </Card>
          ))}
        </div>
      )}

      <Dialog open={dialogOpen} onClose={() => setDialogOpen(false)} title="New Savings Goal">
        {errorMsg && (
          <div className="mb-4 rounded-lg border border-red-200 bg-red-50 px-3 py-2 text-sm text-red-700">
            {errorMsg}
          </div>
        )}
        <form onSubmit={handleSubmit((d) => createMutation.mutate(d))} className="space-y-4">
          <div className="space-y-1.5">
            <Label>Goal Name</Label>
            <Input {...register('name')} placeholder="e.g., Emergency Fund" />
            {errors.name && <p className="text-xs text-red-600">{errors.name.message}</p>}
          </div>
          <div className="grid grid-cols-2 gap-3">
            <div className="space-y-1.5">
              <Label>Target Amount</Label>
              <Input type="number" step="0.01" {...register('targetAmount', { valueAsNumber: true })} />
              {errors.targetAmount && <p className="text-xs text-red-600">{errors.targetAmount.message}</p>}
            </div>
            <div className="space-y-1.5">
              <Label>Initial Saved</Label>
              <Input type="number" step="0.01" {...register('currentAmount', { valueAsNumber: true })} />
            </div>
          </div>
          <div className="space-y-1.5">
            <Label>Target Date (optional)</Label>
            <Input type="date" {...register('targetDate')} />
          </div>
          <div className="flex justify-end gap-2 pt-2">
            <Button type="button" variant="outline" onClick={() => setDialogOpen(false)}>
              Cancel
            </Button>
            <Button type="submit" disabled={createMutation.isPending}>
              {createMutation.isPending ? 'Creating...' : 'Create Goal'}
            </Button>
          </div>
        </form>
      </Dialog>

      <Dialog
        open={!!contributeGoal}
        onClose={() => setContributeGoal(null)}
        title={`Contribute to "${contributeGoal?.name}"`}
      >
        {errorMsg && (
          <div className="mb-4 rounded-lg border border-red-200 bg-red-50 px-3 py-2 text-sm text-red-700">
            {errorMsg}
          </div>
        )}
        <div className="space-y-4">
          <div className="space-y-1.5">
            <Label>Amount (use negative to withdraw)</Label>
            <Input
              type="number"
              step="0.01"
              value={contributeAmount}
              onChange={(e) => setContributeAmount(e.target.value)}
            />
          </div>
          <div className="flex justify-end gap-2">
            <Button variant="outline" onClick={() => setContributeGoal(null)}>
              Cancel
            </Button>
            <Button
              disabled={contributeMutation.isPending}
              onClick={() =>
                contributeGoal &&
                contributeMutation.mutate({
                  id: contributeGoal.id,
                  amount: Number(contributeAmount),
                })
              }
            >
              Update Savings
            </Button>
          </div>
        </div>
      </Dialog>
    </div>
  )
}
