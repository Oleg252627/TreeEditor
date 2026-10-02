import { Moon, Sun } from 'lucide-react'

interface ThemeToggleProps {
    isDark: boolean
    onToggle: () => void
}

export function ThemeToggle({ isDark, onToggle }: ThemeToggleProps) {
    const label = isDark ? 'Switch to light theme' : 'Switch to dark theme'

    return (
        <button
            type="button"
            onClick={onToggle}
            aria-label={label}
            title={label}
            className="relative flex h-9 w-16 items-center rounded-full bg-slate-200 p-1 transition-colors duration-200 dark:bg-slate-700"
        >
            <span
                className={`flex h-7 w-7 items-center justify-center rounded-full bg-white text-amber-500 shadow-sm transition-transform duration-200 dark:bg-slate-900 dark:text-slate-200 ${
                    isDark ? 'translate-x-7' : 'translate-x-0'
                }`}
            >
                {isDark ? <Moon size={16} /> : <Sun size={16} />}
            </span>
        </button>
    )
}
