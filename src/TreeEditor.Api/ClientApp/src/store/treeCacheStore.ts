import { create } from 'zustand'
import type {
    ApplyTreeChangesRequest,
    ApplyTreeChangesResponse,
    CachedNodeState,
    CachedTreeNode,
    LoadedTreeNode,
} from '../types/tree'

interface TreeCacheState {
    nodes: Map<string, CachedTreeNode>
    selectedId: string | null

    addLoadedNode: (node: LoadedTreeNode) => void
    addChild: (parentId: string, value: string) => void
    selectNode: (id: string) => void
    updateValue: (id: string, value: string) => void
    deleteNode: (id: string) => void
    clear: () => void
    markApplied: (changeSet: ApplyTreeChangesRequest, response: ApplyTreeChangesResponse) => void

    hasChanges: () => boolean
    buildChangeSet: () => ApplyTreeChangesRequest
}

export const useTreeCacheStore =
    create<TreeCacheState>((set, get) => ({
        nodes: new Map(),
        selectedId: null,

        addLoadedNode: (node) => {
            set((state) => {
                if (state.nodes.has(node.id)) {
                    return state
                }

                const nodes = new Map(state.nodes)

                nodes.set(node.id, {
                    id: node.id,
                    parentId: node.parentId,
                    ancestorIds: node.ancestorIds,
                    value: node.value,
                    originalValue: node.value,
                    version: node.version,
                    state: node.ancestorIds.some(
                        (id) => isDeletedState(nodes.get(id)?.state),
                    ) ? 'deleted' : 'unchanged',
                })

                return { nodes }
            })
        },

        addChild: (parentId, value) => {
            set((state) => {
                const parent = state.nodes.get(parentId)

                if (!parent || isDeletedState(parent.state)) {
                    return state
                }

                const normalizedValue = value.trim()

                if (!normalizedValue) {
                    return state
                }

                const id = crypto.randomUUID()
                const nodes = new Map(state.nodes)

                nodes.set(id, {
                    id,
                    parentId,
                    ancestorIds: [parentId, ...parent.ancestorIds],
                    value: normalizedValue,
                    originalValue: normalizedValue,
                    version: 0,
                    state: 'added',
                })

                return {
                    nodes,
                    selectedId: id,
                }
            })
        },

        selectNode: (id) => {
            set({
                selectedId: id,
            })
        },

        updateValue: (id, value) => {
            set((state) => {
                const node = state.nodes.get(id)

                if (!node || isDeletedState(node.state)) {
                    return state
                }

                const normalizedValue = value.trim()

                if (!normalizedValue) {
                    return state
                }

                const nodes = new Map(state.nodes)

                const newState =
                    node.state === 'added'
                        ? 'added'
                        : normalizedValue === node.originalValue
                            ? 'unchanged'
                            : 'modified'

                nodes.set(id, {
                    ...node,
                    value: normalizedValue,
                    state: newState,
                })

                return { nodes }
            })
        },

        deleteNode: (id) => {
            set((state) => {
                const node = state.nodes.get(id)

                if (!node || isDeletedState(node.state)) {
                    return state
                }

                const nodes = new Map(state.nodes)

                if (node.state === 'added') {
                    removeAddedSubtree(nodes, id)
                } else {
                    markSubtreeDeleted(nodes, id)
                }

                return {
                    nodes,
                    selectedId: null,
                }
            })
        },

        clear: () => {
            set({
                nodes: new Map(),
                selectedId: null,
            })
        },

        markApplied: (changeSet, response) => {
            set((state) => {
                const versions = new Map(
                    response.nodes.map((node) => [node.id, node.version]),
                )
                const sentValues = new Map<string, string>()

                for (const node of changeSet.added) {
                    sentValues.set(node.id, node.value)
                }

                for (const node of changeSet.modified) {
                    sentValues.set(node.id, node.value)
                }

                const appliedDeletedIds = new Set(
                    changeSet.deleted.map((node) => node.id),
                )
                const nodes = new Map(state.nodes)

                for (const node of state.nodes.values()) {
                    if (node.state === 'deleted' && appliedDeletedIds.has(node.id)) {
                        nodes.set(node.id, { ...node, state: 'removed' })
                        continue
                    }

                    const sentValue = sentValues.get(node.id)
                    const version = versions.get(node.id)

                    if (sentValue === undefined || version === undefined || isDeletedState(node.state)) {
                        continue
                    }

                    // The value may have been edited again while Apply was in flight.
                    nodes.set(node.id, {
                        ...node,
                        originalValue: sentValue,
                        version,
                        state: node.value === sentValue ? 'unchanged' : 'modified',
                    })
                }

                return { nodes }
            })
        },

        hasChanges: () => {
            const nodes = get().nodes

            for (const node of nodes.values()) {
                if (isPendingState(node.state)) {
                    return true
                }
            }

            return false
        },

        buildChangeSet: () => {
            const nodes = get().nodes

            const changeSet: ApplyTreeChangesRequest = {
                added: [],
                modified: [],
                deleted: [],
            }

            for (const node of nodes.values()) {
                switch (node.state) {
                    case 'added': {
                        if (!node.parentId) {
                            throw new Error(
                                `Added node '${node.id}' must have a parent.`,
                            )
                        }

                        changeSet.added.push({
                            id: node.id,
                            parentId: node.parentId,
                            value: node.value,
                        })

                        break
                    }

                    case 'modified': {
                        changeSet.modified.push({
                            id: node.id,
                            value: node.value,
                            version: node.version,
                        })

                        break
                    }

                    case 'deleted': {
                        changeSet.deleted.push({
                            id: node.id,
                            version: node.version,
                        })

                        break
                    }
                }
            }

            return changeSet
        },
    }))

function removeAddedSubtree(
    nodes: Map<string, CachedTreeNode>,
    rootId: string,
) {
    const idsToRemove = collectCachedSubtree(
        nodes,
        rootId,
    )

    for (const id of idsToRemove) {
        nodes.delete(id)
    }
}

function markSubtreeDeleted(
    nodes: Map<string, CachedTreeNode>,
    rootId: string,
) {
    const idsToDelete = collectCachedSubtree(
        nodes,
        rootId,
    )

    for (const id of idsToDelete) {
        const node = nodes.get(id)

        if (!node) {
            continue
        }

        if (node.state === 'added') {
            nodes.delete(id)
            continue
        }

        if (node.state === 'removed') {
            continue
        }

        nodes.set(id, {
            ...node,
            state: 'deleted',
        })
    }
}

function collectCachedSubtree(
    nodes: Map<string, CachedTreeNode>,
    rootId: string,
): Set<string> {
    const result = new Set([rootId])

    for (const node of nodes.values()) {
        if (node.ancestorIds.includes(rootId)) {
            result.add(node.id)
        }
    }

    return result
}
export function isDeletedState(state: CachedNodeState | undefined): boolean {
    return state === 'deleted' || state === 'removed'
}

export function isPendingState(state: CachedNodeState): boolean {
    return state !== 'unchanged' && state !== 'removed'
}
