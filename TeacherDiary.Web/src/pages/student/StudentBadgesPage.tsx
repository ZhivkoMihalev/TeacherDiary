import { useQuery } from '@tanstack/react-query'
import { studentApi } from '../../api/student'
import { Spinner } from '../../components/ui/Spinner'

export function StudentBadgesPage() {
  const { data: badges = [], isLoading } = useQuery({
    queryKey: ['student-badges'],
    queryFn: studentApi.getMyBadges,
  })

  if (isLoading) return <div className="flex items-center justify-center h-64"><Spinner /></div>

  return (
    <div className="p-6 max-w-3xl mx-auto">
      {/* Header */}
      <div className="mb-6">
        <h1 className="text-3xl font-black text-[#1C1917]">Моите значки</h1>
        <p className="text-sm text-[#78716C] font-semibold mt-0.5">
          Спечелени: <span className="text-[#14532D] font-black">{badges.length}</span>
          {' '}· Чети и изпълнявай задачи, за да спечелиш повече!
        </p>
      </div>

      {badges.length === 0 ? (
        <div className="bg-white rounded-3xl border border-[#EAE8E0] shadow-sm p-12 text-center">
          <div className="text-6xl mb-4">🏅</div>
          <p className="font-black text-xl text-[#1C1917]">Все още нямаш значки</p>
          <p className="text-sm text-[#A8A29E] mt-2 font-semibold">
            Чети книги и изпълнявай задачи, за да спечелиш!
          </p>
        </div>
      ) : (
        <div className="grid grid-cols-2 sm:grid-cols-3 gap-4">
          {badges.map((b) => (
            <BadgeCard key={b.code} badge={b} />
          ))}
        </div>
      )}
    </div>
  )
}

interface BadgeData {
  code: string
  name: string
  description: string
  icon: string
  awardedAt: string | Date
}

function BadgeCard({ badge: b }: { badge: BadgeData }) {
  // Pick a gradient based on icon/code hint
  const gradient = pickGradient(b.code)

  return (
    <div
      className="relative flex flex-col items-center rounded-3xl p-5 text-center border border-white/60 shadow-md hover:shadow-lg hover:-translate-y-0.5 transition-all duration-200 overflow-hidden"
      style={{ background: gradient.bg }}
    >
      {/* Decorative circle glow behind icon */}
      <div
        className="absolute top-3 left-1/2 -translate-x-1/2 w-20 h-20 rounded-full opacity-30 blur-xl"
        style={{ background: gradient.glow }}
      />

      {/* Icon */}
      <div className="relative z-10 mb-3">
        <div
          className="w-16 h-16 rounded-2xl flex items-center justify-center shadow-md"
          style={{ background: gradient.iconBg }}
        >
          {b.icon && (b.icon.startsWith('/') || b.icon.startsWith('http')) ? (
            <img src={b.icon} alt={b.name} className="w-10 h-10 object-contain" />
          ) : b.icon && /\P{ASCII}/u.test(b.icon) ? (
            <span className="text-3xl leading-none">{b.icon}</span>
          ) : (
            <span className="text-3xl leading-none">{gradient.fallbackEmoji}</span>
          )}
        </div>
      </div>

      {/* Name */}
      <p className="relative z-10 font-black text-sm text-[#1C1917] leading-tight">{b.name}</p>

      {/* Description */}
      <p className="relative z-10 text-xs text-[#78716C] leading-relaxed mt-1 font-semibold">{b.description}</p>

      {/* Date chip */}
      <div
        className="relative z-10 mt-3 px-2.5 py-0.5 rounded-full text-xs font-bold"
        style={{ background: gradient.dateBg, color: gradient.dateText }}
      >
        {new Date(b.awardedAt).toLocaleDateString('bg-BG')}
      </div>
    </div>
  )
}

interface GradientDef {
  bg: string
  glow: string
  iconBg: string
  dateBg: string
  dateText: string
  fallbackEmoji: string
}

function pickGradient(code: string): GradientDef {
  const c = code.toLowerCase()

  if (c.includes('streak') || c.includes('day')) {
    return {
      bg: 'linear-gradient(145deg, #FFF7ED 0%, #FFEDD5 100%)',
      glow: '#FB923C',
      iconBg: 'linear-gradient(135deg, #FB923C, #F97316)',
      dateBg: '#FFEDD5',
      dateText: '#C2410C',
      fallbackEmoji: '🔥',
    }
  }
  if (c.includes('points') || c.includes('reach')) {
    return {
      bg: 'linear-gradient(145deg, #FFFBEB 0%, #FEF3C7 100%)',
      glow: '#FBBF24',
      iconBg: 'linear-gradient(135deg, #FBBF24, #F59E0B)',
      dateBg: '#FEF3C7',
      dateText: '#B45309',
      fallbackEmoji: '⭐',
    }
  }
  if (c.includes('pages') || c.includes('read_')) {
    return {
      bg: 'linear-gradient(145deg, #ECFDF5 0%, #D1FAE5 100%)',
      glow: '#34D399',
      iconBg: 'linear-gradient(135deg, #34D399, #10B981)',
      dateBg: '#D1FAE5',
      dateText: '#065F46',
      fallbackEmoji: '📜',
    }
  }
  if (c.includes('book')) {
    return {
      bg: 'linear-gradient(145deg, #ECFDF5 0%, #D1FAE5 100%)',
      glow: '#34D399',
      iconBg: 'linear-gradient(135deg, #34D399, #10B981)',
      dateBg: '#D1FAE5',
      dateText: '#065F46',
      fallbackEmoji: '📖',
    }
  }
  if (c.includes('assignment') || c.includes('complete')) {
    return {
      bg: 'linear-gradient(145deg, #EEF2FF 0%, #E0E7FF 100%)',
      glow: '#818CF8',
      iconBg: 'linear-gradient(135deg, #818CF8, #6366F1)',
      dateBg: '#E0E7FF',
      dateText: '#3730A3',
      fallbackEmoji: '✅',
    }
  }
  // Default
  return {
    bg: 'linear-gradient(145deg, #F0F9FF 0%, #E0F2FE 100%)',
    glow: '#38BDF8',
    iconBg: 'linear-gradient(135deg, #38BDF8, #0EA5E9)',
    dateBg: '#E0F2FE',
    dateText: '#0369A1',
    fallbackEmoji: '🏅',
  }
}
