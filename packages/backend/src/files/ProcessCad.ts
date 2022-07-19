import { File } from './File'
import { ProcessBase } from './ProcessBase'
import { ProcessXcgm } from './ProcessXcgm'

const zeaDebug = console.info

class ProcessCad extends ProcessBase {
  constructor() {
    super()

    this.name = 'ProcessCAD'
  }

  start(file: File) {
    super.bootstrap('SPAToZCadBridge.exe', [
      '-i',
      file.getPath(),
      '-o',
      './public/converted.zcad',
      '-meta',
      'EMBEDDED',
    ])

    this.childProcess.stdout.on('data', (data) => {
      zeaDebug(`Converter STDOUT: ${data}`)

      const dataString = data.toString()

      const outputLines = dataString.split('\n')

      outputLines.forEach((outputLine: string) => {
        if (outputLine.trim() && outputLine.startsWith('PROGRESS:')) {
          try {
            const toBeJson = outputLine.substring(outputLine.indexOf(':') + 1)
            const dataJson = JSON.parse(toBeJson)

            this.latestMessage = {
              type: 'converter-progress',
              payload: {
                ...dataJson,
                filename: file.getFilename(),
              },
            }
          } catch (error) {
            console.error('Error parsing progress:', error)
          }
        }
      })
    })

    this.childProcess.stderr.on('data', (data) => {
      zeaDebug(`Converter STDERR: ${data}`)

      const dataString = data.toString()

      this.latestError = dataString
    })

    this.childProcess.on('exit', async (code) => {
      zeaDebug(`Converter exited with code ${code}.`)

      this.emit('converter-exited')

      const hasError = code > 0

      if (hasError) {
        this.stopCollabSession({
          type: 'converter-error',
          payload: {
            filename: file.getFilename(),
            error: this.latestError,
          },
        })
        return
      }

      this.stopCollabSession({
        type: 'converter-done',
        payload: {
          filename: file.getFilename(),
        },
      })

       const processXcgm = new ProcessXcgm()
      // //const xcgmFile = new File('./public/converted.xcgm') // pcu:TBR did ZeaSpatialBridge generate converted.xcgm???
       processXcgm.start(file)
    })
  }
}

export { ProcessCad }
