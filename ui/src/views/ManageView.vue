<script setup lang="ts">
import { computed, inject, ref } from 'vue'
import { useRouter } from 'vue-router'
import { api, ApiError } from '../lib/api'
import { forgetGroup } from '../lib/recentGroups'
import { avatarStyle, formatMoney, initials } from '../lib/format'
import { copyText } from '../lib/clipboard'
import { GroupCtxKey } from '../lib/groupContext'

const ctx = inject(GroupCtxKey)!
const { group, expenses, me } = ctx
const router = useRouter()

const participantName = ref('')
const participantError = ref('')

const names = computed(() => new Map(group.value?.participants.map((p) => [p.id, p.name])))

function nameOf(id: string): string {
  return names.value.get(id) ?? '—'
}

function bubbleSize(index: number): number {
  return [84, 72, 92, 76, 88, 68][index % 6]!
}

function bobStyle(index: number): Record<string, string> {
  return {
    animationDelay: `${(index % 5) * 0.6}s`,
    animationDuration: `${4 + (index % 3) * 0.8}s`,
  }
}

async function copyPersonalLink(participantId: string) {
  await copyText(`${window.location.origin}/groups/${ctx.groupId}?p=${participantId}`)
  ctx.showToast(`${nameOf(participantId)}'s personal link copied`)
}

async function addParticipant() {
  participantError.value = ''
  try {
    const participant = await api.addParticipant(ctx.groupId, participantName.value.trim())
    group.value!.participants.push(participant)
    participantName.value = ''
  } catch (e) {
    participantError.value = e instanceof ApiError ? e.message : 'Could not reach the server'
  }
}

async function removeParticipant(participantId: string) {
  participantError.value = ''
  try {
    await api.removeParticipant(ctx.groupId, participantId)
    group.value!.participants = group.value!.participants.filter((p) => p.id !== participantId)
  } catch (e) {
    participantError.value = e instanceof ApiError ? e.message : 'Could not reach the server'
  }
}

async function removeExpense(expenseId: string) {
  await api.removeExpense(ctx.groupId, expenseId)
  await ctx.refreshMoney()
}

async function deleteGroup() {
  if (!confirm(`Delete “${group.value?.name}” and all its expenses?`)) return
  await api.deleteGroup(ctx.groupId)
  forgetGroup(ctx.groupId)
  await router.push('/')
}
</script>

<template>
  <div v-if="group">
    <!-- People bubbles -->
    <section class="glass mt-6 p-6">
      <h2 class="text-lg font-semibold tracking-tight">People</h2>
      <p class="mt-1 text-[13px] text-ink-secondary">
        Tap a person to copy their personal link — opening it signs them in, no typing.
      </p>

      <TransitionGroup
        tag="div"
        name="list"
        class="mt-6 flex flex-wrap items-center justify-center gap-x-8 gap-y-10 px-2 pb-4"
      >
        <div
          v-for="(participant, index) in group.participants"
          :key="participant.id"
          class="float-bob group relative flex flex-col items-center"
          :style="bobStyle(index)"
        >
          <button
            type="button"
            class="flex cursor-pointer items-center justify-center rounded-full font-semibold shadow-card transition duration-200 hover:scale-105 active:scale-95"
            :style="{
              ...avatarStyle(participant.id),
              width: `${bubbleSize(index)}px`,
              height: `${bubbleSize(index)}px`,
              fontSize: `${bubbleSize(index) / 3.4}px`,
              border: participant.id === me ? '2.5px solid var(--color-accent)' : 'none',
            }"
            :aria-label="`Copy ${participant.name}'s personal link`"
            @click="copyPersonalLink(participant.id)"
          >
            {{ initials(participant.name) }}
          </button>
          <span class="mt-2 text-[13px] font-medium">
            {{ participant.name }}
            <span v-if="participant.id === me" class="text-accent">· you</span>
          </span>
          <button
            class="absolute -top-1 -right-1 flex size-6 items-center justify-center rounded-full bg-white text-[13px] text-ink-secondary opacity-0 shadow-card transition duration-200 group-hover:opacity-100 hover:text-negative"
            :aria-label="`Remove ${participant.name}`"
            @click="removeParticipant(participant.id)"
          >
            ×
          </button>
        </div>
      </TransitionGroup>

      <form class="mx-auto mt-2 flex max-w-sm gap-2" @submit.prevent="addParticipant">
        <input
          v-model="participantName"
          required
          placeholder="Add a person"
          class="flex-1 rounded-xl border border-line/70 bg-white/70 px-4 py-2 text-[14px] outline-none transition duration-200 placeholder:text-ink-secondary/50 focus:border-accent focus:ring-4 focus:ring-accent/15"
        />
        <button
          type="submit"
          class="rounded-full bg-accent px-5 text-[14px] font-medium text-white transition duration-200 hover:bg-accent-hover active:scale-95"
        >
          Add
        </button>
      </form>
      <p v-if="participantError" class="mt-3 text-center text-[13px] text-negative">
        {{ participantError }}
      </p>
    </section>

    <!-- Expense history -->
    <section v-if="expenses.length" class="glass mt-6 p-6">
      <h2 class="text-lg font-semibold tracking-tight">Expenses</h2>
      <TransitionGroup tag="ul" name="list" class="mt-2 divide-y divide-line/40">
        <li v-for="expense in expenses" :key="expense.id" class="group flex items-center py-3">
          <div>
            <p class="text-[15px] font-medium">{{ expense.description }}</p>
            <p class="mt-0.5 text-[13px] text-ink-secondary">
              {{ nameOf(expense.paidById) }} paid · split {{ expense.splitAmong.length }} ways ·
              {{ expense.spentOn }}
            </p>
          </div>
          <span class="ml-auto text-[15px] font-semibold tabular-nums">
            {{ formatMoney(expense.amount, group.currency) }}
          </span>
          <button
            class="ml-4 text-ink-secondary/0 transition duration-200 group-hover:text-ink-secondary/60 hover:!text-negative"
            :aria-label="`Delete ${expense.description}`"
            @click="removeExpense(expense.id)"
          >
            ×
          </button>
        </li>
      </TransitionGroup>
    </section>

    <div class="mt-8 text-center">
      <button
        class="text-[13px] text-negative/80 transition duration-200 hover:text-negative"
        @click="deleteGroup"
      >
        Delete this group
      </button>
    </div>
  </div>
</template>
