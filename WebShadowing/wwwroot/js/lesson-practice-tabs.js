"use strict";

/**
 * Chức năng dùng chung của trang bài học.
 * - Minh: Nghe chép, Ghép IPA, favorite và contract evaluate-answer.
 * - Minh Anh: Đối thoại AI và hành vi chuyển tab/giao diện.
 * Thay đổi selector phải đồng bộ Views/Home/LessonDetail.cshtml.
 */
(function () {
    const lesson = window.__initialLesson;
    if (!lesson) return;
    const sentences = [...(lesson.sentences || [])].sort((a, b) => a.order - b.order);
    const exercise = {
        dictationIndex: 0,
        ipaIndex: 0,
        dictationPassed: false,
        ipaPassed: false,
        dialogueSessionId: null,
        dialogueRecorder: null,
        dialogueChunks: [],
        favoriteBySentence: new Map(),
        ipaTargetBySentence: new Map()
    };

    document.addEventListener("DOMContentLoaded", function () {
        initTabs();
        initFavorites();
        initDictation();
        initIpaMatch();
        if (window.__isVip) initDialogue();
    });

    function initTabs() {
        const tabs = [...document.querySelectorAll(".lesson-mode-tab:not(:disabled)")];
        tabs.forEach((tab, index) => {
            tab.addEventListener("keydown", event => {
                if (!["ArrowLeft", "ArrowRight", "Home", "End"].includes(event.key)) return;
                event.preventDefault();
                let target = index;
                if (event.key === "ArrowRight") target = (index + 1) % tabs.length;
                if (event.key === "ArrowLeft") target = (index - 1 + tabs.length) % tabs.length;
                if (event.key === "Home") target = 0;
                if (event.key === "End") target = tabs.length - 1;
                tabs[target].focus();
                activateTab(tabs[target].dataset.tab);
            });
        });
        window.switchTab = activateTab;
    }

    function activateTab(name) {
        const panels = {
            shadowing: document.getElementById("shadowing-tab-panel"),
            dictation: document.getElementById("dictation-tab-panel"),
            "ipa-match": document.getElementById("ipa-match-tab-panel"),
            "ai-dialogue": document.getElementById("ai-dialogue-tab-panel")
        };
        if (!panels[name]) return;
        document.querySelectorAll(".lesson-mode-tab").forEach(tab => {
            const active = tab.dataset.tab === name;
            tab.classList.toggle("is-active", active);
            tab.setAttribute("aria-selected", String(active));
            tab.setAttribute("tabindex", active ? "0" : "-1");
        });
        Object.entries(panels).forEach(([key, panel]) => panel?.classList.toggle("d-none", key !== name));
        document.querySelector("[data-shadowing-content]")?.classList.toggle("d-none", name !== "shadowing");
        if (name === "dictation") renderDictation();
        if (name === "ipa-match") {
            prefetchAllIpa();
            renderIpa();
        }
        if (name === "ai-dialogue" && window.__isVip) ensureDialogueSession();
    }

    async function initFavorites() {
        const button = document.getElementById("favorite-sentence-btn");
        if (!button) return;
        try {
            const response = await fetch("/api/favorites", { cache: "no-store" });
            if (response.ok) (await response.json()).forEach(item => exercise.favoriteBySentence.set(item.sentenceId, item.favoriteSentenceId));
        } catch (_) { }
        updateFavoriteButton();
        document.getElementById("subtitle-list")?.addEventListener("click", () => setTimeout(updateFavoriteButton));
        button.addEventListener("click", toggleFavorite);
    }

    function updateFavoriteButton() {
        const button = document.getElementById("favorite-sentence-btn");
        const sentence = window.lessonPlayer?.getCurrentSentence();
        if (!button || !sentence) return;
        const saved = exercise.favoriteBySentence.has(sentence.sentenceId);
        button.classList.toggle("is-saved", saved);
        button.setAttribute("aria-pressed", String(saved));
        button.querySelector("span").textContent = saved ? "Đã lưu" : "Lưu câu";
    }

    async function toggleFavorite() {
        const sentence = window.lessonPlayer?.getCurrentSentence();
        if (!sentence) return;
        const favoriteId = exercise.favoriteBySentence.get(sentence.sentenceId);
        try {
            const response = await fetch(favoriteId ? `/api/favorites/${favoriteId}` : "/api/favorites", {
                method: favoriteId ? "DELETE" : "POST",
                headers: favoriteId ? {} : { "Content-Type": "application/json" },
                body: favoriteId ? null : JSON.stringify({ sentenceId: sentence.sentenceId })
            });
            if (!response.ok) throw new Error("Không thể cập nhật câu yêu thích.");
            if (favoriteId) exercise.favoriteBySentence.delete(sentence.sentenceId);
            else {
                const result = await response.json();
                exercise.favoriteBySentence.set(sentence.sentenceId, result.favoriteSentenceId);
            }
            updateFavoriteButton();
            window.showAppToast?.(favoriteId ? "Đã xóa khỏi câu yêu thích." : "Đã lưu câu vào Tiến trình.");
        } catch (error) { window.showAppToast?.(error.message, "error"); }
    }

    let dictationAnimationInterval = null;
    let dictationAnimationTimeout = null;

    function stopDictationProgressBar() {
        clearInterval(dictationAnimationInterval);
        clearTimeout(dictationAnimationTimeout);
        exercise.dictationPlaying = false;

        const playIcon = document.getElementById("dictation-play-icon");
        if (playIcon) {
            playIcon.setAttribute("data-lucide", "play");
            if (window.lucide) {
                window.lucide.createIcons();
            }
        }
    }

    function animateDictationProgressBar(sentence) {
        clearInterval(dictationAnimationInterval);
        clearTimeout(dictationAnimationTimeout);
        exercise.dictationPlaying = true;

        const playIcon = document.getElementById("dictation-play-icon");
        if (playIcon) {
            playIcon.setAttribute("data-lucide", "square");
            if (window.lucide) {
                window.lucide.createIcons();
            }
        }

        const progressBar = document.getElementById("dictation-progress-bar");
        const currentTimeEl = document.getElementById("dictation-current-time");
        const totalTimeEl = document.getElementById("dictation-total-time");
        if (!progressBar || !currentTimeEl || !totalTimeEl) return;

        const duration = (sentence.endTime && sentence.startTime)
            ? (sentence.endTime - sentence.startTime)
            : 0;

        if (duration <= 0) {
            progressBar.style.width = "100%";
            currentTimeEl.textContent = "0:00";
            totalTimeEl.textContent = "0:00";
            stopDictationProgressBar();
            return;
        }

        totalTimeEl.textContent = formatDuration(duration);
        currentTimeEl.textContent = "0:00";
        progressBar.style.width = "0%";

        const startTime = Date.now();
        const durationMs = duration * 1000;

        dictationAnimationInterval = setInterval(() => {
            const elapsedMs = Date.now() - startTime;
            const percent = Math.min(100, (elapsedMs / durationMs) * 100);
            progressBar.style.width = percent + "%";
            currentTimeEl.textContent = formatDuration(Math.min(duration, elapsedMs / 1000));

            if (elapsedMs >= durationMs) {
                clearInterval(dictationAnimationInterval);
            }
        }, 50);

        dictationAnimationTimeout = setTimeout(() => {
            stopDictationProgressBar();
            progressBar.style.width = "100%";
            currentTimeEl.textContent = formatDuration(duration);
        }, durationMs + 200);
    }

    function formatDuration(sec) {
        const m = Math.floor(sec / 60);
        const s = Math.floor(sec % 60);
        return `${m}:${s < 10 ? '0' : ''}${s}`;
    }

    function initDictation() {
        document.getElementById("dictation-play")?.addEventListener("click", () => {
            const sentence = sentences[exercise.dictationIndex];
            if (!sentence) return;
            if (exercise.dictationPlaying) {
                window.lessonPlayer?.stop();
                stopDictationProgressBar();
            } else {
                window.lessonPlayer?.playSentence(exercise.dictationIndex);
                animateDictationProgressBar(sentence);
            }
        });
        document.getElementById("dictation-submit")?.addEventListener("click", submitDictation);
        document.getElementById("dictation-next")?.addEventListener("click", () => {
            exercise.dictationIndex = Math.min(sentences.length - 1, exercise.dictationIndex + 1);
            exercise.dictationPassed = false;

            // Clear progress bar when moving to next sentence
            const progressBar = document.getElementById("dictation-progress-bar");
            if (progressBar) progressBar.style.width = "0%";
            const currentTimeEl = document.getElementById("dictation-current-time");
            if (currentTimeEl) currentTimeEl.textContent = "0:00";

            stopDictationProgressBar();
            renderDictation();
        });
    }

    function renderDictation() {
        const sentence = sentences[exercise.dictationIndex];
        if (!sentence) return;
        setText("dictation-progress", `${exercise.dictationIndex + 1} / ${sentences.length}`);
        const input = document.getElementById("dictation-answer");
        if (input) { input.value = ""; input.disabled = false; input.focus(); }
        setText("dictation-result", sentence.translation ? `Gợi ý: ${sentence.translation}` : "Nghe kỹ nhịp và nối âm.");
        document.getElementById("dictation-next")?.classList.add("d-none");

        // Set total duration initially
        const totalTimeEl = document.getElementById("dictation-total-time");
        if (totalTimeEl && sentence.endTime && sentence.startTime) {
            totalTimeEl.textContent = formatDuration(sentence.endTime - sentence.startTime);
        }
    }

    async function submitDictation() {
        const sentence = sentences[exercise.dictationIndex];
        const answer = document.getElementById("dictation-answer")?.value.trim();
        if (!answer) return setText("dictation-result", "Hãy nhập câu bạn nghe được.");
        await submitAnswer("dictation", sentence, answer, "dictation-result", passed => {
            exercise.dictationPassed = passed;
            document.getElementById("dictation-next")?.classList.toggle("d-none", !passed || exercise.dictationIndex >= sentences.length - 1);
        });
    }

    function initIpaMatch() {
        document.getElementById("ipa-submit")?.addEventListener("click", submitIpa);
        document.getElementById("ipa-prev")?.addEventListener("click", () => {
            exercise.ipaIndex = Math.max(0, exercise.ipaIndex - 1);
            exercise.ipaPassed = false;
            renderIpa();
        });
        document.getElementById("ipa-next")?.addEventListener("click", () => {
            exercise.ipaIndex = Math.min(sentences.length - 1, exercise.ipaIndex + 1);
            exercise.ipaPassed = false;
            renderIpa();
        });
    }

    async function prefetchAllIpa() {
        for (const sentence of sentences) {
            if (sentence.ipa) continue;
            try {
                const response = await fetch("/api/sentence-ipa", {
                    method: "POST",
                    headers: { "Content-Type": "application/json" },
                    body: JSON.stringify({ sentenceId: sentence.sentenceId })
                });
                const result = await response.json().catch(() => ({}));
                if (response.ok && result.ipa) {
                    sentence.ipa = result.ipa;
                }
            } catch (_) {}
        }
    }

    async function renderIpa() {
        const sentence = sentences[exercise.ipaIndex];
        if (!sentence) return;
        setText("ipa-progress", `${exercise.ipaIndex + 1} / ${sentences.length}`);
        setText("ipa-context", sentence.text);
        setText("ipa-word", "...");
        setText("ipa-result", sentence.ipa ? "Chọn phiên âm phù hợp nhất với câu trên." : "Đang tạo phiên âm cho câu này...");

        // Hide next button, show check button
        document.getElementById("ipa-next")?.classList.add("d-none");
        const submit = document.getElementById("ipa-submit");
        if (submit) {
            submit.disabled = true;
            submit.classList.remove("d-none");
        }

        // Configure prev button
        const prev = document.getElementById("ipa-prev");
        if (prev) {
            prev.disabled = exercise.ipaIndex === 0;
        }

        const container = document.getElementById("ipa-options");
        if (!container) return;
        container.innerHTML = "";
        if (!sentence.ipa) {
            try {
                const response = await fetch("/api/sentence-ipa", {
                    method: "POST",
                    headers: { "Content-Type": "application/json" },
                    body: JSON.stringify({ sentenceId: sentence.sentenceId })
                });
                const result = await response.json().catch(() => ({}));
                if (!response.ok || !result.ipa) throw new Error(result.message || "Chưa tạo được phiên âm.");
                sentence.ipa = result.ipa;
            } catch (error) {
                container.innerHTML = '<div class="practice-inline-error">Không thể tải phiên âm lúc này.</div>';
                setText("ipa-result", error.message || "Không thể tải phiên âm lúc này.");
                return;
            }
        }
        const words = sentence.text.match(/[\p{L}']+/gu) || [];
        const ipaTokens = sentence.ipa.trim()
            .replace(/^[\/\[]/, "")
            .replace(/[\/\]]$/, "")
            .split(/\s+/)
            .filter(Boolean);
        if (!words.length || words.length !== ipaTokens.length) {
            container.innerHTML = '<div class="practice-inline-error">Dữ liệu từ và IPA chưa đồng bộ.</div>';
            setText("ipa-result", "Không thể tạo lượt ghép từ cho câu này.");
            return;
        }
        if (!exercise.ipaTargetBySentence.has(sentence.sentenceId)) {
            exercise.ipaTargetBySentence.set(sentence.sentenceId, Math.floor(Math.random() * words.length));
        }
        const targetIndex = exercise.ipaTargetBySentence.get(sentence.sentenceId);
        setText("ipa-word", words[targetIndex]);
        const options = buildWordIpaOptions(ipaTokens[targetIndex], ipaTokens);
        container.replaceChildren(...options.map((option, index) => {
            const label = document.createElement("label");
            label.className = "ipa-option";
            label.innerHTML = `<input type="radio" name="ipa-choice" value="${escapeAttribute(option)}"><span>${escapeHtml(option)}</span>`;
            return label;
        }));
        if (submit) submit.disabled = false;
        setText("ipa-result", "Chọn phiên âm phù hợp nhất với câu trên.");
    }

    async function submitIpa() {
        const sentence = sentences[exercise.ipaIndex];
        const selectedInput = document.querySelector('input[name="ipa-choice"]:checked');
        const answer = selectedInput?.value;
        if (!answer) return setText("ipa-result", "Hãy chọn một đáp án.");

        await submitAnswer("ipa-match", sentence, answer, "ipa-result", passed => {
            exercise.ipaPassed = passed;
            const label = selectedInput.closest(".ipa-option");
            if (passed) {
                // Clear any previous incorrect/correct classes, set correct style
                document.querySelectorAll(".ipa-option").forEach(lbl => {
                    lbl.classList.remove("is-incorrect", "is-correct");
                    const input = lbl.querySelector('input');
                    if (input) input.disabled = true;
                });
                label.classList.add("is-correct");

                // Hide submit button, show next button
                document.getElementById("ipa-submit")?.classList.add("d-none");
                document.getElementById("ipa-next")?.classList.toggle("d-none", exercise.ipaIndex >= sentences.length - 1);
            } else {
                // Set incorrect style and disable this radio option
                label.classList.add("is-incorrect");
                selectedInput.disabled = true;
                selectedInput.checked = false; // Deselect so they can choose another
            }
        });
    }

    async function submitAnswer(tab, sentence, answer, resultId, onDone) {
        if (lesson.source === "ai-generated") {
            const normalize = value => String(value || "").normalize("NFKC").toLowerCase()
                .replace(tab === "dictation" ? /[^\p{L}\p{N}']+/gu : /[\s/\[\]]+/g, "");
            const targetIndex = exercise.ipaTargetBySentence.get(sentence.sentenceId);
            const expected = tab === "ipa-match"
                ? String(sentence.ipa || "").trim().replace(/^[/\[]|[/\]]$/g, "").split(/\s+/)[targetIndex]
                : sentence.text;
            const passed = normalize(answer) === normalize(expected);
            setText(resultId, passed ? "Đúng. Bạn đã mở câu tiếp theo." : "Chưa đúng. Nghe lại và thử thêm một lần.");
            onDone(passed);
            return;
        }
        setText(resultId, "Đang chấm...");
        try {
            const idempotencyKey = key(tab);
            const response = await fetch("/api/practice/evaluate-answer", {
                method: "POST",
                headers: {
                    "Content-Type": "application/json",
                    "Idempotency-Key": idempotencyKey
                },
                body: JSON.stringify({
                    lessonId: lesson.lessonId,
                    sentenceId: sentence.sentenceId,
                    practiceTab: tab,
                    answer,
                    idempotencyKey,
                    targetIndex: tab === "ipa-match" ? exercise.ipaTargetBySentence.get(sentence.sentenceId) : null
                })
            });
            const result = await response.json();
            window.applyGamificationTransaction?.(result.gamification);
            if (!response.ok && !result.gamification) throw new Error(result.message || "Không chấm được câu trả lời.");
            setText(resultId, result.passed ? "Đúng. Bạn đã mở câu tiếp theo." : "Chưa đúng. Nghe lại và thử thêm một lần.");
            onDone(Boolean(result.passed));
        } catch (error) { setText(resultId, error.message || "Mất kết nối. Vui lòng thử lại."); }
    }

    function buildWordIpaOptions(correct, sentenceTokens) {
        const variants = [correct, ...sentenceTokens.filter(token => token !== correct)];
        const unique = [...new Set(variants)];
        const fallbacks = [
            correct.replace(/ɪ/g, "iː").replace(/ə/g, "ʌ"),
            correct.replace(/θ/g, "t").replace(/ð/g, "d").replace(/æ/g, "e"),
            correct.replace(/ˈ/g, "").replace(/iː/g, "ɪ").replace(/ɑː/g, "ʌ")
        ];
        fallbacks.forEach(value => { if (value && !unique.includes(value)) unique.push(value); });
        while (unique.length < 4) unique.push(`${correct}${"ˌ".repeat(unique.length)}`);
        const distractors = unique
            .filter(value => value !== correct)
            .map(value => ({ value, score: hash(`${correct}:${value}`) }))
            .sort((a, b) => a.score - b.score)
            .slice(0, 3)
            .map(item => item.value);
        return [correct, ...distractors]
            .map(value => ({ value, score: hash(`${value}:${correct}:position`) }))
            .sort((a, b) => a.score - b.score)
            .map(item => item.value);
    }

    function initDialogue() {
        document.getElementById("dialogue-send")?.addEventListener("click", sendDialogueText);
        document.getElementById("dialogue-input")?.addEventListener("keydown", event => {
            if (event.key === "Enter" && !event.shiftKey) { event.preventDefault(); sendDialogueText(); }
        });
        document.getElementById("dialogue-record")?.addEventListener("click", toggleDialogueRecording);
    }

    async function ensureDialogueSession() {
        if (exercise.dialogueSessionId) return exercise.dialogueSessionId;
        setText("dialogue-status", "Đang mở phòng đối thoại...");
        try {
            const response = await fetch("/api/ai-dialogue/sessions", { method: "POST", headers: { "Content-Type": "application/json" }, body: JSON.stringify({ lessonId: lesson.lessonId }) });
            if (!response.ok) throw new Error(response.status === 403 ? "Tính năng này cần tài khoản VIP." : "Không mở được phiên đối thoại.");
            const session = await response.json();
            exercise.dialogueSessionId = session.sessionId;

            // Render existing turns
            const container = document.getElementById("dialogue-messages");
            if (container && session.turns) {
                container.innerHTML = "";
                session.turns.forEach((turn, idx) => {
                    const shouldPlay = (session.turns.length === 1 && turn.role === "assistant");
                    appendMessage(turn.role, turn.text, turn.audioUrl, shouldPlay);
                });
            }

            setText("dialogue-turn-count", `${session.turnCount} / ${session.maxTurns}`);
            setText("dialogue-status", "Phòng đã sẵn sàng.");
            return session.sessionId;
        } catch (error) { setText("dialogue-status", error.message); return null; }
    }

    async function sendDialogueText() {
        const input = document.getElementById("dialogue-input");
        const text = input?.value.trim();
        if (!text) return;
        const sessionId = await ensureDialogueSession();
        if (!sessionId) return;
        input.value = "";
        appendMessage("user", text);
        await sendDialogueRequest(`/api/ai-dialogue/sessions/${sessionId}/messages`, { json: { message: text } });
    }

    async function toggleDialogueRecording() {
        const button = document.getElementById("dialogue-record");
        if (exercise.dialogueRecorder?.state === "recording") {
            exercise.dialogueRecorder.stop();
            button?.classList.remove("is-recording");
            button?.querySelector("span")?.replaceChildren("Thu âm");
            return;
        }
        const sessionId = await ensureDialogueSession();
        if (!sessionId) return;
        try {
            const stream = await navigator.mediaDevices.getUserMedia({ audio: true });
            exercise.dialogueChunks = [];
            exercise.dialogueRecorder = new MediaRecorder(stream);
            exercise.dialogueRecorder.ondataavailable = event => { if (event.data.size) exercise.dialogueChunks.push(event.data); };
            exercise.dialogueRecorder.onstop = async () => {
                stream.getTracks().forEach(track => track.stop());
                const blob = new Blob(exercise.dialogueChunks, { type: exercise.dialogueRecorder.mimeType || "audio/webm" });
                const form = new FormData();
                form.append("audio", blob, "voice-message.webm");
                setText("dialogue-status", "Đang nghe và phản hồi...");
                await sendDialogueRequest(`/api/ai-dialogue/sessions/${sessionId}/audio`, { form });
            };
            exercise.dialogueRecorder.start();
            button?.classList.add("is-recording");
            button?.querySelector("span")?.replaceChildren("Dừng");
            setText("dialogue-status", "Đang thu âm...");
        } catch (_) { setText("dialogue-status", "Không truy cập được microphone."); }
    }

    async function sendDialogueRequest(url, request) {
        try {
            const response = await fetch(url, request.form
                ? { method: "POST", body: request.form }
                : { method: "POST", headers: { "Content-Type": "application/json" }, body: JSON.stringify(request.json) });
            const result = await response.json().catch(() => ({}));
            if (!response.ok) throw new Error(result.message || "AI chưa thể trả lời lúc này.");
            if (request.form) appendMessage("user", result.userText, null, false);
            appendMessage("assistant", result.replyText, result.audioUrl, true);
            setText("dialogue-turn-count", `${result.turnCount} / 30`);
            setText("dialogue-status", result.completed ? "Phiên đã hoàn tất." : "AI đã trả lời.");
        } catch (error) { setText("dialogue-status", error.message); }
    }

    function appendMessage(role, text, audioUrl, shouldPlay = false) {
        const container = document.getElementById("dialogue-messages");
        if (!container) return;
        container.querySelector(".dialogue-empty")?.remove();
        const bubble = document.createElement("div");
        bubble.className = `dialogue-message is-${role}`;
        const copy = document.createElement("p"); copy.textContent = text; bubble.appendChild(copy);
        if (audioUrl) {
            const audio = document.createElement("audio");
            audio.controls = true;
            audio.src = audioUrl;
            bubble.appendChild(audio);
            if (shouldPlay) {
                setTimeout(() => audio.play().catch(() => {}), 100);
            }
        }
        container.appendChild(bubble);
        bubble.scrollIntoView({ behavior: "smooth", block: "nearest" });
    }

    function setText(id, value) { const node = document.getElementById(id); if (node) node.textContent = value; }
    function key(prefix) { return `${prefix}-${crypto.randomUUID?.() || Date.now() + "-" + Math.random().toString(16).slice(2)}`; }
    function hash(value) { let result = 0; for (const char of value) result = ((result << 5) - result + char.charCodeAt(0)) | 0; return result; }
    function escapeHtml(value) { return String(value).replace(/&/g, "&amp;").replace(/</g, "&lt;").replace(/>/g, "&gt;").replace(/"/g, "&quot;"); }
    function escapeAttribute(value) { return escapeHtml(value).replace(/'/g, "&#39;"); }
})();
