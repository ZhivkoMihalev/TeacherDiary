import { useState, useEffect } from 'react'
import { useLocation } from 'react-router-dom'
import { BANNERS } from '../banners.config'

function pickRandom(exclude?: number): number {
  if (BANNERS.length <= 1) return 0
  let idx: number
  do { idx = Math.floor(Math.random() * BANNERS.length) } while (idx === exclude)
  return idx
}

export function InlineBanner() {
  const { pathname } = useLocation()
  const [index, setIndex] = useState(() => Math.floor(Math.random() * Math.max(BANNERS.length, 1)))

  useEffect(() => {
    setIndex(prev => pickRandom(prev))
  }, [pathname])

  useEffect(() => {
    if (BANNERS.length <= 1) return
    const id = setInterval(() => setIndex(prev => pickRandom(prev)), 10_000)
    return () => clearInterval(id)
  }, [pathname])

  if (BANNERS.length === 0) return null

  const banner = BANNERS[index]

  return (
    <div style={{
      display: 'flex',
      flexDirection: 'column',
      alignItems: 'center',
      gap: '6px',
      padding: '20px 16px',
      borderTop: '1px solid rgba(124,58,237,0.08)',
    }}>
      <p style={{
        margin: 0,
        fontSize: '0.68rem',
        fontWeight: 600,
        color: '#c4b5fd',
        letterSpacing: '0.08em',
        textTransform: 'uppercase',
        fontFamily: "'Plus Jakarta Sans', sans-serif",
      }}>
        Реклама
      </p>

      {/* IAB Medium Rectangle — 300×250 */}
      <a
        href={banner.linkUrl}
        target="_blank"
        rel="noopener noreferrer"
        style={{
          display: 'block',
          minWidth: '300px',
          minHeight: '250px',
          borderRadius: '12px',
          overflow: 'hidden',
          boxShadow: '0 2px 12px rgba(124,58,237,0.10)',
          transition: 'opacity 0.2s, transform 0.2s',
        }}
        aria-label="Реклама"
        onMouseEnter={e => {
          e.currentTarget.style.opacity = '0.92'
          e.currentTarget.style.transform = 'translateY(-1px)'
        }}
        onMouseLeave={e => {
          e.currentTarget.style.opacity = '1'
          e.currentTarget.style.transform = 'translateY(0)'
        }}
      >
        <img
          src={banner.imageUrl}
          alt="Реклама"
          style={{ width: '300px', height: '250px', objectFit: 'cover', objectPosition: 'top', display: 'block' }}
          onError={(e) => { (e.target as HTMLImageElement).style.display = 'none' }}
        />
      </a>
    </div>
  )
}
