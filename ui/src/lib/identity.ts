const KEY = 'splitly.identity'

type IdentityMap = Record<string, string>

function all(): IdentityMap {
  try {
    return JSON.parse(localStorage.getItem(KEY) ?? '{}')
  } catch {
    return {}
  }
}

export const SKIPPED = 'skipped'

export function identityFor(groupId: string): string | null {
  return all()[groupId] ?? null
}

export function setIdentity(groupId: string, participantId: string): void {
  localStorage.setItem(KEY, JSON.stringify({ ...all(), [groupId]: participantId }))
}

export function clearIdentity(groupId: string): void {
  const map = all()
  delete map[groupId]
  localStorage.setItem(KEY, JSON.stringify(map))
}
