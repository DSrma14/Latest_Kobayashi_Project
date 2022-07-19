import {
  Controller,
  Delete,
  HttpException,
  HttpStatus,
  Post,
  UploadedFile,
  UseInterceptors,
} from '@nestjs/common'
import { FileInterceptor } from '@nestjs/platform-express'
import { tmpdir } from 'os'

import { File } from './File'
import { ProcessCad } from './ProcessCad'
import { taskManager } from './taskManager'

const dest = tmpdir()

@Controller('files')
export class FilesController {
  @Post('upload')
  @UseInterceptors(
    FileInterceptor('file', {
      dest,
    })
  )
  uploadFile(@UploadedFile() multerFile: Express.Multer.File) {
    const processCad = new ProcessCad()

    if (taskManager.has(processCad.getName())) {
      throw new HttpException(
        {
          status: HttpStatus.TOO_MANY_REQUESTS,
          message: 'The CAD converter is already busy with another file.',
        },
        HttpStatus.TOO_MANY_REQUESTS
      )
    }

    const file = new File(multerFile.path)

    const finalPath = `${multerFile.destination}/${multerFile.originalname}`

    file.renameTo(finalPath)

    processCad.start(file)

    processCad.on('converter-exited', () => {
      // file.remove()
    })
  }

  @Delete('stop')
  stopCad() {
    taskManager.stopAll()
  }
}
