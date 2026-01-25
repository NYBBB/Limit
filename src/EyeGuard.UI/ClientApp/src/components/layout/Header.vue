<script setup lang="ts">
defineProps<{
  title: string
  subtitle?: string
}>()
</script>

<template>
  <header class="flex items-center justify-between px-8 py-5 border-b border-border-default/60 bg-surface/80 backdrop-blur-md sticky top-0 z-20">
    <div class="flex flex-col">
      <h1 class="font-display font-bold text-2xl text-text-primary">{{ title }}</h1>
      <p v-if="subtitle" class="text-sm text-text-tertiary mt-0.5">{{ subtitle }}</p>
    </div>

    <div class="flex items-center gap-4">
      <!-- Search (Hidden on mobile) -->
      <div class="hidden md:flex items-center bg-white h-10 px-4 rounded-xl border border-border-default shadow-sm w-64 focus-within:ring-2 focus-within:ring-primary/10 focus-within:border-primary transition-all">
        <span class="material-symbols-outlined text-text-tertiary text-[20px]">search</span>
        <input 
          type="text" 
          placeholder="Search..." 
          class="bg-transparent border-none outline-none text-sm text-text-primary placeholder-text-tertiary w-full ml-2"
        />
      </div>

      <!-- Notifications -->
      <button class="size-10 flex items-center justify-center rounded-xl bg-white border border-border-default shadow-sm text-text-tertiary hover:text-primary hover:border-primary/20 transition-colors relative">
        <span class="material-symbols-outlined text-[22px]">notifications</span>
        <span class="absolute top-2.5 right-2.5 size-2 bg-red-500 rounded-full border border-white"></span>
      </button>
    </div>
  </header>
</template>
