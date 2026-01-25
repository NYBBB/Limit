<script setup lang="ts">
import { use } from 'echarts/core'
import { CanvasRenderer } from 'echarts/renderers'
import { HeatmapChart } from 'echarts/charts'
import {
  GridComponent,
  TooltipComponent,
  VisualMapComponent
} from 'echarts/components'
import VChart from 'vue-echarts'
import { computed } from 'vue'

use([
  CanvasRenderer,
  HeatmapChart,
  GridComponent,
  TooltipComponent,
  VisualMapComponent
])

const props = defineProps<{
  data?: { dayIndex: number; hour: number; value: number }[]
  days?: string[]
}>()

const defaultDays = ['Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat', 'Sun']
const hours = Array.from({ length: 24 }, (_, i) => `${i}`)

// 判断是否有数据
const hasData = computed(() => props.data && props.data.length > 0)

const getChartData = computed(() => {
  if (!hasData.value) return []
  return props.data!.map(d => [d.hour, d.dayIndex, d.value])
})

const option = computed(() => ({
  backgroundColor: 'transparent',
  tooltip: {
    position: 'top',
    formatter: (params: any) => {
      const d = (props.days || defaultDays)[params.value[1]]
      const h = params.value[0]
      const v = params.value[2]
      return `${d} ${h}:00<br/>Fatigue: ${v}%`
    }
  },
  grid: {
    height: '70%',
    top: '10%'
  },
  xAxis: {
    type: 'category',
    data: hours,
    splitArea: {
      show: true
    },
    axisLine: { show: false },
    axisTick: { show: false },
    axisLabel: { color: '#94a3b8' }
  },
  yAxis: {
    type: 'category',
    data: props.days || defaultDays,
    splitArea: {
      show: true
    },
    axisLine: { show: false },
    axisTick: { show: false },
    axisLabel: { color: '#94a3b8' }
  },
  visualMap: {
    min: 0,
    max: 100,
    calculable: true,
    orient: 'horizontal',
    left: 'center',
    bottom: '0%',
    inRange: {
      color: ['#e0e7ff', '#818cf8', '#4f46e5', '#312e81']
    },
    textStyle: { color: '#94a3b8' }
  },
  series: [
    {
      name: 'Fatigue Heatmap',
      type: 'heatmap',
      data: getChartData.value,
      label: {
        show: false
      },
      itemStyle: {
        borderRadius: 4,
        borderColor: '#fff',
        borderWidth: 2
      },
      emphasis: {
        itemStyle: {
          shadowBlur: 10,
          shadowColor: 'rgba(0, 0, 0, 0.5)'
        }
      }
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
          <span class="material-symbols-outlined text-4xl mb-2 block opacity-50">grid_on</span>
          <p>暂无热力图数据</p>
          <p class="text-xs opacity-75 mt-1">需要至少使用一周才有数据</p>
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
