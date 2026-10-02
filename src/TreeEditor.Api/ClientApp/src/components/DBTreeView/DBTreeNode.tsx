import { useState } from 'react'
import { ChevronRight, File, Folder, FolderOpen, LoaderCircle } from 'lucide-react'
import { getChildren } from '../../api/treeApi'
import type { TreeNode } from '../../types/tree'

interface DBTreeNodeProps {
    node: TreeNode
    depth: number
    selectedId: string | null
    onSelect: (node: TreeNode) => void
}

export function DBTreeNode({ node, depth, selectedId, onSelect }: DBTreeNodeProps) {
    const [expanded, setExpanded] = useState(false)
    const [children, setChildren] = useState<TreeNode[] | null>(null)
    const [loading, setLoading] = useState(false)
    const [error, setError] = useState(false)

    async function toggle() {
        if (!node.hasChildren) {
            return
        }

        if (expanded) {
            setExpanded(false)
            return
        }

        if (children !== null) {
            setExpanded(true)
            return
        }

        try {
            setLoading(true)
            setError(false)

            const result = await getChildren(node.id)

            setChildren(result)
            setExpanded(true)
        } catch {
            setError(true)
        } finally {
            setLoading(false)
        }
    }

    return (
        <div>
            <div
                className="group flex min-h-9 items-center rounded-lg pr-2 hover:bg-slate-100 dark:hover:bg-slate-800"
                style={{ paddingLeft: `${depth * 20 + 4}px` }}
            >
                <button
                    type="button"
                    onClick={toggle}
                    disabled={!node.hasChildren || loading}
                    className="flex h-7 w-7 shrink-0 items-center justify-center rounded-md text-slate-400 hover:bg-slate-200 disabled:cursor-default disabled:hover:bg-transparent dark:hover:bg-slate-700"
                >
                    {loading ? (
                        <LoaderCircle size={15} className="animate-spin" />
                    ) : node.hasChildren ? (
                        <ChevronRight
                            size={16}
                            className={`transition-transform ${expanded ? 'rotate-90' : ''}`}
                        />
                    ) : null}
                </button>

                <div className="mr-2 shrink-0 text-slate-400 dark:text-slate-500">
                    {node.hasChildren ? (
                        expanded ? <FolderOpen size={17} /> : <Folder size={17} />
                    ) : (
                        <File size={17} />
                    )}
                </div>

                <button
                    type="button"
                    onClick={() => onSelect(node)}
                    className={`flex min-w-0 flex-1 items-center rounded-md px-2 py-1 text-left transition-colors ${
                        selectedId === node.id
                            ? 'bg-blue-50 text-blue-700 dark:bg-blue-950 dark:text-blue-300'
                            : 'text-slate-700 dark:text-slate-200'
                    }`}
                >
                    <span className="whitespace-nowrap text-sm">{node.value}</span>
                </button>
            </div>

            {error && (
                <div
                    className="py-1 text-xs text-red-500"
                    style={{ paddingLeft: `${(depth + 1) * 20 + 32}px` }}
                >
                    Failed to load children
                </div>
            )}

            {expanded && children?.map((child) => (
                <DBTreeNode
                    key={child.id}
                    node={child}
                    depth={depth + 1}
                    selectedId={selectedId}
                    onSelect={onSelect}
                />
            ))}
        </div>
    )
}
