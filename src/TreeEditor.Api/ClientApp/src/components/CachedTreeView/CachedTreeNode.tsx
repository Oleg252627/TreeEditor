import { File, Folder } from 'lucide-react'
import { isDeletedState, useTreeCacheStore } from '../../store/treeCacheStore'
import { getCachedParentId, getMissingAncestorCount } from '../../store/cachedHierarchy'
import type { CachedTreeNode } from '../../types/tree'

interface CachedTreeNodeProps {
    node: CachedTreeNode
    depth: number
}

export function CachedTreeNodeView({ node, depth }: CachedTreeNodeProps) {
    const nodes = useTreeCacheStore((state) => state.nodes)
    const selectedId = useTreeCacheStore((state) => state.selectedId)
    const selectNode = useTreeCacheStore((state) => state.selectNode)

    const children: CachedTreeNode[] = []

    for (const child of nodes.values()) {
        if (getCachedParentId(child, nodes) === node.id) {
            children.push(child)
        }
    }

    children.sort((a, b) => a.value.localeCompare(b.value))

    const selected = selectedId === node.id
    const deleted = isDeletedState(node.state)

    return (
        <div>
            <button
                type="button"
                onClick={() => selectNode(node.id)}
                className={`flex min-h-9 w-full min-w-max items-center rounded-lg pr-2 text-left transition-colors ${
                    selected
                        ? 'bg-violet-50 dark:bg-violet-950/60'
                        : 'hover:bg-slate-100 dark:hover:bg-slate-800'
                }`}
                style={{ paddingLeft: `${depth * 20 + 8}px` }}
            >
                <div
                    className={`mr-2 flex h-7 w-7 shrink-0 items-center justify-center ${
                        deleted
                            ? 'text-red-400 dark:text-red-500'
                            : selected
                                ? 'text-violet-600 dark:text-violet-400'
                                : 'text-slate-400 dark:text-slate-500'
                    }`}
                >
                    {children.length > 0 ? <Folder size={17} /> : <File size={17} />}
                </div>

                <span
                    className={`whitespace-nowrap text-sm ${
                        deleted
                            ? 'text-red-500 line-through dark:text-red-400'
                            : selected
                                ? 'text-violet-700 dark:text-violet-300'
                                : 'text-slate-700 dark:text-slate-200'
                    }`}
                >
                    {node.value}
                </span>

                {node.state !== 'unchanged' && (
                    <span
                        className={`ml-3 shrink-0 rounded-full px-2 py-0.5 text-[10px] font-medium ${
                            node.state === 'modified'
                                ? 'bg-amber-100 text-amber-700 dark:bg-amber-950 dark:text-amber-300'
                                : node.state === 'added'
                                    ? 'bg-emerald-100 text-emerald-700 dark:bg-emerald-950 dark:text-emerald-300'
                                    : node.state === 'removed'
                                        ? 'bg-slate-200 text-slate-600 dark:bg-slate-800 dark:text-slate-300'
                                        : 'bg-red-100 text-red-700 dark:bg-red-950 dark:text-red-300'
                        }`}
                    >
                        {node.state}
                    </span>
                )}
            </button>

            {children.map((child) => {
                const missing = getMissingAncestorCount(child, node.id)

                return (
                    <div key={child.id}>
                        {missing > 0 && (
                            <div
                                className="py-1 text-xs italic text-slate-400 dark:text-slate-500"
                                style={{ paddingLeft: `${(depth + 1) * 20 + 8}px` }}
                            >
                                {missing} uncached {missing === 1 ? 'ancestor' : 'ancestors'}
                            </div>
                        )}
                        <CachedTreeNodeView node={child} depth={depth + 1} />
                    </div>
                )
            })}
        </div>
    )
}
