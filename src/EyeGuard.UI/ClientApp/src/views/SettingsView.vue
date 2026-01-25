<script setup lang="ts">
import { ref, onMounted, onUnmounted, watch, computed } from 'vue'
import { bridge, MessageTypes } from '../bridge'
import ClusterEditor from '../components/settings/ClusterEditor.vue'

interface Settings {
  // 疲劳度设置
  softReminderThreshold: number
  forceBreakThreshold: number
  idleThresholdSeconds: number
  
  // 检测方式
  enableKeyboardMonitor: boolean
  enableAudioMonitor: boolean
  
  // 疲劳敏感度
  careSensitivity: number
  
  // 干预策略 (0-Polite, 1-Balanced, 2-Force)
  interventionMode: number
  
  // 提醒设置
  enableReminders: boolean
  reminderType: number  // 0-全屏弹窗, 1-通知横幅
  
  // 高级设置
  showTrayIcon: boolean
  autoStart: boolean
  snapshotInterval: number
  chartInterval: number
  refreshInterval: number
}

const defaultSettings: Settings = {
  softReminderThreshold: 40,
  forceBreakThreshold: 80,
  idleThresholdSeconds: 60,
  enableKeyboardMonitor: true,
  enableAudioMonitor: true,
  careSensitivity: 50,
  interventionMode: 1,
  enableReminders: true,
  reminderType: 0,
  showTrayIcon: true,
  autoStart: false,
  snapshotInterval: 60,
  chartInterval: 5,
  refreshInterval: 60
}

const settings = ref<Settings>({ ...defaultSettings })
const isLoading = ref(true)
const saveStatus = ref('')
const clusterEditorRef = ref<any>(null)
let saveTimeout: any = null

// 敏感度描述
const sensitivityDescription = computed(() => {
  const val = settings.value.careSensitivity
  if (val <= 25) return '低敏感度：疲劳增长较慢，适合耐久型用户'
  if (val <= 50) return '中等敏感度：疲劳增长速率为标准值'
  if (val <= 75) return '高敏感度：疲劳增长较快，适合易疲劳用户'
  return '极高敏感度：疲劳增长非常快，建议只在需要时使用'
})

function saveSettings() {
  bridge.send(MessageTypes.SAVE_SETTINGS, settings.value)
  
  saveStatus.value = '✓ 设置已保存'
  if (saveTimeout) clearTimeout(saveTimeout)
  saveTimeout = setTimeout(() => {
    saveStatus.value = ''
  }, 3000)
}

function resetToDefault() {
  if (confirm('确定要恢复所有默认设置吗？')) {
    settings.value = { ...defaultSettings }
    saveSettings()
  }
}

// 防抖保存
let debounceTimer: any = null
watch(settings, () => {
  if (isLoading.value) return
  
  if (debounceTimer) clearTimeout(debounceTimer)
  debounceTimer = setTimeout(() => {
    saveSettings()
  }, 1000)
}, { deep: true })

onMounted(() => {
  bridge.send(MessageTypes.REQUEST_SETTINGS)
  
  bridge.on(MessageTypes.SETTINGS_LOADED, (data: any) => {
    isLoading.value = true
    settings.value = { ...defaultSettings, ...data }
    setTimeout(() => { isLoading.value = false }, 100)
  })
})

onUnmounted(() => {
  if (debounceTimer) clearTimeout(debounceTimer)
  if (saveTimeout) clearTimeout(saveTimeout)
})
</script>

