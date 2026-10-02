import type {
    ApplyTreeChangesRequest,
    ApplyTreeChangesResponse,
    LoadedTreeNode,
    TreeNode,
} from '../types/tree'

const baseUrl = '/api/tree'

export interface ProblemDetails {
    type?: string
    title?: string
    status?: number
    detail?: string
    instance?: string
}

export class ApiError extends Error {
    readonly status: number
    readonly problem?: ProblemDetails

    constructor(
        status: number,
        problem?: ProblemDetails,
    ) {
        super(
            problem?.detail ??
            problem?.title ??
            `Request failed with status ${status}`,
        )

        this.name = 'ApiError'
        this.status = status
        this.problem = problem
    }
}

async function ensureSuccess(
    response: Response,
): Promise<void> {
    if (response.ok) {
        return
    }

    let problem: ProblemDetails | undefined

    const contentType =
        response.headers.get('content-type')

    if (contentType?.includes('application/problem+json') ||
        contentType?.includes('application/json')) {
        try {
            problem =
                await response.json() as ProblemDetails
        } catch {
            // Ignore malformed or empty error response.
        }
    }

    throw new ApiError(
        response.status,
        problem,
    )
}

async function get<T>(
    url: string,
    signal?: AbortSignal,
): Promise<T> {
    const response = await fetch(
        url,
        {
            signal,
        },
    )

    await ensureSuccess(response)

    return await response.json() as T
}

export function getRoots(
    signal?: AbortSignal,
): Promise<TreeNode[]> {
    return get<TreeNode[]>(
        `${baseUrl}/roots`,
        signal,
    )
}

export function getChildren(
    parentId: string,
    signal?: AbortSignal,
): Promise<TreeNode[]> {
    return get<TreeNode[]>(
        `${baseUrl}/${parentId}/children`,
        signal,
    )
}

export function getNode(
    id: string,
    signal?: AbortSignal,
): Promise<LoadedTreeNode> {
    return get<LoadedTreeNode>(
        `${baseUrl}/${id}`,
        signal,
    )
}

export async function applyChanges(
    request: ApplyTreeChangesRequest,
): Promise<ApplyTreeChangesResponse> {
    const response = await fetch(
        `${baseUrl}/apply`,
        {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
            },
            body: JSON.stringify(request),
        },
    )

    await ensureSuccess(response)

    return await response.json() as ApplyTreeChangesResponse
}

export async function resetTree(): Promise<void> {
    const response = await fetch(
        `${baseUrl}/reset`,
        {
            method: 'POST',
        },
    )

    await ensureSuccess(response)
}