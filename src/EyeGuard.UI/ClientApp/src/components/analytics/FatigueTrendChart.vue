<script setup lang="ts">
import { use } from 'echarts/core'
import { CanvasRenderer } from 'echarts/renderers'
import { LineChart } from 'echarts/charts'
import {
  GridComponent,
  TooltipComponent,
  LegendComponent,
  MarkAreaComponent
} from 'echarts/components'
import VChart from 'vue-echarts'
import { computed } from 'vue'

use([
  CanvasRenderer,
  LineChart,
  GridComponent,
  TooltipComponent,
  LegendComponent,
  MarkAreaComponent
])

const props = defineProps<{
  data?: { hour: number; value: number }[]
}>()

// 判断是否有数据
const hasData = computed(() => props.data && props.data.length > 0)

const chartData = computed(() => {
  if (!hasData.value) return []
  return props.data!.map(d => [d.hour, d.value])
})

const formatTime = (h: number) => {
  const hour = Math.floor(h)
  const minute = Math.round((h - hour) * 60)
  return `${hour.toString().padStart(2, '0')}:${minute.toString().padStart(2, '0')}`
}

const option = computed(() => ({
  backgroundColor: 'transparent',
  tooltip: {
    trigger: 'axis',
    formatter: (params: any) => {
      if (!params || !params[0]) return ''
      const data = params[0].data
      const time = formatTime(data[0])
      return `${time} : ${data[1]}% Fatigue`
    },
    axisPointer: {
      type: 'line',
      lineStyle: {
        color: '#8b5cf6',
        width: 1,
        type: 'dashed'
      }
    }
  },
  grid: {
    top: '15%',
    left: '3%',
    right: '4%',
    bottom: '3%',
    containLabel: true
  },
  xAxis: {
    type: 'value',
    min: 0,
    max: 24,
    interval: 2, // 每2小时一个刻度
    splitLine: { show: false }, // 隐藏竖向网格线
    axisLabel: {
      color: '#94a3b8', // Slate-400
      formatter: (val: number) => `${val}:00` // 0:00, 2:00...
    }
  },
  yAxis: {
    type: 'value',
    max: 100,
    interval: 25,
    axisLine: { show: false },
    axisTick: { show: false },
    splitLine: {
      lineStyle: {
        color: '#e2e8f0', // Slate-200
        type: 'dashed'
      }
    },
    axisLabel: {
      color: '#94a3b8',
      formatter: '{value}%'
    }
  },
  series: [
    {
      name: 'Fatigue',
      type: 'line',
      smooth: true,
      lineStyle: {
        width: 3,
        color: '#8b5cf6' // Violet-500
      },
      showSymbol: false,
      areaStyle: {
        opacity: 0.8,
        color: {
          type: 'linear',
          x: 0,
          y: 0,
          x2: 0,
          y2: 1,
          colorStops: [
            { offset: 0, color: 'rgba(139, 92, 246, 0.4)' }, // Violet-500 alpha 0.4
            { offset: 1, color: 'rgba(139, 92, 246, 0.05)' }
          ]
        }
      },
      data: chartData.value
    }
  ]
}))
</script>

<template>
  <div class="w-full h-full">
    <template v-if="hasData">
      <v-chart class="chart" :option="option" autoresize />
    </template>
    <template v-else>
      <div class="w-full h-full flex items-center justify-center text-text-tertiary">
        <div class="text-center">
          <span class="material-symbols-outlined text-4xl mb-2 block opacity-50">show_chart</span>
          <p>暂无数据</p>
          <p class="text-xs opacity-75 mt-1">疲劳快照数据将在使用过程中自动收集</p>
        </div>
      </div>
    </template>
  </div>
</template>

<style scoped>
.chart {
  height: 100%;
  width: 100%;
}
</style>
