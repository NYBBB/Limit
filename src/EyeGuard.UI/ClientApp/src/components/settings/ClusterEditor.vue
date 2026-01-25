<script setup lang="ts">
import { ref, onMounted, computed } from 'vue'
import { bridge, MessageTypes } from '../../bridge'

interface AppItem {
  name: string
  processName: string
  icon: string
  isImage?: boolean
}

interface Cluster {
  id: number
  name: string
  color: string
  apps: AppItem[]
}

const unclassifiedApps = ref<AppItem[]>([])
const clusters = ref<Cluster[]>([])
const isLoading = ref(true)
const draggingApp = ref<AppItem | null>(null)
const sourceClusterId = ref<number | null>(null) // null means from unclassified

onMounted(() => {
  // 请求数据
  bridge.send(MessageTypes.REQUEST_CLUSTERS)
  bridge.send(MessageTypes.REQUEST_UNCLASSIFIED_APPS)

  // 监听 Cluster 数据 (已分类)
  bridge.on(MessageTypes.CLUSTERS_LOADED, (data: any) => {
    // 转换后端数据格式
    // 后端格式: { idStr: "1", name: "Coding", color: "#...", appList: ["Code.exe", ...] }
    // 我们需要将其转换为前端展示用的 Cluster 对象，这需要手动组装 apps
    // 但后端 CLUSTERS_LOADED 发送的数据并不包含详细的 app info (friendly name, icon)
    // 所以我们需要后端修改 SendClustersUpdate 逻辑，或者在前端 mapping
    // 为了简单，我们先假设后端能发包含基本信息的 app list
    
    // 纠正：查看 BridgeService.SendClustersUpdate，它只发送了 idStr, name, color, appList(string[])
    // 我们需要 processName 到 AppItem 的映射。
    // 临时方案：我们用 processName 去 unclassifiedApps 里找？不行，unclassifiedApps 只包含未分类的。
    // 方案：需要在前端自己生成 AppItem，图标暂时使用默认，等待后端优化 SendClustersUpdate
    // 或者，我们在 ClusterEditor 内部维护一个 icon cache?
    
    // 简化实现：先用简单 mapping
    clusters.value = data.map((c: any) => ({
      id: parseInt(c.id),
      name: c.name,
      color: c.color,
      apps: c.apps.map((processName: string) => ({
        name: processName.replace('.exe', ''), // 简单处理，实际上应该用 IconMapper，但前端没有
        processName: processName,
        icon: 'apps', // 默认图标
        isImage: false
      }))
    }))
    isLoading.value = false
  })

  // 监听未分类应用 (Unclassified)
  bridge.on(MessageTypes.UNCLASSIFIED_APPS_LOADED, (data: any) => {
    unclassifiedApps.value = data
  })
})

function onDragStart(event: DragEvent, app: AppItem, fromClusterId: number | null) {
  draggingApp.value = app
  sourceClusterId.value = fromClusterId
  
  if (event.dataTransfer) {
    event.dataTransfer.effectAllowed = 'move'
    event.dataTransfer.dropEffect = 'move'
    // 某些浏览器需要 setData 才能拖拽
    event.dataTransfer.setData('text/plain', app.processName)
  }
}

function onDrop(event: DragEvent, targetClusterId: number) {
  event.preventDefault()
  
  if (!draggingApp.value) return
  
  // 1. 从源移除
  if (sourceClusterId.value === null) {
    const index = unclassifiedApps.value.findIndex(a => a.processName === draggingApp.value!.processName)
    if (index > -1) unclassifiedApps.value.splice(index, 1)
  } else {
    const sourceCluster = clusters.value.find(c => c.id === sourceClusterId.value)
    if (sourceCluster) {
      const index = sourceCluster.apps.findIndex(a => a.processName === draggingApp.value!.processName)
      if (index > -1) sourceCluster.apps.splice(index, 1)
    }
  }
  
  // 2. 添加到目标
  const targetCluster = clusters.value.find(c => c.id === targetClusterId)
  if (targetCluster) {
    // 查重
    if (!targetCluster.apps.find(a => a.processName === draggingApp.value!.processName)) {
      targetCluster.apps.push(draggingApp.value)
    }
  }
  
  draggingApp.value = null
  sourceClusterId.value = null
  
  saveChanges()
}

function onDropToUnclassified(event: DragEvent) {
  event.preventDefault()
  
  if (!draggingApp.value) return
  if (sourceClusterId.value === null) return // 已经在 unclassified
  
  // 1. 从源移除
  const sourceCluster = clusters.value.find(c => c.id === sourceClusterId.value)
  if (sourceCluster) {
    const index = sourceCluster.apps.findIndex(a => a.processName === draggingApp.value!.processName)
    if (index > -1) sourceCluster.apps.splice(index, 1)
  }
  
  // 2. 添加到 Unclassified
  if (!unclassifiedApps.value.find(a => a.processName === draggingApp.value!.processName)) {
    unclassifiedApps.value.push(draggingApp.value)
  }
  
  draggingApp.value = null
  sourceClusterId.value = null
  
  saveChanges()
}

