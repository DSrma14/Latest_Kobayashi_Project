<script>
  // A dialog based on 'svelte-accessible-dialog'
  // https://www.npmjs.com/package/svelte-accessible-dialog
  import { DialogOverlay, DialogContent } from 'svelte-accessible-dialog'
  import { onMount } from 'svelte'

  export let isOpen
  export let close
  export let asset
  export let contextItem
  export let subGeom
  export let savedData

  let selected = ""

  $: {
    if (contextItem && subGeom) {
      const key = contextItem.path + ":" + subGeom.name
        console.log(key, savedData)
      if (savedData[key]) {
        selected = savedData[key].BendType
        console.log(key, selected)

      }

        
    }
  }
  
  onMount(() => {
    console.log("savedData", savedData)

    // IF there was already a bend type sepcified, select it here.

  })

  function closeDialog() {

    // Save the data for the context item 

    // savedData

    console.log("Close Dialog")
    close()

  }

  function onSelectProperty(event) {

    const value = event.srcElement.value

    console.log("Select", value)
    console.log("savedData", savedData)
    const key = contextItem.path + ":" + subGeom.name
    savedData[key] = {
      "BendType": value
    }
    localStorage.setItem(asset.name, JSON.stringify(savedData))

  }

</script>

<DialogOverlay {isOpen} onDismiss={close} class="overlay">
  <DialogContent class="content">
    <section class="p-2">
      <header>Properties</header>
      <main>
        <pre class="my-3 py-3">
        <select name="Property"  value={selected}  on:change="{onSelectProperty}">  
          <option value="Tap"> Tap </option>  
          <option value="Burring"> Burring </option>  
          <option value="Simple"> Simple </option>
        </select>
        </pre>
      </main>
      <div class="text-right">
        <button
          on:click={closeDialog}
          class="bg-gray-700 border rounded px-2 text-white"
        >
          Close
        </button>
      </div>
    </section>
  </DialogContent>
</DialogOverlay>

<style>
  :global([data-svelte-dialog-overlay].overlay) {
    z-index: 10;
  }

  :global([data-svelte-dialog-content].content) {
    border: 1px solid #000;
    padding: 1rem;
    background: white;
  }

  section {
    border: 1px solid #0003;
    box-shadow: 2px 2px 5px 0px #0002;
  }
</style>
