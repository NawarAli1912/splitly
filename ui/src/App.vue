<template>
  <div class="pointer-events-none fixed inset-0 -z-10 overflow-hidden" aria-hidden="true">
    <div class="absolute -top-40 -left-40 size-[520px] rounded-full bg-[#7dc0ff]/45 blur-3xl" />
    <div class="absolute top-1/4 -right-48 size-[560px] rounded-full bg-[#c9a6ff]/40 blur-3xl" />
    <div class="absolute -bottom-32 left-1/4 size-[480px] rounded-full bg-[#ffc2d4]/35 blur-3xl" />
  </div>

  <header class="sticky top-0 z-10 border-b border-white/50 bg-white/40 backdrop-blur-xl">
    <div class="mx-auto flex h-13 max-w-3xl items-center px-6">
      <RouterLink to="/" class="text-[17px] font-semibold tracking-tight">
        Splitly
      </RouterLink>
    </div>
  </header>

  <main class="mx-auto max-w-3xl px-6 py-10">
    <RouterView />
  </main>
</template>
