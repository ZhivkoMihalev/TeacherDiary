import { useEffect } from 'react'
import confetti from 'canvas-confetti'

interface Props {
  studentName: string
  bookTitle: string
  onClose: () => void
}

export function CelebrationDialog({ studentName, bookTitle, onClose }: Props) {
  useEffect(() => {
    const end = Date.now() + 2500

    const frame = () => {
      confetti({
        particleCount: 6,
        angle: 60,
        spread: 55,
        origin: { x: 0, y: 0.65 },
        colors: ['#6366f1', '#10b981', '#f59e0b', '#ef4444', '#3b82f6'],
      })
      confetti({
        particleCount: 6,
        angle: 120,
        spread: 55,
        origin: { x: 1, y: 0.65 },
        colors: ['#6366f1', '#10b981', '#f59e0b', '#ef4444', '#3b82f6'],
      })

      if (Date.now() < end) requestAnimationFrame(frame)
    }

    frame()

    const timer = setTimeout(onClose, 3000)
    return () => clearTimeout(timer)
  }, [onClose])

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center">
      <div className="absolute inset-0 bg-black/40" onClick={onClose} />
      <div className="relative bg-white rounded-2xl shadow-xl px-8 py-8 max-w-sm w-full mx-4 text-center">
        <div className="text-5xl mb-4">🎉</div>
        <h2 className="text-xl font-bold text-gray-900 mb-2">Поздравления!</h2>
        <p className="text-gray-600 text-sm leading-relaxed">
          <span className="font-semibold text-gray-800">{studentName}</span> прочете книгата{' '}
          <span className="font-semibold text-gray-800">„{bookTitle}"</span>!
        </p>
        <button
          onClick={onClose}
          className="mt-6 px-5 py-2 text-sm font-medium rounded-lg bg-indigo-600 text-white hover:bg-indigo-700"
        >
          Затвори
        </button>
      </div>
    </div>
  )
}
