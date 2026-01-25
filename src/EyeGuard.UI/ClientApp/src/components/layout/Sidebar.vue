<script setup lang="ts">
import { ref } from 'vue'

const props = defineProps<{
  activeView: string
}>()

const emit = defineEmits<{
  (e: 'update:view', view: string): void
}>()

const menuItems = [
  { id: 'dashboard', label: 'Dashboard', icon: 'dashboard' },
  { id: 'analytics', label: 'Analysis', icon: 'bar_chart' },
  { id: 'settings', label: 'Settings', icon: 'settings' },
]

// 模拟用户信息
const user = {
  name: 'Dr. Smith',
  plan: 'Pro Plan',
  avatar: 'https://lh3.googleusercontent.com/aida-public/AB6AXuBPWZE8aXnXuNp1l-Q2Rig9ZsQeNSyqoTNsDxx21ablPVlnAEvV6R5DxuQcHeayN7ecuTx0wILE0hGj3cHfkkQfOEEa6X1M93zwNUpcX52kOFRk8GpZoxl7sBzSuQiloTvup5fK5BGKi3vkRusLBoA8bR78zqqtYzNxxzwdDguIpa0QOX0irDe_ThHDRIeFRKwP5_nY0VOBrJDQqQOs66A7bF3B-At800rg9HxNKBBWp6sI1UzTwA7vnXaSeCOy2CxzdQqnM9eIcwQ'
}
</script>

<template>
  <aside class="w-20 lg:w-72 flex flex-col h-full bg-surface border-r border-border-default shadow-clean transition-all duration-300 z-30">
    <!-- Header / Logo -->
    <div class="p-6">
      <div class="flex items-center gap-3">
        <div class="size-10 bg-primary/10 text-primary flex items-center justify-center rounded-xl shrink-0">
          <span class="material-symbols-outlined text-2xl">visibility</span>
        </div>
        <div class="hidden lg:flex flex-col">
          <h1 class="font-display font-extrabold text-xl text-text-primary tracking-tight">EyeGuard</h1>
        </div>
      </div>
    </div>

    <!-- Navigation -->
    <nav class="flex-1 px-4 flex flex-col gap-2">
      <button 
        v-for="item in menuItems" 
        :key="item.id"
        @click="emit('update:view', item.id)"
        :class="[
          'nav-item group',
          activeView === item.id ? 'active' : ''
        ]"
      >
        <span class="material-symbols-outlined text-[22px] group-hover:scale-105 transition-transform">{{ item.icon }}</span>
        <span class="hidden lg:block">{{ item.label }}</span>
      </button>
    </nav>

    <!-- User Profile -->
    <div class="p-4 mt-auto border-t border-border-default">
      <div class="flex items-center gap-3 p-2 rounded-xl hover:bg-slate-50 transition-colors cursor-pointer group">
        <div 
          class="size-10 rounded-full bg-cover bg-center shrink-0 border-2 border-white shadow-sm group-hover:border-primary/20 transition-colors"
          :style="{ backgroundImage: `url(${user.avatar})` }"
        ></div>
        <div class="hidden lg:flex flex-col overflow-hidden">
          <span class="text-sm font-bold text-text-primary truncate">{{ user.name }}</span>
          <span class="text-xs text-text-tertiary truncate">{{ user.plan }}</span>
        </div>
      </div>
    </div>
  </aside>
</template>
