export default {
  defaultTheme: 'auto',
  start: () => {
    // Keep the navy navigation readable independently of the article's color mode.
    document.querySelector('header')?.setAttribute('data-bs-theme', 'dark');
  },
  iconLinks: [
    {
      icon: 'github',
      href: 'https://github.com/Gh0st3r0k/ChromaTray',
      title: 'View ChromaTray on GitHub'
    }
  ]
};
