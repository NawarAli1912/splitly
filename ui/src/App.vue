<template>
  <header class="liquid sticky top-0 z-20 !rounded-none !border-x-0 !border-t-0">
    <div class="mx-auto flex h-13 max-w-3xl items-center px-6">
      <RouterLink to="/" class="text-[17px] font-semibold tracking-tight">
        Splitly
      </RouterLink>
    </div>
  </header>

  <main class="mx-auto max-w-3xl px-6 py-10">
    <RouterView v-slot="{ Component }">
      <Transition name="page" mode="out-in">
        <component :is="Component" />
      </Transition>
    </RouterView>
  </main>
</template>
