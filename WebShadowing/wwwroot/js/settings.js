"use strict";

/**
 * Chức năng: trang Tài khoản — tải/lưu profile/settings, đổi mode, theme và VIP.
 * Phụ trách chính: Hải Anh. Minh phối hợp policy auth/mode/payment.
 * Checkout hiện là demo local, không được coi là thanh toán production.
 */
(function () {
    let profile = null;
    document.addEventListener("DOMContentLoaded", function () {
        document.getElementById("profile-form")?.addEventListener("submit", saveAllSettings);
        document.getElementById("theme-quick-toggle")?.addEventListener("click", toggleTheme);
        document.querySelectorAll("[data-checkout-provider]").forEach(button => button.addEventListener("click", () => checkout(button.dataset.checkoutProvider)));
        document.getElementById("cancel-subscription")?.addEventListener("click", cancelSubscription);
        loadProfile(); loadSubscription();
        if (document.querySelector(".settings-page")?.dataset.openCheckout === "true") document.getElementById("vip-settings")?.scrollIntoView({ behavior: "smooth" });
    });

    async function loadProfile() {
        status("Đang tải cài đặt...");
        try {
            const response = await fetch("/api/user/profile", { cache: "no-store" });
            if (!response.ok) throw new Error("Không tải được hồ sơ.");
            profile = await response.json();
            value("profile-full-name", profile.fullName); value("profile-email", profile.email); value("profile-phone", profile.phone || "");
            value("pronunciation-target", String(profile.pronunciationTarget)); radio("accent-setting", profile.accent); value("theme-setting", profile.theme || "system");
            value("learning-mode-setting", profile.learningMode); checked("auto-save-ai", profile.autoSaveAiLessons);
            const remaining = profile.isVip ? "Không giới hạn cho VIP" : `${profile.freeModeChangesRemaining} lượt miễn phí còn lại · ${profile.modeChangeExpCost} EXP/lượt tiếp theo`;
            text("mode-policy-copy", remaining); status("");
        } catch (error) { status(error.message, true); }
    }

    async function saveAllSettings(event) {
        event.preventDefault();

        status("Đang lưu...");
        try {
            // Change the learning mode first. The following requests are deliberately
            // sequential because both profile and settings update the Users row,
            // which is protected by an optimistic-concurrency row_version.
            const currentMode = document.getElementById("learning-mode-setting")?.value;
            if (currentMode && currentMode !== profile?.learningMode) {
                if (!window.confirm("Đổi mode sẽ thay đổi thư viện bài học hiển thị. Bạn muốn tiếp tục?")) {
                    document.getElementById("learning-mode-setting").value = profile?.learningMode;
                    status("");
                    return;
                }
                let modeResult = await requestMode(currentMode, false);
                if (!modeResult?.succeeded && modeResult?.message?.includes("EXP") && window.confirm(`${modeResult.message} Bạn có muốn dùng EXP không?`)) {
                    modeResult = await requestMode(currentMode, true);
                }
                if (!modeResult?.succeeded) {
                    document.getElementById("learning-mode-setting").value = profile?.learningMode;
                    throw new Error(modeResult?.message || "Không đổi được mode.");
                }
            }

            // Save Profile
            const profileRes = await fetch("/api/user/profile", {
                method: "PUT",
                headers: { "Content-Type": "application/json" },
                body: JSON.stringify({
                    fullName: document.getElementById("profile-full-name")?.value.trim(),
                    phone: document.getElementById("profile-phone")?.value.trim() || null
                })
            });

            if (!profileRes.ok) {
                const err = await profileRes.json().catch(() => ({}));
                throw new Error(err.message || "Không lưu được thông tin cá nhân.");
            }

            // Save Settings
            const theme = document.getElementById("theme-setting")?.value || "system";
            const settingsRes = await fetch("/api/user/settings", {
                method: "PUT",
                headers: { "Content-Type": "application/json" },
                body: JSON.stringify({
                    pronunciationTarget: Number(document.getElementById("pronunciation-target")?.value),
                    accent: document.querySelector('input[name="accent-setting"]:checked')?.value || "en-us",
                    autoSaveAiLessons: document.getElementById("auto-save-ai")?.checked,
                    theme
                })
            });

            if (!settingsRes.ok) {
                const err = await settingsRes.json().catch(() => ({}));
                throw new Error(err.message || "Không lưu được cấu hình học tập.");
            }

            // Load profile again to get fresh state (including updated mode changes count, etc.)
            await loadProfile();

            // Apply theme logic
            const effective = theme === "system" ? (matchMedia("(prefers-color-scheme: dark)").matches ? "dark" : "light") : theme;
            localStorage.setItem("theme", effective);
            document.documentElement.classList.toggle("dark", effective === "dark");

            status("Đã lưu thay đổi thành công.");
        } catch (error) {
            status(error.message, true);
        }
    }

    async function requestMode(learningMode, useExpIfNeeded) {
        const response = await fetch("/api/user/mode", { method: "POST", headers: { "Content-Type": "application/json" }, body: JSON.stringify({ learningMode, useExpIfNeeded }) });
        return response.json().catch(() => ({ succeeded: false, message: "Không đổi được mode." }));
    }

    function toggleTheme() {
        const select = document.getElementById("theme-setting");
        if (!select) return;
        const isDark = document.documentElement.classList.contains("dark");
        select.value = isDark ? "light" : "dark";
        document.getElementById("profile-form")?.requestSubmit();
    }

    async function loadSubscription() {
        try {
            const response = await fetch("/api/subscription", { cache: "no-store" });
            if (!response.ok) return;
            const item = response.status === 204 ? null : await response.json();
            if (!item) { text("subscription-summary", "Bạn đang dùng gói Free."); return; }
            const ends = item.endsAt ? new Date(item.endsAt).toLocaleDateString("vi-VN") : "không thời hạn";
            text("subscription-summary", `${item.status.toUpperCase()} · ${item.provider} · hết hạn ${ends}`);
            document.getElementById("cancel-subscription")?.classList.toggle("d-none", item.status !== "active" || !item.autoRenew);
        } catch (_) { text("subscription-summary", "Không tải được trạng thái subscription."); }
    }

    async function checkout(provider) {
        const period = document.querySelector('input[name="vip-period"]:checked')?.value || "monthly";
        text("payment-status", `Đang kết nối ${provider === "momo" ? "MoMo" : "ZaloPay"}...`);
        document.querySelectorAll("[data-checkout-provider]").forEach(button => button.disabled = true);
        try {
            const response = await fetch("/api/payment/checkout", {
                method: "POST", headers: { "Content-Type": "application/json" },
                body: JSON.stringify({ provider, billingPeriod: period, idempotencyKey: `checkout-${crypto.randomUUID?.() || Date.now()}` })
            });
            const result = await response.json().catch(() => ({}));
            if (!response.ok || !result.succeeded) throw new Error(result.message || "Không tạo được phiên thanh toán.");
            if (result.payUrl) {
                location.assign(result.payUrl);
                return;
            }
            text("payment-status", result.message || "VIP đã được kích hoạt.");
            document.querySelectorAll("[data-checkout-provider]").forEach(button => button.disabled = false);
            await Promise.all([loadProfile(), loadSubscription()]);
        } catch (error) { text("payment-status", error.message); document.querySelectorAll("[data-checkout-provider]").forEach(button => button.disabled = false); }
    }

    async function cancelSubscription() {
        const response = await fetch("/api/subscription/cancel", { method: "POST" });
        text("payment-status", response.ok ? "Đã tắt gia hạn. Quyền VIP giữ đến hết kỳ hiện tại." : "Không hủy được gia hạn.");
        if (response.ok) loadSubscription();
    }

    function status(message, error) { const node = document.getElementById("settings-global-status"); if (node) { node.textContent = message || ""; node.classList.toggle("is-error", Boolean(error)); } }
    function text(id, content) { const node = document.getElementById(id); if (node) node.textContent = content; }
    function value(id, content) { const node = document.getElementById(id); if (node) node.value = content; }
    function checked(id, content) { const node = document.getElementById(id); if (node) node.checked = Boolean(content); }
    function radio(name, content) { document.querySelectorAll(`input[name="${name}"]`).forEach(node => { node.checked = node.value === content; }); }
})();
