<script setup lang="ts">
import { computed, inject } from 'vue'
import { avatarStyle, chartColor, formatMoney, initials } from '../lib/format'
import { GroupCtxKey } from '../lib/groupContext'

const ctx = inject(GroupCtxKey)!
const { group, expenses, payments } = ctx

const total = computed(() => expenses.value.reduce((sum, e) => sum + e.amount, 0))
const perPerson = computed(() =>
  group.value?.participants.length ? total.value / group.value.participants.length : 0,
)

interface PersonStat {
  id: string
  name: string
  paid: number
  net: number
  color: string
}

const people = computed<PersonStat[]>(() => {
  if (!group.value) return []
  return group.value.participants.map((participant, index) => {
    let paid = 0
    let consumed = 0
    for (const expense of expenses.value) {
      if (expense.paidById === participant.id) paid += expense.amount
      if (expense.splitAmong.includes(participant.id))
        consumed += expense.amount / expense.splitAmong.length
    }
    let settled = 0
    for (const payment of payments.value) {
      if (payment.fromParticipantId === participant.id) settled += payment.amount
      if (payment.toParticipantId === participant.id) settled -= payment.amount
    }
    return {
      id: participant.id,
      name: participant.name,
      paid,
      net: paid - consumed + settled,
      color: chartColor(index),
    }
  })
})

const maxPaid = computed(() => Math.max(...people.value.map((p) => p.paid), 0.01))
const maxNet = computed(() => Math.max(...people.value.map((p) => Math.abs(p.net)), 0.01))

const byDay = computed(() => {
  const totals = new Map<string, number>()
  for (const expense of expenses.value) {
    totals.set(expense.spentOn, (totals.get(expense.spentOn) ?? 0) + expense.amount)
  }
  return [...totals.entries()]
    .sort(([a], [b]) => a.localeCompare(b))
    .map(([date, amount]) => ({ date, amount }))
})

const maxDay = computed(() => Math.max(...byDay.value.map((d) => d.amount), 0.01))

function shortDate(iso: string): string {
  return new Date(iso).toLocaleDateString(undefined, { day: 'numeric', month: 'short' })
}
</script>

<template>
  <div v-if="group">
    <p v-if="expenses.length === 0" class="pt-16 text-center text-ink-secondary">
      No expenses yet — add some and the numbers appear here.
    </p>

    <template v-else>
      <!-- Stat tiles -->
      <div class="mt-6 grid grid-cols-2 gap-3 sm:grid-cols-4">
        <div class="glass p-5">
          <p class="text-[13px] font-medium text-ink-secondary">Total spent</p>
          <p class="mt-1 text-2xl font-semibold tracking-tight text-[#0a84ff] tabular-nums">
            {{ formatMoney(total, group.currency) }}
          </p>
        </div>
        <div class="glass p-5">
          <p class="text-[13px] font-medium text-ink-secondary">Expenses</p>
          <p class="mt-1 text-2xl font-semibold tracking-tight text-[#bf5af2] tabular-nums">
            {{ expenses.length }}
          </p>
        </div>
        <div class="glass p-5">
          <p class="text-[13px] font-medium text-ink-secondary">People</p>
          <p class="mt-1 text-2xl font-semibold tracking-tight text-[#ff9f0a] tabular-nums">
            {{ group.participants.length }}
          </p>
        </div>
        <div class="glass p-5">
          <p class="text-[13px] font-medium text-ink-secondary">Per person</p>
          <p class="mt-1 text-2xl font-semibold tracking-tight text-positive tabular-nums">
            {{ formatMoney(perPerson, group.currency) }}
          </p>
        </div>
      </div>

      <!-- Who paid -->
      <section class="glass mt-6 p-6">
        <h2 class="text-lg font-semibold tracking-tight">Who paid</h2>
        <ul class="mt-4 space-y-4">
          <li v-for="person in people" :key="person.id">
            <div class="flex items-center gap-2">
              <span
                class="flex size-7 items-center justify-center rounded-full text-[11px] font-semibold"
                :style="avatarStyle(person.id)"
              >
                {{ initials(person.name) }}
              </span>
              <span class="text-[14px] font-medium">{{ person.name }}</span>
              <span class="ml-auto text-[14px] font-semibold tabular-nums">
                {{ formatMoney(person.paid, group.currency) }}
              </span>
            </div>
            <div class="mt-2 h-2.5 overflow-hidden rounded-full bg-canvas">
              <div
                class="h-full rounded-full transition-all duration-500"
                :style="{ width: `${(person.paid / maxPaid) * 100}%`, backgroundColor: person.color }"
              />
            </div>
          </li>
        </ul>
      </section>

      <!-- Balances -->
      <section class="glass mt-6 p-6">
        <h2 class="text-lg font-semibold tracking-tight">Balances</h2>
        <p class="mt-1 text-[13px] text-ink-secondary">
          Paid minus fair share, after recorded payments — green is owed money, red owes.
        </p>
        <ul class="mt-4 space-y-3">
          <li v-for="person in people" :key="person.id" class="flex items-center gap-3">
            <span class="w-20 truncate text-[14px] font-medium">{{ person.name }}</span>
            <div class="relative h-2.5 flex-1">
              <div class="absolute inset-y-0 left-1/2 w-px bg-line" />
              <div
                class="absolute inset-y-0 rounded-full transition-all duration-500"
                :class="person.net >= 0 ? 'bg-positive' : 'bg-negative'"
                :style="
                  person.net >= 0
                    ? { left: '50%', width: `${(person.net / maxNet) * 50}%` }
                    : { right: '50%', width: `${(-person.net / maxNet) * 50}%` }
                "
              />
            </div>
            <span
              class="w-24 text-right text-[14px] font-semibold tabular-nums"
              :class="person.net >= 0 ? 'text-positive' : 'text-negative'"
            >
              {{ (person.net >= 0 ? '+' : '−') + formatMoney(Math.abs(person.net), group.currency) }}
            </span>
          </li>
        </ul>
      </section>

      <!-- Over time -->
      <section class="glass mt-6 p-6">
        <h2 class="text-lg font-semibold tracking-tight">Spending over time</h2>
        <div class="mt-4 flex h-44 items-end gap-2">
          <div
            v-for="day in byDay"
            :key="day.date"
            class="group flex h-full flex-1 flex-col items-center justify-end"
          >
            <span
              class="mb-1 text-[11px] font-medium text-ink-secondary opacity-0 transition duration-200 tabular-nums group-hover:opacity-100"
            >
              {{ formatMoney(day.amount, group.currency) }}
            </span>
            <div
              class="w-full max-w-12 rounded-t-lg bg-accent/85 transition-all duration-500 group-hover:bg-accent"
              :style="{ height: `${(day.amount / maxDay) * 100}%` }"
            />
            <span class="mt-2 text-[11px] text-ink-secondary">{{ shortDate(day.date) }}</span>
          </div>
        </div>
      </section>
    </template>
  </div>
</template>
