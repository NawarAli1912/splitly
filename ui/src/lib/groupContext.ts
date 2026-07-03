import type { InjectionKey, Ref } from 'vue'
import type {
  ExpenseResponse,
  GroupResponse,
  PaymentResponse,
  SettlementStrategy,
  TransferResponse,
} from './api'

export interface GroupContext {
  groupId: string
  group: Ref<GroupResponse | undefined>
  expenses: Ref<ExpenseResponse[]>
  payments: Ref<PaymentResponse[]>
  transfers: Ref<TransferResponse[]>
  strategy: Ref<SettlementStrategy>
  banker: Ref<string>
  me: Ref<string | null>
  refreshMoney(): Promise<void>
  copyInvite(): Promise<void>
  showToast(message: string): void
}

export const GroupCtxKey: InjectionKey<GroupContext> = Symbol('group-context')
