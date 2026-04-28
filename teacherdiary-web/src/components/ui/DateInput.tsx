import ReactDatePicker from 'react-datepicker'
import 'react-datepicker/dist/react-datepicker.css'

interface Props {
  label: string
  value: string
  onChange: (isoValue: string) => void
  required?: boolean
  className?: string
}

export function DateInput({ label, value, onChange, required, className }: Props) {
  const selected = value ? new Date(value + 'T00:00:00') : null

  function handleChange(date: Date | null) {
    if (!date) { onChange(''); return }
    const y = date.getFullYear()
    const m = String(date.getMonth() + 1).padStart(2, '0')
    const d = String(date.getDate()).padStart(2, '0')
    onChange(`${y}-${m}-${d}`)
  }

  return (
    <div className={`flex flex-col gap-1 ${className ?? ''}`}>
      <label className="text-sm font-medium text-gray-700">{label}</label>
      <ReactDatePicker
        selected={selected}
        onChange={handleChange}
        dateFormat="dd/MM/yyyy"
        placeholderText="дд/мм/гггг"
        required={required}
        className="block w-full rounded-lg border border-gray-300 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-indigo-500"
      />
    </div>
  )
}
