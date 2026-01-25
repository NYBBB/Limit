<script setup lang="ts">
import { use } from 'echarts/core'
import { CanvasRenderer } from 'echarts/renderers'
import { PieChart } from 'echarts/charts'
import {
  TooltipComponent,
  LegendComponent
} from 'echarts/components'
import VChart from 'vue-echarts'
import { computed } from 'vue'

use([
  CanvasRenderer,
  PieChart,
  TooltipComponent,
  LegendComponent
])

const props = defineProps<{
  data?: {
    name: string
    value: number
    color?: string
  }[]
}>()

// 判断是否有数据
const hasData = computed(() => props.data && props.data.length > 0)

const chartData = computed(() => {
  if (!hasData.value) return []
  return props.data!.map(item => ({
    ...item,
    itemStyle: item.color ? { color: item.color } : undefined
  }))
})

const totalMinutes = computed(() => chartData.value.reduce((acc, cur) => acc + cur.value, 0))

const option = computed(() => ({
  backgroundColor: 'transparent',
  tooltip: {
    trigger: 'item',
    formatter: (params: any) => {
      const percent = ((params.value / totalMinutes.value) * 100).toFixed(1)
      return `<div class="font-bold mb-1">${params.name}</div>
              <div class="text-xs">
                Time: ${params.value} min<br/>
                Share: ${percent}%
              </div>`
    }
  },
  legend: {
    bottom: 0,
    icon: 'circle',
    itemWidth: 8,
    itemHeight: 8,
    textStyle: { color: '#64748b' }
  },
  series: [
    {
      name: 'Energy Distribution',
      type: 'pie',
      radius: ['50%', '70%'],
      avoidLabelOverlap: false,
      itemStyle: {
        borderRadius: 5,
        borderColor: '#fff',
        borderWidth: 2
      },
      label: {
        show: false,
        position: 'center'
      },
      emphasis: {
        label: {
          show: true,
          fontSize: 20,
          fontWeight: 'bold',
          formatter: '{b}\n{d}%',
          color: '#1e293b'
        }
      },
      labelLine: {
        show: false
      },
      data: chartData.value
    }
  ]
}))
</script>

<template>
  <div class="w-full h-full relative">
    <template v-if="hasData">
      <v-chart class="chart" :option="option" autoresize />
      
      <!-- 中心总时间 (当不hover时显示) -->
      <div class="absolute inset-0 flex flex-col items-center justify-center pointer-events-none opacity-100 transition-opacity duration-300">
        <div class="text-3xl font-bold text-text-primary">{{ String(Math.round(totalMinutes / 60)).padStart(2, '0') }}<span class="text-sm text-text-tertiary">h</span></div>
        <div class="text-xs text-text-tertiary font-bold tracking-wider uppercase">Active</div>
      </div>
    </template>
    <template v-else>
      <div class="w-full h-full flex items-center justify-center text-text-tertiary">
        <div class="text-center">
          <span class="material-symbols-outlined text-4xl mb-2 block opacity-50">pie_chart</span>
          <p>暂无分类数据</p>
          <p class="text-xs opacity-75 mt-1">应用使用分类数据将自动收集</p>
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
