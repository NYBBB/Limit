# Limit 3.0 UI Design Specification: "Bio-Digital Essence"

> **Design Vision**: A dashboard that feels like a living organism monitoring biological energy.
> **Key Aesthetic**: Cyber-Organic, Bioluminescent, Glassmorphism, Deep Dark Mode.

---

## 🎨 Visual Identity System

### 1. Color Palette: "Bioluminescence"

Commit to a deep, immersive dark mode with glowing accents.

```css
:root {
  /* Backgrounds */
  --bg-deep: #050507;       /* Void Black */
  --bg-surface: #0a0a0f;    /* Deep Space */
  --bg-glass: rgba(20, 20, 30, 0.4); /* Glass Panel */

  /* Accents (Neon/Glow) */
  --accent-cyan: #00f0ff;   /* Focus / Energy */
  --accent-purple: #bc13fe; /* Creativity / Insight */
  --accent-amber: #ffaa00;  /* Fatigue Warning */
  --accent-red: #ff2a2a;    /* Critical Strain */

  /* Gradients */
  --grad-energy: linear-gradient(135deg, #00f0ff 0%, #0088ff 100%);
  --grad-flow: linear-gradient(135deg, #bc13fe 0%, #6600ff 100%);
  
  /* Text */
  --text-primary: #ffffff;
  --text-secondary: rgba(255, 255, 255, 0.6);
  --text-dim: rgba(255, 255, 255, 0.3);
}
```

### 2. Typography

Distinctive, readable, yet futuristic.

- **Headings**: **'Outfit'** (Sans-serif, geometric but friendly).
  - *Why?* High x-height, modern feel without being sterile.
- **Body**: **'Manrope'** (Semi-geometric).
  - *Why?* Excellent legibility for dashboard data.
- **Monospace**: **'JetBrains Mono'** (Code/Data).

### 3. Glassmorphism & Depth

Use multiple layers of blur and opacity to create depth.

- **Panel Style**:
  ```css
  background: var(--bg-glass);
  backdrop-filter: blur(20px) saturate(180%);
  border: 1px solid rgba(255, 255, 255, 0.08);
  box-shadow: 0 8px 32px 0 rgba(0, 0, 0, 0.3);
  border-radius: 24px;
  ```

---

## 🧩 Core Components Design

### 1. The Energy Core (Fatigue Ring Redesigned)
*Abandon the static SVG ring. Make it alive.*

- **Concept**: A "Liquid Energy" containment field.
- **Implementation**:
  - **Outer Ring**: Glowing segmented track (SVG).
  - **Inner Core**: A **WebGL/Canvas fluid simulation** or a CSS-animated gradient mesh that "breathes" (expands/contracts) based on user's heart rate/fatigue.
  - **State**:
    - *High Energy*: Fast, bright cyan ripples.
    - *Fatigued*: Slow, heavy amber sludge.
  - **Interaction**: Hovering over the core reveals a tooltip with precise metrics (like checking a sci-fi gauge).

### 2. Insight Banner ("The Synapse")
*Not just a banner, but a floating intelligence context.*

- **Visual**: A floating pill-shaped glass container at the top center.
- **Micro-interaction**:
  - On new insight: Glows gently via `box-shadow` animation.
  - Icon: Animated SVG (e.g., a lightbulb that actually turns on).

### 3. Top Drainers ("Energy Leaks")
*Visualize energy loss effectively.*

- **Design**: Horizontal progress bars are boring. Use **"Comet Trails"**.
  - A glowing line that starts bright and fades out.
  - **Color Coding**: 
    - Critical (Social Media): Red glowing trail.
    - Productive (VS Code): Blue steady beam.
  - **Hover**: Expands to show session details and "Kill Process" shortcut.

### 4. Activity Heatmap ("The Grid")
*Cyberpunk city grid vibe.*

- **Visual**: Cells are not just squares, but slightly rounded glowing tiles.
- **Effect**: Hovering a cell lights up neighboring cells slightly (proximity glow).
- **Color**: Dark empty cells, bright neon filled cells.

---

## 📐 Layout Specifications (Bento Grid)

Move away from rigid rows. Use a fluid **CSS Grid** layout.

```css
.dashboard-grid {
  display: grid;
  grid-template-columns: 320px 1fr 300px;
  grid-template-rows: auto 1fr;
  gap: 24px;
  padding: 32px;
}
```

### Layout Zones
1. **Zone A (Left)**: **The Core**. Dedicated vertical column. Large Energy Ring + Current Status.
2. **Zone B (Center)**: **The Workflow**.
   - Top: Insight Banner (spanning Center + Right).
   - Middle: Activity Timeline / Heatmap.
   - Bottom: Focus Session Controls.
3. **Zone C (Right)**: **The Metrics**.
   - Top Drainers List.
   - KPI Cards (Stacked).

---

## 🎬 Motion & Interaction

- **Enter Animation**: Staggered fade-up for all cards (`animation-delay`).
- **Hover**: Cards should "lift" (transform: translateY(-4px)) and increase border brightness.
- **Click**: Subtle scale-down (0.98) for tactile feel.
- **Focus Mode**: When "Focus" is activated:
  - Background dims.
  - Non-essential cards blur out (`filter: blur(4px)`).
  - Energy Core glows brighter.

---

## 🛠️ Implementation Requirements

- **Icons**: Use **Phosphor Icons** (Duotone variant) for technical/modern look.
- **Charts**: **ECharts** with custom dark theme (disable default grids/axes where possible for cleaner look).
- **Framework**: Vue 3 + TailwindCSS.
- **Dependencies**: `@vueuse/core` (for resize observer, intersection observer), `gsap` (for fluid animations).
