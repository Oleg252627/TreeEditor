import { beforeEach, describe, expect, it } from 'vitest'
import { useTreeCacheStore } from './treeCacheStore'
import { getCachedParentId, getMissingAncestorCount } from './cachedHierarchy'
import type { LoadedTreeNode } from '../types/tree'

const rootId = '10000000-0000-0000-0000-000000000001'
const childId = '20000000-0000-0000-0000-000000000001'
const grandchildId = '30000000-0000-0000-0000-000000000001'

function loadedNode(
    id: string,
    parentId: string | null,
    value: string,
    ancestorIds: string[] = parentId ? [parentId] : [],
): LoadedTreeNode {
    return { id, parentId, ancestorIds, value, version: 1, hasChildren: false }
}

describe('tree cache', () => {
    beforeEach(() => useTreeCacheStore.getState().clear())

    it('links separately loaded parent and child regardless of load order', () => {
        const store = useTreeCacheStore.getState()
        store.addLoadedNode(loadedNode(childId, rootId, 'Child'))
        store.addLoadedNode(loadedNode(rootId, null, 'Root'))

        expect(useTreeCacheStore.getState().nodes.get(childId)?.parentId).toBe(rootId)
        expect(useTreeCacheStore.getState().nodes.size).toBe(2)
        expect(store.hasChanges()).toBe(false)
    })

    it.each(['ancestor first', 'descendant first'])(
        'links nodes across an uncached intermediate parent (%s)',
        (order) => {
            const store = useTreeCacheStore.getState()
            const root = loadedNode(rootId, null, 'Company')
            const descendant = loadedNode(
                grandchildId, childId, 'Backend', [childId, rootId],
            )

            if (order === 'ancestor first') {
                store.addLoadedNode(root)
                store.addLoadedNode(descendant)
            } else {
                store.addLoadedNode(descendant)
                store.addLoadedNode(root)
            }

            const nodes = useTreeCacheStore.getState().nodes
            expect(nodes.size).toBe(2)
            expect(getCachedParentId(nodes.get(grandchildId)!, nodes)).toBe(rootId)
            expect(getCachedParentId(nodes.get(rootId)!, nodes)).toBeNull()
            expect(getMissingAncestorCount(nodes.get(grandchildId)!, rootId)).toBe(1)
            expect(nodes.get(grandchildId)?.parentId).toBe(childId)

            store.addLoadedNode(loadedNode(childId, rootId, 'Engineering', [rootId]))
            const updated = useTreeCacheStore.getState().nodes
            expect(getCachedParentId(updated.get(grandchildId)!, updated)).toBe(childId)
            expect(getMissingAncestorCount(updated.get(grandchildId)!, childId)).toBe(0)
            expect(store.hasChanges()).toBe(false)
        },
    )

    it('places a newly added child below its cached parent across an uncached gap', () => {
        const store = useTreeCacheStore.getState()
        store.addLoadedNode(loadedNode(
            grandchildId, childId, 'Backend', [childId, rootId],
        ))
        store.addLoadedNode(loadedNode(rootId, null, 'Company'))
        store.addChild(grandchildId, 'New child')

        const nodes = useTreeCacheStore.getState().nodes
        const newChild = nodes.get(useTreeCacheStore.getState().selectedId!)!
        expect(getCachedParentId(newChild, nodes)).toBe(grandchildId)
        expect(newChild.ancestorIds).toEqual([grandchildId, childId, rootId])
        expect(store.buildChangeSet().added[0].parentId).toBe(grandchildId)
    })

    it('marks separately loaded descendants deleted across an uncached gap', () => {
        const store = useTreeCacheStore.getState()
        store.addLoadedNode(loadedNode(rootId, null, 'Company'))
        store.addLoadedNode(loadedNode(
            grandchildId, childId, 'Backend', [childId, rootId],
        ))
        store.updateValue(grandchildId, 'Edited')
        store.addChild(grandchildId, 'Pending child')
        const pendingChildId = useTreeCacheStore.getState().selectedId!
        store.deleteNode(rootId)
        store.updateValue(grandchildId, 'Cannot edit')
        store.addChild(grandchildId, 'Cannot add')

        const state = useTreeCacheStore.getState()
        expect(state.nodes.get(grandchildId)).toMatchObject({
            value: 'Edited',
            state: 'deleted',
        })
        expect(state.nodes.has(pendingChildId)).toBe(false)
        expect(state.selectedId).toBeNull()
        expect(state.nodes.size).toBe(2)
        expect(store.buildChangeSet().added).toEqual([])
        expect(store.buildChangeSet().modified).toEqual([])
        expect(store.buildChangeSet().deleted).toEqual([
            { id: rootId, version: 1 },
            { id: grandchildId, version: 1 },
        ])
    })

    it('marks a descendant loaded after its ancestor was deleted', () => {
        const store = useTreeCacheStore.getState()
        store.addLoadedNode(loadedNode(rootId, null, 'Company'))
        store.deleteNode(rootId)
        store.addLoadedNode(loadedNode(
            grandchildId, childId, 'Backend', [childId, rootId],
        ))
        store.updateValue(grandchildId, 'Cannot edit')
        store.addChild(grandchildId, 'Cannot add')

        const state = useTreeCacheStore.getState()
        expect(state.nodes.get(grandchildId)).toMatchObject({
            value: 'Backend',
            state: 'deleted',
        })
        expect(state.nodes.size).toBe(2)
        expect(store.buildChangeSet().added).toEqual([])
        expect(store.buildChangeSet().modified).toEqual([])
    })

    it('does not overwrite pending edits when the same node is loaded again', () => {
        const store = useTreeCacheStore.getState()
        store.addLoadedNode(loadedNode(rootId, null, 'Original'))
        store.updateValue(rootId, 'Edited')
        store.addLoadedNode(loadedNode(rootId, null, 'Original'))

        expect(useTreeCacheStore.getState().nodes.get(rootId)?.value).toBe('Edited')
        expect(store.buildChangeSet().modified).toEqual([
            { id: rootId, value: 'Edited', version: 1 },
        ])
    })

    it('keeps edits local and removes a modification when the value is restored', () => {
        const store = useTreeCacheStore.getState()
        store.addLoadedNode(loadedNode(rootId, null, 'Original'))
        store.updateValue(rootId, '  Edited  ')

        expect(store.hasChanges()).toBe(true)
        expect(store.buildChangeSet().modified).toEqual([
            { id: rootId, value: 'Edited', version: 1 },
        ])

        store.updateValue(rootId, 'Original')
        expect(store.hasChanges()).toBe(false)
        expect(store.buildChangeSet().modified).toEqual([])
    })

    it('adds a child of a newly added node and cancels the whole subtree on deletion', () => {
        const store = useTreeCacheStore.getState()
        store.addLoadedNode(loadedNode(rootId, null, 'Root'))
        store.addChild(rootId, 'New parent')
        const newParentId = useTreeCacheStore.getState().selectedId!
        store.addChild(newParentId, 'New child')
        const newChildId = useTreeCacheStore.getState().selectedId!

        expect(store.buildChangeSet().added).toEqual([
            { id: newParentId, parentId: rootId, value: 'New parent' },
            { id: newChildId, parentId: newParentId, value: 'New child' },
        ])

        store.deleteNode(newParentId)
        expect(useTreeCacheStore.getState().nodes.has(newChildId)).toBe(false)
        expect(store.buildChangeSet().added).toEqual([])
        expect(store.hasChanges()).toBe(false)
    })

    it('marks loaded descendants deleted and prevents editing or adding under them', () => {
        const store = useTreeCacheStore.getState()
        store.addLoadedNode(loadedNode(rootId, null, 'Root'))
        store.addLoadedNode(loadedNode(childId, rootId, 'Child'))
        store.addLoadedNode(loadedNode(
            grandchildId, childId, 'Grandchild', [childId, rootId],
        ))
        store.addChild(childId, 'Unsaved child')
        const newChildId = useTreeCacheStore.getState().selectedId!

        store.deleteNode(rootId)
        store.updateValue(childId, 'Changed after deletion')
        store.addChild(grandchildId, 'Invalid child')

        const state = useTreeCacheStore.getState()
        expect(state.nodes.has(newChildId)).toBe(false)
        expect(state.nodes.get(childId)).toMatchObject({
            value: 'Child',
            state: 'deleted',
        })
        expect(state.nodes.get(grandchildId)?.state).toBe('deleted')
        expect(store.buildChangeSet()).toEqual({
            added: [],
            modified: [],
            deleted: [
                { id: rootId, version: 1 },
                { id: childId, version: 1 },
                { id: grandchildId, version: 1 },
            ],
        })
    })

    it('clears pending changes and selection without touching loaded data elsewhere', () => {
        const store = useTreeCacheStore.getState()
        store.addLoadedNode(loadedNode(rootId, null, 'Root'))
        store.selectNode(rootId)
        store.updateValue(rootId, 'Changed')
        store.clear()

        expect(useTreeCacheStore.getState().nodes.size).toBe(0)
        expect(useTreeCacheStore.getState().selectedId).toBeNull()
        expect(store.hasChanges()).toBe(false)
    })

    it('keeps the cache after Apply with new versions and applied deletions marked as removed', () => {
        const store = useTreeCacheStore.getState()
        store.addLoadedNode(loadedNode(rootId, null, 'Root'))
        store.addLoadedNode(loadedNode(childId, rootId, 'Child'))
        store.addLoadedNode(loadedNode(grandchildId, childId, 'Grandchild', [childId, rootId]))

        store.updateValue(rootId, 'Renamed root')
        store.addChild(rootId, 'New child')
        const addedId = useTreeCacheStore.getState().selectedId!
        store.deleteNode(childId)

        const changeSet = useTreeCacheStore.getState().buildChangeSet()
        store.markApplied(changeSet, {
            nodes: [{ id: rootId, version: 7 }, { id: addedId, version: 8 }],
        })

        const nodes = useTreeCacheStore.getState().nodes
        expect(nodes.get(rootId)).toMatchObject({
            state: 'unchanged', value: 'Renamed root', originalValue: 'Renamed root', version: 7,
        })
        expect(nodes.get(addedId)).toMatchObject({ state: 'unchanged', version: 8 })
        expect(nodes.get(childId)?.state).toBe('removed')
        expect(nodes.get(grandchildId)?.state).toBe('removed')
        expect(useTreeCacheStore.getState().hasChanges()).toBe(false)
        expect(useTreeCacheStore.getState().buildChangeSet()).toEqual({ added: [], modified: [], deleted: [] })

        store.updateValue(childId, 'Edited')
        store.addChild(childId, 'Child of removed')
        store.deleteNode(rootId)
        expect(useTreeCacheStore.getState().nodes.get(childId)?.state).toBe('removed')
        expect(useTreeCacheStore.getState().buildChangeSet().deleted).toEqual(expect.arrayContaining([
            { id: rootId, version: 7 },
            { id: addedId, version: 8 },
        ]))
        expect(useTreeCacheStore.getState().buildChangeSet().deleted).toHaveLength(2)
    })

    it('keeps edits made while Apply was in flight as pending', () => {
        const store = useTreeCacheStore.getState()
        store.addLoadedNode(loadedNode(rootId, null, 'Root'))
        store.updateValue(rootId, 'First')

        const changeSet = useTreeCacheStore.getState().buildChangeSet()
        store.updateValue(rootId, 'Second')
        store.markApplied(changeSet, { nodes: [{ id: rootId, version: 5 }] })

        expect(useTreeCacheStore.getState().nodes.get(rootId)).toMatchObject({
            state: 'modified', value: 'Second', originalValue: 'First', version: 5,
        })
    })
})
