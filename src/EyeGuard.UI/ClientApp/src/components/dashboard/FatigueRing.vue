<script setup lang="ts">
import { computed, watch } from 'vue'
import gsap from 'gsap'

const props = defineProps<{
  value: number // 0-100
  status: string
  isCareMode?: boolean
  focusTotalSeconds?: number
  focusRemainingSeconds?: number
}>()

// 调试：监听 value 变化
watch(() => props.value, (newVal) => {
  // console.log('[FatigueRing] Value changed:', newVal)
})

const isFocusCommitment = computed(() => {
  return (props.focusTotalSeconds || 0) > 0
})

// 颜色计算 (Blue/Indigo theme)
const ringColor = computed(() => {
  if (isFocusCommitment.value) return '#8b5cf6' // Violet for Focus Session
  if (props.isCareMode) return '#f59e0b' // Amber for Care
  if (props.value < 40) return '#4f46e5' // Indigo (Fresh)
  if (props.value < 70) return '#8b5cf6' // Violet (Focused)
  return '#ef4444' // Red (Drain - only for critical)
})

const statusLabel = computed(() => {
  if (isFocusCommitment.value) return 'FOCUSING'
  if (props.isCareMode) return 'CARE MODE'
  if (props.value < 30) return 'FRESH'
  if (props.value < 50) return 'FOCUSED'
  if (props.value < 70) return 'FLOW'
  if (props.value < 85) return 'STRAIN'
  return 'DRAIN'
})

// SVG 计算 - 内环 (疲劳)
const innerRadius = 100
const innerCircumference = 2 * Math.PI * innerRadius
const innerDashOffset = computed(() => {
  return innerCircumference - (props.value / 100) * innerCircumference
})

// SVG 计算 - 外环 (专注倒计时)
const outerRadius = 130
const outerCircumference = 2 * Math.PI * outerRadius
const outerDashOffset = computed(() => {
  if (!props.focusTotalSeconds || !props.focusRemainingSeconds) return 0
  const progress = props.focusRemainingSeconds / props.focusTotalSeconds
  return outerCircumference - progress * outerCircumference
})

const formattedCountdown = computed(() => {
  const s = props.focusRemainingSeconds || 0
  const m = Math.floor(s / 60)
  const sec = s % 60
  return `${m.toString().padStart(2, '0')}:${sec.toString().padStart(2, '0')}`
})
</script>

<template>
  <div class="relative flex items-center justify-center p-8">
    <!-- 呼吸光晕背景 -->
    <div 
      class="absolute inset-0 bg-primary/5 rounded-full blur-[80px] animate-pulse-slow"
      :style="{ backgroundColor: `${ringColor}10` }"
    ></div>

    <div class="relative size-[300px] flex items-center justify-center">
      <!-- SVG 圆环 -->
      <svg class="size-full -rotate-90 transform drop-shadow-lg" viewBox="0 0 300 300">
        
        <!-- 外环 (专注倒计时) - 仅在专注模式显示 -->
        <g v-if="isFocusCommitment">
          <circle cx="150" cy="150" :r="outerRadius" fill="none" stroke="#e2e8f0" stroke-width="8" stroke-linecap="round" class="dark:stroke-slate-800 opacity-30"></circle>
          <circle
            cx="150" cy="150" :r="outerRadius" fill="none" :stroke="ringColor" stroke-width="8" stroke-linecap="round"
            class="transition-all duration-1000 linear"
            :stroke-dasharray="outerCircumference"
            :stroke-dashoffset="outerDashOffset"
          ></circle>
        </g>

        <!-- 内环 (疲劳值) -->
        <g>
          <circle cx="150" cy="150" :r="innerRadius" fill="none" stroke="#e2e8f0" stroke-width="12" stroke-linecap="round" class="dark:stroke-slate-800"></circle>
          <circle
            cx="150" cy="150" :r="innerRadius" fill="none" :stroke="ringColor" stroke-width="12" stroke-linecap="round"
            class="transition-all duration-1000 ease-out"
            :stroke-dasharray="innerCircumference"
            :stroke-dashoffset="innerDashOffset"
          ></circle>
        </g>
      </svg>

      <!-- 中心内容 -->
      <div class="absolute inset-0 flex flex-col items-center justify-center text-center">
        <!-- 专注倒计时模式 -->
        <div v-if="isFocusCommitment" class="flex flex-col items-center animate-in zoom-in duration-300">
          <span class="material-symbols-outlined text-4xl mb-1 text-text-tertiary">timer</span>
          <h1 class="text-6xl font-display font-bold tracking-tight text-text-primary tabular-nums">
            {{ formattedCountdown }}
          </h1>
          <p class="text-xs font-bold text-text-tertiary tracking-[0.2em] uppercase mt-2 text-primary">Focus Session</p>
        </div>

        <!-- 正常疲劳模式 -->
        <div v-else class="flex flex-col items-center animate-in zoom-in duration-300">
          <span class="material-symbols-outlined text-5xl mb-2 animate-pulse" :style="{ color: ringColor }">cardiology</span>
          <h1 class="text-7xl font-display font-extrabold tracking-tight text-text-primary">
            {{ Math.round(value) }}<span class="text-3xl font-bold align-top text-text-tertiary ml-1">%</span>
          </h1>
          <p class="text-xs font-bold text-text-tertiary tracking-[0.2em] uppercase mt-2">Fatigue Level</p>
        </div>

        <!-- 状态指示器 (通用) -->
        <div 
          class="mt-4 flex items-center gap-2 px-3 py-1.5 rounded-full bg-white border shadow-sm backdrop-blur-md transition-colors"
          :style="{ borderColor: `${ringColor}30`, backgroundColor: `${ringColor}10` }"
        >
          <span class="flex size-2 rounded-full animate-ping" :style="{ backgroundColor: ringColor }"></span>
          <span class="text-xs font-bold tracking-wider" :style="{ color: ringColor }">{{ statusLabel }}</span>
        </div>
      </div>
      <!-- Care 模式按钮 -->
      <button 
        class="absolute bottom-4 right-4 size-12 bg-white border border-border-default rounded-full flex items-center justify-center text-rose-500 shadow-md transition-all hover:scale-110 hover:shadow-lg active:scale-95 group z-20"
        title="Activate Care Mode"
      >
        <span class="material-symbols-outlined text-xl group-hover:scale-125 transition-transform">favorite</span>
      </button>
    </div>
  </div>
</template>

<style scoped>
.animate-pulse-slow {
  animation: pulse 4s cubic-bezier(0.4, 0, 0.6, 1) infinite;
}
</style>
