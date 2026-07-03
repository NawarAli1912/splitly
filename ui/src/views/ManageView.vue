<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { useRouter } from 'vue-router'
import { api, ApiError, type ExpenseResponse, type GroupResponse } from '../lib/api'
import { forgetGroup } from '../lib/recentGroups'
import { identityFor } from '../lib/identity'
import { avatarStyle, formatMoney, initials } from '../lib/format'
import GroupTabs from '../components/GroupTabs.vue'

const props = defineProps<{ groupId: string }>()
const router = useRouter()

const group = ref<GroupResponse>()
const expenses = ref<ExpenseResponse[]>([])
const loadError = ref('')

const me = identityFor(props.groupId)
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

async function load() {
  try {
    const [g, page] = await Promise.all([
      api.getGroup(props.groupId),
      api.listExpenses(props.groupId, 1, 100),
    ])
    group.value = g
    expenses.value = page.items
  } catch (e) {
    loadError.value =
      e instanceof ApiError && e.status === 404
        ? 'This group does not exist.'
        : 'Could not reach the server.'
  }
}

async function addParticipant() {
  participantError.value = ''
  try {
    const participant = await api.addParticipant(props.groupId, participantName.value.trim())
    group.value!.participants.push(participant)
    participantName.value = ''
  } catch (e) {
    participantError.value = e instanceof ApiError ? e.message : 'Could not reach the server'
  }
}

async function removeParticipant(participantId: string) {
  participantError.value = ''
  try {
    await api.removeParticipant(props.groupId, participantId)
    group.value!.participants = group.value!.participants.filter((p) => p.id !== participantId)
  } catch (e) {
    participantError.value = e instanceof ApiError ? e.message : 'Could not reach the server'
  }
}

async function removeExpense(expenseId: string) {
  await api.removeExpense(props.groupId, expenseId)
  expenses.value = expenses.value.filter((e) => e.id !== expenseId)
}

async function deleteGroup() {
  if (!confirm(`Delete “${group.value?.name}” and all its expenses?`)) return
  await api.deleteGroup(props.groupId)
  forgetGroup(props.groupId)
  await router.push('/')
}

onMounted(load)
</script>

<template>
  <p v-if="loadError" class="pt-16 text-center text-ink-secondary">{{ loadError }}</p>

  <template v-else-if="group">
    <h1 class="text-3xl font-semibold tracking-tight">{{ group.name }}</h1>
    <p class="mt-1 text-[13px] text-ink-secondary">Amounts in {{ group.currency }}</p>

    <GroupTabs :group-id="groupId" />

    <!-- People bubbles -->
    <section class="glass mt-6 p-6">
      <h2 class="text-lg font-semibold tracking-tight">People</h2>

      <div class="mt-6 flex flex-wrap items-center justify-center gap-x-8 gap-y-10 px-2 pb-4">
        <div
          v-for="(participant, index) in group.participants"
          :key="participant.id"
          class="float-bob group relative flex flex-col items-center"
          :style="bobStyle(index)"
        >
          <span
            class="flex items-center justify-center rounded-full font-semibold shadow-card"
            :style="{
              ...avatarStyle(participant.id),
              width: `${bubbleSize(index)}px`,
              height: `${bubbleSize(index)}px`,
              fontSize: `${bubbleSize(index) / 3.4}px`,
              border: participant.id === me ? '2.5px solid var(--color-accent)' : 'none',
            }"
          >
            {{ initials(participant.name) }}
          </span>
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
      </div>

      <form class="mx-auto mt-2 flex max-w-sm gap-2" @submit.prevent="addParticipant">
        <input
          v-model="participantName"
          required
          placeholder="Add a person"
          class="flex-1 rounded-xl border border-line/70 bg-white/70 px-4 py-2 text-[14px] outline-none transition duration-200 placeholder:text-ink-secondary/50 focus:border-accent focus:ring-4 focus:ring-accent/15"
        />
        <button
          type="submit"
          class="rounded-full bg-accent px-5 text-[14px] font-medium text-white transition duration-200 hover:bg-accent-hover"
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
      <ul class="mt-2 divide-y divide-line/40">
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
      </ul>
    </section>

    <div class="mt-8 text-center">
      <button
        class="text-[13px] text-negative/80 transition duration-200 hover:text-negative"
        @click="deleteGroup"
      >
        Delete this group
      </button>
    </div>
  </template>
</template>
