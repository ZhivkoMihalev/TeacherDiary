import { useTranslation } from 'react-i18next'

export function Footer() {
  const { t } = useTranslation()
  const currentYear = new Date().getFullYear()

  const infoLinks = [
    t('footer.privacyPolicy'),
    t('footer.termsOfUse'),
    t('footer.helpCenter'),
    t('footer.faq'),
  ]

  return (
    <footer style={{
      background: 'rgba(255,255,255,0.65)',
      backdropFilter: 'blur(16px)',
      WebkitBackdropFilter: 'blur(16px)',
      borderTop: '1px solid rgba(255,255,255,0.9)',
    }}>
      <div style={{ padding: '36px 28px' }}>
        <div className="grid grid-cols-1 md:grid-cols-3 gap-10">

          {/* Brand */}
          <div>
            <p style={{
              margin: '0 0 10px 0',
              fontFamily: "'Bricolage Grotesque', system-ui, sans-serif",
              fontWeight: 800,
              fontSize: '1.2rem',
              letterSpacing: '-0.03em',
              background: 'linear-gradient(135deg, #7c3aed 0%, #db2777 100%)',
              WebkitBackgroundClip: 'text',
              WebkitTextFillColor: 'transparent',
              backgroundClip: 'text',
              lineHeight: 1,
            }}>
              TeacherDiary
            </p>
            <p style={{ fontSize: '0.875rem', lineHeight: 1.65, color: '#6d28d9', margin: 0, fontFamily: "'Plus Jakarta Sans', system-ui, sans-serif" }}>
              {t('footer.description')}
            </p>
          </div>

          {/* Information */}
          <div>
            <p style={{ margin: '0 0 12px 0', fontSize: '0.7rem', fontWeight: 700, letterSpacing: '0.1em', textTransform: 'uppercase', color: '#a78bfa', fontFamily: "'Plus Jakarta Sans', sans-serif" }}>
              {t('footer.information')}
            </p>
            <ul style={{ listStyle: 'none', margin: 0, padding: 0, display: 'flex', flexDirection: 'column', gap: '8px' }}>
              {infoLinks.map(label => (
                <li key={label}>
                  <a href="#" style={{ fontSize: '0.875rem', color: '#6d28d9', textDecoration: 'none', transition: 'color 0.15s', fontFamily: "'Plus Jakarta Sans', sans-serif" }}
                    onMouseEnter={e => (e.currentTarget.style.color = '#3b0764')}
                    onMouseLeave={e => (e.currentTarget.style.color = '#6d28d9')}>
                    {label}
                  </a>
                </li>
              ))}
            </ul>
          </div>

          {/* Contacts */}
          <div>
            <p style={{ margin: '0 0 12px 0', fontSize: '0.7rem', fontWeight: 700, letterSpacing: '0.1em', textTransform: 'uppercase', color: '#a78bfa', fontFamily: "'Plus Jakarta Sans', sans-serif" }}>
              {t('footer.contacts')}
            </p>
            <ul style={{ listStyle: 'none', margin: 0, padding: 0, display: 'flex', flexDirection: 'column', gap: '9px' }}>
              <li style={{ display: 'flex', alignItems: 'flex-start', gap: '8px' }}>
                <svg xmlns="http://www.w3.org/2000/svg" style={{ width: '14px', height: '14px', marginTop: '2px', flexShrink: 0, color: '#7c3aed' }} fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={1.75}>
                  <path strokeLinecap="round" strokeLinejoin="round" d="M3 8l7.89 5.26a2 2 0 002.22 0L21 8M5 19h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v10a2 2 0 002 2z" />
                </svg>
                <a href="mailto:support@teacherdiary.bg" style={{ fontSize: '0.875rem', color: '#6d28d9', textDecoration: 'none', transition: 'color 0.15s', fontFamily: "'Plus Jakarta Sans', sans-serif", wordBreak: 'break-all' }}
                  onMouseEnter={e => (e.currentTarget.style.color = '#3b0764')}
                  onMouseLeave={e => (e.currentTarget.style.color = '#6d28d9')}>
                  support@teacherdiary.bg
                </a>
              </li>
              <li style={{ display: 'flex', alignItems: 'flex-start', gap: '8px' }}>
                <svg xmlns="http://www.w3.org/2000/svg" style={{ width: '14px', height: '14px', marginTop: '2px', flexShrink: 0, color: '#7c3aed' }} fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={1.75}>
                  <path strokeLinecap="round" strokeLinejoin="round" d="M8.228 9c.549-1.165 2.03-2 3.772-2 2.21 0 4 1.343 4 3 0 1.4-1.278 2.575-3.006 2.907-.542.104-.994.54-.994 1.093m0 3h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
                </svg>
                <span style={{ fontSize: '0.875rem', color: '#6d28d9', fontFamily: "'Plus Jakarta Sans', sans-serif" }}>{t('footer.businessHours')}</span>
              </li>
              <li style={{ display: 'flex', alignItems: 'flex-start', gap: '8px' }}>
                <svg xmlns="http://www.w3.org/2000/svg" style={{ width: '14px', height: '14px', marginTop: '3px', flexShrink: 0, color: '#7c3aed' }} fill="currentColor" viewBox="0 0 24 24">
                  <path d="M12 0C5.37 0 0 5.37 0 12c0 5.3 3.438 9.8 8.205 11.385.6.113.82-.258.82-.577 0-.285-.01-1.04-.015-2.04-3.338.724-4.042-1.61-4.042-1.61-.546-1.385-1.335-1.755-1.335-1.755-1.087-.744.084-.729.084-.729 1.205.084 1.838 1.236 1.838 1.236 1.07 1.835 2.809 1.305 3.495.998.108-.776.417-1.305.76-1.605-2.665-.3-5.466-1.332-5.466-5.93 0-1.31.465-2.38 1.235-3.22-.135-.303-.54-1.523.105-3.176 0 0 1.005-.322 3.3 1.23.96-.267 1.98-.399 3-.405 1.02.006 2.04.138 3 .405 2.28-1.552 3.285-1.23 3.285-1.23.645 1.653.24 2.873.12 3.176.765.84 1.23 1.91 1.23 3.22 0 4.61-2.805 5.625-5.475 5.92.42.36.81 1.096.81 2.22 0 1.606-.015 2.896-.015 3.286 0 .315.21.69.825.57C20.565 21.795 24 17.295 24 12c0-6.63-5.37-12-12-12z"/>
                </svg>
                <a href="https://github.com/ZhivkoMihalev" target="_blank" rel="noopener noreferrer"
                  style={{ fontSize: '0.875rem', color: '#6d28d9', textDecoration: 'none', transition: 'color 0.15s', fontFamily: "'Plus Jakarta Sans', sans-serif" }}
                  onMouseEnter={e => (e.currentTarget.style.color = '#3b0764')}
                  onMouseLeave={e => (e.currentTarget.style.color = '#6d28d9')}>
                  github.com/ZhivkoMihalev
                </a>
              </li>
            </ul>
          </div>
        </div>
      </div>

      <div style={{ borderTop: '1px solid rgba(124,58,237,0.1)', padding: '12px 28px', display: 'flex', flexWrap: 'wrap', alignItems: 'center', justifyContent: 'space-between', gap: '8px', fontSize: '0.75rem', fontFamily: "'Plus Jakarta Sans', sans-serif", color: '#a78bfa' }}>
        <span>{t('footer.copyright', { year: currentYear })}</span>
        <span style={{ background: 'linear-gradient(135deg, #7c3aed, #db2777)', WebkitBackgroundClip: 'text', WebkitTextFillColor: 'transparent', backgroundClip: 'text', fontWeight: 600 }}>
          {t('footer.madeWith')}
        </span>
      </div>
    </footer>
  )
}
