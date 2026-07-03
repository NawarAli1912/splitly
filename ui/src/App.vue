<template>
  <header class="sticky top-0 z-10 border-b border-line/60 bg-canvas/80 backdrop-blur-xl">
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
