const { Color, GeomItem, Material ,CADBody} = window.zeaEngine

const highlightColor = new Color('#2D5DBD')
highlightColor.a = 0.2
const highlightMaterial = new Material(
  'highlightMaterial',
  'StandardSurfaceShader'
)
highlightMaterial.getParameter('BaseColor').setValue(new Color(1, 0, 0))


const calculateEstimate = (asset, metadata, tagsMapping ) => {

  const collectBendFaces = (bend) =>{
    let body
    const faceIds = []
    for(let j=0; j<bend.faces.side_1.length; j++) {
      faceIds.push(tagsMapping[bend.faces.side_1[j]].id)
      body = tagsMapping[bend.faces.side_1[j]].body
    }
    for(let j=0; j<bend.faces.side_2.length; j++) {
      faceIds.push(tagsMapping[bend.faces.side_2[j]].id)
    }
    return  { faceIds, body }
  }

  const bends = metadata.bends
  let thickness = 0
  let minradius =0
  let maxradius=0
  bends.forEach((bend) => {
    const faces = collectBendFaces(bend)
    
    const body = faces.body
    body.addHighlight("bend:" + faces.faceIds, new Color(1, 0, 0, 0.4))

    thickness += bend.thickness
    minradius = bend.minradius
    maxradius+=bend.maxradius


  })

  const numBends = bends.length
  const costPerBend = 1100
  thickness /= numBends
  const price = numBends * costPerBend * thickness
  return {
    numBends,
    thickness,
    price,
  }
}

export { calculateEstimate }
