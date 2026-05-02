export function Footer() {
  const currentYear = new Date().getFullYear()

  return (
    <footer style={{ backgroundColor: '#1e2640' }} className="text-slate-200 mt-auto shrink-0">
      <div className="px-8 py-10">
        <div className="grid grid-cols-1 md:grid-cols-3 gap-10">

          {/* Брандинг */}
          <div>
            <p className="text-white font-bold text-lg mb-2">TeacherDiary</p>
            <p className="text-sm text-slate-300 leading-relaxed">
              Платформа за проследяване на учебния напредък на учениците и стимулиране на тяхното развитие чрез четене, задачи и предизвикателства.
            </p>
          </div>

          {/* Информация */}
          <div>
            <p className="text-white font-semibold mb-3 text-sm uppercase tracking-wide">Информация</p>
            <ul className="space-y-2 text-sm text-slate-300">
              <li>
                <a href="#" className="hover:text-white transition-colors">
                  Политика за поверителност
                </a>
              </li>
              <li>
                <a href="#" className="hover:text-white transition-colors">
                  Условия за ползване
                </a>
              </li>
              <li>
                <a href="#" className="hover:text-white transition-colors">
                  Помощен център
                </a>
              </li>
              <li>
                <a href="#" className="hover:text-white transition-colors">
                  Често задавани въпроси
                </a>
              </li>
            </ul>
          </div>

          {/* Контакти */}
          <div>
            <p className="text-white font-semibold mb-3 text-sm uppercase tracking-wide">Контакти</p>
            <ul className="space-y-3 text-sm text-slate-300">
              <li className="flex items-start gap-2">
                <svg xmlns="http://www.w3.org/2000/svg" className="w-4 h-4 mt-0.5 shrink-0 text-indigo-400" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={2}>
                  <path strokeLinecap="round" strokeLinejoin="round" d="M3 8l7.89 5.26a2 2 0 002.22 0L21 8M5 19h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v10a2 2 0 002 2z" />
                </svg>
                <a href="mailto:support@teacherdiary.bg" className="hover:text-white transition-colors break-all">
                  support@teacherdiary.bg
                </a>
              </li>
              <li className="flex items-start gap-2">
                <svg xmlns="http://www.w3.org/2000/svg" className="w-4 h-4 mt-0.5 shrink-0 text-indigo-400" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={2}>
                  <path strokeLinecap="round" strokeLinejoin="round" d="M8.228 9c.549-1.165 2.03-2 3.772-2 2.21 0 4 1.343 4 3 0 1.4-1.278 2.575-3.006 2.907-.542.104-.994.54-.994 1.093m0 3h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
                </svg>
                <span>Понеделник – Петък, 09:00 – 18:00 ч.</span>
              </li>
              <li className="flex items-start gap-2">
                <svg xmlns="http://www.w3.org/2000/svg" className="w-4 h-4 mt-0.5 shrink-0 text-indigo-400" fill="currentColor" viewBox="0 0 24 24">
                  <path d="M12 0C5.37 0 0 5.37 0 12c0 5.3 3.438 9.8 8.205 11.385.6.113.82-.258.82-.577 0-.285-.01-1.04-.015-2.04-3.338.724-4.042-1.61-4.042-1.61-.546-1.385-1.335-1.755-1.335-1.755-1.087-.744.084-.729.084-.729 1.205.084 1.838 1.236 1.838 1.236 1.07 1.835 2.809 1.305 3.495.998.108-.776.417-1.305.76-1.605-2.665-.3-5.466-1.332-5.466-5.93 0-1.31.465-2.38 1.235-3.22-.135-.303-.54-1.523.105-3.176 0 0 1.005-.322 3.3 1.23.96-.267 1.98-.399 3-.405 1.02.006 2.04.138 3 .405 2.28-1.552 3.285-1.23 3.285-1.23.645 1.653.24 2.873.12 3.176.765.84 1.23 1.91 1.23 3.22 0 4.61-2.805 5.625-5.475 5.92.42.36.81 1.096.81 2.22 0 1.606-.015 2.896-.015 3.286 0 .315.21.69.825.57C20.565 21.795 24 17.295 24 12c0-6.63-5.37-12-12-12z"/>
                </svg>
                <a
                  href="https://github.com/ZhivkoMihalev"
                  target="_blank"
                  rel="noopener noreferrer"
                  className="hover:text-white transition-colors"
                >
                  github.com/ZhivkoMihalev
                </a>
              </li>
            </ul>
          </div>

        </div>
      </div>

      {/* Copyright лента */}
      <div className="border-t border-white/10 px-8 py-4 flex flex-col sm:flex-row items-center justify-between gap-2 text-xs">
        <span className="text-slate-400">© {currentYear} TeacherDiary. Всички права запазени.</span>
        <span className="text-white font-medium">Изградено с ❤️ за учители, родители и ученици.</span>
      </div>
    </footer>
  )
}
