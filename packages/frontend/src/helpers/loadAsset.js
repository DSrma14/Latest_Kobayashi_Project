const { resourceLoader , CADAsset,CADBody} = window.zeaEngine



const loadAsset = (url, callback) => {
  const asset = new CADAsset()

  asset.on('error', (event) => {
    console.error('Error:', event)
  })

  console.log("LoadAsset")
  asset.on('loaded', () => {
    const materials = asset.getMaterialLibrary().getMaterials()
    materials.forEach((material) => {
      const baseColor = material.getParameter('BaseColor')
      if (baseColor) {
        baseColor.setValue(baseColor.getValue().toGamma())
      }
    })
  })

  asset.load(url).then(()=>{
    asset.traverse((item)=>{
      if(item instanceof CADBody){
        item.setShatterState(true)
      }
    })
  })

  

  const tagsMapping = {}

  asset.geomLibrary.once('loaded', ()=>{
    asset.loadMetadata().then(()=>{
      asset.traverse((item) => {
        if (item instanceof CADBody) {
          const geom = item.geomParam.value
          if (!geom) console.warn("Missing Geom")
          else {
            for (let i=0; i<geom.subGeoms.length; i++) {
              const subGeom = geom.subGeoms[i]
              console.log(i, subGeom.name)
              tagsMapping[subGeom.name] = {
                id: i,
                body: item
              }

              const surfaceTypeParam = subGeom.getParameter("SurfaceType")
              if (surfaceTypeParam) {
                console.log("Type:", surfaceTypeParam.value)

              }
            }
          }
        }
      })

      // console.log(tagsMapping)
      fetch('http://localhost:3333/converted.json')
        .then(response => response.json())
        .then(data => {
          console.log(data)
          callback(tagsMapping, data)
        });
      
    })
  })
  

  return asset
}

export { loadAsset }
