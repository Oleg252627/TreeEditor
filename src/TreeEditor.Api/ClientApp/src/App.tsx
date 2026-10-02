import { useState } from 'react'
import { Database, LoaderCircle, RotateCcw } from 'lucide-react'
import { toast, Toaster } from 'sonner'
import { useTheme } from './hooks/useTheme'
import { ThemeToggle } from './components/ThemeToggle/ThemeToggle'
import { DBTreeView } from './components/DBTreeView/DBTreeView'
import { CachedTreeView } from './components/CachedTreeView/CachedTreeView'
import { resetTree } from './api/treeApi'
import { useTreeCacheStore } from './store/treeCacheStore'

function App() {
    const { isDark, toggleTheme } = useTheme()
    const [treeRevision, setTreeRevision] = useState(0)
    const [resetting, setResetting] = useState(false)
    const [reloadRequired, setReloadRequired] = useState(false)
    const clearCache = useTreeCacheStore((state) => state.clear)

    function refreshTree() {
        setTreeRevision((revision) => revision + 1)
        setReloadRequired(false)
    }

    function requireReload() {
        setReloadRequired(true)
    }

    async function handleReset() {
        if (resetting) {
            return
        }

        try {
            setResetting(true)

            await resetTree()

            clearCache()
            refreshTree()
            toast.success('Tree restored to the initial data')
        } catch (error) {
            console.error(error)
            toast.error('Failed to reset the tree')
        } finally {
            setResetting(false)
        }
    }

    return (
        <div className="min-h-screen bg-slate-50 text-slate-900 transition-colors duration-200 dark:bg-slate-950 dark:text-slate-100">
            <Toaster
                position="bottom-left"
                richColors
                closeButton
                theme={isDark ? 'dark' : 'light'}
            />

            <header className="sticky top-0 z-10 border-b border-slate-200 bg-white/80 backdrop-blur dark:border-slate-800 dark:bg-slate-950/80">
                <div className="mx-auto flex max-w-7xl items-center justify-between px-6 py-4">
                    <div className="flex items-center gap-3">
                        <div className="flex h-10 w-10 items-center justify-center rounded-xl bg-blue-600 text-white shadow-sm">
                            <Database size={20} />
                        </div>

                        <div>
                            <h1 className="text-lg font-semibold">Tree Editor</h1>
                            <p className="text-sm text-slate-500 dark:text-slate-400">
                                Database tree with local cached changes
                            </p>
                        </div>
                    </div>

                    <div className="flex items-center gap-3">
                        <ThemeToggle isDark={isDark} onToggle={toggleTheme} />

                        <button
                            type="button"
                            disabled={resetting}
                            onClick={handleReset}
                            className="flex items-center gap-2 rounded-lg border border-slate-300 bg-white px-3 py-2 text-sm font-medium transition hover:bg-slate-100 disabled:cursor-not-allowed disabled:opacity-50 dark:border-slate-700 dark:bg-slate-900 dark:hover:bg-slate-800"
                        >
                            {resetting ? (
                                <LoaderCircle size={16} className="animate-spin" />
                            ) : (
                                <RotateCcw size={16} />
                            )}
                            {resetting ? 'Resetting...' : 'Reset'}
                        </button>
                    </div>
                </div>
            </header>

            <main className="mx-auto max-w-7xl px-6 py-6">
                <div className="grid grid-cols-1 gap-6 lg:grid-cols-2 lg:items-stretch">
                    <DBTreeView
                        key={treeRevision}
                        reloadRequired={reloadRequired}
                        onReload={refreshTree}
                    />
                    <CachedTreeView
                        onApplied={refreshTree}
                        onConcurrencyConflict={requireReload}
                    />
                </div>
            </main>
        </div>
    )
}

export default App
