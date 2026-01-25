<script setup lang="ts">
import { ref, watch } from 'vue'

const props = defineProps<{
  show: boolean
}>()

const emit = defineEmits<{
  (e: 'close'): void
  (e: 'start', duration: number, taskName: string): void
}>()

const selectedTime = ref(30)
const taskName = ref('')
const customTime = ref(30)
const isCustom = ref(false)

const presets = [15, 30, 45, 60, 90]

const selectPreset = (time: number) => {
  selectedTime.value = time
  customTime.value = time
  isCustom.value = false
}

const onSliderChange = () => {
  selectedTime.value = customTime.value
  isCustom.value = true
}

const startFocus = () => {
  emit('start', selectedTime.value, taskName.value)
  // Reset fields
  taskName.value = ''
  selectedTime.value = 30
}
</script>

<template>
  <Transition name="modal">
    <div v-if="show" class="fixed inset-0 z-50 flex items-center justify-center p-4">
      <!-- Backdrop -->
      <div class="absolute inset-0 bg-black/40 backdrop-blur-sm" @click="emit('close')"></div>
      
      <!-- Modal Card -->
      <div class="card-base w-full max-w-md bg-white p-6 shadow-xl relative z-10 transform transition-all">
        <div class="flex justify-between items-center mb-6">
          <h2 class="text-xl font-bold text-text-primary flex items-center gap-2">
            <span class="material-symbols-outlined text-primary">timer</span>
            Set Focus Session
          </h2>
          <button @click="emit('close')" class="btn-ghost p-2 rounded-full hover:bg-slate-100">
            <span class="material-symbols-outlined">close</span>
          </button>
        </div>

        <div class="space-y-6">
          <!-- Time Selection -->
          <div class="space-y-3">
            <label class="text-sm font-bold text-text-secondary uppercase tracking-wider">Duration (Minutes)</label>
            
            <!-- Presets -->
            <div class="flex flex-wrap gap-2">
              <button 
                v-for="time in presets" 
                :key="time"
                @click="selectPreset(time)"
                class="px-4 py-2 rounded-xl text-sm font-semibold transition-all border"
                :class="selectedTime === time && !isCustom
                  ? 'bg-primary text-white border-primary shadow-glow' 
                  : 'bg-white text-text-secondary border-slate-200 hover:border-primary/50 hover:text-primary'"
              >
                {{ time }}m
              </button>
            </div>

            <!-- Slider -->
            <div class="pt-4 px-2">
              <div class="flex justify-between text-xs text-text-tertiary font-bold mb-2">
                <span>Custom</span>
                <span class="text-primary text-base">{{ customTime }} min</span>
              </div>
              <input 
                type="range" 
                min="5" 
                max="120" 
                step="5" 
                v-model.number="customTime"
                @input="onSliderChange"
                class="w-full accent-primary h-2 bg-slate-100 rounded-lg appearance-none cursor-pointer"
              />
            </div>
          </div>

          <!-- Task Input -->
          <div class="space-y-3">
            <label class="text-sm font-bold text-text-secondary uppercase tracking-wider">I intend to...</label>
            <div class="relative">
              <span class="absolute left-4 top-1/2 -translate-y-1/2 material-symbols-outlined text-text-tertiary">edit</span>
              <input 
                v-model="taskName"
                type="text" 
                placeholder="e.g. Finish the report, Debug the API..." 
                class="w-full pl-12 pr-4 py-3 bg-slate-50 border border-slate-200 rounded-xl focus:border-primary focus:ring-2 focus:ring-primary/20 outline-none transition-all font-medium text-text-primary placeholder:font-normal"
                @keyup.enter="startFocus"
              />
            </div>
          </div>

          <!-- Info Tip -->
          <div class="bg-indigo-50 rounded-xl p-4 flex gap-3 text-sm text-indigo-800">
            <span class="material-symbols-outlined text-indigo-600">info</span>
            <p class="leading-snug opacity-90">
              During focus mode, the Fatigue Ring will transform into a session timer.
            </p>
          </div>

          <!-- Actions -->
          <div class="flex gap-3 pt-2">
            <button 
              @click="emit('close')" 
              class="flex-1 px-6 py-3 rounded-xl border border-slate-200 font-bold text-text-secondary hover:bg-slate-50 hover:text-text-primary transition-colors"
            >
              Cancel
            </button>
            <button 
              @click="startFocus" 
              class="flex-1 px-6 py-3 rounded-xl bg-primary hover:bg-primary-hover text-white font-bold shadow-lg shadow-primary/30 transition-all active:scale-95 flex items-center justify-center gap-2"
            >
              <span class="material-symbols-outlined">play_arrow</span>
              Start Focus
            </button>
          </div>
        </div>
      </div>
    </div>
  </Transition>
</template>

<style scoped>
.modal-enter-active,
.modal-leave-active {
  transition: opacity 0.3s ease;
}

.modal-enter-from,
.modal-leave-to {
  opacity: 0;
}

.modal-enter-active .card-base,
.modal-leave-active .card-base {
  transition: all 0.3s cubic-bezier(0.4, 0, 0.2, 1);
}

.modal-enter-from .card-base,
.modal-leave-to .card-base {
  opacity: 0;
  transform: scale(0.95) translateY(10px);
}
</style>
