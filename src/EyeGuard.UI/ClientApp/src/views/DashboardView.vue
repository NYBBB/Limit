<script setup lang="ts">
import { ref, onMounted, onUnmounted, watch, computed } from 'vue'
import FatigueRing from '../components/dashboard/FatigueRing.vue'
import ClusterGalaxy from '../components/dashboard/ClusterGalaxy.vue'
import FocusCommitmentModal from '../components/dashboard/FocusCommitmentModal.vue'
import { bridge, MessageTypes } from '../bridge'

// 疲劳数据状态
const fatigueData = ref({
  value: 0,
  status: 'FRESH',
  isCareMode: false
})

// 专注会话状态 (独立状态，避免被 Fatigue Update 覆盖)
const focusSession = ref({
  isActive: false,
  totalSeconds: 0,
  remainingSeconds: 0,
  taskName: ''
})

// Focus Commit Modal 状态
const showFocusModal = ref(false)

// 处理 Focus/Chill 切换点击
const handleToggleFocus = () => {
  if (zoneBData.value.isFocusMode) {
    // 已经在 Focus 模式 -> 切换回 Chill (停止专注)
    bridge.send(MessageTypes.STOP_FOCUS_COMMITMENT)
  } else {
    // 在 Chill 模式 -> 打开弹窗开始专注
    showFocusModal.value = true
  }
}

const onStartFocus = (duration: number, taskName: string) => {
  showFocusModal.value = false
  bridge.send(MessageTypes.START_FOCUS_COMMITMENT, { 
    durationMinutes: duration, 
    taskName 
  })
}

// Zone B 数据 (Cluster Galaxy)
const zoneBData = ref({
  insight: { icon: '💻', text: '正常工作中' },
  mainApp: { name: 'Unknown', processName: '', icon: 'apps', color: '#64748b' },
  satellites: [] as Array<{ name: string, processName: string, icon: string }>,
  clusterName: 'Unclassified',
  clusterColor: '#64748b',
  sessionSeconds: 0,
  isFocusMode: false
})

// 会话时间格式化
const sessionTimeFormatted = computed(() => {
  const seconds = zoneBData.value.sessionSeconds
  const h = Math.floor(seconds / 3600)
  const m = Math.floor((seconds % 3600) / 60)
  const s = seconds % 60
  return `${h.toString().padStart(2, '0')}:${m.toString().padStart(2, '0')}:${s.toString().padStart(2, '0')}`
})

// 消耗排行数据
const drainersData = ref<{name: string, impact: number, color: string}[]>([])
const todayStats = ref({
  totalMinutes: 0,
  fragmentation: 0
})

// Debug 面板状态
const showDebug = ref(false)
const debugStatus = ref({
  state: 'Unknown',
  stateDescription: '未知状态',
  idleSeconds: 0,
  audioPlaying: false,
  isFullscreen: false,
  isPassiveConsumption: false,
  fatigue: 0,
  fatigueSlope: 0,
  sensitivityBias: 0,
  isCareMode: false,
  isFlowMode: false,
  isRefocusing: false,
  currentProcessName: '',
  todayActiveMinutes: 0,
  currentSessionMinutes: 0,
  longestSessionMinutes: 0,
  fragmentationCount: 0
})

// 数据订阅清理函数
let cleanupFatigue: (() => void) | null = null
let cleanupDebug: (() => void) | null = null
let debugTimer: any = null

onMounted(() => {
  // 订阅 Zone B 数据更新 (Cluster Galaxy)
  bridge.on(MessageTypes.ZONE_B_UPDATE, (data: any) => {
    // console.log('[Dashboard] ZONE_B_UPDATE:', data)
    zoneBData.value = {
      insight: data.insight || { icon: '💻', text: '正常工作中' },
      mainApp: data.mainApp || { name: 'Unknown', processName: '', icon: 'apps', color: '#64748b' },
      satellites: data.satellites || [],
      clusterName: data.clusterName || 'Unclassified',
      clusterColor: data.clusterColor || '#64748b',
      sessionSeconds: data.sessionSeconds || 0,
      isFocusMode: data.isFocusMode || false
    }

    // 更新 Focus 倒计时数据
    if (data.focusCommitment) {
      focusSession.value = {
        isActive: true,
        totalSeconds: data.focusCommitment.totalSeconds,
        remainingSeconds: data.focusCommitment.remainingSeconds,
        taskName: data.focusCommitment.taskName
      }
    } else {
      focusSession.value = {
        isActive: false,
        totalSeconds: 0,
        remainingSeconds: 0,
        taskName: ''
      }
    }
  })

  // 订阅消耗排行更新
  bridge.on(MessageTypes.DRAINERS_UPDATE, (data: any) => {
    console.log('[Dashboard] DRAINERS_UPDATE:', data)
    drainersData.value = data.items || []
    todayStats.value = {
      totalMinutes: data.totalDuration,
      fragmentation: data.fragmentationCount
    }
  })

  // 订阅疲劳更新
  cleanupFatigue = bridge.on(MessageTypes.FATIGUE_UPDATE, (data: any) => {
    console.log('[Dashboard] FATIGUE_UPDATE:', data)
    fatigueData.value = {
      value: data.fatigueValue,
      status: data.state,
      isCareMode: data.isCareMode
    }
  })

  // 订阅 Debug 状态更新
  cleanupDebug = bridge.on(MessageTypes.DEBUG_STATUS_UPDATE, (data: any) => {
    debugStatus.value = data
  })

  // 请求初始数据
  console.log('[Dashboard] 发送 REQUEST_REFRESH')
  bridge.send('REQUEST_REFRESH')
})

