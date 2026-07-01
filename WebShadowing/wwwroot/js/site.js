(function () {
  const storageKey = 'theme';

  function applyTheme(theme) {
    const isDark = theme === 'dark';
    document.documentElement.classList.toggle('dark', isDark);

    const moon = document.querySelector('.btn-theme-toggle .icon-moon');
    const sun = document.querySelector('.btn-theme-toggle .icon-sun');
    if (moon) {
      moon.classList.toggle('d-none', isDark);
    }
    if (sun) {
      sun.classList.toggle('d-none', !isDark);
    }

    const label = document.getElementById('theme-mode-label');
    if (label) {
      label.textContent = isDark ? 'Dark' : 'Light';
    }
  }

  function getInitialTheme() {
    const saved = localStorage.getItem(storageKey);
    if (saved === 'dark' || saved === 'light') {
      return saved;
    }

    return window.matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light';
  }

  document.addEventListener('DOMContentLoaded', function () {
    applyTheme(getInitialTheme());

    const toggle = document.querySelector('.btn-theme-toggle');
    if (!toggle) {
      return;
    }

    toggle.addEventListener('click', function () {
      const nextTheme = document.documentElement.classList.contains('dark') ? 'light' : 'dark';
      localStorage.setItem(storageKey, nextTheme);
      applyTheme(nextTheme);
    });
  });
})();
