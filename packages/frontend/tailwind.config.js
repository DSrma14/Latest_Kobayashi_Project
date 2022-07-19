const production = !process.env.ROLLUP_WATCH

module.exports = {
  future: {
    removeDeprecatedGapUtilities: true,
    purgeLayersByDefault: true,
  },
  plugins: [],
  purge: {
    content: ['./src/**/*.svelte'],
    enabled: production,
  },
  theme: {
    extend: {
      colors: {
        primary: {
          DEFAULT: '#15357d',
          light: '#3163c6',
        },
      },
    },
  },
}
