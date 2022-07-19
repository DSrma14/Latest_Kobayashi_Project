import { File } from './File'
import { ProcessBase } from './ProcessBase'

const zeaDebug = console.info

class ProcessXcgm extends ProcessBase {
  constructor() {
    super()

    this.name = 'ProcessXCGM'
  }

  start(file: File) {
    super.bootstrap('CGMBendRecog.exe', [
      '-i',
      file.getPath(),
      '-o',
      './public/converted.json',
    ])

    this.childProcess.stdout.on('data', (data) => {
      zeaDebug(`Processor STDOUT: ${data}`)
    })

    this.childProcess.stderr.on('data', (data) => {
      zeaDebug(`Processor STDERR: ${data}`)

      const dataString = data.toString()

      this.latestError = dataString
    })

    this.childProcess.on('exit', async (code) => {
      zeaDebug(`Processor exited with code ${code}.`)

      this.emit('processor-exited')

      const hasError = code > 0

      if (hasError) {
        this.stopCollabSession({
          type: 'processor-error',
          payload: {
            filename: file.getFilename(),
            error: this.latestError,
          },
        })
        return
      }

      this.stopCollabSession({
        type: 'processor-done',
        payload: {
          filename: file.getFilename(),
        },
      })
    })
  }
}

export { ProcessXcgm }
