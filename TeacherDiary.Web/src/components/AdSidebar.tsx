import { useState, useEffect } from 'react'
import { useLocation } from 'react-router-dom'
import { BANNERS } from '../banners.config'

function pickRandom(exclude?: number): number {
  if (BANNERS.length <= 1) return 0
  let idx: number
  do { idx = Math.floor(Math.random() * BANNERS.length) } while (idx === exclude)
  return idx
}

export function AdSidebar() {
  const { pathname } = useLocation()
  const [index, setIndex] = useState(() => Math.floor(Math.random() * Math.max(BANNERS.length, 1)))

  // New random banner on every page navigation
  useEffect(() => {
    setIndex(prev => pickRandom(prev))
  }, [pathname])

  // Rotate every 10 seconds
  useEffect(() => {
    if (BANNERS.length <= 1) return
    const id = setInterval(() => setIndex(prev => pickRandom(prev)), 10_000)
    return () => clearInterval(id)
  }, [pathname])

  if (BANNERS.length === 0) return null

  const banner = BANNERS[index]

  return (
    <aside className="w-64 shrink-0 border-l border-gray-200 bg-white flex flex-col">
      <div className="px-3 py-3 border-b border-gray-100">
        <p className="text-xs font-semibold text-gray-400 uppercase tracking-wider">Реклами</p>
      </div>
      <div className="flex-1 relative">
        <a
          href={banner.linkUrl}
          target="_blank"
          rel="noopener noreferrer"
          className="absolute inset-0 hover:opacity-90 transition-opacity"
        >
          <img
            src={banner.imageUrl}
            alt="Реклама"
            className="w-full h-full object-cover object-top"
            onError={(e) => { (e.target as HTMLImageElement).style.display = 'none' }}
          />
        </a>
      </div>
    </aside>
  )
}
