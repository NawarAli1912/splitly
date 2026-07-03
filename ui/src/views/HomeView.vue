<script setup lang="ts">
import { ref } from 'vue'
import { useRouter } from 'vue-router'
import { api, ApiError } from '../lib/api'
import { recentGroups, rememberGroup } from '../lib/recentGroups'
import { setIdentity } from '../lib/identity'

const router = useRouter()

const name = ref('')
const yourName = ref('')
const currency = ref('EUR')
const error = ref('')
const busy = ref(false)
const recent = recentGroups()

const currencies = ['EUR', 'USD', 'GBP', 'SEK', 'CHF', 'AED']

async function createGroup() {
  error.value = ''
  busy.value = true
  try {
    const group = await api.createGroup(name.value.trim(), currency.value)
    const creator = await api.addParticipant(group.id, yourName.value.trim())
    setIdentity(group.id, creator.id)
    rememberGroup({ id: group.id, name: group.name, currency: group.currency })
    await router.push(`/groups/${group.id}`)
  } catch (e) {
    error.value = e instanceof ApiError ? e.message : 'Could not reach the server'
  } finally {
    busy.value = false
  }
}
</script>

<template>
  <section class="pt-8 pb-12 text-center">
    <h1 class="text-5xl font-semibold tracking-tight">Split expenses.<br />Settle simply.</h1>
    <p class="mx-auto mt-4 max-w-md text-lg text-ink-secondary">
      Track who paid what, and settle up with the fewest possible payments.
    </p>
  </section>

  <form class="glass mx-auto max-w-md p-8" @submit.prevent="createGroup">
    <h2 class="text-xl font-semibold tracking-tight">New group</h2>

    <label class="mt-6 block text-[13px] font-medium text-ink-secondary" for="group-name">
      Group name
    </label>
    <input
      id="group-name"
      v-model="name"
      required
      placeholder="Trip to Lisbon"
      class="mt-1.5 w-full rounded-xl border border-line/70 bg-white/70 px-4 py-2.5 text-[15px] outline-none transition duration-200 placeholder:text-ink-secondary/50 focus:border-accent focus:ring-4 focus:ring-accent/15"
    />

    <div class="mt-4 grid grid-cols-2 gap-3">
      <div>
        <label class="block text-[13px] font-medium text-ink-secondary" for="your-name">
          Your name
        </label>
        <input
          id="your-name"
          v-model="yourName"
          required
          placeholder="Nawar"
          class="mt-1.5 w-full rounded-xl border border-line/70 bg-white/70 px-4 py-2.5 text-[15px] outline-none transition duration-200 placeholder:text-ink-secondary/50 focus:border-accent focus:ring-4 focus:ring-accent/15"
        />
      </div>
      <div>
        <label class="block text-[13px] font-medium text-ink-secondary" for="group-currency">
          Currency
        </label>
        <select
          id="group-currency"
          v-model="currency"
          class="mt-1.5 w-full appearance-none rounded-xl border border-line/70 bg-white/70 px-4 py-2.5 text-[15px] outline-none transition duration-200 focus:border-accent focus:ring-4 focus:ring-accent/15"
        >
          <option v-for="code in currencies" :key="code" :value="code">{{ code }}</option>
        </select>
      </div>
    </div>

    <p v-if="error" class="mt-4 text-[13px] text-negative">{{ error }}</p>

    <button
      type="submit"
      :disabled="busy"
      class="mt-6 w-full rounded-full bg-accent py-3 text-[15px] font-medium text-white transition duration-200 hover:bg-accent-hover disabled:opacity-50"
    >
      Create group
    </button>
  </form>

  <section v-if="recent.length" class="mx-auto mt-12 max-w-md">
    <h3 class="px-1 text-[13px] font-medium tracking-wide text-ink-secondary uppercase">
      Recent groups
    </h3>
    <div class="glass mt-3 overflow-hidden">
      <RouterLink
        v-for="group in recent"
        :key="group.id"
        :to="`/groups/${group.id}`"
        class="flex items-center justify-between border-b border-line/40 px-5 py-3.5 transition duration-200 last:border-b-0 hover:bg-white/50"
      >
        <span class="text-[15px] font-medium">{{ group.name }}</span>
        <span class="text-[13px] text-ink-secondary">{{ group.currency }}</span>
      </RouterLink>
    </div>
  </section>
</template>
