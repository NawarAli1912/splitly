<script setup lang="ts">
import { computed, inject, ref } from 'vue'
import { api, ApiError, type SettlementStrategy, type TransferResponse } from '../lib/api'
import { SKIPPED } from '../lib/identity'
import { avatarStyle, formatMoney, initials } from '../lib/format'
import { GroupCtxKey } from '../lib/groupContext'

const ctx = inject(GroupCtxKey)!
const { group, expenses, transfers, strategy, banker, me } = ctx

const strategyOptions: { value: SettlementStrategy; label: string }[] = [
  { value: 'minimum-transfers', label: 'Fewest transfers' },
  { value: 'direct-payback', label: 'Pay back directly' },
  { value: 'via-banker', label: 'Through one person' },
]

function pickStrategy(value: SettlementStrategy) {
  if (value === 'via-banker' && banker.value === '') {
    banker.value = hasIdentity.value ? me.value! : (group.value?.participants[0]?.id ?? '')
  }
  strategy.value = value
}

const today = new Date().toISOString().slice(0, 10)
const expensePaidBy = ref('')
const expenseAmount = ref('')
const expenseDescription = ref('')
const expenseDate = ref(today)
const expenseSplitAmong = ref<string[]>([])
const expenseError = ref('')

const names = computed(() => new Map(group.value?.participants.map((p) => [p.id, p.name])))
const hasIdentity = computed(() => me.value !== null && me.value !== SKIPPED)
const justMe = computed(() => hasIdentity.value && (group.value?.participants.length ?? 0) === 1)

const iPay = computed(() => transfers.value.filter((t) => t.fromParticipantId === me.value))
const paysMe = computed(() => transfers.value.filter((t) => t.toParticipantId === me.value))
const myNet = computed(
  () =>
    paysMe.value.reduce((sum, t) => sum + t.amount, 0) -
    iPay.value.reduce((sum, t) => sum + t.amount, 0),
)

resetExpenseDefaults()

function nameOf(id: string): string {
  return names.value.get(id) ?? '—'
}

function involvesMe(transfer: TransferResponse): boolean {
  return transfer.fromParticipantId === me.value || transfer.toParticipantId === me.value
}

function transferKey(transfer: TransferResponse): string {
  return `${transfer.fromParticipantId}-${transfer.toParticipantId}`
}

function resetExpenseDefaults() {
  const participants = group.value?.participants ?? []
  expenseSplitAmong.value = participants.map((p) => p.id)
  const payerStillHere = participants.some((p) => p.id === expensePaidBy.value)
  if (!payerStillHere) {
    expensePaidBy.value = hasIdentity.value ? me.value! : (participants[0]?.id ?? '')
  }
}

const markingPaid = ref('')

async function markPaid(transfer: TransferResponse) {
  if (markingPaid.value) return
  markingPaid.value = transferKey(transfer)
  try {
    await api.recordPayment(ctx.groupId, {
      fromParticipantId: transfer.fromParticipantId,
      toParticipantId: transfer.toParticipantId,
      amount: transfer.amount,
      paidOn: today,
    })
    await ctx.refreshMoney()
    ctx.showToast(
      `${nameOf(transfer.fromParticipantId)} paid ${nameOf(transfer.toParticipantId)} — recorded`,
    )
  } catch (e) {
    ctx.showToast(e instanceof ApiError ? e.message : 'Could not reach the server')
  } finally {
    markingPaid.value = ''
  }
}

async function addExpense() {
  expenseError.value = ''
  try {
    await api.addExpense(ctx.groupId, {
      paidById: expensePaidBy.value,
      amount: Number(expenseAmount.value),
      description: expenseDescription.value.trim(),
      spentOn: expenseDate.value,
      splitAmong: expenseSplitAmong.value,
    })
    expenseAmount.value = ''
    expenseDescription.value = ''
    expenseDate.value = today
    resetExpenseDefaults()
    await ctx.refreshMoney()
  } catch (e) {
    expenseError.value = e instanceof ApiError ? e.message : 'Could not reach the server'
  }
}
</script>

