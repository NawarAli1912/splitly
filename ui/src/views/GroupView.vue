<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import {
  api,
  ApiError,
  type ExpenseResponse,
  type GroupResponse,
  type TransferResponse,
} from '../lib/api'
import { forgetGroup, rememberGroup } from '../lib/recentGroups'
import { identityFor, setIdentity, SKIPPED } from '../lib/identity'
import { avatarStyle, formatMoney, initials } from '../lib/format'
import GroupTabs from '../components/GroupTabs.vue'

const props = defineProps<{ groupId: string }>()

const group = ref<GroupResponse>()
const expenses = ref<ExpenseResponse[]>([])
const transfers = ref<TransferResponse[]>([])
const loadError = ref('')
const toast = ref('')

const me = ref(identityFor(props.groupId))
const joinName = ref('')
const joinError = ref('')
const joining = ref(false)

const today = new Date().toISOString().slice(0, 10)
const expensePaidBy = ref('')
const expenseAmount = ref('')
const expenseDescription = ref('')
const expenseDate = ref(today)
const expenseSplitAmong = ref<string[]>([])
const expenseError = ref('')

const names = computed(() => new Map(group.value?.participants.map((p) => [p.id, p.name])))
const needsJoin = computed(() => me.value === null)
const hasIdentity = computed(() => me.value !== null && me.value !== SKIPPED)
const totalSpent = computed(() => expenses.value.reduce((sum, e) => sum + e.amount, 0))
const justMe = computed(
  () => hasIdentity.value && (group.value?.participants.length ?? 0) === 1,
)

const iPay = computed(() => transfers.value.filter((t) => t.fromParticipantId === me.value))
const paysMe = computed(() => transfers.value.filter((t) => t.toParticipantId === me.value))
const myNet = computed(
  () =>
    paysMe.value.reduce((sum, t) => sum + t.amount, 0) -
    iPay.value.reduce((sum, t) => sum + t.amount, 0),
)

function nameOf(id: string): string {
  return names.value.get(id) ?? '—'
}

function involvesMe(transfer: TransferResponse): boolean {
  return transfer.fromParticipantId === me.value || transfer.toParticipantId === me.value
}

function resetExpenseDefaults() {
  const participants = group.value?.participants ?? []
  expenseSplitAmong.value = participants.map((p) => p.id)
  const payerStillHere = participants.some((p) => p.id === expensePaidBy.value)
  if (!payerStillHere) {
    expensePaidBy.value = hasIdentity.value ? me.value! : (participants[0]?.id ?? '')
  }
}

async function load() {
  try {
    group.value = await api.getGroup(props.groupId)
    rememberGroup(group.value)
    resetExpenseDefaults()
    await refreshMoney()
  } catch (e) {
    loadError.value =
      e instanceof ApiError && e.status === 404
        ? 'This group does not exist.'
        : 'Could not reach the server.'
    if (e instanceof ApiError && e.status === 404) forgetGroup(props.groupId)
  }
}

async function refreshMoney() {
  const [page, settlement] = await Promise.all([
    api.listExpenses(props.groupId),
    api.getSettlement(props.groupId),
  ])
  expenses.value = page.items
  transfers.value = settlement.transfers
}

async function join() {
  joinError.value = ''
  joining.value = true
  try {
    const typed = joinName.value.trim()
    const existing = group.value!.participants.find(
      (p) => p.name.toLowerCase() === typed.toLowerCase(),
    )
    let claimedId: string
    if (existing) {
      claimedId = existing.id
    } else {
      const participant = await api.addParticipant(props.groupId, typed)
      group.value!.participants.push(participant)
      claimedId = participant.id
    }
    setIdentity(props.groupId, claimedId)
    me.value = claimedId
    expensePaidBy.value = claimedId
    resetExpenseDefaults()
  } catch (e) {
    joinError.value = e instanceof ApiError ? e.message : 'Could not reach the server'
  } finally {
    joining.value = false
  }
}

function browse() {
  setIdentity(props.groupId, SKIPPED)
  me.value = SKIPPED
}

async function copyInvite() {
  const url = `${window.location.origin}/groups/${props.groupId}`
  try {
    await navigator.clipboard.writeText(url)
  } catch {
    const area = document.createElement('textarea')
    area.value = url
    document.body.appendChild(area)
    area.select()
    document.execCommand('copy')
    area.remove()
  }
  toast.value = 'Invite link copied'
  setTimeout(() => (toast.value = ''), 2000)
}

