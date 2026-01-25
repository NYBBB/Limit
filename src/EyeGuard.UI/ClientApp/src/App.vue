<script setup lang="ts">
import { ref, computed } from 'vue'
import MainLayout from './components/layout/MainLayout.vue'
import DashboardView from './views/DashboardView.vue'
import AnalyticsView from './views/AnalyticsView.vue'
import SettingsView from './views/SettingsView.vue'

// 当前视图
const activeView = ref('dashboard')

// 根据视图动态显示标题
const pageTitle = computed(() => {
  switch (activeView.value) {
    case 'dashboard': return 'Dashboard'
    case 'analytics': return 'Analysis'
    case 'settings': return 'Settings'
    default: return 'EyeGuard'
  }
})

const pageSubtitle = computed(() => {
  switch (activeView.value) {
    case 'dashboard': return 'Monitor your cognitive fatigue in real-time'
    case 'analytics': return 'Weekly fatigue and productivity breakdown'
    case 'settings': return 'Manage your workspace and preferences'
    default: return ''
  }
})
</script>

<template>
  <MainLayout
    v-model:view="activeView"
    :active-view="activeView"
    :page-title="pageTitle"
    :page-subtitle="pageSubtitle"
  >
    <!-- View Switcher -->
    <DashboardView v-if="activeView === 'dashboard'" />
    <AnalyticsView v-else-if="activeView === 'analytics'" />

    <div v-else-if="activeView === 'settings'" class="max-w-7xl mx-auto flex flex-col gap-6">
      <SettingsView />
    </div>
  </MainLayout>
</template>