<template>
  <div v-if="group">
    <!-- Your position -->
    <section v-if="hasIdentity && expenses.length > 0" class="glass mt-6 p-6">
      <p class="text-[13px] font-medium text-ink-secondary">Your position</p>
      <Transition name="tab" mode="out-in">
        <p
          :key="myNet"
          class="mt-1 text-3xl font-semibold tracking-tight tabular-nums"
          :class="myNet < 0 ? 'text-negative' : 'text-positive'"
        >
          <template v-if="myNet < 0">You owe {{ formatMoney(-myNet, group.currency) }}</template>
          <template v-else-if="myNet > 0">
            You get back {{ formatMoney(myNet, group.currency) }}
          </template>
          <template v-else>You're all settled</template>
        </p>
      </Transition>
      <ul v-if="iPay.length || paysMe.length" class="mt-3 space-y-1">
        <li v-for="(t, i) in iPay" :key="`out-${i}`" class="text-[14px] text-ink-secondary">
          You pay <span class="font-medium text-ink">{{ nameOf(t.toParticipantId) }}</span>
          {{ formatMoney(t.amount, group.currency) }}
        </li>
        <li v-for="(t, i) in paysMe" :key="`in-${i}`" class="text-[14px] text-ink-secondary">
          <span class="font-medium text-ink">{{ nameOf(t.fromParticipantId) }}</span> pays you
          {{ formatMoney(t.amount, group.currency) }}
        </li>
      </ul>
    </section>

    <!-- Invite empty state -->
    <section v-if="justMe" class="glass mt-6 p-8 text-center">
      <h2 class="text-xl font-semibold tracking-tight">It's just you here</h2>
      <p class="mx-auto mt-2 max-w-sm text-[14px] text-ink-secondary">
        Share the link — whoever opens it joins the group. No accounts needed.
      </p>
      <button
        class="mt-5 rounded-full bg-accent px-6 py-2.5 text-[14px] font-medium text-white transition duration-200 hover:bg-accent-hover active:scale-95"
        @click="ctx.copyInvite"
      >
        Copy invite link
      </button>
    </section>

    <!-- Add expense -->
    <section v-if="group.participants.length > 0" class="glass mt-6 p-6">
      <h2 class="text-lg font-semibold tracking-tight">Add expense</h2>

      <form class="mt-4 space-y-3" @submit.prevent="addExpense">
        <div class="flex gap-2">
          <input
            v-model="expenseDescription"
            required
            placeholder="What was it?"
            class="min-w-0 flex-1 rounded-xl border border-line/70 bg-white/70 px-4 py-2.5 text-[15px] outline-none transition duration-200 placeholder:text-ink-secondary/50 focus:border-accent focus:ring-4 focus:ring-accent/15"
          />
          <input
            v-model="expenseAmount"
            required
            type="number"
            step="0.01"
            min="0.01"
            placeholder="0.00"
            class="w-28 rounded-xl border border-line/70 bg-white/70 px-4 py-2.5 text-[15px] outline-none transition duration-200 placeholder:text-ink-secondary/50 focus:border-accent focus:ring-4 focus:ring-accent/15"
          />
        </div>

        <div class="grid grid-cols-2 gap-3">
          <select
            v-model="expensePaidBy"
            class="appearance-none rounded-xl border border-line/70 bg-white/70 px-4 py-2 text-[14px] outline-none transition duration-200 focus:border-accent focus:ring-4 focus:ring-accent/15"
          >
            <option v-for="p in group.participants" :key="p.id" :value="p.id">
              Paid by {{ p.id === me ? 'you' : p.name }}
            </option>
          </select>
          <input
            v-model="expenseDate"
            required
            type="date"
            class="rounded-xl border border-line/70 bg-white/70 px-4 py-2 text-[14px] outline-none transition duration-200 focus:border-accent focus:ring-4 focus:ring-accent/15"
          />
        </div>

        <fieldset>
          <legend class="text-[13px] font-medium text-ink-secondary">Split among</legend>
          <div class="mt-2 flex flex-wrap gap-2">
            <label
              v-for="p in group.participants"
              :key="p.id"
              class="cursor-pointer rounded-full border px-3.5 py-1.5 text-[13px] transition duration-200"
              :class="
                expenseSplitAmong.includes(p.id)
                  ? 'border-accent bg-accent/10 text-accent'
                  : 'border-line/70 bg-white/40 text-ink-secondary hover:border-ink-secondary/50'
              "
            >
              <input v-model="expenseSplitAmong" type="checkbox" :value="p.id" class="sr-only" />
              {{ p.id === me ? 'You' : p.name }}
            </label>
          </div>
        </fieldset>

        <button
          type="submit"
          class="w-full rounded-full bg-accent py-2.5 text-[14px] font-medium text-white transition duration-200 hover:bg-accent-hover active:scale-[0.98]"
        >
          Add
        </button>
      </form>
      <p v-if="expenseError" class="mt-3 text-[13px] text-negative">{{ expenseError }}</p>
    </section>

    <!-- Settlement -->
    <section v-if="expenses.length > 0" class="glass mt-6 p-6">
      <h2 class="text-lg font-semibold tracking-tight">Who pays whom</h2>

      <div class="mt-3 flex flex-wrap items-center gap-2">
        <div class="flex rounded-full border border-line/70 bg-white/40 p-0.5">
          <button
            v-for="option in strategyOptions"
            :key="option.value"
            type="button"
            class="rounded-full px-3 py-1 text-[12px] font-medium transition duration-200"
            :class="
              strategy === option.value
                ? 'bg-accent text-white'
                : 'text-ink-secondary hover:text-ink'
            "
            @click="pickStrategy(option.value)"
          >
            {{ option.label }}
          </button>
        </div>
        <select
          v-if="strategy === 'via-banker'"
          v-model="banker"
          class="appearance-none rounded-full border border-line/70 bg-white/70 px-3 py-1 text-[12px] outline-none transition duration-200 focus:border-accent"
        >
          <option v-for="p in group.participants" :key="p.id" :value="p.id">
            via {{ p.id === me ? 'you' : p.name }}
          </option>
        </select>
      </div>

      <div v-if="transfers.length === 0" class="mt-3 py-6 text-center">
        <p class="text-3xl">🎉</p>
        <p class="mt-2 text-[15px] font-medium">All settled</p>
        <p class="mt-1 text-[13px] text-ink-secondary">Nobody owes anything — enjoy.</p>
      </div>

      <TransitionGroup v-else tag="ul" name="list" class="mt-2 divide-y divide-line/40">
        <li
          v-for="transfer in transfers"
          :key="transferKey(transfer)"
          class="flex items-center gap-3 py-3"
          :class="{ 'opacity-45': hasIdentity && !involvesMe(transfer) }"
        >
          <span
            class="flex size-8 items-center justify-center rounded-full text-[12px] font-semibold"
            :style="avatarStyle(transfer.fromParticipantId)"
          >
            {{ initials(nameOf(transfer.fromParticipantId)) }}
          </span>
          <span class="text-[15px]">{{ nameOf(transfer.fromParticipantId) }}</span>
          <span class="text-ink-secondary">→</span>
          <span
            class="flex size-8 items-center justify-center rounded-full text-[12px] font-semibold"
            :style="avatarStyle(transfer.toParticipantId)"
          >
            {{ initials(nameOf(transfer.toParticipantId)) }}
          </span>
          <span class="text-[15px]">{{ nameOf(transfer.toParticipantId) }}</span>
          <span class="ml-auto text-[15px] font-semibold tabular-nums">
            {{ formatMoney(transfer.amount, group.currency) }}
          </span>
          <button
            type="button"
            :disabled="markingPaid !== ''"
            class="ml-3 rounded-full border border-positive/40 px-3 py-1 text-[12px] font-medium text-positive transition duration-200 hover:bg-positive/10 active:scale-95 disabled:opacity-50"
            :aria-label="`Mark ${nameOf(transfer.fromParticipantId)}'s payment to ${nameOf(transfer.toParticipantId)} as paid`"
            @click="markPaid(transfer)"
          >
            {{ markingPaid === transferKey(transfer) ? 'Saving…' : 'Mark paid' }}
          </button>
        </li>
      </TransitionGroup>
    </section>
  </div>
</template>