async function addExpense() {
  expenseError.value = ''
  try {
    await api.addExpense(props.groupId, {
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
    await refreshMoney()
  } catch (e) {
    expenseError.value = e instanceof ApiError ? e.message : 'Could not reach the server'
  }
}

onMounted(load)
</script>

<template>
  <p v-if="loadError" class="pt-16 text-center text-ink-secondary">{{ loadError }}</p>

  <!-- Join screen -->
  <div v-else-if="group && needsJoin" class="flex justify-center pt-10">
    <form class="glass w-full max-w-sm p-8 text-center" @submit.prevent="join">
      <p class="text-[13px] text-ink-secondary">You're invited to</p>
      <h1 class="mt-1 text-2xl font-semibold tracking-tight">{{ group.name }}</h1>
      <p class="mt-1.5 text-[13px] text-ink-secondary/80">
        {{ group.participants.length }}
        {{ group.participants.length === 1 ? 'person' : 'people' }}
        <template v-if="totalSpent > 0">
          · {{ formatMoney(totalSpent, group.currency) }} so far
        </template>
      </p>
      <input
        v-model="joinName"
        required
        placeholder="What's your name?"
        class="mt-6 w-full rounded-xl border border-line/70 bg-white/70 px-4 py-2.5 text-center text-[15px] outline-none transition duration-200 placeholder:text-ink-secondary/50 focus:border-accent focus:ring-4 focus:ring-accent/15"
      />
      <button
        type="submit"
        :disabled="joining"
        class="mt-3 w-full rounded-full bg-accent py-2.5 text-[14px] font-medium text-white transition duration-200 hover:bg-accent-hover disabled:opacity-50"
      >
        Join group
      </button>
      <p v-if="joinError" class="mt-3 text-[13px] text-negative">{{ joinError }}</p>
      <p class="mt-4 text-[12px] text-ink-secondary/70">
        Already here? Typing your existing name signs you in.
      </p>
      <button
        type="button"
        class="mt-3 text-[13px] text-ink-secondary transition duration-200 hover:text-ink"
        @click="browse"
      >
        Just browsing
      </button>
    </form>
  </div>

  <template v-else-if="group">
    <div class="flex items-start justify-between">
      <div>
        <h1 class="text-3xl font-semibold tracking-tight">{{ group.name }}</h1>
        <p class="mt-1 text-[13px] text-ink-secondary">Amounts in {{ group.currency }}</p>
      </div>
      <button
        class="rounded-full bg-accent px-4 py-1.5 text-[13px] font-medium text-white transition duration-200 hover:bg-accent-hover"
        @click="copyInvite"
      >
        Invite
      </button>
    </div>

    <GroupTabs :group-id="groupId" />

    <!-- Your position -->
    <section v-if="hasIdentity && expenses.length > 0" class="glass mt-6 p-6">
      <p class="text-[13px] font-medium text-ink-secondary">Your position</p>
      <p
        class="mt-1 text-3xl font-semibold tracking-tight tabular-nums"
        :class="myNet < 0 ? 'text-negative' : 'text-positive'"
      >
        <template v-if="myNet < 0">You owe {{ formatMoney(-myNet, group.currency) }}</template>
        <template v-else-if="myNet > 0">
          You get back {{ formatMoney(myNet, group.currency) }}
        </template>
        <template v-else>You're all settled</template>
      </p>
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
        class="mt-5 rounded-full bg-accent px-6 py-2.5 text-[14px] font-medium text-white transition duration-200 hover:bg-accent-hover"
        @click="copyInvite"
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
          class="w-full rounded-full bg-accent py-2.5 text-[14px] font-medium text-white transition duration-200 hover:bg-accent-hover"
        >
          Add
        </button>
      </form>
      <p v-if="expenseError" class="mt-3 text-[13px] text-negative">{{ expenseError }}</p>
    </section>

    <!-- Settlement -->
    <section v-if="expenses.length > 0" class="glass mt-6 p-6">
      <h2 class="text-lg font-semibold tracking-tight">Who pays whom</h2>

      <p v-if="transfers.length === 0" class="mt-3 text-[14px] text-ink-secondary">
        All settled — nobody owes anything.
      </p>

      <ul v-else class="mt-2 divide-y divide-line/40">
        <li
          v-for="(transfer, index) in transfers"
          :key="index"
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
        </li>
      </ul>
    </section>

    <Transition
      enter-active-class="transition duration-200"
      enter-from-class="translate-y-2 opacity-0"
      leave-active-class="transition duration-200"
      leave-to-class="opacity-0"
    >
      <div
        v-if="toast"
        class="fixed bottom-8 left-1/2 -translate-x-1/2 rounded-full bg-ink px-5 py-2.5 text-[13px] font-medium text-white shadow-card"
      >
        {{ toast }}
      </div>
    </Transition>
  </template>
</template>
