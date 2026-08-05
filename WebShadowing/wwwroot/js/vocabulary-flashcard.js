"use strict";

/**
 * Chức năng: trang Tiến trình & Thẻ nhớ — flashcard từ vựng, favorite và mic đọc từ.
 * Phụ trách chính: Hải Anh. Minh cung cấp API và logic sinh vocabulary từ lỗi sai.
 */
(function () {
    const state = { items: [], index: 0, recorder: null, chunks: [] };
    document.addEventListener("DOMContentLoaded", function () {
        document.getElementById("vocabulary-filter")?.addEventListener("change", loadVocabulary);
        document.getElementById("vocabulary-prev")?.addEventListener("click", () => move(-1));
        document.getElementById("vocabulary-next")?.addEventListener("click", () => move(1));
        document.getElementById("vocabulary-mastered")?.addEventListener("click", updateReview);
        document.getElementById("vocabulary-delete")?.addEventListener("click", removeCurrent);
        document.getElementById("vocabulary-speak")?.addEventListener("click", toggleRecording);
        document.getElementById("vocabulary-flashcard")?.addEventListener("keydown", event => {
            if (event.key === "ArrowLeft") move(-1);
            if (event.key === "ArrowRight") move(1);
        });
        loadVocabulary();
        loadFavorites();
    });

    async function loadVocabulary() {
        setVisible("vocabulary-loading", true); setVisible("vocabulary-flashcard", false); setVisible("vocabulary-empty", false);
        const status = document.getElementById("vocabulary-filter")?.value || "";
        try {
            const response = await fetch(`/api/vocabulary?status=${encodeURIComponent(status)}&page=1`, { cache: "no-store" });
            if (!response.ok) throw new Error("Không tải được sổ tay.");
            const result = await response.json();
            state.items = result.items || []; state.index = 0;
            setVisible("vocabulary-loading", false);
            if (!state.items.length) return setVisible("vocabulary-empty", true);
            setVisible("vocabulary-flashcard", true); renderCard();
        } catch (error) { setVisible("vocabulary-loading", false); setVisible("vocabulary-empty", true); text("vocabulary-empty", error.message); }
    }

    function renderCard() {
        const item = state.items[state.index]; if (!item) return;
        text("vocabulary-counter", `${state.index + 1} / ${state.items.length}`);
        text("vocabulary-word", item.word); text("vocabulary-ipa", item.ipa || "Chưa có IPA");
        text("vocabulary-meaning", item.meaning || "Chưa có nghĩa phù hợp.");
        text("vocabulary-example", item.exampleSentence || "Từ này được lưu từ quá trình luyện phát âm.");
        text("vocabulary-status", item.reviewStatus === "mastered" ? "Từ này đã được đánh dấu đã nhớ." : "Dùng phím ← → để chuyển thẻ.");
        const action = document.getElementById("vocabulary-mastered");
        if (action) action.textContent = item.reviewStatus === "mastered" ? "Học lại" : "Đã nhớ";
    }

    function move(delta) { if (!state.items.length) return; state.index = (state.index + delta + state.items.length) % state.items.length; renderCard(); }

    async function updateReview() {
        const item = state.items[state.index]; if (!item) return;
        const endpoint = item.reviewStatus === "mastered" ? "review" : "mastered";
        const response = await fetch(`/api/vocabulary/${item.vocabularyItemId}/${endpoint}`, { method: "POST" });
        if (response.ok) loadVocabulary(); else text("vocabulary-status", "Không cập nhật được trạng thái.");
    }

    async function removeCurrent() {
        const item = state.items[state.index]; if (!item) return;
        const response = await fetch(`/api/vocabulary/${item.vocabularyItemId}`, { method: "DELETE" });
        if (response.ok) loadVocabulary(); else text("vocabulary-status", "Không xóa được từ này.");
    }

    async function toggleRecording() {
        const button = document.getElementById("vocabulary-speak");
        if (state.recorder?.state === "recording") { state.recorder.stop(); button?.classList.remove("is-recording"); button?.lastChild?.replaceWith(" Đọc lại"); return; }
        try {
            const stream = await navigator.mediaDevices.getUserMedia({ audio: true });
            state.chunks = []; state.recorder = new MediaRecorder(stream);
            state.recorder.ondataavailable = event => { if (event.data.size) state.chunks.push(event.data); };
            state.recorder.onstop = async () => {
                stream.getTracks().forEach(track => track.stop());
                text("vocabulary-status", "Đang chấm phát âm...");
                try {
                    const browserBlob = new Blob(state.chunks, { type: state.recorder.mimeType || "audio/webm" });
                    const wav = await toWav(browserBlob);
                    const form = new FormData(); form.append("audio", wav, "flashcard.wav");
                    const item = state.items[state.index];
                    const response = await fetch(`/api/vocabulary/${item.vocabularyItemId}/pronunciation`, { method: "POST", body: form });
                    const result = await response.json().catch(() => ({}));
                    if (!response.ok) throw new Error(result.message || "Không chấm được phát âm.");
                    text("vocabulary-status", `${result.score}% · ${result.feedback || (result.passed ? "Đạt mục tiêu." : "Hãy thử lại.")}`);
                } catch (error) { text("vocabulary-status", error.message || "Không xử lý được bản thu."); }
            };
            state.recorder.start(); button?.classList.add("is-recording"); text("vocabulary-status", "Đang thu âm. Nhấn lại để dừng.");
        } catch (_) { text("vocabulary-status", "Không truy cập được microphone."); }
    }

    async function toWav(blob) {
        const context = new (window.AudioContext || window.webkitAudioContext)();
        try {
            const buffer = await context.decodeAudioData(await blob.arrayBuffer());
            const mono = new Float32Array(buffer.length);
            for (let channel = 0; channel < buffer.numberOfChannels; channel++) {
                const data = buffer.getChannelData(channel);
                for (let index = 0; index < data.length; index++) mono[index] += data[index] / buffer.numberOfChannels;
            }
            const output = new ArrayBuffer(44 + mono.length * 2); const view = new DataView(output);
            const write = (offset, value) => [...value].forEach((char, index) => view.setUint8(offset + index, char.charCodeAt(0)));
            write(0, "RIFF"); view.setUint32(4, 36 + mono.length * 2, true); write(8, "WAVE"); write(12, "fmt ");
            view.setUint32(16, 16, true); view.setUint16(20, 1, true); view.setUint16(22, 1, true); view.setUint32(24, buffer.sampleRate, true);
            view.setUint32(28, buffer.sampleRate * 2, true); view.setUint16(32, 2, true); view.setUint16(34, 16, true); write(36, "data"); view.setUint32(40, mono.length * 2, true);
            mono.forEach((sample, index) => view.setInt16(44 + index * 2, Math.max(-1, Math.min(1, sample)) * 0x7fff, true));
            return new Blob([output], { type: "audio/wav" });
        } finally { await context.close(); }
    }

    async function loadFavorites() {
        const list = document.getElementById("favorites-list"); if (!list) return;
        try {
            const response = await fetch("/api/favorites", { cache: "no-store" });
            if (!response.ok) throw new Error();
            const favorites = await response.json(); text("favorites-count", favorites.length);
            if (!favorites.length) { list.innerHTML = '<div class="feature-empty"><h3>Chưa lưu câu thoại</h3><p>Mở một bài học và chọn “Lưu câu”.</p></div>'; return; }
            list.replaceChildren(...favorites.map(item => {
                const article = document.createElement("article"); article.className = "favorite-row";
                const copy = document.createElement("a"); copy.href = `/Home/LessonDetail/${item.lessonId}`;
                const sentence = document.createElement("strong"); sentence.textContent = item.text;
                const meta = document.createElement("span"); meta.textContent = `${item.lessonTitle}${item.translation ? " · " + item.translation : ""}`;
                copy.append(sentence, meta);
                const remove = document.createElement("button"); remove.type = "button"; remove.className = "btn btn-sm btn-link text-danger"; remove.textContent = "Xóa";
                remove.addEventListener("click", async () => { if ((await fetch(`/api/favorites/${item.favoriteSentenceId}`, { method: "DELETE" })).ok) loadFavorites(); });
                article.append(copy, remove); return article;
            }));
        } catch (_) { list.innerHTML = '<p class="text-danger">Không tải được câu yêu thích.</p>'; }
    }

    function text(id, value) { const node = document.getElementById(id); if (node) node.textContent = value; }
    function setVisible(id, visible) { document.getElementById(id)?.classList.toggle("d-none", !visible); }
})();
