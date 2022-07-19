import { writable } from 'svelte/store'

const ui = writable({
  disableEstimateButton: true,
})

export { ui }