<template>
  <div class="h-full overflow-y-auto p-6 flex flex-col gap-8">
    
    <!-- Header -->
    <header class="flex items-center justify-between">
      <div>
        <h1 class="text-2xl font-display font-bold text-text-primary">偏好设置</h1>
        <p class="text-text-tertiary text-sm">配置 EyeGuard 以适应您的工作流程</p>
      </div>
      <div v-if="saveStatus" class="bg-green-50 text-green-600 px-4 py-2 rounded-lg text-sm font-medium">
        {{ saveStatus }}
      </div>
    </header>

    <div v-if="isLoading" class="flex items-center justify-center py-20">
      <div class="animate-spin rounded-full h-8 w-8 border-b-2 border-primary"></div>
    </div>

    <!-- Content -->
    <div v-else class="flex flex-col gap-8 max-w-3xl pb-20">
      
      <!-- ========== 疲劳度设置 ========== -->
      <section class="space-y-6">
        <h2 class="text-lg font-bold border-b border-border-default pb-2 flex items-center gap-2">
          <span class="material-symbols-outlined text-primary">psychology</span>
          疲劳度设置
        </h2>
        
        <!-- 休息提醒阈值 -->
        <div class="card-base p-6 space-y-6">
          <div class="flex items-center gap-2">
            <span class="font-bold">休息提醒阈值</span>
            <span class="material-symbols-outlined text-text-tertiary text-sm cursor-help" 
                  title="当疲劳度达到此值时，系统会弹出休息提醒。">info</span>
          </div>
          
          <!-- 轻度提醒 -->
          <div class="space-y-2">
            <div class="flex justify-between">
              <span class="text-sm text-text-secondary">轻度提醒</span>
              <span class="font-mono font-bold text-primary">{{ settings.softReminderThreshold }}%</span>
            </div>
            <input type="range" v-model.number="settings.softReminderThreshold" min="20" max="80" step="5" 
                   class="w-full h-2 bg-surface-200 rounded-lg appearance-none cursor-pointer accent-primary">
          </div>
          
          <!-- 强制休息 -->
          <div class="space-y-2">
            <div class="flex justify-between">
              <span class="text-sm text-text-secondary">强制休息</span>
              <span class="font-mono font-bold text-red-500">{{ settings.forceBreakThreshold }}%</span>
            </div>
            <input type="range" v-model.number="settings.forceBreakThreshold" min="50" max="100" step="5" 
                   class="w-full h-2 bg-surface-200 rounded-lg appearance-none cursor-pointer accent-red-500">
          </div>
        </div>
      </section>

      <!-- ========== 检测方式 ========== -->
      <section class="space-y-6">
        <h2 class="text-lg font-bold border-b border-border-default pb-2 flex items-center gap-2">
          <span class="material-symbols-outlined text-primary">sensors</span>
          检测方式
        </h2>
        
        <div class="card-base p-6 divide-y divide-border-default">
          <!-- 鼠标（始终开启） -->
          <div class="flex items-center justify-between py-4">
            <div class="flex items-center gap-2">
              <span class="text-text-primary">鼠标/触摸板活动</span>
              <span class="material-symbols-outlined text-text-tertiary text-sm cursor-help" 
                    title="检测鼠标点击和滚轮操作。此为核心检测方式，始终启用。">info</span>
            </div>
            <span class="material-symbols-outlined text-primary">check_circle</span>
          </div>
          
          <!-- 键盘 -->
          <div class="flex items-center justify-between py-4">
            <div class="flex items-center gap-2">
              <span class="text-text-primary">键盘活动</span>
              <span class="material-symbols-outlined text-text-tertiary text-sm cursor-help" 
                    title="检测键盘按键。只记录是否有按键，不记录具体内容。">info</span>
            </div>
            <label class="relative inline-flex items-center cursor-pointer">
              <input type="checkbox" v-model="settings.enableKeyboardMonitor" class="sr-only peer">
              <div class="w-11 h-6 bg-surface-200 peer-focus:outline-none rounded-full peer peer-checked:after:translate-x-full peer-checked:after:border-white after:content-[''] after:absolute after:top-[2px] after:left-[2px] after:bg-white after:border-gray-300 after:border after:rounded-full after:h-5 after:w-5 after:transition-all peer-checked:bg-primary"></div>
            </label>
          </div>
          
          <!-- 音频 -->
          <div class="flex items-center justify-between py-4">
            <div class="flex items-center gap-2">
              <span class="text-text-primary">音频播放检测</span>
              <span class="material-symbols-outlined text-text-tertiary text-sm cursor-help" 
                    title="检测系统是否有音频输出。有音频时会进入「媒体模式」。">info</span>
            </div>
            <label class="relative inline-flex items-center cursor-pointer">
              <input type="checkbox" v-model="settings.enableAudioMonitor" class="sr-only peer">
              <div class="w-11 h-6 bg-surface-200 peer-focus:outline-none rounded-full peer peer-checked:after:translate-x-full peer-checked:after:border-white after:content-[''] after:absolute after:top-[2px] after:left-[2px] after:bg-white after:border-gray-300 after:border after:rounded-full after:h-5 after:w-5 after:transition-all peer-checked:bg-primary"></div>
            </label>
          </div>
          
          <!-- 空闲判定时间 -->
          <div class="flex items-center justify-between py-4">
            <div class="flex items-center gap-2">
              <span class="text-text-primary">空闲判定时间</span>
              <span class="material-symbols-outlined text-text-tertiary text-sm cursor-help" 
                    title="无键鼠活动超过此时间后，判定为用户空闲。">info</span>
            </div>
            <div class="flex items-center gap-2">
              <input type="number" v-model.number="settings.idleThresholdSeconds" min="30" max="300" step="10"
                     class="w-20 px-3 py-1 border border-border-default rounded-lg text-center font-mono">
              <span class="text-text-tertiary">秒</span>
            </div>
          </div>
        </div>
      </section>

      <!-- ========== 应用分类 ========== -->
      <section class="space-y-6">
        <div class="flex items-center justify-between border-b border-border-default pb-2">
          <h2 class="text-lg font-bold flex items-center gap-2">
            <span class="material-symbols-outlined text-primary">category</span>
            应用分类
          </h2>
        </div>
        
        <div class="card-base p-6 space-y-4">
          <div class="flex items-center gap-2 mb-2">
            <span class="material-symbols-outlined text-text-tertiary">info</span>
            <span class="text-sm text-text-secondary">拖放应用到不同分类，系统会根据分类自动判断您的工作状态 (Focus / Flow)</span>
          </div>
          
          <ClusterEditor ref="clusterEditorRef" />
        </div>
      </section>

      <!-- ========== 疲劳敏感度 ========== -->
      <section class="space-y-6">
        <h2 class="text-lg font-bold border-b border-border-default pb-2 flex items-center gap-2">
          <span class="material-symbols-outlined text-primary">tune</span>
          疲劳敏感度
        </h2>
        
        <div class="card-base p-6 space-y-4">
          <div class="flex items-center gap-2">
            <span class="font-bold">Care Mode 敏感度</span>
            <span class="material-symbols-outlined text-text-tertiary text-sm cursor-help" 
                  title="调节疲劳增长速率。更高的敏感度意味着疲劳更快累积。">info</span>
          </div>
          
          <div class="flex items-center gap-4">
            <span class="text-sm text-text-tertiary">低</span>
            <input type="range" v-model.number="settings.careSensitivity" min="0" max="100" step="25" 
                   class="flex-1 h-2 bg-surface-200 rounded-lg appearance-none cursor-pointer accent-primary">
            <span class="text-sm text-text-tertiary">高</span>
          </div>
          
          <div class="bg-surface-50 rounded-lg p-3 text-sm text-text-secondary flex items-start gap-2">
            <span class="material-symbols-outlined text-text-tertiary">info</span>
            {{ sensitivityDescription }}
          </div>
        </div>
      </section>

      <!-- ========== 干预策略 ========== -->
      <section class="space-y-6">
        <h2 class="text-lg font-bold border-b border-border-default pb-2 flex items-center gap-2">
          <span class="material-symbols-outlined text-primary">notifications_active</span>
          干预策略
        </h2>
        
        <div class="card-base p-6 space-y-4">
          <div class="flex items-center gap-2">
            <span class="font-bold">休息提醒强度</span>
            <span class="material-symbols-outlined text-text-tertiary text-sm cursor-help" 
                  title="选择系统如何提醒您休息。">info</span>
          </div>
          
          <div class="space-y-3">
            <label class="flex items-start gap-3 p-3 rounded-lg cursor-pointer hover:bg-surface-50 transition-colors"
                   :class="settings.interventionMode === 0 ? 'bg-primary/5 ring-1 ring-primary' : ''">
              <input type="radio" v-model.number="settings.interventionMode" :value="0" class="mt-1 accent-primary">
              <div>
                <div class="font-medium">😊 礼貌模式</div>
                <div class="text-sm text-text-tertiary">Toast 通知 + 可快速跳过的弹窗</div>
              </div>
            </label>
            
            <label class="flex items-start gap-3 p-3 rounded-lg cursor-pointer hover:bg-surface-50 transition-colors"
                   :class="settings.interventionMode === 1 ? 'bg-primary/5 ring-1 ring-primary' : ''">
              <input type="radio" v-model.number="settings.interventionMode" :value="1" class="mt-1 accent-primary">
              <div>
                <div class="font-medium">⚖️ 平衡模式</div>
                <div class="text-sm text-text-tertiary">全屏弹窗 + 需要点击按钮跳过</div>
              </div>
            </label>
            
            <label class="flex items-start gap-3 p-3 rounded-lg cursor-pointer hover:bg-surface-50 transition-colors"
                   :class="settings.interventionMode === 2 ? 'bg-primary/5 ring-1 ring-primary' : ''">
              <input type="radio" v-model.number="settings.interventionMode" :value="2" class="mt-1 accent-primary">
              <div>
                <div class="font-medium">💪 强制模式</div>
                <div class="text-sm text-text-tertiary">全屏弹窗 + 长按 3 秒才能跳过</div>
              </div>
            </label>
          </div>
        </div>
      </section>

      <!-- ========== 提醒设置 ========== -->
      <section class="space-y-6">
        <h2 class="text-lg font-bold border-b border-border-default pb-2 flex items-center gap-2">
          <span class="material-symbols-outlined text-primary">alarm</span>
          提醒设置
        </h2>
        
        <div class="card-base p-6 divide-y divide-border-default">
          <!-- 开启提醒 -->
          <div class="flex items-center justify-between py-4">
            <span class="text-text-primary">开启休息提醒</span>
            <label class="relative inline-flex items-center cursor-pointer">
              <input type="checkbox" v-model="settings.enableReminders" class="sr-only peer">
              <div class="w-11 h-6 bg-surface-200 peer-focus:outline-none rounded-full peer peer-checked:after:translate-x-full peer-checked:after:border-white after:content-[''] after:absolute after:top-[2px] after:left-[2px] after:bg-white after:border-gray-300 after:border after:rounded-full after:h-5 after:w-5 after:transition-all peer-checked:bg-primary"></div>
            </label>
          </div>
          
          <!-- 提醒方式 -->
          <div class="flex items-center justify-between py-4">
            <div class="flex items-center gap-2">
              <span class="text-text-primary">提醒方式</span>
              <span class="material-symbols-outlined text-text-tertiary text-sm cursor-help" 
                    title="全屏弹窗会覆盖整个屏幕；通知横幅只在角落显示。">info</span>
            </div>
            <select v-model.number="settings.reminderType" 
                    class="px-3 py-2 border border-border-default rounded-lg bg-white text-text-primary">
              <option :value="0">全屏弹窗</option>
              <option :value="1">通知横幅</option>
            </select>
          </div>
        </div>
      </section>

      <!-- ========== 高级设置 ========== -->
      <section class="space-y-6">
        <h2 class="text-lg font-bold border-b border-border-default pb-2 flex items-center gap-2">
          <span class="material-symbols-outlined text-primary">settings</span>
          高级设置
        </h2>
        
        <div class="card-base p-6 divide-y divide-border-default">
          <!-- 系统托盘 -->
          <div class="flex items-center justify-between py-4">
            <div>
              <div class="text-text-primary">显示系统托盘图标</div>
              <div class="text-sm text-text-tertiary">允许从托盘快速访问</div>
            </div>
            <label class="relative inline-flex items-center cursor-pointer">
              <input type="checkbox" v-model="settings.showTrayIcon" class="sr-only peer">
              <div class="w-11 h-6 bg-surface-200 peer-focus:outline-none rounded-full peer peer-checked:after:translate-x-full peer-checked:after:border-white after:content-[''] after:absolute after:top-[2px] after:left-[2px] after:bg-white after:border-gray-300 after:border after:rounded-full after:h-5 after:w-5 after:transition-all peer-checked:bg-primary"></div>
            </label>
          </div>
          
          <!-- 开机自启 -->
          <div class="flex items-center justify-between py-4">
            <div>
              <div class="text-text-primary">开机自动启动</div>
              <div class="text-sm text-text-tertiary">登录 Windows 后自动运行</div>
            </div>
            <label class="relative inline-flex items-center cursor-pointer">
              <input type="checkbox" v-model="settings.autoStart" class="sr-only peer">
              <div class="w-11 h-6 bg-surface-200 peer-focus:outline-none rounded-full peer peer-checked:after:translate-x-full peer-checked:after:border-white after:content-[''] after:absolute after:top-[2px] after:left-[2px] after:bg-white after:border-gray-300 after:border after:rounded-full after:h-5 after:w-5 after:transition-all peer-checked:bg-primary"></div>
            </label>
          </div>
          
          <!-- 数据保存间隔 -->
          <div class="py-4 space-y-3">
            <div class="flex justify-between">
              <div>
                <div class="text-text-primary">数据保存间隔</div>
                <div class="text-sm text-text-tertiary">后台保存数据的频率，用于断电恢复（影响所有数据）</div>
              </div>
              <div class="font-mono font-bold text-primary">{{ settings.snapshotInterval }}秒</div>
            </div>
            <input type="range" v-model.number="settings.snapshotInterval" min="30" max="180" step="30" 
                   class="w-full h-2 bg-surface-200 rounded-lg appearance-none cursor-pointer accent-primary">
          </div>
          
          <!-- 疲劳趋势快照频率 -->
          <div class="py-4 space-y-3">
            <div class="flex justify-between">
              <div>
                <div class="text-text-primary">疲劳趋势快照频率</div>
                <div class="text-sm text-text-tertiary">每隔多久保存一次疲劳值（用于图表绘制和恢复）</div>
              </div>
              <div class="font-mono font-bold text-primary">{{ settings.chartInterval }}分钟</div>
            </div>
            <input type="range" v-model.number="settings.chartInterval" min="1" max="15" step="1" 
                   class="w-full h-2 bg-surface-200 rounded-lg appearance-none cursor-pointer accent-primary">
          </div>
        </div>
      </section>

      <!-- ========== 关于 ========== -->
      <section class="space-y-4">
        <h2 class="text-lg font-bold border-b border-border-default pb-2 flex items-center gap-2">
          <span class="material-symbols-outlined text-primary">info</span>
          关于
        </h2>
        
        <div class="card-base p-6 space-y-4">
          <div>
            <div class="font-display font-bold text-xl">Limit <span class="text-sm font-normal text-text-tertiary ml-2">v3.0</span></div>
            <p class="text-text-tertiary text-sm mt-1">一款智能护眼与生产力工具</p>
          </div>
          <a href="https://github.com/your-repo" target="_blank" class="text-primary hover:underline text-sm flex items-center gap-1">
            <span class="material-symbols-outlined text-sm">open_in_new</span>
            在 GitHub 上查看源代码
          </a>
        </div>
      </section>

      <!-- 底部按钮 -->
      <div class="flex justify-between items-center pt-4 border-t border-border-default">
        <button @click="resetToDefault" class="btn-ghost">
          恢复所有默认设置
        </button>
        <button @click="saveSettings" class="btn-primary">
          保存设置
        </button>
      </div>

    </div>
  </div>
</template>

<style scoped>
@keyframes fade-in-up {
  from { opacity: 0; transform: translateY(10px); }
  to { opacity: 1; transform: translateY(0); }
}
.animate-fade-in-up {
  animation: fade-in-up 0.3s ease-out forwards;
}
</style>
