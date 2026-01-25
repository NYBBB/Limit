<script setup lang="ts">
import { ref, onMounted, onUnmounted, computed } from 'vue'
import { bridge, MessageTypes } from '../bridge'
import FatigueTrendChart from '../components/analytics/FatigueTrendChart.vue'
import HourlyUsageChart from '../components/analytics/HourlyUsageChart.vue'
import EnergyPieChart from '../components/analytics/EnergyPieChart.vue'
import DailyRhythmChart from '../components/analytics/DailyRhythmChart.vue'
import WeeklyTrendsChart from '../components/analytics/WeeklyTrendsChart.vue'
import HeatmapChart from '../components/analytics/HeatmapChart.vue'
import TimelineChart from '../components/analytics/TimelineChart.vue'

// 状态
const currentDate = ref(new Date())
const isLoading = ref(true)
const showDatePicker = ref(false)

// 图表数据状态
const analyticsData = ref({
  fatigueTrend: [] as any[],
  hourlyUsage: [] as any[],
  energyPie: [] as any[],
  dailyRhythm: [] as any[],
  weeklyTrends: [] as any[],
  heatmap: [] as any[],
  timeline: [] as any[]
})

// Insight 状态
const insight = ref({
  icon: '💡',
  text: 'Select a date to view insights'
})

// Grind 统计状态
const grindStats = ref({
  longestSession: 0,
  overloadMinutes: 0,
  overloadPercentage: 0
})

// Bridge 监听清理函数
let cleanupAnalytics: (() => void) | null = null

function formatDate(date: Date): string {
  return date.toLocaleDateString('en-US', { month: 'short', day: 'numeric', year: 'numeric' })
}

// 用于 date input 的格式 (YYYY-MM-DD)
const dateInputValue = computed({
  get: () => {
    const d = currentDate.value
    return `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}-${String(d.getDate()).padStart(2, '0')}`
  },
  set: (val: string) => {
    const parts = val.split('-')
    if (parts.length === 3) {
      const newDate = new Date(parseInt(parts[0]), parseInt(parts[1]) - 1, parseInt(parts[2]))
      currentDate.value = newDate
      requestData()
    }
  }
})

// 检查是否为今天
const isToday = computed(() => {
  const today = new Date()
  return currentDate.value.toDateString() === today.toDateString()
})

// 检查是否为昨天
const isYesterday = computed(() => {
  const yesterday = new Date()
  yesterday.setDate(yesterday.getDate() - 1)
  return currentDate.value.toDateString() === yesterday.toDateString()
})

function requestData() {
  isLoading.value = true
  const d = currentDate.value
  // 使用本地时间格式化为 YYYY-MM-DD，避免时区偏移
  const year = d.getFullYear()
  const month = String(d.getMonth() + 1).padStart(2, '0')
  const day = String(d.getDate()).padStart(2, '0')
  const dateStr = `${year}-${month}-${day}`
  
  console.log('[AnalyticsView] 请求数据:', dateStr)
  bridge.send(MessageTypes.REQUEST_ANALYTICS, { date: dateStr })
}

onMounted(() => {
  // 订阅 Analytics 数据更新
  cleanupAnalytics = bridge.on(MessageTypes.ANALYTICS_DATA_UPDATE, (data: any) => {
    console.log('[AnalyticsView] 收到数据:', data)
    console.log('[AnalyticsView] fatigueSnapshots:', data.fatigueSnapshots?.length || 0, '条')
    if (data.fatigueSnapshots && data.fatigueSnapshots.length > 0) {
      console.log('[AnalyticsView] 第一个点示例:', data.fatigueSnapshots[0])
    }
    console.log('[AnalyticsView] hourlyUsage:', data.hourlyUsage?.length || 0, '条')
    
    analyticsData.value = {
      fatigueTrend: data.fatigueSnapshots || [],
      hourlyUsage: data.hourlyUsage || [],
      energyPie: data.energyPie || [],
      dailyRhythm: data.dailyRhythm || [],
      weeklyTrends: data.weeklyTrends || [],
      heatmap: data.heatmap || [],
      timeline: data.timeline || []
    }
    
    if (data.insights) {
      insight.value = data.insights
    }

    if (data.grindStats) {
      grindStats.value = data.grindStats
    }
    
    isLoading.value = false
  })

  // 请求初始数据（一次性，不使用定时器）
  requestData()
})

