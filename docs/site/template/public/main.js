const languages = [
  ['en', 'English'], ['sk', 'Slovenčina'], ['cs', 'Čeština'],
  ['ru', 'Русский'], ['uk', 'Українська']
];

// Bootstrap has several hardcoded accessibility labels outside DocFX's token file.
const labels = {
  en: ['Language', 'Toggle navigation', 'Table of Contents', 'Close', 'Show table of contents', 'Search'],
  sk: ['Jazyk', 'Prepnúť navigáciu', 'Obsah', 'Zavrieť', 'Zobraziť obsah', 'Hľadať'],
  cs: ['Jazyk', 'Přepnout navigaci', 'Obsah', 'Zavřít', 'Zobrazit obsah', 'Hledat'],
  ru: ['Язык', 'Открыть меню', 'Содержание', 'Закрыть', 'Показать содержание', 'Поиск'],
  uk: ['Мова', 'Відкрити меню', 'Зміст', 'Закрити', 'Показати зміст', 'Пошук']
};

function addLanguageSelector() {
  const language = document.documentElement.lang || 'en';
  const text = labels[language] || labels.en;
  const toggle = document.querySelector('[data-bs-target="#navpanel"]');
  if (!toggle) return;

  const select = document.createElement('select');
  select.id = 'documentation-language';
  select.className = 'form-select form-select-sm language-selector';
  select.setAttribute('aria-label', text[0]);
  for (const [code, name] of languages) {
    select.add(new Option(name, code, false, code === language));
  }

  // DocFX's relative root also works on /ChromaTray/ GitHub Pages and nested API
  // pages. No absolute domain, fixed repository path, or guessed filename needed.
  const localRoot = new URL(document.querySelector('meta[name="docfx:rel"]').content || './', location.href);
  const siteRoot = language === 'en' ? localRoot : new URL('../', localRoot);
  const page = location.pathname.slice(localRoot.pathname.length) || 'index.html';
  select.addEventListener('change', () => {
    const prefix = select.value === 'en' ? '' : `${select.value}/`;
    const target = new URL(prefix + page, siteRoot);
    target.search = location.search;
    target.hash = location.hash;
    location.assign(target.href);
  });
  toggle.before(select);

  toggle.setAttribute('aria-label', text[1]);
  document.querySelector('#tocOffcanvasLabel')?.replaceChildren(text[2]);
  document.querySelector('.btn-close')?.setAttribute('aria-label', text[3]);
  document.querySelector('[data-bs-target="#tocOffcanvas"]')?.setAttribute('aria-label', text[4]);
  document.querySelector('#search-query')?.setAttribute('aria-label', text[5]);
}

export default {
  defaultTheme: 'auto',
  start: () => {
    // Keep the navy navigation readable independently of the article's color mode.
    document.querySelector('header')?.setAttribute('data-bs-theme', 'dark');
    addLanguageSelector();
  },
  iconLinks: [
    {
      icon: 'github',
      href: 'https://github.com/Gh0st3r0k/ChromaTray',
      title: 'View ChromaTray on GitHub'
    }
  ]
};
