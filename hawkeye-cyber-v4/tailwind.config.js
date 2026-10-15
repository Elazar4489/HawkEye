/** @type {import('tailwindcss').Config} */
export default {
  content: [
    "./index.html",
    "./src/**/*.{js,ts,jsx,tsx}",
  ],
  darkMode: 'class',
  theme: {
    extend: {
      colors: {
        cyber: {
          bg: '#050811',
          card: 'rgba(11, 15, 25, 0.75)',
          border: 'rgba(0, 240, 255, 0.2)',
          accent: '#00F0FF',
          neonRed: '#FF2E54',
          gold: '#FFC700',
          purple: '#A855F7'
        }
      },
      fontFamily: {
        sans: ['Assistant', 'system-ui', 'sans-serif'],
        mono: ['Fira Code', 'monospace']
      },
      animation: {
        'pulse-glow': 'pulseGlow 2s cubic-bezier(0.4, 0, 0.6, 1) infinite',
        'radar-sweep': 'radarSweep 4s linear infinite',
      },
      keyframes: {
        pulseGlow: {
          '0%, 100%': { opacity: '1', filter: 'drop-shadow(0 0 12px rgba(0, 240, 255, 0.8))' },
          '50%': { opacity: '0.4', filter: 'drop-shadow(0 0 4px rgba(0, 240, 255, 0.2))' }
        },
        radarSweep: {
          '0%': { transform: 'rotate(0deg)' },
          '100%': { transform: 'rotate(360deg)' }
        }
      }
    },
  },
  plugins: [],
}
