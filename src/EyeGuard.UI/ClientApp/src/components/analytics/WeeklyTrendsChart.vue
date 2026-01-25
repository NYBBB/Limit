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
    day: string
    peak: number
    average: number
  }[]
}>()

// 判断是否有数据
const hasData = computed(() => props.data && props.data.length > 0)

const option = computed(() => ({
  backgroundColor: 'transparent',
  tooltip: {
    trigger: 'axis',
    axisPointer: { type: 'shadow' }
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
    data: hasData.value ? props.data!.map(d => d.day) : [],
    axisLine: { show: false },
    axisTick: { show: false },
    axisLabel: {
      color: '#94a3b8'
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
      name: 'Peak Fatigue',
      type: 'bar',
      data: hasData.value ? props.data!.map(d => d.peak) : [],
      itemStyle: {
        color: '#f59e0b',
        borderRadius: [4, 4, 0, 0]
      },
      barGap: '20%',
      barCategoryGap: '40%'
    },
    {
      name: 'Avg Fatigue',
      type: 'bar',
      data: hasData.value ? props.data!.map(d => d.average) : [],
      itemStyle: {
        color: '#8b5cf6',
        borderRadius: [4, 4, 0, 0]
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
          <span class="material-symbols-outlined text-4xl mb-2 block opacity-50">calendar_month</span>
          <p>暂无周趋势数据</p>
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
