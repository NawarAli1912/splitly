export interface RecentGroup {
  id: string
  name: string
  currency: string
}

const KEY = 'splitly.recentGroups'

export function recentGroups(): RecentGroup[] {
  try {
    return JSON.parse(localStorage.getItem(KEY) ?? '[]')
  } catch {
    return []
  }
}

export function rememberGroup(group: RecentGroup): void {
  const others = recentGroups().filter((g) => g.id !== group.id)
  localStorage.setItem(KEY, JSON.stringify([group, ...others].slice(0, 8)))
}

export function forgetGroup(groupId: string): void {
  localStorage.setItem(KEY, JSON.stringify(recentGroups().filter((g) => g.id !== groupId)))
}
