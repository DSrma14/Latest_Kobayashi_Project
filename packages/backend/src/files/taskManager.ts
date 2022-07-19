import { ProcessBase } from './ProcessBase'

class TaskManager {
  private tasks: ProcessBase[] = []

  stopAll() {
    this.tasks.forEach((task) => {
      task.kill()
    })
    this.tasks = []
  }

  add(task: ProcessBase) {
    this.tasks = [...this.tasks, task]
  }

  remove(task: ProcessBase) {
    this.tasks = this.tasks.filter((e) => e !== task)
  }

  has(name: string) {
    const found = this.tasks.find((e) => e.getName() === name)
    return found
  }
}

const taskManager = new TaskManager()

export { taskManager }
