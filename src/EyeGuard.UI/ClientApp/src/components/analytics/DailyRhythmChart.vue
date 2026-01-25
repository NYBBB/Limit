<script setup lang="ts">
import { use } from 'echarts/core'
import { CanvasRenderer } from 'echarts/renderers'
import { LineChart } from 'echarts/charts'
import {
  GridComponent,
  TooltipComponent,
  LegendComponent
} from 'echarts/components'
import VChart from 'vue-echarts'
import { computed } from 'vue'

use([
  CanvasRenderer,
  LineChart,
  GridComponent,
  TooltipComponent,
  LegendComponent
])

const props = defineProps<{
  data?: { hour: number; value: number }[]
}>()

// 判断是否有数据
const hasData = computed(() => props.data && props.data.length > 0)

const chartData = computed(() => {
  if (!hasData.value) return { hours: [], values: [] }
  return {
    hours: props.data!.map(d => d.hour),
    values: props.data!.map(d => d.value)
  }
})

const option = computed(() => ({
  backgroundColor: 'transparent',
  tooltip: {
    trigger: 'axis',
    formatter: (params: any[]) => {
      const p = params[0]
      const h = Math.floor(p.axisValue)
      const m = (p.axisValue % 1) * 60
      const timeStr = `${h}:${m === 0 ? '00' : '30'}`
      return `${timeStr} : ${Math.round(p.value)}% Fatigue`
    }
  },
  grid: {
    top: '10%',
    left: '3%',
    right: '4%',
    bottom: '5%',
    containLabel: true
  },
  xAxis: {
    type: 'category',
    boundaryGap: false,
    data: chartData.value.hours,
    axisLine: { show: false },
    axisTick: { show: false },
    axisLabel: {
      interval: 7,
      color: '#94a3b8',
      formatter: (val: number) => `${Math.floor(val)}:00`
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
        color: '#e2e8f0',
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
      name: 'Rhythm',
      type: 'line',
      smooth: true,
      lineStyle: {
        width: 2,
        color: '#8b5cf6'
      },
      showSymbol: false,
      data: chartData.value.values
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
          <span class="material-symbols-outlined text-4xl mb-2 block opacity-50">waves</span>
          <p>暂无节奏数据</p>
          <p class="text-xs opacity-75 mt-1">疲劳节奏数据将自动收集</p>
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
