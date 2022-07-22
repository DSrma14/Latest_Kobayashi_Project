<script>
  import { toast } from '@zerodevx/svelte-toast'
  import request from 'superagent'
  import { onMount } from 'svelte'
  import Dropzone from 'svelte-file-dropzone'

  import { loadAsset } from '../helpers/loadAsset.js'
  import { calculateEstimate } from '../helpers/calculateEstimate.js'

  import Spinner from './Spinner.svelte'

  import { assets } from '../stores/assets.js'
  import { scene } from '../stores/scene.js'
  import { ui } from '../stores/ui.js'
 // import { APP_DATA } from '../stores/appData'
  //import { selectionManager } from '../stores/selectionManager.js'
//mycode
  //import '@zeainc/zea-tree-view'
 // import { ZeaTreeView } from '@zeainc/zea-tree-view'
 //import {SelectionManager} from '@zeainc/zea-ux'

  import Button from './Button.svelte'
  import Menu from './ContextMenu/Menu.svelte'
  import MenuOption from './ContextMenu/MenuOption.svelte'
import { text } from 'svelte/internal';
  import Dialog from './Dialog.svelte'

   let isMenuVisible = false
  let pos = { x: 0, y: 0 }
  let contextAsset
  let contextItem
  let contextSubGeom
  const openMenu = (event, asset, item, subGeom) => {
    contextAsset = asset
    contextItem = item
    contextSubGeom = subGeom
    pos = event.touches
      ? { x: event.touches[0].clientX, y: event.touches[0].clientY }
      : { x: event.clientX, y: event.clientY }
    isMenuVisible = true
  }
  const closeMenu = () => {
    console.log('closeMenu:')
    isMenuVisible = false
  }
  
  let isDialogOpen = false
  const closeDialog = () => {
    isDialogOpen = false
  }

  let canvas
  let estimateInterval
  let metadata
  let savedData = {}
  let tagsMapping = {}


  let numBends = ''
  let price = ''
  let processorDone = false
  let processorLatestError
  let progress
  let shouldDisplayDropZone = true
  let shouldShowSpinner = false
  let thickness = ''
  let minradius = ''
  let maxradius = ''

  const ASSET_URL = 'http://localhost:3333/converted.zcad'
  const urlParams = new URLSearchParams(window.location.search)

  const formatter = new Intl.NumberFormat('ja-JP', {
    style: 'currency',
    currency: 'JPY',
    minimumFractionDigits: 2,
  })

  const sendFileRequest = async (file) => {
    const response = await request
      .post('http://localhost:3333/files/upload')
      .attach('file', file)
      .on('progress', (event) => {
        progress = {
          completed: event.percent / 100,
          filename: file.name,
          label: 'Uploading',
        }
        shouldDisplayDropZone = false
      })
  }

  const showError = (error) => {
    toast.push(error, {
      theme: {
        '--toastBackground': '#F56565',
        '--toastProgressBackground': '#C53030',
      },
    })
  }

  const resetAppState = () => {
    //$assets.removeAllChildren()  // pcu:TBR

    $ui.disableEstimateButton = true

    processorDone = false
    processorLatestError = null
    shouldShowSpinner = false

    window.clearInterval(estimateInterval)

    numBends = ''
    thickness = ''
    price = ''
    minradius = ''
  }

  const doEstimate = async () => {
    // const response = await request.get('http://localhost:3333/converted.json')
    const asset = $assets.getChild(0)
    const estimate = calculateEstimate(asset, metadata, tagsMapping)

    numBends = estimate.numBends
    thickness = estimate.thickness
    minradius=estimate.minradius
    price = formatter.format(estimate.price)
  }

  /* {{{ Handlers. */
  const handleMainDragOver = () => {
    if (progress) {
      return
    }

    shouldDisplayDropZone = true
  }

  const handleFileSelect = async (event) => {
    resetAppState()

    const { acceptedFiles } = event.detail
    const file = acceptedFiles[0]
    await sendFileRequest(file)
  }

  const handleClickStop = async () => {
    const response = await request.delete('http://localhost:3333/files/stop')
  }

  const handleClickEstimate = () => {
    
    window.clearInterval(estimateInterval)
    shouldShowSpinner = true

    estimateInterval = window.setInterval(() => {
      if (!processorDone) {
        return
      }

      window.clearInterval(estimateInterval)
      shouldShowSpinner = false

      if (processorLatestError) {
        showError(processorLatestError)
        return
      }

      doEstimate()
    }, 2000)
  }
  const handleClickExport = () => {
    alert("Export")
  }

  /* }}} Handlers. */

  onMount(() => {
    const {
      Color,
      TreeItem,
      GLRenderer,
      Scene,
      SystemDesc,
      EnvMap,
      CameraManipulator,
      StringParameter,
      StandardSurfaceMaterial,
      CADAsset
    } = window.zeaEngine
    //debugger

    const renderer = new GLRenderer(canvas, {
      debugGeomIds: urlParams.has('debugGeomIds'),
      xrCompatible: false,
    })

    $scene = new Scene()

   //const scene = new ZeaEngine.Scene();
   // const appData = {
    //scene,
   // renderer
 //}
  //ppData.selectionManager  = new ZeaUx.SelectionManager(appData, {
   // enableXfoHandles: true
  //}); 


/*const appData = {
    scene,
    renderer,
  }
  // Setup Selection Manager
  const selectionColor = new Color('#F9CE03')
  selectionColor.a = 0.1
  const selectionManager = new SelectionManager(appData, {
    selectionOutlineColor: selectionColor,
    branchSelectionOutlineColor: selectionColor,
  })
  appData.selectionManager = selectionManager

 // let highlightedItem
  const highlightColor = new Color('#F9CE03')
  highlightColor.a = 0.1
  const filterItem = (srcItem: TreeItem) => {
    let item: TreeItem = srcItem
    while (item && !(item instanceof CADBody) && !(item instanceof PMIItem)) {
      item = <TreeItem>item.getOwner()
    }
    if (!item) return srcItem
    if (item.getOwner() instanceof InstanceItem) {
      item = <TreeItem>item.getOwner()
    }
    return item
  }

  renderer.getViewport().on('pointerUp', (event) => {
    // Detect a right click
    if (event.button == 0) {
      if (event.intersectionData) {
        // // if the selection tool is active then do nothing, as it will
        // // handle single click selection.s
        // const toolStack = toolManager.toolStack
        // if (toolStack[toolStack.length - 1] == selectionTool) return

        // To provide a simple selection when the SelectionTool is not activated,
        // we toggle selection on the item that is selcted.
        const item = filterItem(event.intersectionData.geomItem)
        if (item) {
          if (!event.shiftKey) {
            selectionManager.toggleItemSelection(item, !event.ctrlKey)
          } else {
            const items = new Set<TreeItem>()
            items.add(item)
            selectionManager.deselectItems(items)
          }
        }
      } else {
        selectionManager.clearSelection()
      }
    }
  })
    
    const asset=new CADAsset()
  asset.load(Models/06_複数部品1.SLDASM.zcad')
  scene.getRoot().addChild(asset)*/





    // $scene.setupGrid(10.0, 10)

    // Assigning an Environment Map enables PBR lighting
    // for nicer shiny surfaces.
    // if (!SystemDesc.isMobileDevice && SystemDesc.gpuDesc.supportsWebGL2) {
    //   const envMap = new EnvMap('envMap')
    //   envMap.getParameter('FilePath').setValue(`/data/StudioA.zenv`)
    //   envMap.getParameter('HeadLightMode').setValue(true)
    //   $scene.getSettings().getParameter('EnvMap').setValue(envMap)
    // }
    // Assigning an Environment Map enables PBR lighting for nicer shiny surfaces.
     if (!SystemDesc.isMobileDevice && SystemDesc.gpuDesc.supportsWebGL2) { // pcu:TBR
      const envMap = new EnvMap('envMap')
      envMap.load(`/data//HDR_029_Sky_Cloudy_Ref.vlenv`)
      envMap.headlightModeParam.value = true
      $scene.envMapParam.value = envMap
    }

    // $scene.setupGrid(10, 10)
    //$scene
    // .getSettings()
    // .getParameter('BackgroundColor')
    // .setValue(new Color('#F9FAFB'))
    renderer.setScene($scene)

    renderer
      .getViewport()
      .getManipulator()
      .setDefaultManipulationMode(CameraManipulator.MANIPULATION_MODES.tumbler)


    /* {{{ Event Handlers */


    const customFaceMaterial = new StandardSurfaceMaterial('customFaceMAterial')
    customFaceMaterial.baseColorParam.value = new Color(0, 0.7, 0)

    let highlightKey = ""
    let highlightedItem 
    renderer.getViewport().on('pointerMove', (event) =>{
      if (event.intersectionData && event.intersectionData.componentId >= 0) {
        const geomItem = event.intersectionData.geomItem
        const subGeomId = event.intersectionData.componentId
        const key = "highlightedFace:" +  subGeomId
        if (key != highlightKey) {
          if (highlightedItem)  highlightedItem.removeHighlight(highlightKey)
          highlightKey = key
          highlightedItem = geomItem
          highlightedItem.addHighlight(highlightKey, new Color(1, 0, 0, 0.4))
          
        }

        const geom = geomItem.geomParam.value
        const subGeom = geom.subGeoms[subGeomId]

        const collectBendFaces = (bend) =>{
          const faceIds = []
          for(let j=0; j<bend.faces.side_1.length; j++) {
            faceIds.push(tagsMapping[bend.faces.side_1[j]].id)
          }
          for(let j=0; j<bend.faces.side_2.length; j++) {
            faceIds.push(tagsMapping[bend.faces.side_2[j]].id)
          }
          return faceIds
        }
        //console.log(metadata)
        const tagId = subGeom.name
      //   if (metadata.bends) {
      //     let mouseOverBend = false
      //     for(let i=0; i<metadata.bends.length; i++) {
      //       const bend = metadata.bends[i]
            
      //       for(let j=0; j<bend.faces.side_1.length && !mouseOverBend; j++) {
      //         if (bend.faces.side_1[j] == tagId) {
      //           // We have a mouse over a bend
      //           mouseOverBend = true
      //         }
      //       }
      //       for(let j=0; j<bend.faces.side_2.length && !mouseOverBend; j++) {
      //         if (bend.faces.side_2[j] == tagId) {
      //           // We have a mouse over a bend
      //           mouseOverBend = true
      //         }
      //       }
      //       if (mouseOverBend) {
      //         const faces = collectBendFaces(bend)
      //         const key = "highlightedFace:" +  faces
      //         if (key != highlightKey) {
      //           if (highlightedItem)  highlightedItem.removeHighlight(highlightKey)
      //           highlightKey = key
      //           highlightedItem = geomItem
      //           highlightedItem.addHighlight(highlightKey, new Color(1, 0, 0, 0.4))
      //         }

      //         // Display a Tooltip here...
      //       } else {
              
      //         if (highlightedItem)  highlightedItem.removeHighlight(highlightKey)
      //           highlightKey = ""
      //       }
      //     }
      //   }
      //   // const subGeom = geom.subGeoms[subGeomId]
      //   // const surfaceTypeParam = subGeom.getParameter("SurfaceType")
      //   // if (surfaceTypeParam && surfaceTypeParam.value == "Cylinder") {
      //   //   console.log("Bend:", surfaceTypeParam.value)

      //   // }

      // } else {
      //   if (highlightedItem) {
      //     highlightedItem.removeHighlight(highlightKey)
      //     highlightedItem = null
      //     highlightKey = ""
      //   }
      }

    })


    const findAsset = (item) =>{
      while(!(item instanceof CADAsset)) {
        item = item.parent
      }
      return item
    }
    document.oncontextmenu = function(event){
     // if(event.button==2 && event.intersectionData){
     event.stopPropagation();
    event.preventDefault();
      //console.log("hello");
    }
    
    
    renderer.getViewport().on('pointerDown', (event) =>{
      if (event.button == 2 && event.intersectionData && event.intersectionData.componentId >= 0) {
        const geomItem = event.intersectionData.geomItem
        const subGeomId = event.intersectionData.componentId
        const geom = geomItem.geomParam.value

        const subGeom = geom.subGeoms[subGeomId]
        console.log(subGeom.toJSON())

        // subGeom.addParameter(new StringParameter("data", "Clicked"))
        const asset = findAsset(geomItem)

        // Display a UI here to select options.
        // Pass the data to the UIEvent. SubGeom, 
        openMenu(event, asset, geomItem, subGeom)

        event.preventDefault()

        // geom.assignSubGeomMaterial(subGeomId, customFaceMaterial)
      }
    })



    /* }}} Event Handlers */


    /* {{{ CAD */
    $assets = new TreeItem('Assets')
    $scene.getRoot().addChild($assets)
    //renderer.addPass(new GLCADPass()) // pcu:TBR
    /* }}} CAD */

    /* {{{ WebSocket. */
    const SOCKET_URL = 'http://localhost:3334'
    const socket = window.io(SOCKET_URL)

    socket.on('converter-progress', (payload) => {
      progress = {
        completed: payload.percentage / 100,
        filename: payload.filename,
        label: payload.label,
      }
      shouldDisplayDropZone = false
    })

    socket.on('converter-done', (payload) => {
      progress = null
      shouldDisplayDropZone = false

      const asset = loadAsset(ASSET_URL, (mapping, data) => {
        tagsMapping = mapping
        metadata = data
        console.log(metadata)
        const savedDataStr = localStorage.getItem(asset.name)
        if (savedDataStr) savedData = JSON.parse(savedDataStr)
        renderer.frameAll()
      })

      $assets.addChild(asset)

      $ui.disableEstimateButton = false
    })

    socket.on('converter-error', (payload) => {
      progress = null
      shouldDisplayDropZone = true

      showError(payload.error)
    })

    socket.on('processor-done', (payload) => {
      processorDone = true
    })

    socket.on('processor-error', (payload) => {
      processorDone = true
      processorLatestError = payload.error
    })
    /* }}} WebSocket. */

    return () => {
      socket.removeAllListeners()
    }
  })
