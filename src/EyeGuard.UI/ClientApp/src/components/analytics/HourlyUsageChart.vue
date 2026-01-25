<script setup lang="ts">
import { use } from 'echarts/core'
import { CanvasRenderer } from 'echarts/renderers'
import { BarChart } from 'echarts/charts'
import {
  GridComponent,
  TooltipComponent,
  LegendComponent
} from 'echarts/components'
import VChart from 'vue-echarts'
import { computed } from 'vue'

use([
  CanvasRenderer,
  BarChart,
  GridComponent,
  TooltipComponent,
  LegendComponent
])

const props = defineProps<{
  data?: {
    appName: string
    color?: string
    hourlyData: number[] // 24小时数据 (0-23)
  }[]
}>()

// 判断是否有数据
const hasData = computed(() => props.data && props.data.length > 0)

const chartSeries = computed(() => {
  if (!hasData.value) return []
  
  return props.data!.map(item => ({
    name: item.appName,
    type: 'bar',
    stack: 'total',
    emphasis: { focus: 'series' },
    data: item.hourlyData,
    itemStyle: {
      color: item.color
    },
    barMaxWidth: 20
  }))
})

const option = computed(() => ({
  backgroundColor: 'transparent',
  tooltip: {
    trigger: 'axis',
    axisPointer: { type: 'shadow' },
    formatter: (params: any[]) => {
      let result = `<div class="font-bold mb-1">${params[0].name}:00</div>`
      let total = 0
      params.forEach(param => {
        if (param.value > 0) {
          const color = param.color
          result += `<div class="flex items-center gap-2 text-xs">
            <span class="w-2 h-2 rounded-full" style="background:${color}"></span>
            <span class="flex-1">${param.seriesName}</span>
            <span class="font-mono">${Math.round(param.value)} min</span>
          </div>`
          total += param.value
        }
      })
      if (total > 0) {
        result += `<div class="mt-1 pt-1 border-t border-gray-600/20 text-xs font-bold flex justify-between">
          <span>Total</span>
          <span>${Math.round(total)} min</span>
        </div>`
      }
      return result
    }
  },
  legend: {
    bottom: 0,
    icon: 'circle',
    itemWidth: 8,
    itemHeight: 8,
    textStyle: { color: '#64748b' }
  },
  grid: {
    top: '10%',
    left: '3%',
    right: '4%',
    bottom: '10%',
    containLabel: true
  },
  xAxis: {
    type: 'category',
    data: Array.from({ length: 24 }, (_, i) => i),
    axisLine: { show: false },
    axisTick: { show: false },
    axisLabel: {
      interval: 3,
      color: '#94a3b8',
      formatter: '{value}:00'
    }
  },
  yAxis: {
    type: 'value',
    name: 'Minutes',
    nameTextStyle: { color: '#94a3b8', padding: [0, 0, 0, 20] },
    axisLine: { show: false },
    axisTick: { show: false },
    splitLine: {
      lineStyle: {
        color: '#e2e8f0',
        type: 'dashed'
      }
    },
    axisLabel: { color: '#94a3b8' }
  },
  series: chartSeries.value
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
          <span class="material-symbols-outlined text-4xl mb-2 block opacity-50">bar_chart</span>
          <p>暂无应用使用数据</p>
          <p class="text-xs opacity-75 mt-1">数据将随着您的应用使用自动收集</p>
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
