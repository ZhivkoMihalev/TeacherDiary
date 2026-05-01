import { useQuery } from '@tanstack/react-query'
import { studentApi } from '../../api/student'
import { Card, CardBody, CardHeader } from '../../components/ui/Card'
import { Spinner } from '../../components/ui/Spinner'

export function StudentBadgesPage() {
  const { data: badges = [], isLoading } = useQuery({
    queryKey: ['student-badges'],
    queryFn: studentApi.getMyBadges,
  })

  if (isLoading) return <div className="flex items-center justify-center h-64"><Spinner /></div>

  return (
    <div className="p-6 max-w-3xl mx-auto">
      <h1 className="text-2xl font-bold text-gray-900 mb-6">Моите значки</h1>

      <Card>
        <CardHeader>Спечелени значки ({badges.length})</CardHeader>
        <CardBody>
          {badges.length === 0 ? (
            <p className="text-center text-gray-500 py-8">Все още нямаш спечелени значки. Чети и изпълнявай задачи, за да спечелиш!</p>
          ) : (
            <div className="grid grid-cols-2 sm:grid-cols-3 gap-4">
              {badges.map((b) => (
                <div key={b.code} className="flex flex-col items-center gap-2 border border-gray-100 rounded-xl p-4 text-center">
                  {b.icon && (
                    <img src={b.icon} alt={b.name} className="w-12 h-12 object-contain" />
                  )}
                  <p className="font-semibold text-gray-900 text-sm">{b.name}</p>
                  <p className="text-xs text-gray-500">{b.description}</p>
                  <p className="text-xs text-gray-400 mt-1">
                    {new Date(b.awardedAt).toLocaleDateString('bg-BG')}
                  </p>
                </div>
              ))}
            </div>
          )}
        </CardBody>
      </Card>
    </div>
  )
}