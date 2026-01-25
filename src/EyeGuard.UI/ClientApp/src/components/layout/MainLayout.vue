<script setup lang="ts">
import Sidebar from './Sidebar.vue'
import Header from './Header.vue'

defineProps<{
  activeView: string
  pageTitle: string
  pageSubtitle?: string
}>()

defineEmits<{
  (e: 'update:view', view: string): void
}>()
</script>

<template>
  <div class="flex h-screen w-full bg-base text-text-secondary overflow-hidden selection:bg-primary/20 selection:text-primary">
    <!-- Sidebar -->
    <Sidebar 
      :active-view="activeView"
      @update:view="$emit('update:view', $event)"
    />

    <!-- Main Content Area -->
    <div class="flex-1 flex flex-col h-full relative overflow-hidden">
      <Header 
        :title="pageTitle" 
        :subtitle="pageSubtitle"
      />

      <main class="flex-1 overflow-y-auto p-6 md:p-8 lg:px-10 scroll-smooth">
        <slot></slot>
      </main>
    </div>
  </div>
</template>