</script>

<main
  class="Main flex flex-1 relative bg-blue-50"
  on:dragover={handleMainDragOver}
>
  <div class="relative flex-1 h-full">
    <canvas bind:this={canvas} class="absolute" />
  </div>

  <div class="p-5">
    <div class="grid grid-cols-2 gap-4 mb-5">
      <div>Number of bends:</div>
      <input
        type="text"
        disabled
        class="border-2 border-gray-400 rounded"
        value={numBends}
      />

      <div>Thickness:</div>
      <input
        type="text"
        disabled
        class="border-2 border-gray-400 rounded"
        value={thickness}
      />

      <!-- <div>MinRadius:</div>
      <input
        type="text"
        disabled
        class="border-2 border-gray-400 rounded"
        value={minradius}
        />
       
       <div>MaxRadius:</div>
      <input
        type="text"
        disabled
        class="border-2 border-gray-400 rounded"
        value={maxradius}
        /> -->
      
      <Button
        on:click={handleClickEstimate}
        disabled={$ui.disableEstimateButton}
        title="Hello World"
      >
        {#if shouldShowSpinner}
          <Spinner />
        {/if}
        estimate
      </Button>

      <Button
        on:click={handleClickExport}
        
        title="Hello World"
      >
      export
       
           
      </Button>

      <input
        type="text"
        disabled
        class="border-2 border-gray-400 rounded"
        value={price}
      />
    </div>
  </div>

  {#if progress}
    <div
      class="absolute inset-0 bg-white flex flex-col items-center justify-center p-2 bg-opacity-60"
    >
      <p class="text-center text-gray-800 text-lg mb-4">
        {progress.filename}
      </p>
      <progress value={progress.completed} />
      <p class="text-gray-500 text-sm mb-4">
        {progress.label}
      </p>

      <Button on:click={handleClickStop}>stop</Button>
    </div>
  {/if}

  {#if shouldDisplayDropZone}
    <Dropzone
      containerClasses="absolute inset-1 border-dashed border-4 border-blue-300 rounded-2xl shadow bg-white flex flex-col items-center justify-center p-2 bg-opacity-60"
      disableDefaultStyles
      multiple={false}
      on:drop={handleFileSelect}
    >
      <p class="mb-1 text-center text-4xl text-primary">Drop file here</p>
      <p class="mb-8 text-center text-gray-500">or click to select it.</p>
    </Dropzone>
  {/if}
</main>


<Dialog isOpen={isDialogOpen} {savedData} close={closeDialog} {contextItem} asset={contextAsset} subGeom={contextSubGeom}/> 

{#if isMenuVisible}
  <Menu {...pos} on:click={closeMenu} on:clickoutside={closeMenu}>
    <MenuOption
      text="Edit"
      on:click={() => {
        isDialogOpen = true
        //closeMenu()
        console.log("Tap")
      }}
    />
  </Menu>
{/if}

<style>
  canvas {
    touch-action: pinch-zoom;
  }
</style>
