interface Props {
  message: string
  onClose: () => void
}

export function AlertDialog({ message, onClose }: Props) {
  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center">
      <div className="absolute inset-0 bg-black/40" onClick={onClose} />
      <div className="relative bg-white rounded-xl shadow-lg p-6 max-w-sm w-full mx-4">
        <p className="text-gray-800 text-sm mb-5">{message}</p>
        <div className="flex justify-end">
          <button
            onClick={onClose}
            className="px-4 py-2 text-sm font-medium rounded-lg bg-indigo-600 text-white hover:bg-indigo-700"
          >
            Разбрах
          </button>
        </div>
      </div>
    </div>
  )
}
