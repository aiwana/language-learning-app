"use strict";

/**
 * Chức năng: nút Lưu/Xóa trên trang chi tiết bài AI draft hoặc saved.
 * Phụ trách chính: Minh Anh. API phải luôn kiểm tra ownership phía server.
 */
(function () {
    document.addEventListener("DOMContentLoaded", () => {
        document.getElementById("save-ai-detail")?.addEventListener("click", save);
        document.getElementById("delete-ai-detail")?.addEventListener("click", remove);
    });

    async function save() {
        const state = window.__aiLesson || {};
        if (!state.previewId) return;
        const button = document.getElementById("save-ai-detail");
        setStatus("Đang lưu bài học...");
        if (button) button.disabled = true;
        try {
            const response = await fetch("/api/ai-lessons/save", {
                method: "POST",
                headers: { "Content-Type": "application/json" },
                body: JSON.stringify({ previewId: state.previewId })
            });
            const result = await response.json().catch(() => ({}));
            if (!response.ok) throw new Error(result.message || "Không lưu được bài học.");
            window.location.replace(`/ai-lessons/${result.savedLessonId}`);
        } catch (error) {
            setStatus(error.message || "Không lưu được bài học.");
            if (button) button.disabled = false;
        }
    }

    async function remove() {
        const state = window.__aiLesson || {};
        if (!window.confirm("Xóa bài học AI này? Thao tác này không thể hoàn tác.")) return;
        const button = document.getElementById("delete-ai-detail");
        if (button) button.disabled = true;
        setStatus("Đang xóa...");
        const url = state.savedLessonId
            ? `/api/ai-lessons/${state.savedLessonId}`
            : `/api/ai-lessons/preview/${state.previewId}`;
        try {
            const response = await fetch(url, { method: "DELETE" });
            if (!response.ok) throw new Error("Không xóa được bài học.");
            window.location.replace("/");
        } catch (error) {
            setStatus(error.message || "Không xóa được bài học.");
            if (button) button.disabled = false;
        }
    }

    function setStatus(message) {
        const element = document.getElementById("ai-detail-status");
        if (element) element.textContent = message || "";
    }
})();
