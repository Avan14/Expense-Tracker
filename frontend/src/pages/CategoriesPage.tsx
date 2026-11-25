import { useState } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { Plus, Pencil, Trash2 } from 'lucide-react'
import { apiClient, getApiErrorMessage } from '@/api/client'
import type { Category, TransactionType } from '@/types/api'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Select } from '@/components/ui/select'
import { Badge } from '@/components/ui/badge'
import { Card, CardContent } from '@/components/ui/card'
import { Dialog } from '@/components/ui/dialog'

const categorySchema = z.object({
  name: z.string().min(1, 'Name is required').max(80),
  type: z.enum(['Income', 'Expense']),
  icon: z.string().min(1).max(50),
  color: z.string().regex(/^#[0-9A-Fa-f]{6}$/, 'Must be a valid hex color'),
})

type CategoryForm = z.infer<typeof categorySchema>

export function CategoriesPage() {
  const queryClient = useQueryClient()
  const [filterType, setFilterType] = useState<'All' | TransactionType>('All')
  const [dialogOpen, setDialogOpen] = useState(false)
  const [editingCat, setEditingCat] = useState<Category | null>(null)
  const [deleteTarget, setDeleteTarget] = useState<Category | null>(null)
  const [reassignId, setReassignId] = useState('')
  const [errorMsg, setErrorMsg] = useState<string | null>(null)

  const { data: categories = [], isLoading } = useQuery({
    queryKey: ['categories'],
    queryFn: async () => (await apiClient.get<Category[]>('/categories')).data,
  })

  const {
    register,
    handleSubmit,
    reset,
    formState: { errors },
  } = useForm<CategoryForm>({
    resolver: zodResolver(categorySchema),
    defaultValues: { type: 'Expense', icon: 'tag', color: '#6366f1' },
  })

  const openCreate = () => {
    setEditingCat(null)
    setErrorMsg(null)
    reset({ name: '', type: 'Expense', icon: 'tag', color: '#6366f1' })
    setDialogOpen(true)
  }

  const openEdit = (cat: Category) => {
    setEditingCat(cat)
    setErrorMsg(null)
    reset({ name: cat.name, type: cat.type, icon: cat.icon, color: cat.color })
    setDialogOpen(true)
  }

  const saveMutation = useMutation({
    mutationFn: async (data: CategoryForm) => {
      if (editingCat) {
        await apiClient.put(`/categories/${editingCat.id}`, {
          name: data.name,
          icon: data.icon,
          color: data.color,
        })
      } else {
        await apiClient.post('/categories', data)
      }
    },
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ['categories'] })
      setDialogOpen(false)
    },
    onError: (err) => setErrorMsg(getApiErrorMessage(err)),
  })

  const deleteMutation = useMutation({
    mutationFn: async ({ id, reassignTo }: { id: string; reassignTo?: string }) =>
      apiClient.delete(`/categories/${id}`, {
        params: reassignTo ? { reassignToCategoryId: reassignTo } : undefined,
      }),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ['categories'] })
      void queryClient.invalidateQueries({ queryKey: ['transactions'] })
      setDeleteTarget(null)
      setReassignId('')
    },
    onError: (err) => setErrorMsg(getApiErrorMessage(err)),
  })

  const displayed = categories.filter((c) => filterType === 'All' || c.type === filterType)
  const fallbackOptions = deleteTarget
    ? categories.filter((c) => c.type === deleteTarget.type && c.id !== deleteTarget.id)
    : []

  return (
    <div className="space-y-6">
      <div className="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
        <div>
          <h1 className="text-2xl font-bold tracking-tight text-slate-900">Categories</h1>
          <p className="text-sm text-slate-500">Manage default and custom income/expense categories</p>
        </div>
        <Button onClick={openCreate}>
          <Plus className="h-4 w-4" />
          New Category
        </Button>
      </div>

      <div className="flex gap-2">
        {(['All', 'Income', 'Expense'] as const).map((tab) => (
          <Button
            key={tab}
            variant={filterType === tab ? 'default' : 'outline'}
            size="sm"
            onClick={() => setFilterType(tab)}
          >
            {tab}
          </Button>
        ))}
      </div>

      {isLoading ? (
        <p className="text-sm text-slate-400">Loading categories...</p>
      ) : (
        <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
          {displayed.map((cat) => (
            <Card key={cat.id}>
              <CardContent className="flex items-center justify-between p-4">
                <div className="flex items-center gap-3">
                  <div
                    className="h-10 w-10 rounded-lg flex items-center justify-center text-white font-semibold text-sm"
                    style={{ backgroundColor: cat.color }}
                  >
                    {cat.name.slice(0, 2).toUpperCase()}
                  </div>
                  <div>
                    <div className="flex items-center gap-2">
                      <p className="font-medium text-slate-900">{cat.name}</p>
                      <Badge variant={cat.isDefault ? 'secondary' : 'default'}>
                        {cat.isDefault ? 'Default' : 'Custom'}
                      </Badge>
                    </div>
                    <p className="text-xs text-slate-500">
                      {cat.type} · {cat.transactionCount} transaction(s)
                    </p>
                  </div>
                </div>

                {!cat.isDefault && (
                  <div className="flex items-center gap-1">
                    <Button variant="ghost" size="icon" onClick={() => openEdit(cat)}>
                      <Pencil className="h-4 w-4 text-slate-500" />
                    </Button>
                    <Button
                      variant="ghost"
                      size="icon"
                      onClick={() => {
                        setErrorMsg(null)
                        setReassignId(
                          categories.find((c) => c.type === cat.type && c.id !== cat.id)?.id ?? '',
                        )
                        setDeleteTarget(cat)
                      }}
                    >
                      <Trash2 className="h-4 w-4 text-red-500" />
                    </Button>
                  </div>
                )}
              </CardContent>
            </Card>
          ))}
        </div>
      )}

      {/* Create / Edit Dialog */}
      <Dialog
        open={dialogOpen}
        onClose={() => setDialogOpen(false)}
        title={editingCat ? 'Edit Category' : 'Create Custom Category'}
      >
        {errorMsg && (
          <div className="mb-4 rounded-lg border border-red-200 bg-red-50 px-3 py-2 text-sm text-red-700">
            {errorMsg}
          </div>
        )}
        <form onSubmit={handleSubmit((d) => saveMutation.mutate(d))} className="space-y-4">
          <div className="space-y-1.5">
            <Label>Name</Label>
            <Input {...register('name')} placeholder="e.g., Subscriptions" />
            {errors.name && <p className="text-xs text-red-600">{errors.name.message}</p>}
          </div>

          {!editingCat && (
            <div className="space-y-1.5">
              <Label>Type</Label>
              <Select {...register('type')}>
                <option value="Expense">Expense</option>
                <option value="Income">Income</option>
              </Select>
            </div>
          )}

          <div className="grid grid-cols-2 gap-3">
            <div className="space-y-1.5">
              <Label>Icon Name</Label>
              <Input {...register('icon')} />
            </div>
            <div className="space-y-1.5">
              <Label>Color</Label>
              <Input type="color" {...register('color')} className="h-9 p-1" />
              {errors.color && <p className="text-xs text-red-600">{errors.color.message}</p>}
            </div>
          </div>

          <div className="flex justify-end gap-2 pt-2">
            <Button type="button" variant="outline" onClick={() => setDialogOpen(false)}>
              Cancel
            </Button>
            <Button type="submit" disabled={saveMutation.isPending}>
              {saveMutation.isPending ? 'Saving...' : 'Save'}
            </Button>
          </div>
        </form>
      </Dialog>

      {/* Delete Confirmation + Reassignment Dialog */}
      <Dialog
        open={!!deleteTarget}
        onClose={() => setDeleteTarget(null)}
        title={`Delete "${deleteTarget?.name}"?`}
        description={
          deleteTarget && deleteTarget.transactionCount > 0
            ? `This category is used by ${deleteTarget.transactionCount} transaction(s). Select a fallback category to reassign them.`
            : 'This action cannot be undone.'
        }
      >
        {errorMsg && (
          <div className="mb-4 rounded-lg border border-red-200 bg-red-50 px-3 py-2 text-sm text-red-700">
            {errorMsg}
          </div>
        )}

        {deleteTarget && deleteTarget.transactionCount > 0 && (
          <div className="mb-4 space-y-1.5">
            <Label>Reassign Transactions To</Label>
            <Select value={reassignId} onChange={(e) => setReassignId(e.target.value)}>
              {fallbackOptions.map((opt) => (
                <option key={opt.id} value={opt.id}>
                  {opt.name}
                </option>
              ))}
            </Select>
          </div>
        )}

        <div className="flex justify-end gap-2">
          <Button variant="outline" onClick={() => setDeleteTarget(null)}>
            Cancel
          </Button>
          <Button
            variant="destructive"
            disabled={deleteMutation.isPending}
            onClick={() =>
              deleteTarget &&
              deleteMutation.mutate({
                id: deleteTarget.id,
                reassignTo: deleteTarget.transactionCount > 0 ? reassignId : undefined,
              })
            }
          >
            {deleteMutation.isPending ? 'Deleting...' : 'Delete Category'}
          </Button>
        </div>
      </Dialog>
    </div>
  )
}
