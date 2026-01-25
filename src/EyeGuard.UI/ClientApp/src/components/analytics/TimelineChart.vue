<script setup lang="ts">
import { use } from 'echarts/core'
import { CanvasRenderer } from 'echarts/renderers'
import { CustomChart } from 'echarts/charts'
import {
  GridComponent,
  TooltipComponent,
  DataZoomComponent
} from 'echarts/components'
import VChart from 'vue-echarts'
import { computed } from 'vue'
import * as echarts from 'echarts/core'

use([
  CanvasRenderer,
  CustomChart,
  GridComponent,
  TooltipComponent,
  DataZoomComponent
])

const props = defineProps<{
  data?: {
    name: string
    startTime: number // timestamp
    endTime: number // timestamp
    color: string
    category: number // 0, 1, 2... used for yAxis index
  }[]
}>()

// 判断是否有数据
const hasData = computed(() => props.data && props.data.length > 0)

const categories = ['Development', 'Research', 'Communication', 'Break']

// 计算时间范围
const now = new Date()
const todayStart = new Date(now.getFullYear(), now.getMonth(), now.getDate(), 8, 0, 0).getTime()

function renderItem(params: any, api: any) {
  const categoryIndex = api.value(0)
  const start = api.coord([api.value(1), categoryIndex])
  const end = api.coord([api.value(2), categoryIndex])
  const height = api.size([0, 1])[1] * 0.6
  
  const rectShape = echarts.graphic.clipRectByRect({
    x: start[0],
    y: start[1] - height / 2,
    width: end[0] - start[0],
    height: height
  }, {
    x: params.coordSys.x,
    y: params.coordSys.y,
    width: params.coordSys.width,
    height: params.coordSys.height
  })
  
  return rectShape && {
    type: 'rect',
    transition: ['shape'],
    shape: rectShape,
    style: api.style()
  }
}

const option = computed(() => ({
  backgroundColor: 'transparent',
  tooltip: {
    formatter: (params: any) => {
      const start = new Date(params.value[1]).toLocaleTimeString([], {hour: '2-digit', minute:'2-digit'})
      const end = new Date(params.value[2]).toLocaleTimeString([], {hour: '2-digit', minute:'2-digit'})
      return `${params.name}<br/>${start} - ${end}`
    }
  },
  grid: {
    top: '10%',
    left: '3%',
    right: '4%',
    bottom: '15%',
    containLabel: true
  },
  dataZoom: [
    {
      type: 'slider',
      filterMode: 'weakFilter',
      showDataShadow: false,
      top: '90%',
      labelFormatter: ''
    },
    {
      type: 'inside',
      filterMode: 'weakFilter'
    }
  ],
  xAxis: {
    type: 'time',
    min: todayStart,
    max: todayStart + 12 * 3600000,
    axisLabel: {
      formatter: (val: number) => {
        const d = new Date(val)
        return `${d.getHours()}:${d.getMinutes().toString().padStart(2, '0')}`
      },
      color: '#94a3b8'
    },
    axisLine: { show: false },
    splitLine: {
      show: true,
      lineStyle: { color: '#e2e8f0', type: 'dashed' }
    }
  },
  yAxis: {
    type: 'category',
    data: categories,
    axisLine: { show: false },
    axisTick: { show: false },
    axisLabel: { color: '#64748b' }
  },
  series: [
    {
      type: 'custom',
      renderItem: renderItem,
      itemStyle: {
        opacity: 0.8,
        borderRadius: 4
      },
      encode: {
        x: [1, 2],
        y: 0
      },
      data: hasData.value ? props.data!.map(item => ({
        name: item.name,
        value: [item.category, item.startTime, item.endTime],
        itemStyle: { color: item.color }
      })) : []
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
          <span class="material-symbols-outlined text-4xl mb-2 block opacity-50">view_timeline</span>
          <p>暂无时间线数据</p>
          <p class="text-xs opacity-75 mt-1">应用切换记录将自动收集</p>
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
