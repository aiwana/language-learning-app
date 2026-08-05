"use strict";

/**
 * Chức năng: tạo bài AI từ trang Khóa học, tải draft/saved card, hiển thị thời gian
 * draft còn lại, lưu/xóa và điều hướng tới preview/detail.
 * Phụ trách chính: Minh Anh. Minh phối hợp API/schema.
 */
(function () {
    let previewId = null;
    document.addEventListener("DOMContentLoaded", function () {
        document.getElementById("ai-generator-form")?.addEventListener("submit", generate);
        document.getElementById("save-ai-lesson")?.addEventListener("click", savePreview);
        loadLibrary();
    });

    async function generate(event) {
        event.preventDefault();
        const prompt = document.getElementById("ai-prompt-input")?.value.trim();
        const count = Number(document.getElementById("ai-level-input")?.value || 8);
        if (!prompt || prompt.length < 5) return status("Hãy mô tả chủ đề bằng ít nhất 5 ký tự.", true);
        setLoading(true);
        status("AI đang viết nội dung và tạo audio. Quá trình này có thể mất khoảng một phút.");
        try {
            const response = await fetch("/api/ai-lessons/generate", {
                method: "POST", headers: { "Content-Type": "application/json" },
                body: JSON.stringify({ prompt, sentenceCount: count })
            });
            const result = await response.json().catch(() => ({}));
            if (!response.ok) throw new Error(result.message || "Không tạo được bài học.");
            previewId = result.previewId;
            status("Bài học đã sẵn sàng. Đang mở Lesson Studio...");
            window.location.assign(result.saved && result.savedLessonId
                ? `/ai-lessons/${result.savedLessonId}`
                : `/ai-lessons/preview/${result.previewId}`);
        } catch (error) { status(error.message || "Mất kết nối với dịch vụ AI.", true); }
        finally { setLoading(false); }
    }

    function renderPreview(result) {
        const section = document.getElementById("ai-lesson-preview");
        const title = document.getElementById("ai-preview-title");
        const list = document.getElementById("ai-preview-segments");
        if (!section || !title || !list) return;
        title.textContent = result.title;
        document.getElementById("save-ai-lesson")?.classList.remove("d-none");
        list.replaceChildren(...result.segments.map(segment => {
            const item = document.createElement("li");
            const copy = document.createElement("div");
            const text = document.createElement("strong"); text.textContent = segment.text;
            const ipa = document.createElement("small"); ipa.textContent = segment.ipa;
            const translation = document.createElement("span"); translation.textContent = segment.translation;
            copy.append(text, ipa, translation); item.appendChild(copy);
            if (segment.audioUrl) { const audio = document.createElement("audio"); audio.controls = true; audio.preload = "none"; audio.src = segment.audioUrl; item.appendChild(audio); }
            return item;
        }));
        section.classList.remove("d-none");
    }

    async function savePreview() {
        if (!previewId) return;
        const button = document.getElementById("save-ai-lesson");
        if (button) button.disabled = true;
        status("Đang lưu bài học...");
        try {
            const response = await fetch("/api/ai-lessons/save", {
                method: "POST", headers: { "Content-Type": "application/json" }, body: JSON.stringify({ previewId })
            });
            const result = await response.json().catch(() => ({}));
            if (!response.ok) throw new Error(result.message || "Không lưu được bài học.");
            status("Đã lưu bài học vào thư viện cá nhân.");
            button?.classList.add("d-none");
            await loadLibrary();
        } catch (error) { status(error.message, true); if (button) button.disabled = false; }
    }

    async function loadLibrary() {
        const list = document.getElementById("ai-saved-list");
        if (!list) return;
        try {
            const draftResponse = await fetch("/api/ai-lessons/previews", { cache: "no-store" });
            if (!draftResponse.ok) throw new Error("Không tải được bài AI.");
            const drafts = await draftResponse.json();
            const items = drafts.map(item => ({ ...item, kind: "draft" }));
            document.getElementById("ai-saved-count").textContent = `${items.length} bài`;
            if (!items.length) {
                list.innerHTML = '<div class="ai-library-empty"><strong>Chưa có bài AI</strong><span>Tạo một bài từ khung bên phải để bắt đầu luyện tập.</span></div>';
                return;
            }
            list.replaceChildren(...items.map(createCard));
        } catch (error) {
            list.innerHTML = `<div class="ai-library-empty"><strong>Không tải được bài AI</strong><span>${escapeHtml(error.message)}</span></div>`;
        }
    }

    function createCard(item) {
        const draft = item.kind === "draft";
        const href = draft ? `/ai-lessons/preview/${item.previewId}` : `/ai-lessons/${item.savedLessonId}`;
        const card = document.createElement("article");
        card.className = `ai-library-card ${draft ? "is-draft" : "is-saved"}`;
        const link = document.createElement("a");
        link.className = "ai-library-card-link";
        link.href = href;

        const top = document.createElement("div");
        top.className = "ai-library-card-top";
        const badge = document.createElement("span");
        badge.className = "ai-library-badge";
        badge.textContent = draft ? "Bản nháp" : "Đã lưu";
        const icon = document.createElement("span");
        icon.className = "ai-library-card-icon";
        icon.innerHTML = '<i data-lucide="sparkles" aria-hidden="true"></i>';
        top.append(icon, badge);

        const title = document.createElement("h3");
        title.textContent = item.title;
        const meta = document.createElement("p");
        meta.textContent = draft
            ? `${item.segments.length} câu · Còn ${remainingTime(item.expiresAt)}`
            : `${item.segments.length} câu · Không giới hạn thời gian`;
        link.append(top, title, meta);

        const actions = document.createElement("div");
        actions.className = "ai-library-card-actions";
        if (draft) {
            const save = actionButton("bookmark-plus", "Lưu", "Lưu bài vào thư viện");
            save.addEventListener("click", () => saveDraft(item.previewId, save));
            actions.appendChild(save);
        }
        const remove = actionButton("trash-2", "Xóa", "Xóa bài học");
        remove.classList.add("is-delete");
        remove.addEventListener("click", () => deleteItem(item, remove));
        actions.appendChild(remove);
        card.append(link, actions);
        queueMicrotask(() => window.lucide?.createIcons());
        return card;
    }

    function actionButton(icon, label, ariaLabel) {
        const button = document.createElement("button");
        button.type = "button";
        button.className = "ai-library-card-action";
        button.setAttribute("aria-label", ariaLabel);
        button.title = ariaLabel;
        button.innerHTML = `<i data-lucide="${icon}" aria-hidden="true"></i><span>${label}</span>`;
        return button;
    }

    async function saveDraft(id, button) {
        button.disabled = true;
        try {
            const response = await fetch("/api/ai-lessons/save", {
                method: "POST", headers: { "Content-Type": "application/json" },
                body: JSON.stringify({ previewId: id })
            });
            const result = await response.json().catch(() => ({}));
            if (!response.ok) throw new Error(result.message || "Không lưu được bài học.");
            await loadLibrary();
        } catch (error) {
            status(error.message, true);
            button.disabled = false;
        }
    }

    async function deleteItem(item, button) {
        if (!window.confirm(`Xóa "${item.title}"?`)) return;
        button.disabled = true;
        const url = item.kind === "draft"
            ? `/api/ai-lessons/preview/${item.previewId}`
            : `/api/ai-lessons/${item.savedLessonId}`;
        const response = await fetch(url, { method: "DELETE" });
        if (response.ok) await loadLibrary();
        else {
            status("Không xóa được bài học.", true);
            button.disabled = false;
        }
    }

    function remainingTime(value) {
        const expiresAt = new Date(value);
        if (Number.isNaN(expiresAt.getTime())) return "chưa xác định";
        const milliseconds = Math.max(0, expiresAt.getTime() - Date.now());
        const totalMinutes = Math.ceil(milliseconds / 60000);
        if (totalMinutes >= 1440) {
            const days = Math.floor(totalMinutes / 1440);
            const hours = Math.floor((totalMinutes % 1440) / 60);
            return hours ? `${days} ngày ${hours} giờ` : `${days} ngày`;
        }
        if (totalMinutes >= 60) {
            const hours = Math.floor(totalMinutes / 60);
            const minutes = totalMinutes % 60;
            return minutes ? `${hours} giờ ${minutes} phút` : `${hours} giờ`;
        }
        return `${totalMinutes} phút`;
    }

    function escapeHtml(value) {
        const node = document.createElement("span");
        node.textContent = value || "";
        return node.innerHTML;
    }

    function status(message, error) {
        const element = document.getElementById("ai-generator-status");
        if (!element) return;
        element.textContent = message || "";
        element.classList.toggle("is-error", Boolean(error));
    }
    function setLoading(active) {
        document.getElementById("ai-generator-skeleton")?.classList.toggle("d-none", !active);
        const button = document.getElementById("generate-ai-btn");
        if (button) button.disabled = active;
    }
})();
