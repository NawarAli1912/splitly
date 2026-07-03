export interface ParticipantResponse {
  id: string
  name: string
}

export interface GroupResponse {
  id: string
  name: string
  currency: string
  createdAtUtc: string
  participants: ParticipantResponse[]
}

export interface ExpenseResponse {
  id: string
  paidById: string
  amount: number
  description: string
  spentOn: string
  splitAmong: string[]
}

export interface PaginatedResponse<T> {
  items: T[]
  page: number
  pageSize: number
  totalCount: number
  hasNextPage: boolean
  hasPreviousPage: boolean
}

export interface PaymentResponse {
  id: string
  fromParticipantId: string
  toParticipantId: string
  amount: number
  paidOn: string
}

export interface RecordPaymentRequest {
  fromParticipantId: string
  toParticipantId: string
  amount: number
  paidOn: string
}

export interface TransferResponse {
  fromParticipantId: string
  toParticipantId: string
  amount: number
}

export interface SettlementResponse {
  transfers: TransferResponse[]
}

export interface AddExpenseRequest {
  paidById: string
  amount: number
  description: string
  spentOn: string
  splitAmong: string[]
}

export class ApiError extends Error {
  readonly status: number
  readonly title: string

  constructor(status: number, title: string, detail?: string) {
    super(detail ?? title)
    this.status = status
    this.title = title
  }
}

async function request<T>(method: string, path: string, body?: unknown): Promise<T> {
  const response = await fetch(`/api${path}`, {
    method,
    headers: body === undefined ? undefined : { 'Content-Type': 'application/json' },
    body: body === undefined ? undefined : JSON.stringify(body),
  })

  if (!response.ok) {
    const problem = await response.json().catch(() => ({}))
    const firstValidationError = problem.errors
      ? (Object.values(problem.errors).flat()[0] as string | undefined)
      : undefined
    throw new ApiError(
      response.status,
      problem.title ?? 'Something went wrong',
      firstValidationError ?? problem.detail,
    )
  }

  if (response.status === 204) return undefined as T
  return response.json()
}

export const api = {
  createGroup: (name: string, currency: string) =>
    request<GroupResponse>('POST', '/groups', { name, currency }),

  getGroup: (groupId: string) => request<GroupResponse>('GET', `/groups/${groupId}`),

  deleteGroup: (groupId: string) => request<void>('DELETE', `/groups/${groupId}`),

  addParticipant: (groupId: string, name: string) =>
    request<ParticipantResponse>('POST', `/groups/${groupId}/participants`, { name }),

  removeParticipant: (groupId: string, participantId: string) =>
    request<void>('DELETE', `/groups/${groupId}/participants/${participantId}`),

  addExpense: (groupId: string, expense: AddExpenseRequest) =>
    request<ExpenseResponse>('POST', `/groups/${groupId}/expenses`, expense),

  listExpenses: (groupId: string, page = 1, pageSize = 50) =>
    request<PaginatedResponse<ExpenseResponse>>(
      'GET',
      `/groups/${groupId}/expenses?page=${page}&pageSize=${pageSize}`,
    ),

  removeExpense: (groupId: string, expenseId: string) =>
    request<void>('DELETE', `/groups/${groupId}/expenses/${expenseId}`),

  recordPayment: (groupId: string, payment: RecordPaymentRequest) =>
    request<PaymentResponse>('POST', `/groups/${groupId}/payments`, payment),

  listPayments: (groupId: string, page = 1, pageSize = 50) =>
    request<PaginatedResponse<PaymentResponse>>(
      'GET',
      `/groups/${groupId}/payments?page=${page}&pageSize=${pageSize}`,
    ),

  removePayment: (groupId: string, paymentId: string) =>
    request<void>('DELETE', `/groups/${groupId}/payments/${paymentId}`),

  getSettlement: (groupId: string) =>
    request<SettlementResponse>('GET', `/groups/${groupId}/settlement`),
}
