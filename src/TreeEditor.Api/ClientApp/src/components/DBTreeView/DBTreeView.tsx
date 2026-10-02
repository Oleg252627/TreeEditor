import { useEffect, useState } from 'react'
import { Database, Download, LoaderCircle, RefreshCw } from 'lucide-react'
import { toast } from 'sonner'
import { getNode, getRoots } from '../../api/treeApi'
import type { TreeNode } from '../../types/tree'
import { useTreeCacheStore } from '../../store/treeCacheStore'
import { DBTreeNode } from './DBTreeNode'

interface DBTreeViewProps {
    reloadRequired: boolean
    onReload: () => void
}

export function DBTreeView({ reloadRequired, onReload }: DBTreeViewProps) {
    const [nodes, setNodes] = useState<TreeNode[]>([])
    const [selectedId, setSelectedId] = useState<string | null>(null)
    const [loading, setLoading] = useState(true)
    const [loadingToCache, setLoadingToCache] = useState(false)
    const [error, setError] = useState<string | null>(null)
    const addLoadedNode = useTreeCacheStore((state) => state.addLoadedNode)

    useEffect(() => {
        const controller = new AbortController()

        async function loadRoots() {
            try {
                setLoading(true)
                setError(null)

                const roots = await getRoots(controller.signal)
                setNodes(roots)
            } catch (error) {
                if (error instanceof DOMException && error.name === 'AbortError') {
                    return
                }

                setError('Failed to load database tree.')
            } finally {
                setLoading(false)
            }
        }

        void loadRoots()

        return () => controller.abort()
    }, [])

    async function loadSelectedToCache() {
        if (!selectedId) {
            return
        }

        try {
            setLoadingToCache(true)
            setError(null)

            const node = await getNode(selectedId)
            addLoadedNode(node)
            toast.success('Node loaded into cache')
        } catch (error) {
            console.error(error)
            toast.error('Failed to load node into cache')
        } finally {
            setLoadingToCache(false)
        }
    }

    return (
        <section className="flex h-[calc(100vh-140px)] min-h-[500px] flex-col overflow-hidden rounded-2xl border border-slate-200 bg-white shadow-sm dark:border-slate-800 dark:bg-slate-900">
            <header className="flex shrink-0 items-center gap-3 border-b border-slate-200 px-5 py-4 dark:border-slate-800">
                <div className="flex h-9 w-9 items-center justify-center rounded-lg bg-blue-50 text-blue-600 dark:bg-blue-950 dark:text-blue-400">
                    <Database size={18} />
                </div>

                <div>
                    <h2 className="font-semibold">Database Tree</h2>
                    <p className="text-xs text-slate-500 dark:text-slate-400">
                        Expand nodes to load children
                    </p>
                </div>

                {reloadRequired && (
                    <button
                        type="button"
                        onClick={onReload}
                        title="Reload database tree"
                        aria-label="Reload database tree"
                        className="ml-auto flex h-9 w-9 items-center justify-center rounded-lg bg-amber-50 text-amber-600 transition hover:bg-amber-100 dark:bg-amber-950 dark:text-amber-400 dark:hover:bg-amber-900"
                    >
                        <RefreshCw size={17} />
                    </button>
                )}
            </header>

            <div className="min-h-0 flex-1 overflow-auto p-3">
                {loading && (
                    <div className="flex items-center justify-center gap-2 py-8 text-sm text-slate-500">
                        <LoaderCircle size={16} className="animate-spin" />
                        Loading tree...
                    </div>
                )}

                {error && (
                    <div className="rounded-lg bg-red-50 px-3 py-3 text-sm text-red-700 dark:bg-red-950/40 dark:text-red-300">
                        {error}
                    </div>
                )}

                {!loading && !error && (
                    <div className="min-w-max">
                        {nodes.map((node) => (
                            <DBTreeNode
                                key={node.id}
                                node={node}
                                depth={0}
                                selectedId={selectedId}
                                onSelect={(selectedNode) => setSelectedId(selectedNode.id)}
                            />
                        ))}
                    </div>
                )}
            </div>

            <footer className="flex shrink-0 items-center justify-between border-t border-slate-200 px-4 py-3 dark:border-slate-800">
                <span className="text-xs text-slate-500 dark:text-slate-400">
                    {selectedId ? 'Node selected' : 'Select a node'}
                </span>

                <button
                    type="button"
                    disabled={!selectedId || loadingToCache}
                    onClick={loadSelectedToCache}
                    className="flex items-center gap-2 rounded-lg bg-blue-600 px-3 py-2 text-sm font-medium text-white transition hover:bg-blue-700 disabled:cursor-not-allowed disabled:opacity-50"
                >
                    {loadingToCache ? (
                        <LoaderCircle size={16} className="animate-spin" />
                    ) : (
                        <Download size={16} />
                    )}
                    Load to Cache
                </button>
            </footer>
        </section>
    )
}
