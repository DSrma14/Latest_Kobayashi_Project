import { renameSync, unlinkSync } from 'fs'
import { basename, extname } from 'path'

class File {
  constructor(private path: string) {}

  getFilename(includeExtension = true) {
    return basename(
      this.getPath(),
      includeExtension ? '' : extname(this.getPath())
    )
  }

  getPath() {
    return this.path
  }

  renameTo(newPath: string) {
    renameSync(this.getPath(), newPath)
    this.path = newPath
  }

  remove() {
    try {
      unlinkSync(this.getPath())
    } catch (error) {
      console.error(error)
    }
  }
}

export { File }
