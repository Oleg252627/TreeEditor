import type { CachedTreeNode } from '../types/tree'

export function getCachedParentId(
    node: CachedTreeNode,
    nodes: Map<string, CachedTreeNode>,
): string | null {
    return node.ancestorIds.find((id) => nodes.has(id)) ?? null
}

export function getMissingAncestorCount(
    node: CachedTreeNode,
    cachedParentId: string | null,
): number {
    if (cachedParentId === null) {
        return node.ancestorIds.length
    }

    return node.ancestorIds.indexOf(cachedParentId)
}