function saveChanges() {
  // 转换回后端格式
    /*
    var clusterUpdates = new List<(string IdStr, string Name, string Color, List<string> Apps)>();
    */
  
  const payload = {
    clusters: clusters.value.map(c => ({
      id: c.id.toString(),
      name: c.name,
      color: c.color,
      apps: c.apps.map(a => ({ name: a.processName })) // 后端只需要 processName
    }))
  }
  
  bridge.send(MessageTypes.UPDATE_CLUSTERS, payload)
}
</script>

<template>
  <div class="cluster-editor grid grid-cols-3 gap-6 h-[500px]">
    
    <!-- 未分类应用 -->
    <div class="col-span-1 flex flex-col gap-4 bg-surface-50 rounded-xl p-4 border border-border-default">
      <div class="flex items-center justify-between">
        <h3 class="font-bold text-text-secondary flex items-center gap-2">
          <span class="material-symbols-outlined text-sm">inventory_2</span>
          发现应用
        </h3>
        <span class="text-xs bg-surface-200 text-text-tertiary px-2 py-0.5 rounded-full">{{ unclassifiedApps.length }}</span>
      </div>
      
      <div 
        class="flex-1 overflow-y-auto space-y-2 min-h-[200px]"
        @dragover.prevent
        @drop="onDropToUnclassified"
      >
        <div 
          v-for="app in unclassifiedApps" 
          :key="app.processName"
          draggable="true"
          @dragstart="onDragStart($event, app, null)"
          class="flex items-center gap-3 p-3 bg-white rounded-lg border border-border-default cursor-move hover:shadow-sm hover:border-primary transition-all group"
        >
          <div class="size-8 rounded bg-surface-100 flex items-center justify-center shrink-0 overflow-hidden">
             <img v-if="app.isImage" :src="app.icon" class="size-6 object-contain" alt=""/>
             <span v-else class="material-symbols-outlined text-text-secondary text-lg">{{ app.icon }}</span>
          </div>
          <div class="truncate">
            <div class="text-sm font-medium text-text-primary truncate" :title="app.name">{{ app.name }}</div>
            <div class="text-[10px] text-text-tertiary truncate">{{ app.processName }}</div>
          </div>
          <span class="material-symbols-outlined text-text-tertiary ml-auto opacity-0 group-hover:opacity-100 text-sm">drag_indicator</span>
        </div>
        
        <div v-if="unclassifiedApps.length === 0" class="flex flex-col items-center justify-center h-40 text-text-tertiary gap-2">
           <span class="material-symbols-outlined text-3xl opacity-20">check_circle</span>
           <span class="text-sm">所有应用已分类</span>
        </div>
      </div>
    </div>
    
    <!-- 工作簇区域 -->
    <div class="col-span-2 grid grid-cols-2 gap-4 overflow-y-auto content-start pr-1">
      <div 
        v-for="cluster in clusters" 
        :key="cluster.id"
        class="bg-white rounded-xl border-2 border-transparent hover:border-border-default transition-colors flex flex-col h-[220px]"
        :style="{ borderColor: cluster.color + '40' }"
      >
        <!-- Cluster Header -->
        <div 
          class="p-3 border-b border-dashed border-border-default flex items-center gap-2 rounded-t-xl"
          :style="{ backgroundColor: cluster.color + '10' }"
        >
          <div class="size-3 rounded-full" :style="{ backgroundColor: cluster.color }"></div>
          <span class="font-bold text-sm text-text-primary">{{ cluster.name }}</span>
          <span class="ml-auto text-xs text-text-tertiary">{{ cluster.apps.length }}</span>
        </div>
        
        <!-- App List Drop Zone -->
        <div 
          class="flex-1 p-3 space-y-2 overflow-y-auto"
          @dragover.prevent
          @drop="onDrop($event, cluster.id)"
        >
          <div 
            v-for="app in cluster.apps" 
            :key="app.processName"
            draggable="true"
            @dragstart="onDragStart($event, app, cluster.id)"
            class="flex items-center gap-2 p-2 bg-surface-50 rounded border border-border-default cursor-move hover:border-primary/50 text-sm group"
          >
             <div class="size-5 rounded bg-white flex items-center justify-center shrink-0 overflow-hidden">
                <img v-if="app.isImage" :src="app.icon" class="size-4 object-contain" alt=""/>
                <span v-else class="material-symbols-outlined text-text-secondary text-xs">{{ app.icon }}</span>
             </div>
             <span class="truncate flex-1" :title="app.name">{{ app.name }}</span>
             <button 
               @click.stop="() => {
                 // 移回 unclassified
                 cluster.apps = cluster.apps.filter(a => a.processName !== app.processName);
                 unclassifiedApps.push(app);
                 saveChanges();
               }"
               class="text-text-tertiary hover:text-red-500 opacity-0 group-hover:opacity-100"
             >
               <span class="material-symbols-outlined text-sm">close</span>
             </button>
          </div>
          
          <div v-if="cluster.apps.length === 0" class="flex items-center justify-center h-full text-text-tertiary text-xs border-2 border-dashed border-border-default rounded">
            拖拽应用到此处
          </div>
        </div>
      </div>
    </div>
    
  </div>
</template>

<style scoped>
/* 自定义滚动条 */
::-webkit-scrollbar {
  width: 6px;
}
::-webkit-scrollbar-track {
  background: transparent;
}
::-webkit-scrollbar-thumb {
  background-color: #e2e8f0;
  border-radius: 3px;
}
::-webkit-scrollbar-thumb:hover {
  background-color: #cbd5e1;
}
</style>
