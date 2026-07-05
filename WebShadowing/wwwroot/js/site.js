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

    initNavLoginPreview();
  });

  // TODO: [AUTHEN BE INTEGRATION REQUIRED]
  // This function is currently a pure frontend simulation using localStorage.
  // When integrating with the Backend Auth API, replace this logic with 
  // real JWT/Session token verification and actual API calls for login/logout.
  function initNavLoginPreview() {
    const storageKey = 'navLoggedInPreview';

    function applyNavLoginState(isLoggedIn) {
      document.body.classList.toggle('nav-logged-in', isLoggedIn);
    }

    // BE Note: Change this to check valid token/session instead of just localStorage
    applyNavLoginState(localStorage.getItem(storageKey) === 'true');

    document.querySelectorAll('.nav-login-btn').forEach(function (btn) {
      btn.addEventListener('click', function () {
        // TODO: Call BE Login API here, handle token storage (Cookie/Storage)
        localStorage.setItem(storageKey, 'true');
        applyNavLoginState(true);
      });
    });

    document.querySelectorAll('.btn-logout').forEach(function (btn) {
      btn.addEventListener('click', function () {
        // TODO: Call BE Logout API here to revoke token/session
        localStorage.removeItem(storageKey);
        applyNavLoginState(false);
      });
    });
  }
})();
