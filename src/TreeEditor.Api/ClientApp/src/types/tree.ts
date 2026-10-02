export interface TreeNode {
    id: string
    parentId: string | null
    value: string
    version: number
    hasChildren: boolean
}

export interface LoadedTreeNode extends TreeNode {
    ancestorIds: string[]
}

export type CachedNodeState =
    | 'unchanged'
    | 'added'
    | 'modified'
    | 'deleted'
    // Deletion has already been applied to the database; kept only for display.
    | 'removed'

export interface CachedTreeNode {
    id: string
    parentId: string | null
    ancestorIds: string[]

    value: string
    originalValue: string

    version: number
    state: CachedNodeState
}

export interface ApplyTreeChangesRequest {
    added: AddedTreeNode[]
    modified: ModifiedTreeNode[]
    deleted: DeletedTreeNode[]
}

export interface AddedTreeNode {
    id: string
    parentId: string
    value: string
}

export interface ModifiedTreeNode {
    id: string
    value: string
    version: number
}

export interface DeletedTreeNode {
    id: string
    version: number
}
export interface AppliedTreeNode {
    id: string
    version: number
}

export interface ApplyTreeChangesResponse {
    nodes: AppliedTreeNode[]
}
