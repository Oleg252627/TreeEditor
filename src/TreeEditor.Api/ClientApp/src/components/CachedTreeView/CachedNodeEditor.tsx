import { useState } from 'react'
import { Pencil, Plus, Trash2 } from 'lucide-react'
import { isDeletedState, useTreeCacheStore } from '../../store/treeCacheStore'
import type { CachedTreeNode } from '../../types/tree'

const MAX_VALUE_LENGTH = 500

export function CachedNodeEditor() {
    const node = useTreeCacheStore((state) =>
        state.selectedId ? state.nodes.get(state.selectedId) : undefined,
    )

    if (!node) {
        return (
            <div className="border-t border-slate-200 px-5 py-4 text-sm text-slate-400 dark:border-slate-800 dark:text-slate-500">
                Select a cached node to edit it.
            </div>
        )
    }

    return <SelectedNodeEditor key={`${node.id}:${node.value}`} node={node} />
}

function SelectedNodeEditor({ node }: { node: CachedTreeNode }) {
    const updateValue = useTreeCacheStore((state) => state.updateValue)
    const addChild = useTreeCacheStore((state) => state.addChild)
    const deleteNode = useTreeCacheStore((state) => state.deleteNode)

    const [value, setValue] = useState(node.value)
    const [childValue, setChildValue] = useState('')

    const deleted = isDeletedState(node.state)

    function saveValue() {
        const normalizedValue = value.trim()

        if (!normalizedValue) {
            return
        }

        updateValue(node.id, normalizedValue)
    }

    function createChild() {
        const normalizedValue = childValue.trim()

        if (!normalizedValue) {
            return
        }

        addChild(node.id, normalizedValue)
        setChildValue('')
    }

    return (
        <div className="border-t border-slate-200 px-5 py-4 dark:border-slate-800">
            <div className="mb-4">
                <div className="mb-2 flex items-center gap-2">
                    <Pencil size={15} className="text-slate-400" />
                    <span className="text-xs font-medium uppercase tracking-wide text-slate-500 dark:text-slate-400">
                        Edit node
                    </span>
                </div>

                <div className="flex gap-2">
                    <input
                        type="text"
                        value={value}
                        maxLength={MAX_VALUE_LENGTH}
                        disabled={deleted}
                        onChange={(event) => setValue(event.target.value)}
                        onKeyDown={(event) => {
                            if (event.key === 'Enter') {
                                saveValue()
                            }
                        }}
                        className="min-w-0 flex-1 rounded-lg border border-slate-300 bg-white px-3 py-2 text-sm outline-none transition focus:border-violet-500 focus:ring-2 focus:ring-violet-500/20 disabled:cursor-not-allowed disabled:opacity-50 dark:border-slate-700 dark:bg-slate-950"
                    />

                    <button
                        type="button"
                        disabled={deleted || !value.trim() || value.trim() === node.value}
                        onClick={saveValue}
                        className="rounded-lg bg-violet-600 px-4 py-2 text-sm font-medium text-white transition hover:bg-violet-700 disabled:cursor-not-allowed disabled:opacity-50"
                    >
                        Update
                    </button>
                </div>
            </div>

            <div className="border-t border-slate-200 pt-4 dark:border-slate-800">
                <div className="mb-2 flex items-center gap-2">
                    <Plus size={15} className="text-slate-400" />
                    <span className="text-xs font-medium uppercase tracking-wide text-slate-500 dark:text-slate-400">
                        Add child
                    </span>
                </div>

                <div className="flex gap-2">
                    <input
                        type="text"
                        value={childValue}
                        maxLength={MAX_VALUE_LENGTH}
                        disabled={deleted}
                        placeholder="Child value"
                        onChange={(event) => setChildValue(event.target.value)}
                        onKeyDown={(event) => {
                            if (event.key === 'Enter') {
                                createChild()
                            }
                        }}
                        className="min-w-0 flex-1 rounded-lg border border-slate-300 bg-white px-3 py-2 text-sm outline-none transition focus:border-emerald-500 focus:ring-2 focus:ring-emerald-500/20 disabled:cursor-not-allowed disabled:opacity-50 dark:border-slate-700 dark:bg-slate-950"
                    />

                    <button
                        type="button"
                        disabled={deleted || !childValue.trim()}
                        onClick={createChild}
                        className="flex items-center gap-2 rounded-lg bg-emerald-600 px-4 py-2 text-sm font-medium text-white transition hover:bg-emerald-700 disabled:cursor-not-allowed disabled:opacity-50"
                    >
                        <Plus size={15} />
                        Add
                    </button>
                </div>
            </div>

            <div className="mt-4 border-t border-slate-200 pt-4 dark:border-slate-800">
                <button
                    type="button"
                    disabled={deleted}
                    onClick={() => deleteNode(node.id)}
                    className="flex w-full items-center justify-center gap-2 rounded-lg border border-red-200 px-4 py-2 text-sm font-medium text-red-600 transition hover:bg-red-50 disabled:cursor-not-allowed disabled:opacity-50 dark:border-red-900 dark:text-red-400 dark:hover:bg-red-950/40"
                >
                    <Trash2 size={15} />
                    Delete node
                </button>
            </div>

            <p className="mt-3 text-xs text-slate-400 dark:text-slate-500">
                Changes remain local until Apply.
            </p>
        </div>
    )
}
