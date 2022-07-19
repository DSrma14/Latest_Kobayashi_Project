import { NestFactory } from '@nestjs/core'

import { AppModule } from './app.module'
import { startExpress } from './express_app'

async function bootstrapNestJs() {
  const app = await NestFactory.create(AppModule)
  app.enableCors()
  await app.listen(3333)
}

bootstrapNestJs()
startExpress()
