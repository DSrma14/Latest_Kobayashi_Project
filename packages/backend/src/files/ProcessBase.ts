import { Session } from '@zeainc/zea-collab'
import { spawn } from 'child_process'
import { EventEmitter } from 'events'
import { ChildProcess } from 'node:child_process'

import { Message } from '../interfaces/Message'

import { io } from '../express_app'

import { taskManager } from './taskManager'

const zeaDebug = console.info

class ProcessBase extends EventEmitter {
  protected childProcess: ChildProcess
  protected latestMessage: Message
  protected latestError: string
  protected name: string
  protected session: Session

  private repeater: NodeJS.Timeout

  protected bootstrap(command: string, args) {
    const { BINARIES_PATH } = process.env

    //if (!BINARIES_PATH) {
    //  throw new Error('The `BINARIES_PATH` env var is not defined.') // pcu:TBR everything in PATH
    //}

    this.startCollabSession()

    //this.childProcess = spawn(`${BINARIES_PATH}/${command}`, args)
    this.childProcess = spawn(`${command}`, args)

    taskManager.add(this)

    this.childProcess.on('exit', () => {
      taskManager.remove(this)
    })
  }

  getName() {
    return this.name
  }

  kill() {
    this.childProcess && this.childProcess.kill()
  }

  protected startCollabSession() {
    this.repeater = setInterval(() => {
      if (!this.latestMessage) {
        return
      }

      io.emit(this.latestMessage.type, this.latestMessage.payload)
    }, 500)
  }

  protected stopCollabSession(message: Message) {
    clearInterval(this.repeater)

    io.emit(message.type, message.payload)
  }
}

export { ProcessBase }
