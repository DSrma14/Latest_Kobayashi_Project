import * as express from 'express'
import * as http from 'http'
import { Server } from 'socket.io'

const zeaDebug = console.info

const app = express()
const server = http.createServer(app)

app.get('/', (_, res) => {
  res.send('WebSocket server.')
})

const io = new Server(server, {
  cors: {
    origin: '*',
    methods: ['GET', 'POST'],
  },
})

const port = '3334'

const startExpress = () => {
  server.listen(port, () => {
    zeaDebug(`WebSocket server listening on port ${port}.`)
  })
}

export { io, startExpress }
