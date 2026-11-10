import type { CSSProperties } from 'react'

const paths = {
  search: 'm21 21-5.2-5.2M18 10.5a7.5 7.5 0 1 1-15 0 7.5 7.5 0 0 1 15 0Z',
  chevron: 'm6 9 6 6 6-6',
  arrow: 'M4 12h16m-6-6 6 6-6 6',
  heart: 'M20.8 4.6a5.5 5.5 0 0 0-7.8 0L12 5.7l-1.1-1.1a5.5 5.5 0 0 0-7.8 7.8L12 21l8.8-8.6a5.5 5.5 0 0 0 0-7.8Z',
  cart: 'M2 3h3l3 13h11l3-10H6M10 21h.01M18 21h.01',
  bell: 'M18 8a6 6 0 0 0-12 0c0 7-3 7-3 9h18c0-2-3-2-3-9M10 21h4',
  truck: 'M1 4h13v13H1zM14 8h5l4 5v4h-9M8 18a2 2 0 1 1-4 0 2 2 0 0 1 4 0Zm13 0a2 2 0 1 1-4 0 2 2 0 0 1 4 0Z',
  camera: 'M3 6h4l2-3h6l2 3h4v15H3ZM16 13a4 4 0 1 1-8 0 4 4 0 0 1 8 0Z',
  close: 'm6 6 12 12M6 18 18 6',
  bag: 'M4 7h16l1 15H3L4 7Zm4 0V5a4 4 0 0 1 8 0v2',
  shield: 'm12 2 9 4v6c0 6-9 10-9 10S3 18 3 12V6l9-4Zm-4 10 3 3 5-6',
  user: 'M20 22v-2a8 8 0 0 0-16 0v2M17 7a5 5 0 1 1-10 0 5 5 0 0 1 10 0Z',
} as const

export function Icon({ name, size = 22, style }: { name: keyof typeof paths; size?: number; style?: CSSProperties }) {
  return <svg width={size} height={size} viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.65" strokeLinecap="round" strokeLinejoin="round" aria-hidden="true" style={style}><path d={paths[name]} /></svg>
}
