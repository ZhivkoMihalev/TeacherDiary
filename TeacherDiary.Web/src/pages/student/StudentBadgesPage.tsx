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
      <div className="mb-6">
        <h1 className="text-2xl font-bold text-gray-900">Моите значки</h1>
        <p className="text-sm text-gray-500 mt-0.5">Събери ги всички, като четеш и изпълняваш задачи</p>
      </div>

      <Card>
        <CardHeader>
          <span className="flex items-center gap-2">🎖️ Спечелени значки <span className="bg-indigo-100 text-indigo-700 text-xs font-bold px-2 py-0.5 rounded-full">{badges.length}</span></span>
        </CardHeader>
        <CardBody>
          {badges.length === 0 ? (
            <div className="text-center py-12">
              <div className="text-5xl mb-3">🏅</div>
              <p className="font-semibold text-gray-700">Все още нямаш значки</p>
              <p className="text-sm text-gray-400 mt-1">Чети книги и изпълнявай задачи, за да спечелиш!</p>
            </div>
          ) : (
            <div className="grid grid-cols-2 sm:grid-cols-3 gap-4">
              {badges.map((b) => (
                <div key={b.code} className="flex flex-col items-center gap-2 bg-gradient-to-b from-indigo-50 to-white border border-indigo-100 rounded-2xl p-5 text-center hover:shadow-md transition-shadow">
                  {b.icon ? (
                    <img src={b.icon} alt={b.name} className="w-14 h-14 object-contain" />
                  ) : (
                    <div className="w-14 h-14 rounded-full bg-indigo-100 flex items-center justify-center text-2xl">🎖️</div>
                  )}
                  <p className="font-bold text-gray-900 text-sm">{b.name}</p>
                  <p className="text-xs text-gray-500 leading-relaxed">{b.description}</p>
                  <p className="text-xs text-indigo-400 mt-1 font-medium">
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