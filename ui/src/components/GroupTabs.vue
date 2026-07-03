<script setup lang="ts">
import { useRoute } from 'vue-router'

const props = defineProps<{ groupId: string }>()
const route = useRoute()

const tabs = [
  { label: 'Home', to: `/groups/${props.groupId}` },
  { label: 'Manage', to: `/groups/${props.groupId}/manage` },
  { label: 'Insights', to: `/groups/${props.groupId}/insights` },
]
</script>

<template>
  <nav class="glass-pill mt-6 flex w-fit gap-1 rounded-full p-1">
    <RouterLink
      v-for="tab in tabs"
      :key="tab.to"
      :to="tab.to"
      class="rounded-full px-4 py-1.5 text-[13px] font-medium transition duration-200"
      :class="
        route.path === tab.to
          ? 'bg-white shadow-sm'
          : 'text-ink-secondary hover:text-ink'
      "
    >
      {{ tab.label }}
    </RouterLink>
  </nav>
</template>
