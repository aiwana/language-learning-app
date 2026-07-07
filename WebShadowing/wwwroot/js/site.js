(function () {
  const storageKey = 'theme';

  function applyTheme(theme) {
    const isDark = theme === 'dark';
    document.documentElement.classList.toggle('dark', isDark);

    document.querySelectorAll('.btn-theme-toggle').forEach(function (toggle) {
      const moon = toggle.querySelector('.icon-moon');
      const sun = toggle.querySelector('.icon-sun');
      if (moon) {
        moon.classList.toggle('d-none', isDark);
      }
      if (sun) {
        sun.classList.toggle('d-none', !isDark);
      }
    });

    document.querySelectorAll('.theme-toggle-label-light').forEach(function (el) {
      el.classList.toggle('d-none', isDark);
    });
    document.querySelectorAll('.theme-toggle-label-dark').forEach(function (el) {
      el.classList.toggle('d-none', !isDark);
    });

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

    document.querySelectorAll('.btn-theme-toggle').forEach(function (toggle) {
      toggle.addEventListener('click', function () {
        const nextTheme = document.documentElement.classList.contains('dark') ? 'light' : 'dark';
        localStorage.setItem(storageKey, nextTheme);
        applyTheme(nextTheme);
      });
    });

    if (document.body.classList.contains('nav-logged-in')) {
      document.body.classList.add('nav-logged-in');
    }
  });
})();
