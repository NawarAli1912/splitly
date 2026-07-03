<script setup lang="ts">
import { computed, onMounted, provide, ref, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import {
  api,
  ApiError,
  type ExpenseResponse,
  type GroupResponse,
  type PaymentResponse,
  type SettlementStrategy,
  type TransferResponse,
} from '../lib/api'
import { forgetGroup, rememberGroup } from '../lib/recentGroups'
import { identityFor, setIdentity, SKIPPED } from '../lib/identity'
import { formatMoney } from '../lib/format'
import { copyText } from '../lib/clipboard'
import { GroupCtxKey } from '../lib/groupContext'
import GroupTabs from '../components/GroupTabs.vue'

const props = defineProps<{ groupId: string }>()
const route = useRoute()
const router = useRouter()

const group = ref<GroupResponse>()
const expenses = ref<ExpenseResponse[]>([])
const payments = ref<PaymentResponse[]>([])
const transfers = ref<TransferResponse[]>([])
const strategy = ref<SettlementStrategy>('minimum-transfers')
const banker = ref('')
const loadError = ref('')
const toast = ref('')

const me = ref(identityFor(props.groupId))
const joinName = ref('')
const joinError = ref('')
const joining = ref(false)

const needsJoin = computed(() => me.value === null)
const totalSpent = computed(() => expenses.value.reduce((sum, e) => sum + e.amount, 0))

async function load() {
  try {
    group.value = await api.getGroup(props.groupId)
    rememberGroup(group.value)
    claimFromPersonalLink()
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
  // via-banker without a banker (e.g. banker was removed) falls back to the default
  const useStrategy =
    strategy.value === 'via-banker' && banker.value === '' ? 'minimum-transfers' : strategy.value
  const [expensePage, paymentPage, settlement] = await Promise.all([
    api.listExpenses(props.groupId, 1, 100),
    api.listPayments(props.groupId, 1, 100),
    api.getSettlement(
      props.groupId,
      useStrategy,
      useStrategy === 'via-banker' ? banker.value : undefined,
    ),
  ])
  expenses.value = expensePage.items
  payments.value = paymentPage.items
  transfers.value = settlement.transfers
}

watch([strategy, banker], () => {
  if (strategy.value === 'via-banker' && banker.value === '') return
  refreshMoney()
})

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

function claimFromPersonalLink() {
  const p = route.query.p
  if (typeof p !== 'string') return
  const participant = group.value!.participants.find((x) => x.id === p)
  if (participant) {
    setIdentity(props.groupId, participant.id)
    me.value = participant.id
    showToast(`Welcome, ${participant.name} — this device is you now`)
  }
  router.replace({ query: {} })
}

function showToast(message: string) {
  toast.value = message
  setTimeout(() => (toast.value = ''), 2000)
}

async function copyInvite() {
  await copyText(`${window.location.origin}/groups/${props.groupId}`)
  showToast('Invite link copied')
}

provide(GroupCtxKey, {
  groupId: props.groupId,
  group,
  expenses,
  payments,
  transfers,
  strategy,
  banker,
  me,
  refreshMoney,
  copyInvite,
  showToast,
})

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

  <div v-else-if="group">
    <div class="flex items-start justify-between">
      <div>
        <h1 class="text-3xl font-semibold tracking-tight">{{ group.name }}</h1>
        <p class="mt-1 text-[13px] text-ink-secondary">Amounts in {{ group.currency }}</p>
      </div>
      <button
        class="rounded-full bg-accent px-4 py-1.5 text-[13px] font-medium text-white transition duration-200 hover:bg-accent-hover active:scale-95"
        @click="copyInvite"
      >
        Invite
      </button>
    </div>

    <GroupTabs :group-id="groupId" />

    <RouterView v-slot="{ Component }">
      <Transition name="tab" mode="out-in">
        <component :is="Component" />
      </Transition>
    </RouterView>

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
  </div>
</template>
