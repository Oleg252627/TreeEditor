import { useEffect, useState } from 'react'

export type Theme = 'light' | 'dark'

const storageKey = 'tree-editor-theme'

function getInitialTheme(): Theme {
    const stored = localStorage.getItem(storageKey)

    if (stored === 'light' || stored === 'dark') {
        return stored
    }

    return window.matchMedia(
        '(prefers-color-scheme: dark)',
    ).matches
        ? 'dark'
        : 'light'
}

export function useTheme() {
    const [theme, setTheme] = useState<Theme>(
        getInitialTheme,
    )

    useEffect(() => {
        document.documentElement.classList.toggle(
            'dark',
            theme === 'dark',
        )
    }, [theme])

    const changeTheme = (value: Theme) => {
        localStorage.setItem(storageKey, value)
        setTheme(value)
    }

    const toggleTheme = () => {
        changeTheme(
            theme === 'dark'
                ? 'light'
                : 'dark',
        )
    }

    return {
        theme,
        isDark: theme === 'dark',
        setTheme: changeTheme,
        toggleTheme,
    }
}