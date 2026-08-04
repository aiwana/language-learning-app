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

  });
})();

window.showAppToast = function (message, type) {
  document.querySelector('.app-toast')?.remove();
  const toast = document.createElement('div');
  toast.className = `app-toast${type === 'error' ? ' is-error' : ''}`;
  toast.setAttribute('role', type === 'error' ? 'alert' : 'status');
  toast.textContent = message;
  document.body.appendChild(toast);
  requestAnimationFrame(function () { toast.classList.add('is-visible'); });
  setTimeout(function () {
    toast.classList.remove('is-visible');
    setTimeout(function () { toast.remove(); }, 220);
  }, 3200);
};

(function () {
  function createIdempotencyKey(prefix) {
    const id = window.crypto && window.crypto.randomUUID
      ? window.crypto.randomUUID()
      : `${Date.now()}-${Math.random().toString(16).slice(2)}`;
    return `${prefix}-${id}`;
  }

  function setStat(name, value) {
    document.querySelectorAll(`[data-gamification-stat="${name}"]`).forEach(function (element) {
      element.textContent = value;
    });
  }

  window.applyGamificationBalance = function (balance, broadcast) {
    if (!balance) return;

    setStat('exp', balance.exp);
    setStat('streak', balance.streakDays);
    setStat('hearts', balance.hasInfiniteHearts || balance.isVip ? '∞' : balance.hearts);
    document.querySelectorAll('[data-gamification-vip]').forEach(function (element) {
      element.textContent = balance.isVip ? 'VIP' : 'FREE';
    });

    document.querySelectorAll('[data-exchange-heart]').forEach(function (button) {
      const maxHearts = Number(button.dataset.maxHearts || Number.MAX_SAFE_INTEGER);
      button.disabled = Boolean(balance.isVip) || balance.hearts >= maxHearts;
    });

    document.dispatchEvent(new CustomEvent('gamification:balance', { detail: balance }));

    if (broadcast !== false) {
      try {
        localStorage.setItem('gamification-balance-updated', String(Date.now()));
      } catch (_) {
        // Balance rendering still works when browser storage is unavailable.
      }
    }
  };

  window.applyGamificationTransaction = function (transaction) {
    if (!transaction || !transaction.balance) return;
    window.applyGamificationBalance(transaction.balance);
  };

  async function exchangeHeart(button) {
    const status = document.querySelector('[data-exchange-status]');
    const idempotencyKey = button.dataset.pendingKey || createIdempotencyKey('heart-exchange');
    button.dataset.pendingKey = idempotencyKey;
    button.disabled = true;
    if (status) status.textContent = 'Đang đổi tim...';

    try {
      const response = await fetch('/api/gamification/exchange-heart', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ idempotencyKey: idempotencyKey })
      });
      const result = await response.json().catch(function () { return {}; });
      delete button.dataset.pendingKey;
      window.applyGamificationTransaction(result);

      if (!response.ok || !result.succeeded) {
        throw new Error(result.message || 'Không thể đổi tim lúc này.');
      }

      if (status) {
        status.textContent = result.alreadyProcessed
          ? 'Yêu cầu này đã được xử lý.'
          : `Đã đổi ${Math.abs(result.delta.exp)} EXP lấy ${result.delta.hearts} tim.`;
      }
    } catch (error) {
      if (status) status.textContent = error.message || 'Mất kết nối. Hãy thử lại.';
      if (button.dataset.pendingKey) {
        button.disabled = false;
        return;
      }
    }

    if (!button.dataset.pendingKey && !button.disabled) button.disabled = false;
  }

  document.addEventListener('DOMContentLoaded', function () {
    document.querySelectorAll('[data-exchange-heart]').forEach(function (button) {
      button.addEventListener('click', function () { exchangeHeart(button); });
    });
  });

  window.addEventListener('storage', async function (event) {
    if (event.key !== 'gamification-balance-updated') return;
    try {
      const response = await fetch('/api/gamification/balance', { cache: 'no-store' });
      if (!response.ok) return;
      window.applyGamificationBalance(await response.json(), false);
    } catch (_) {
      // The next successful server transaction will reconcile the balance.
    }
  });
})();