// Debug 定时器控制: 仅当面板显示时才轮询
watch(() => showDebug.value, (visible) => {
  if (visible) {
    // 立即请求一次
    bridge.send(MessageTypes.REQUEST_DEBUG_STATUS)
    // 每秒轮询
    debugTimer = setInterval(() => {
      bridge.send(MessageTypes.REQUEST_DEBUG_STATUS)
    }, 1000)
  } else {
    if (debugTimer) {
      clearInterval(debugTimer)
      debugTimer = null
    }
  }
})

// 设置疲劳值（调试用）
function setFatigueValue(value: string) {
  const numValue = parseFloat(value)
  if (!isNaN(numValue)) {
    bridge.send(MessageTypes.SET_FATIGUE_VALUE, { value: numValue })
  }
}

onUnmounted(() => {
  if (cleanupFatigue) cleanupFatigue()
  if (cleanupDebug) cleanupDebug()
  if (debugTimer) clearInterval(debugTimer)
})
</script>

<template>
  <div class="grid grid-cols-1 lg:grid-cols-12 gap-6 h-full relative">
    <!-- Zone A: 精力反应堆 (Left Column) -->
    <div class="lg:col-span-5 flex flex-col">
      <div class="card-base flex-1 flex items-center justify-center p-8 relative overflow-hidden group hover:border-primary/20 transition-colors">
        <!-- 装饰背景 -->
        <div class="absolute top-0 right-0 p-12 opacity-5 pointer-events-none group-hover:opacity-10 transition-opacity duration-700">
          <span class="material-symbols-outlined text-[200px] text-primary">bolt</span>
        </div>
        
        <FatigueRing 
          :value="fatigueData.value" 
          :status="fatigueData.status"
          :is-care-mode="fatigueData.isCareMode"
          :focus-total-seconds="focusSession.totalSeconds"
          :focus-remaining-seconds="focusSession.remainingSeconds"
        />
      </div>
    </div>

    <!-- Right Column -->
    <div class="lg:col-span-7 flex flex-col gap-6">
      <!-- Zone B: Cluster Galaxy (工作流星系) -->
      <div class="card-base p-5 flex flex-col h-[280px] hover:border-primary/30 transition-colors overflow-hidden">
        <!-- Insight Header (实时洞察) -->
        <div class="flex items-center gap-2 mb-3">
          <span class="text-xl">{{ zoneBData.insight.icon }}</span>
          <span class="text-sm font-medium text-text-secondary truncate">{{ zoneBData.insight.text }}</span>
        </div>
        
        <!-- Cluster Galaxy (工作流星系) -->
        <div class="flex-1 flex items-center justify-center">
          <ClusterGalaxy 
            :main-app="zoneBData.mainApp"
            :satellites="zoneBData.satellites"
            :cluster-color="zoneBData.clusterColor"
          />
        </div>
        
        <!-- Control Deck (控制甲板) -->
        <div class="flex justify-between items-center pt-3 border-t border-slate-100">
          <!-- Session Timer -->
          <div class="flex items-center gap-2">
            <span class="material-symbols-outlined text-text-tertiary text-lg">timer</span>
            <span class="text-2xl font-display font-bold text-text-primary tracking-tight">
              {{ sessionTimeFormatted }}
            </span>
          </div>
          
          <!-- Mode Switch -->
          <button 
            @click="handleToggleFocus"
            class="flex items-center gap-1.5 px-4 py-2 rounded-full text-sm font-semibold transition-all"
            :class="zoneBData.isFocusMode 
              ? 'bg-primary/10 text-primary border border-primary/30' 
              : 'bg-slate-100 text-slate-600 hover:bg-slate-200'"
          >
            <span>{{ zoneBData.isFocusMode ? '⚡' : '🧘' }}</span>
            <span>{{ zoneBData.isFocusMode ? 'Focus' : 'Chill' }}</span>
          </button>
        </div>
      </div>

      <!-- Zone C: 消耗排行 (Bottom Right) -->
      <div class="card-base flex-1 p-6 lg:p-8 hover:border-primary/30 transition-colors">
        <div class="flex items-center justify-between mb-6">
          <h3 class="font-bold text-lg text-text-primary flex items-center gap-2">
            <span class="material-symbols-outlined text-primary">bolt</span>
            Top Drainers
          </h3>
          <button class="btn-ghost text-xs font-bold uppercase">View All</button>
        </div>

        <div class="space-y-5">
          <div 
            v-for="(item, index) in drainersData" 
            :key="item.name"
            class="group"
          >
            <div class="flex justify-between text-sm mb-2">
              <span class="font-medium text-text-primary flex items-center gap-2">
                <span class="size-2 rounded-full shadow-[0_0_8px_rgba(0,0,0,0.1)]" :style="{ backgroundColor: item.color }"></span>
                {{ item.name }}
              </span>
              <span class="font-bold" :class="index === 0 ? 'text-primary' : 'text-text-tertiary'">{{ item.impact }}% Impact</span>
            </div>
            <div class="w-full bg-slate-100 rounded-full h-2 overflow-hidden">
              <div 
                class="h-full rounded-full shadow-sm transition-all duration-1000 ease-out"
                :style="{ width: `${item.impact}%`, backgroundColor: item.color }"
              ></div>
            </div>
          </div>
          
          <div v-if="drainersData.length === 0" class="text-center text-text-tertiary text-sm py-4 italic">
            Gathering usage data...
          </div>
        </div>
      </div>
    </div>

    <!-- Modals -->
    <FocusCommitmentModal 
      :show="showFocusModal" 
      @close="showFocusModal = false"
      @start="onStartFocus"
    />

    <!-- Debug 面板开关按钮 -->
    <button 
      @click="showDebug = !showDebug"
      class="fixed bottom-4 right-4 z-50 size-12 bg-amber-100 hover:bg-amber-200 border border-amber-300 rounded-full flex items-center justify-center text-amber-700 shadow-lg transition-all hover:scale-110"
      :class="{ 'ring-2 ring-amber-400': showDebug }"
      title="Toggle Debug Panel"
    >
      <span class="material-symbols-outlined">bug_report</span>
    </button>

    <!-- Debug 面板 (可折叠) -->
    <Transition name="slide">
      <div 
        v-if="showDebug"
        class="fixed bottom-20 right-4 z-40 w-[500px] max-h-[60vh] overflow-y-auto bg-white border border-amber-300 rounded-xl shadow-2xl p-4"
      >
        <h3 class="text-lg font-bold mb-4 flex items-center gap-2 text-amber-700">
          <span class="material-symbols-outlined">bug_report</span>
          系统状态 (Debug)
        </h3>
        <div class="grid grid-cols-2 gap-3 text-sm font-mono">
          <div class="p-2 bg-amber-50 rounded border border-amber-100">
            <div class="text-text-tertiary text-xs mb-1">状态</div>
            <div class="font-bold">{{ debugStatus.stateDescription || debugStatus.state }}</div>
          </div>
          <div class="p-2 bg-amber-50 rounded border border-amber-100">
            <div class="text-text-tertiary text-xs mb-1">空闲秒数</div>
            <div class="font-bold">{{ typeof debugStatus.idleSeconds === 'number' ? debugStatus.idleSeconds.toFixed(1) : 0 }}s</div>
          </div>
          <div class="p-2 bg-amber-50 rounded border border-amber-100">
            <div class="text-text-tertiary text-xs mb-1">音频播放</div>
            <div :class="{'text-green-600': debugStatus.audioPlaying}" class="font-bold">{{ debugStatus.audioPlaying ? '是' : '否' }}</div>
          </div>
          <div class="p-2 bg-amber-50 rounded border border-amber-100">
            <div class="text-text-tertiary text-xs mb-1">全屏</div>
            <div class="font-bold">{{ debugStatus.isFullscreen ? '是' : '否' }}</div>
          </div>
          <div class="p-2 bg-amber-50 rounded border border-amber-100">
            <div class="text-text-tertiary text-xs mb-1">被动消耗</div>
            <div class="font-bold">{{ debugStatus.isPassiveConsumption ? '是' : '否' }}</div>
          </div>
          
          <!-- 疲劳值设置区域（占满一行） -->
          <div class="col-span-2 p-3 bg-red-50 rounded border border-red-200">
            <div class="text-text-tertiary text-xs mb-2 flex justify-between">
              <span>疲劳值（可调节）</span>
              <span class="font-bold text-primary">{{ typeof debugStatus.fatigue === 'number' ? debugStatus.fatigue.toFixed(1) : 0 }}%</span>
            </div>
            <input 
              type="range" 
              min="0" 
              max="100" 
              step="1"
              :value="debugStatus.fatigue"
              @input="setFatigueValue(($event.target as HTMLInputElement).value)"
              class="w-full h-2 bg-gradient-to-r from-cyan-400 via-amber-400 to-red-500 rounded-lg appearance-none cursor-pointer"
            />
            <div class="flex justify-between text-xs text-text-tertiary mt-1">
              <span>0%</span>
              <span>50%</span>
              <span>100%</span>
            </div>
          </div>
          
          <div class="p-2 bg-amber-50 rounded border border-amber-100">
            <div class="text-text-tertiary text-xs mb-1">疲劳斜率</div>
            <div class="font-bold">{{ typeof debugStatus.fatigueSlope === 'number' ? debugStatus.fatigueSlope.toFixed(4) : 0 }}/min</div>
          </div>
          <div class="p-2 bg-amber-50 rounded border border-amber-100">
            <div class="text-text-tertiary text-xs mb-1">敏感度偏差</div>
            <div class="font-bold">{{ typeof debugStatus.sensitivityBias === 'number' ? (debugStatus.sensitivityBias * 100).toFixed(0) : 0 }}%</div>
          </div>
          <div class="p-2 bg-amber-50 rounded border border-amber-100">
            <div class="text-text-tertiary text-xs mb-1">关怀模式</div>
            <div :class="{'text-amber-600': debugStatus.isCareMode}" class="font-bold">{{ debugStatus.isCareMode ? '开启' : '关闭' }}</div>
          </div>
          <div class="p-2 bg-amber-50 rounded border border-amber-100">
            <div class="text-text-tertiary text-xs mb-1">心流模式</div>
            <div :class="{'text-blue-600': debugStatus.isFlowMode}" class="font-bold">{{ debugStatus.isFlowMode ? '是' : '否' }}</div>
          </div>
          <div class="p-2 bg-amber-50 rounded border border-amber-100">
            <div class="text-text-tertiary text-xs mb-1">重聚焦中</div>
            <div :class="{'text-orange-500': debugStatus.isRefocusing}" class="font-bold">{{ debugStatus.isRefocusing ? '是' : '否' }}</div>
          </div>
          <div class="p-2 bg-amber-50 rounded border border-amber-100">
            <div class="text-text-tertiary text-xs mb-1">当前进程</div>
            <div class="font-bold truncate" :title="debugStatus.currentProcessName">{{ debugStatus.currentProcessName || '未知' }}</div>
          </div>
          <div class="p-2 bg-amber-50 rounded border border-amber-100">
            <div class="text-text-tertiary text-xs mb-1">今日活跃</div>
            <div class="font-bold">{{ debugStatus.todayActiveMinutes }} 分钟</div>
          </div>
          <div class="p-2 bg-amber-50 rounded border border-amber-100">
            <div class="text-text-tertiary text-xs mb-1">当前会话</div>
            <div class="font-bold">{{ debugStatus.currentSessionMinutes }} 分钟</div>
          </div>
          <div class="p-2 bg-amber-50 rounded border border-amber-100">
            <div class="text-text-tertiary text-xs mb-1">最长会话</div>
            <div class="font-bold">{{ debugStatus.longestSessionMinutes }} 分钟</div>
          </div>
          <div class="p-2 bg-amber-50 rounded border border-amber-100">
            <div class="text-text-tertiary text-xs mb-1">碎片化次数</div>
            <div class="font-bold">{{ debugStatus.fragmentationCount }}</div>
          </div>
        </div>
      </div>
    </Transition>
  </div>
</template>

<style scoped>
.slide-enter-active,
.slide-leave-active {
  transition: all 0.3s ease;
}
.slide-enter-from,
.slide-leave-to {
  opacity: 0;
  transform: translateY(20px);
}
</style>