onUnmounted(() => {
  if (cleanupAnalytics) cleanupAnalytics()
})

function setDate(offset: number) {
  const newDate = new Date()
  newDate.setDate(newDate.getDate() + offset)
  currentDate.value = newDate
  requestData()
}

function prevDay() {
  const newDate = new Date(currentDate.value)
  newDate.setDate(newDate.getDate() - 1)
  currentDate.value = newDate
  requestData()
}

function nextDay() {
  const newDate = new Date(currentDate.value)
  newDate.setDate(newDate.getDate() + 1)
  // 不允许超过今天
  if (newDate <= new Date()) {
    currentDate.value = newDate
    requestData()
  }
}
</script>

<template>
  <div class="h-full flex flex-col gap-6 p-6 overflow-y-auto">
    <!-- Header -->
    <header class="flex items-center justify-between">
      <div>
        <h1 class="text-2xl font-display font-bold text-text-primary">Analytics</h1>
        <p class="text-text-tertiary text-sm">Analyze your fatigue patterns and productivity</p>
      </div>
      
      <!-- Date Picker -->
      <div class="flex items-center gap-4">
        <!-- 日期导航 -->
        <div class="flex items-center gap-2">
          <button @click="prevDay" class="btn-ghost p-2 rounded-full" title="Previous day">
            <span class="material-symbols-outlined">chevron_left</span>
          </button>
          
          <!-- 当前日期（可点击打开日期选择器） -->
          <div class="relative">
            <button 
              @click="showDatePicker = !showDatePicker" 
              class="flex items-center gap-2 px-3 py-2 rounded-lg bg-surface-50 hover:bg-surface-100 border border-border-default transition-colors"
            >
              <span class="material-symbols-outlined text-lg">calendar_today</span>
              <span class="text-sm font-bold text-text-primary">{{ formatDate(currentDate) }}</span>
            </button>
            
            <!-- 日期选择器弹窗 -->
            <div 
              v-if="showDatePicker"
              class="absolute top-full right-0 mt-2 p-4 bg-white border border-border-default rounded-xl shadow-xl z-50"
            >
              <input 
                type="date" 
                v-model="dateInputValue"
                :max="new Date().toISOString().split('T')[0]"
                class="px-3 py-2 border border-border-default rounded-lg text-text-primary"
                @change="showDatePicker = false"
              />
            </div>
          </div>
          
          <button 
            @click="nextDay" 
            :disabled="isToday"
            class="btn-ghost p-2 rounded-full disabled:opacity-30 disabled:cursor-not-allowed" 
            title="Next day"
          >
            <span class="material-symbols-outlined">chevron_right</span>
          </button>
        </div>
        
        <!-- 快捷按钮 -->
        <div class="flex gap-2">
          <button 
            @click="setDate(0)" 
            :class="isToday ? 'btn-secondary' : 'btn-ghost'"
            class="text-sm"
          >Today</button>
          <button 
            @click="setDate(-1)" 
            :class="isYesterday ? 'btn-secondary' : 'btn-ghost'"
            class="text-sm"
          >Yesterday</button>
        </div>
      </div>
    </header>

    <!-- Content Grid -->
    <div v-if="isLoading" class="flex items-center justify-center p-20">
      <div class="animate-spin rounded-full h-12 w-12 border-b-2 border-primary"></div>
    </div>
    
    <div v-else class="grid grid-cols-1 lg:grid-cols-12 gap-6 pb-20">
      
      <!-- Insight Banner (Full Width) -->
      <div class="lg:col-span-12 card-base p-6 flex items-start gap-4 bg-primary/5 border-primary/10">
        <span class="text-3xl">{{ insight.icon }}</span>
        <div>
          <h3 class="font-bold text-primary mb-1">Insight</h3>
          <p class="text-text-secondary text-sm">{{ insight.text }}</p>
        </div>
      </div>

      <!-- Fatigue Trend (Left Col) -->
      <div class="lg:col-span-8 card-base p-6 min-h-[350px]">
        <h3 class="text-lg font-bold mb-4 flex items-center gap-2">
          <span class="material-symbols-outlined">monitoring</span>
          Fatigue Trend
        </h3>
        <div class="h-[280px]">
          <FatigueTrendChart :data="analyticsData.fatigueTrend" />
        </div>
      </div>

      <!-- The Grind (Right Col) -->
      <div class="lg:col-span-4 card-base p-6">
        <h3 class="text-lg font-bold mb-4 flex items-center gap-2">
          <span class="material-symbols-outlined">local_fire_department</span>
          The Grind
        </h3>
        <!-- Stats -->
        <div class="space-y-6">
          <div>
            <div class="text-sm text-text-tertiary mb-1">Longest Session</div>
            <div class="text-3xl font-bold font-display">{{ grindStats.longestSession }} <span class="text-sm font-normal text-text-tertiary">min</span></div>
          </div>
          <div>
            <div class="text-sm text-text-tertiary mb-1">Overload Time (>80%)</div>
            <div class="text-3xl font-bold font-display text-rose-500">{{ grindStats.overloadMinutes }} <span class="text-sm font-normal text-text-tertiary">min</span></div>
            <div class="mt-2 h-2 w-full bg-surface-100 rounded-full overflow-hidden">
              <div class="h-full bg-rose-500 rounded-full" :style="{ width: `${grindStats.overloadPercentage}%` }"></div>
            </div>
          </div>
        </div>
      </div>

      <!-- Hourly Usage (Full Width) -->
      <div class="lg:col-span-12 card-base p-6 min-h-[350px]">
        <h3 class="text-lg font-bold mb-4 flex items-center gap-2">
          <span class="material-symbols-outlined">apps</span>
          24h App Usage
        </h3>
        <div class="h-[300px]">
          <HourlyUsageChart :data="analyticsData.hourlyUsage" />
        </div>
      </div>
      
      <!-- Energy Pie (Left Col) -->
      <div class="lg:col-span-4 card-base p-6 min-h-[300px]">
        <h3 class="text-lg font-bold mb-4 flex items-center gap-2">
          <span class="material-symbols-outlined">pie_chart</span>
          Energy Distribution
        </h3>
        <div class="h-[250px]">
          <EnergyPieChart :data="analyticsData.energyPie" />
        </div>
      </div>
      
      <!-- Daily Rhythm (Left Col) -->
      <div class="lg:col-span-6 card-base p-6 min-h-[300px]">
        <h3 class="text-lg font-bold mb-4 flex items-center gap-2">
          <span class="material-symbols-outlined">schedule</span>
          Daily Rhythm
        </h3>
        <div class="h-[250px]">
          <DailyRhythmChart :data="analyticsData.dailyRhythm" />
        </div>
      </div>
      
      <!-- Weekly Trends (Right Col) -->
      <div class="lg:col-span-6 card-base p-6 min-h-[300px]">
        <h3 class="text-lg font-bold mb-4 flex items-center gap-2">
          <span class="material-symbols-outlined">trending_up</span>
          Weekly Trends
        </h3>
        <div class="h-[250px]">
          <WeeklyTrendsChart :data="analyticsData.weeklyTrends" />
        </div>
      </div>

      <!-- Heatmap (Full Width) -->
      <div class="lg:col-span-12 card-base p-6 min-h-[350px]">
        <h3 class="text-lg font-bold mb-4 flex items-center gap-2">
          <span class="material-symbols-outlined">grid_on</span>
          Fatigue Heatmap (7 Days)
        </h3>
        <div class="h-[300px]">
          <HeatmapChart :data="analyticsData.heatmap" />
        </div>
      </div>
      
      <!-- Timeline (Full Width) -->
      <div class="lg:col-span-12 card-base p-6 min-h-[300px]">
        <h3 class="text-lg font-bold mb-4 flex items-center gap-2">
          <span class="material-symbols-outlined">view_timeline</span>
          Activity Timeline
        </h3>
        <div class="h-[250px]">
          <TimelineChart :data="analyticsData.timeline" />
        </div>
      </div>

    </div>
  </div>
</template>
