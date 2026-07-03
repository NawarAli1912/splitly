export function formatMoney(amount: number, currency: string): string {
  return new Intl.NumberFormat(undefined, { style: 'currency', currency }).format(amount)
}

export function initials(name: string): string {
  return name
    .trim()
    .split(/\s+/)
    .slice(0, 2)
    .map((part) => part[0]!.toUpperCase())
    .join('')
}

const AVATAR_COLORS = [
  '#e8f1fd',
  '#eaf7ee',
  '#fdf1e7',
  '#f3ecfb',
  '#fdeaec',
  '#e7f6f8',
  '#f9f0e1',
  '#eef0f9',
]

const AVATAR_TEXT_COLORS = [
  '#0b63c4',
  '#1e7d3c',
  '#b35a12',
  '#6a3bb0',
  '#c0303c',
  '#0e7c8c',
  '#96700f',
  '#3d4a9e',
]

export function avatarStyle(id: string): { backgroundColor: string; color: string } {
  let hash = 0
  for (const char of id) hash = (hash * 31 + char.charCodeAt(0)) | 0
  const index = Math.abs(hash) % AVATAR_COLORS.length
  return { backgroundColor: AVATAR_COLORS[index]!, color: AVATAR_TEXT_COLORS[index]! }
}

// Apple system colors — vivid, for charts and data accents.
const CHART_COLORS = [
  '#0a84ff',
  '#30d158',
  '#ff9f0a',
  '#bf5af2',
  '#ff375f',
  '#64d2ff',
  '#ffd60a',
  '#5e5ce6',
]

export function chartColor(index: number): string {
  return CHART_COLORS[index % CHART_COLORS.length]!
}
