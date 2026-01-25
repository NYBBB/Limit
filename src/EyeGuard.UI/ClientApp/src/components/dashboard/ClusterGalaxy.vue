<script setup lang="ts">
/**
 * ClusterGalaxy - Zone B 工作流星系组件
 * 显示主应用（主星）和最近使用的应用（卫星）
 */
import { computed } from 'vue'

const props = defineProps<{
  mainApp: {
    name: string
    processName: string
    icon: string
    isImage?: boolean
    color: string
  }
  satellites: Array<{
    name: string
    processName: string
    icon: string
    isImage?: boolean
  }>
  clusterColor: string
}>()

// 卫星位置计算（圆形布局）
const satellitePositions = computed(() => {
  const count = props.satellites.length
  if (count === 0) return []
  
  const radius = 60 // 距离中心的距离
  const startAngle = -90 // 从顶部开始
  
  return props.satellites.map((sat, index) => {
    const angle = startAngle + (360 / Math.max(count, 3)) * index
    const rad = (angle * Math.PI) / 180
    return {
      ...sat,
      x: Math.cos(rad) * radius,
      y: Math.sin(rad) * radius
    }
  })
})
</script>

<template>
  <div class="cluster-galaxy relative flex items-center justify-center h-40">
    <!-- 背景光晕 -->
    <div 
      class="absolute w-32 h-32 rounded-full blur-2xl opacity-20 transition-colors duration-500"
      :style="{ backgroundColor: clusterColor }"
    ></div>
    
    <!-- 连接线 (SVG) -->
    <svg class="absolute w-full h-full pointer-events-none" viewBox="-80 -80 160 160">
      <line 
        v-for="(sat, index) in satellitePositions" 
        :key="'line-' + index"
        x1="0" y1="0" 
        :x2="sat.x" :y2="sat.y"
        class="stroke-current text-slate-200"
        stroke-width="1"
        stroke-dasharray="4 2"
      />
    </svg>
    
    <!-- 卫星应用 -->
    <div 
      v-for="(sat, index) in satellitePositions" 
      :key="'sat-' + index"
      class="absolute flex flex-col items-center transition-all duration-500 group"
      :style="{ 
        transform: `translate(${sat.x}px, ${sat.y}px)`,
      }"
      :title="sat.name"
    >
      <div class="size-8 rounded-lg bg-white border border-slate-200 shadow-sm flex items-center justify-center group-hover:scale-110 transition-transform overflow-hidden">
        <img 
          v-if="sat.isImage"
          :src="sat.icon" 
          class="size-6 object-contain"
          alt=""
        />
        <span 
          v-else
          class="material-symbols-outlined text-slate-600 text-lg"
        >{{ sat.icon }}</span>
      </div>
      <span class="text-[10px] text-text-tertiary mt-1 opacity-0 group-hover:opacity-100 transition-opacity whitespace-nowrap">
        {{ sat.name }}
      </span>
    </div>
    
    <!-- 主星应用 (中心) -->
    <div class="relative z-10 flex flex-col items-center">
      <div 
        class="size-16 rounded-2xl shadow-lg flex items-center justify-center transition-all duration-300 hover:scale-105 ring-2 ring-offset-2 overflow-hidden bg-white"
        :style="{ 
          backgroundColor: mainApp.isImage ? '#ffffff' : clusterColor + '15',
          borderColor: clusterColor,
          '--tw-ring-color': clusterColor 
        }"
        :class="'border-2'"
      >
        <img 
          v-if="mainApp.isImage"
          :src="mainApp.icon" 
          class="size-10 object-contain"
          alt=""
        />
        <span 
          v-else
          class="material-symbols-outlined text-3xl"
          :style="{ color: clusterColor }"
        >{{ mainApp.icon }}</span>
      </div>
      <span class="text-sm font-semibold text-text-primary mt-2 text-center max-w-[120px] truncate">
        {{ mainApp.name }}
      </span>
    </div>
  </div>
</template>

<style scoped>
.cluster-galaxy {
  perspective: 1000px;
}
</style>
