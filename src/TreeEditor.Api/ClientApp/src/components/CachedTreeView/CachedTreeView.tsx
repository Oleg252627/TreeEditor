import { useState } from 'react'
import { HardDrive, LoaderCircle, Save, Trash2 } from 'lucide-react'
import { toast } from 'sonner'
import { ApiError, applyChanges } from '../../api/treeApi'
import { isPendingState, useTreeCacheStore } from '../../store/treeCacheStore'
import { getCachedParentId } from '../../store/cachedHierarchy'
import type { CachedTreeNode } from '../../types/tree'
import { CachedTreeNodeView } from './CachedTreeNode'
import { CachedNodeEditor } from './CachedNodeEditor'

interface CachedTreeViewProps {
    onApplied: () => void
    onConcurrencyConflict: () => void
}

export function CachedTreeView({ onApplied, onConcurrencyConflict }: CachedTreeViewProps) {
    const nodes = useTreeCacheStore((state) => state.nodes)
    const buildChangeSet = useTreeCacheStore((state) => state.buildChangeSet)
    const clear = useTreeCacheStore((state) => state.clear)
    const markApplied = useTreeCacheStore((state) => state.markApplied)
    const [applying, setApplying] = useState(false)

    const roots: CachedTreeNode[] = []
    let hasPendingChanges = false

    for (const node of nodes.values()) {
        if (getCachedParentId(node, nodes) === null) {
            roots.push(node)
        }

        if (isPendingState(node.state)) {
            hasPendingChanges = true
        }
    }

    roots.sort((a, b) => a.value.localeCompare(b.value))

    async function handleApply() {
        if (!hasPendingChanges || applying) {
            return
        }

        try {
            setApplying(true)

            const changeSet = buildChangeSet()
            const response = await applyChanges(changeSet)

            markApplied(changeSet, response)
            onApplied()
            toast.success('Changes applied successfully')
        } catch (error) {
            console.error(error)

            if (error instanceof ApiError && error.status === 409) {
                onConcurrencyConflict()
                toast.error('Cached data is out of date', {
                    description: 'The database tree has changed. Reload the database tree before continuing.',
                })
                return
            }

            if (error instanceof ApiError && error.status === 404) {
                toast.error('Tree node was not found', { description: error.message })
                return
            }

            if (error instanceof ApiError && error.status === 400) {
                toast.error('Invalid changes', { description: error.message })
                return
            }

            toast.error('Failed to apply changes')
        } finally {
            setApplying(false)
        }
    }

    return (
        <section className="flex h-[calc(100vh-140px)] min-h-[500px] flex-col overflow-hidden rounded-2xl border border-slate-200 bg-white shadow-sm dark:border-slate-800 dark:bg-slate-900">
            <header className="flex shrink-0 items-center gap-3 border-b border-slate-200 px-5 py-4 dark:border-slate-800">
                <div className="flex h-9 w-9 items-center justify-center rounded-lg bg-violet-50 text-violet-600 dark:bg-violet-950 dark:text-violet-400">
                    <HardDrive size={18} />
                </div>

                <div>
                    <h2 className="font-semibold">Cached Tree</h2>
                    <p className="text-xs text-slate-500 dark:text-slate-400">
                        Local pending changes
                    </p>
                </div>

                <div className="ml-auto flex items-center gap-2">
                    {hasPendingChanges && (
                        <span className="rounded-full bg-amber-100 px-2.5 py-1 text-xs font-medium text-amber-700 dark:bg-amber-950 dark:text-amber-300">
                            Unsaved changes
                        </span>
                    )}

                    {nodes.size > 0 && (
                        <button
                            type="button"
                            onClick={() => {
                                clear()
                                toast.success('Cache cleared')
                            }}
                            title="Clear cache"
                            aria-label="Clear cache"
                            className="flex h-9 w-9 items-center justify-center rounded-lg text-slate-500 transition hover:bg-red-50 hover:text-red-600 dark:text-slate-400 dark:hover:bg-red-950/40 dark:hover:text-red-400"
                        >
                            <Trash2 size={17} />
                        </button>
                    )}
                </div>
            </header>

            <div className="min-h-0 flex-1 overflow-auto p-3">
                {roots.length === 0 ? (
                    <div className="flex h-full min-h-60 flex-col items-center justify-center text-center text-slate-400 dark:text-slate-500">
                        <HardDrive size={32} strokeWidth={1.5} />
                        <p className="mt-3 text-sm font-medium">Cache is empty</p>
                        <p className="mt-1 max-w-56 text-xs">
                            Select a node in the database tree and load it into the local cache.
                        </p>
                    </div>
                ) : (
                    <div className="min-w-max">
                        {roots.map((node) => (
                            <CachedTreeNodeView key={node.id} node={node} depth={0} />
                        ))}
                    </div>
                )}
            </div>

            {nodes.size > 0 && (
                <div className="shrink-0">
                    <CachedNodeEditor />
                </div>
            )}

            <footer className="shrink-0 border-t border-slate-200 px-4 py-3 dark:border-slate-800">
                <button
                    type="button"
                    disabled={!hasPendingChanges || applying}
                    onClick={handleApply}
                    className="flex w-full items-center justify-center gap-2 rounded-lg bg-violet-600 px-4 py-2.5 text-sm font-medium text-white transition hover:bg-violet-700 disabled:cursor-not-allowed disabled:opacity-50"
                >
                    {applying ? (
                        <LoaderCircle size={16} className="animate-spin" />
                    ) : (
                        <Save size={16} />
                    )}
                    {applying ? 'Applying...' : 'Apply changes'}
                </button>
            </footer>
        </section>
    )
}
