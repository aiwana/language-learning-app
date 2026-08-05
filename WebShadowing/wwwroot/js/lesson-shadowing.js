"use strict";

/**
 * Chức năng: tab Shadowing Studio — đồng bộ media/timeline, chọn câu, ghi âm bằng
 * MediaRecorder, nghe lại, upload chấm phát âm và tra nghĩa.
 * Phụ trách chính: Minh Anh. Minh phối hợp API scoring/persistence/từ sai liên tiếp.
 */
// Playback and session progress stay in the browser. Scoring, speech recognition,
// dictionary meaning, and IPA enrichment are provided by replaceable backend adapters.

const AI_UNAVAILABLE_MESSAGE = "Chưa kết nối AI, vui lòng kiểm tra lại!";

const SCORE_CIRCUMFERENCE = 364;

// =====================================================================
// STATE
// =====================================================================
const state = {
    lessonData: null,
    sentences: [],
    pronunciationTarget: 70,
    currentIndex: 0,
    unlockedIndex: 0,
    completedIndexes: new Set(),
    aiAvailable: window.__pronunciationAiConfigured !== false,
    isRecording: false,
    mediaRecorder: null,
    recordedChunks: [],
    audioBlob: null,
    audioUrl: null,
    audioPlayback: null,
    recognizedText: "",
    ipaRequestVersion: 0,
    evaluationController: null,
    audioStopTimer: null,
    youtubePlayer: null,
    youtubeReady: false,
    youtubeApiFailed: false,
    youtubeStopTimer: null,
    youtubePollTimer: null,
    ipaIdleHandle: null,
    subtitleButtons: [],
    dictController: null,
    attemptIdempotencyKey: null,
};

const dom = {};

function initDom() {
    dom.lessonTitle = document.getElementById("lesson-title");
    dom.courseTitle = document.getElementById("lesson-course-title");
    dom.breadcrumbTitle = document.getElementById("lesson-breadcrumb-title");
    dom.subtitlesCount = document.getElementById("subtitles-count");
    dom.pronunciationTargetLabel = document.getElementById("pronunciation-target-label");
    dom.youtubePlayerWrap = document.getElementById("youtube-player-wrap");
    dom.audioPlayerWrap = document.getElementById("audio-player-wrap");
    dom.audio = document.getElementById("lesson-audio");
    dom.youtubePlayer = document.getElementById("youtube-player");
    dom.subtitleList = document.getElementById("subtitle-list");
    dom.wordsContainer = document.getElementById("words-container");
    dom.targetTranslation = document.getElementById("target-translation");
    dom.sentenceTimeInfo = document.getElementById("sentence-time-info");
    dom.playSampleBtn = document.getElementById("play-sample-btn");
    dom.playSampleBtnLabel = document.querySelector("#play-sample-btn span");
    dom.recordBtn = document.getElementById("record-btn");
    dom.recordBtnLabel = document.getElementById("record-btn-label");
    dom.playbackBtn = document.getElementById("playback-btn");
    dom.clearBtn = document.getElementById("clear-btn");
    dom.nextSentenceBtn = document.getElementById("next-sentence-btn");
    dom.recStatusText = document.getElementById("rec-status-text");
    dom.scoreRing = document.getElementById("score-ring");
    dom.scoreRingWrap = document.getElementById("score-ring-wrap");
    dom.scoreNum = document.getElementById("score-num");
    dom.scoreFeedbackTitle = document.getElementById("score-feedback-title");
    dom.scoreFeedbackSub = document.getElementById("score-feedback-sub");
    dom.detailedFeedback = document.getElementById("ai-detailed-feedback");
    dom.scoreLoading = document.getElementById("score-loading");
    dom.pageError = document.getElementById("page-error");
    dom.pageErrorMsg = document.getElementById("page-error-msg");
    dom.lessonStudio = document.getElementById("lesson-studio");
    dom.dictBubble = document.getElementById("dict-bubble");
    dom.dictWord = document.getElementById("dict-word");
    dom.dictIpa = document.getElementById("dict-ipa");
    dom.dictMeaning = document.getElementById("dict-meaning");
}

// =====================================================================
// BOOT
// =====================================================================
document.addEventListener("DOMContentLoaded", () => {
    initDom();
    initTabBar();
    if (window.__initialLesson) {
        state.lessonData = window.__initialLesson;
        initLesson(window.__initialLesson);
        return;
    }

    showPageError("Không có dữ liệu bài học ban đầu.");
});

function initTabBar() {
    document.querySelectorAll(".lesson-mode-tab").forEach(tab => {
        tab.setAttribute("tabindex", tab.classList.contains("is-active") ? "0" : "-1");
    });
}

function switchTab(tabName) {
    if (tabName === "shadowing") return;
    showToast(tabName === "ai-dialogue"
        ? "Tính năng VIP sẽ được mở ở phiên bản sau."
        : "Tính năng này sắp ra mắt.");
}

function initLesson(lesson) {
    if (dom.lessonTitle) dom.lessonTitle.textContent = lesson.title ?? "Bài học";

    if (dom.courseTitle) {
        dom.courseTitle.textContent = lesson.course?.title ?? "";
    }

    if (dom.breadcrumbTitle) {
        dom.breadcrumbTitle.textContent = lesson.title ?? "";
    }

    state.sentences = [...(lesson.sentences ?? [])].sort((a, b) => a.order - b.order);
    state.pronunciationTarget = lesson.pronunciationTarget ?? 70;

    if (dom.subtitlesCount) dom.subtitlesCount.textContent = state.sentences.length;
    if (dom.pronunciationTargetLabel) dom.pronunciationTargetLabel.textContent = `${state.pronunciationTarget}%`;

    initMediaPlayer(lesson);
    initYoutubePlayback(lesson);
    initDictionaryBubble();

    if (state.sentences.length === 0) {
        showPageError("Bài học chưa có câu luyện tập.");
        return;
    }

    renderSubtitleList();
    selectSentence(0);
}

// =====================================================================
// MEDIA PLAYER
// =====================================================================
function initMediaPlayer(lesson) {
    const topGrid = document.querySelector(".lesson-top-grid");
    const mediaStage = document.querySelector(".lesson-media-stage");
    const youtubeWrap = dom.youtubePlayerWrap;
    const audioWrap = dom.audioPlayerWrap;
    const audioEl = dom.audio;

    const hasSentenceAudio = Boolean(lesson.sentences?.some(sentence => sentence.audioUrl));
    const isAudioOnly = (Boolean(lesson.media?.audioUrl) || hasSentenceAudio) && !lesson.media?.youtubeId;
    const isVideoEnabled = Boolean(lesson.media?.youtubeId);

    topGrid?.classList.toggle("is-audio-only", isAudioOnly);
    topGrid?.classList.toggle("is-video-enabled", isVideoEnabled);
    mediaStage?.classList.toggle("is-audio-only", isAudioOnly);
    mediaStage?.classList.toggle("is-video-enabled", isVideoEnabled);

    youtubeWrap?.classList.add("d-none");
    audioWrap?.classList.add("d-none");

    if (audioEl && lesson.media?.audioUrl) {
        audioEl.src = lesson.media.audioUrl;
        audioWrap?.classList.remove("d-none");
    }
    if (audioEl && hasSentenceAudio) audioWrap?.classList.remove("d-none");
}

function initYoutubePlayback(lesson) {
    if (!lesson.media?.youtubeId) return;
    dom.youtubePlayerWrap?.classList.remove("d-none");
    window.onYouTubeIframeAPIReady = createYoutubePlayer;

    if (window.YT?.Player) {
        createYoutubePlayer();
        return;
    }

    if (document.getElementById("yt-api-script")) return;

    const script = document.createElement("script");
    script.id = "yt-api-script";
    script.src = "https://www.youtube.com/iframe_api";
    script.async = true;
    script.onerror = () => { state.youtubeApiFailed = true; renderYoutubeFallback(); };
    document.head.appendChild(script);
}

function createYoutubePlayer() {
    const el = dom.youtubePlayer;
    if (!el || state.youtubePlayer || !window.YT?.Player) return;

    state.youtubePlayer = new YT.Player("youtube-player", {
        videoId: state.lessonData.media.youtubeId,
        playerVars: { rel: 0, modestbranding: 1, playsinline: 1 },
        events: {
            onReady: () => { state.youtubeReady = true; preloadYoutubePosition(); },
            onError: () => { state.youtubeApiFailed = true; renderYoutubeFallback(); }
        }
    });
}

function playCurrentSentenceAudio() {
    const sentence = state.sentences[state.currentIndex];
    if (!sentence) return;

    if (sentence.audioUrl && dom.audio) {
        dom.audio.src = sentence.audioUrl;
        dom.audio.currentTime = 0;
        dom.audio.play().catch(() => showToast("Không phát được audio của câu này."));
        return;
    }

    if (state.lessonData.media?.audioUrl && !state.lessonData.media?.youtubeId) {
        playAudioSegment(sentence);
        return;
    }

    if (state.lessonData.media?.youtubeId && !hasTimestamp(sentence)) {
        showToast("Câu này chưa có timestamp. Phát từ vị trí hiện tại.");
    }

    if (state.lessonData.media?.youtubeId) {
        playYoutubeSegment(sentence);
    } else {
        const utterance = new SpeechSynthesisUtterance(sentence.text);
        utterance.lang = window.__initialLesson?.course?.learningMode === "academic" ? "en-GB" : "en-US";
        utterance.rate = 0.88;
        window.speechSynthesis?.cancel();
        window.speechSynthesis?.speak(utterance);
    }
}

function playYoutubeSegment(sentence) {
    if (!state.youtubeReady || !state.youtubePlayer || state.youtubeApiFailed) {
        const start = hasTimestamp(sentence) ? Math.floor(sentence.startTime) : 0;
        renderYoutubeFallback(buildYoutubeEmbedUrl({ autoplay: 1, start }));
        return;
    }

    stopYoutubeSegment();

    if (hasTimestamp(sentence)) {
        state.youtubePlayer.seekTo(Math.max(0, sentence.startTime), true);
        state.youtubePlayer.playVideo();
        watchYoutubeEnd(sentence.endTime, sentence.startTime);
    } else {
        // No timestamp: just play from current position, no auto-stop
        state.youtubePlayer.playVideo();
    }
}

function watchYoutubeEnd(endTime, startTime) {
    window.clearTimeout(state.youtubeStopTimer);
    window.clearInterval(state.youtubePollTimer);

    const durationMs = Math.max(600, (endTime - startTime) * 1000 + 400);

    state.youtubePollTimer = setInterval(() => {
        try {
            if (state.youtubePlayer.getCurrentTime() >= endTime) {
                stopYoutubeSegment({ pauseOnly: true });
            }
        } catch (_) {}
    }, 100);

    state.youtubeStopTimer = setTimeout(() => {
        stopYoutubeSegment({ pauseOnly: true });
    }, durationMs);
}

function stopYoutubeSegment(opts = {}) {
    clearTimeout(state.youtubeStopTimer);
    clearInterval(state.youtubePollTimer);
    state.youtubeStopTimer = null;
    state.youtubePollTimer = null;
    if (state.youtubePlayer?.pauseVideo) state.youtubePlayer.pauseVideo();
    if (!opts.pauseOnly) preloadYoutubePosition();
}

function preloadYoutubePosition() {
    const sentence = state.sentences[state.currentIndex];
    if (state.youtubePlayer?.seekTo && hasTimestamp(sentence)) {
        state.youtubePlayer.seekTo(Math.max(0, sentence.startTime), true);
    }
}

function playAudioSegment(sentence) {
    const audio = dom.audio;
    if (!audio) return;
    clearTimeout(state.audioStopTimer);

    const timed = hasTimestamp(sentence);
    if (timed) {
        audio.currentTime = Math.max(0, sentence.startTime);
    } else if (audio.ended) {
        audio.currentTime = 0;
    }

    audio.play().catch(err => {
        console.error("Audio play error:", err);
        showToast("Không phát được audio. Kiểm tra file audio hoặc tải lại trang.");
    });

    if (timed) {
        const ms = Math.max(500, (sentence.endTime - sentence.startTime) * 1000);
        state.audioStopTimer = setTimeout(() => audio.pause(), ms);
    }
    // If no timestamp: audio plays until end or user pauses manually
}

function stopLessonPlayback() {
    window.clearTimeout(state.audioStopTimer);
    state.audioStopTimer = null;

    const lessonAudio = dom.audio;
    lessonAudio?.pause();
    state.audioPlayback?.pause();

    try {
        stopYoutubeSegment({ pauseOnly: true });
    } catch (error) {
        console.warn("Could not pause YouTube player before recording:", error);
    }

    if ((!state.youtubeReady || state.youtubeApiFailed) && state.lessonData?.media?.youtubeId) {
        const fallbackFrame = document.querySelector("#youtube-player-wrap iframe");
        if (fallbackFrame) {
            const sentence = state.sentences[state.currentIndex];
            const start = hasTimestamp(sentence) ? Math.floor(sentence.startTime) : 0;
            fallbackFrame.src = buildYoutubeEmbedUrl({ start });
        }
    }
}

function buildYoutubeEmbedUrl({ autoplay = 0, start = null } = {}) {
    const youtubeId = state.lessonData?.media?.youtubeId;
    if (!youtubeId) return "";
    let url = `https://www.youtube.com/embed/${youtubeId}?rel=0&modestbranding=1`;
    if (autoplay) url += "&autoplay=1";
    if (start !== null && start > 0) url += `&start=${start}`;
    return url;
}

function renderYoutubeFallback(src) {
    const wrap = dom.youtubePlayerWrap;
    if (!wrap) return;
    const url = src ?? buildYoutubeEmbedUrl();
    wrap.innerHTML = `<div class="ratio ratio-16x9"><iframe src="${url}" allowfullscreen allow="autoplay; encrypted-media"></iframe></div>`;
}

function hasTimestamp(sentence) {
    return sentence
        && typeof sentence.startTime === "number"
        && typeof sentence.endTime === "number"
        && sentence.endTime > sentence.startTime;
}

// =====================================================================
// SUBTITLE LIST
// =====================================================================
function renderSubtitleList() {
    const container = dom.subtitleList;
    if (!container) return;
    const fragment = document.createDocumentFragment();
    state.subtitleButtons = [];

    state.sentences.forEach((sentence, idx) => {
        const btn = document.createElement("button");
        btn.type = "button";
        btn.setAttribute("data-index", idx);
        btn.setAttribute("aria-label", `Câu ${idx + 1}: ${sentence.text}`);

        btn.onclick = () => {
            const isLocked = idx > state.unlockedIndex;
            if (isLocked) {
                showToast(`Đạt ${state.pronunciationTarget}% ở câu hiện tại để mở câu này.`);
                return;
            }
            selectSentence(idx);
        };

        const timeLabel = hasTimestamp(sentence)
            ? `<span class="subtitle-time">${formatTime(sentence.startTime)}</span>`
            : "";

        btn.innerHTML = `
            <span class="subtitle-num">${idx + 1}</span>
            <span class="subtitle-body">
                <span class="subtitle-text">${escapeHtml(sentence.text)}</span>
                ${sentence.translation ? `<span class="subtitle-trans">${escapeHtml(sentence.translation)}</span>` : ""}
            </span>
            ${timeLabel}
        `;

        const listItem = document.createElement("li");
        listItem.className = "subtitle-list-entry";
        listItem.appendChild(btn);
        fragment.appendChild(listItem);
        state.subtitleButtons.push(btn);
    });

    container.replaceChildren(fragment);
    updateSubtitleListUI();
}

function updateSubtitleListUI() {
    state.subtitleButtons.forEach((btn, idx) => {
        const isActive = idx === state.currentIndex;
        const isDone = state.completedIndexes.has(idx);
        const isLocked = idx > state.unlockedIndex;

        btn.className = ["subtitle-item",
            isActive ? "is-active" : "",
            isDone ? "is-done" : "",
            isLocked ? "is-locked" : "",
        ].filter(Boolean).join(" ");

        btn.setAttribute("aria-disabled", String(isLocked));
        const numEl = btn.querySelector(".subtitle-num");
        if (numEl) {
            numEl.textContent = isDone ? "\u2713" : idx + 1;
        }
    });

    const active = dom.subtitleList?.querySelector(".is-active");
    if (active && state.currentIndex > 0) {
        requestAnimationFrame(() => {
            active.scrollIntoView({ block: "nearest", behavior: "smooth" });
        });
    }
}

function formatTime(seconds) {
    if (typeof seconds !== "number") return "";
    const m = Math.floor(seconds / 60);
    const s = Math.floor(seconds % 60);
    return `${m}:${String(s).padStart(2, "0")}`;
}

// =====================================================================
// SENTENCE SELECTION
// =====================================================================
function selectSentence(idx) {
    if (idx < 0 || idx >= state.sentences.length || idx > state.unlockedIndex) return;

    stopYoutubeSegment();
    clearTimeout(state.audioStopTimer);
    resetRecordingState({ resetScore: true });

    state.currentIndex = idx;
    renderCurrentSentence(state.sentences[idx]);
    updateSubtitleListUI();
}

function renderCurrentSentence(sentence) {
    const container = dom.wordsContainer;
    if (!container) return;

    const words = sentence.text.split(/\s+/);
    const sentenceIpaTokens = splitSentenceIpa(sentence.ipa);
    const fragment = document.createDocumentFragment();

    words.forEach((rawWord, index) => {
        const wordIpa = sentenceIpaTokens[index] ?? "";
        const el = document.createElement("button");
        el.type = "button";
        el.dataset.word = normalizeWord(rawWord);
        el.dataset.wordIndex = index;
        el.onclick = (event) => showDictionary(event.currentTarget, rawWord, wordIpa, sentence.text);
        el.className = "word-token" + (wordIpa ? " has-ipa" : "");
        el.innerHTML = `<span class="word-text">${escapeHtml(rawWord)}</span>`
            + `<small class="word-ipa">${escapeHtml(wordIpa || "…")}</small>`;
        fragment.appendChild(el);
    });

    container.replaceChildren(fragment);
    const requestVersion = ++state.ipaRequestVersion;
    const missingIpaWords = words.filter((_, index) => !sentenceIpaTokens[index]);
    scheduleIpaHydration(missingIpaWords, requestVersion);

    if (dom.targetTranslation) dom.targetTranslation.textContent = sentence.translation ?? "";

    // Show sentence-level timestamp info
    const timeInfo = dom.sentenceTimeInfo;
    if (timeInfo) {
        if (hasTimestamp(sentence)) {
            timeInfo.textContent = `${formatTime(sentence.startTime)} - ${formatTime(sentence.endTime)}`;
            timeInfo.classList.remove("d-none");
        } else {
            timeInfo.classList.add("d-none");
        }
    }

    if (dom.playSampleBtnLabel) {
        dom.playSampleBtnLabel.textContent = isUntimedAudioLesson() ? "Phát audio" : "Nghe mẫu";
    }

    if (dom.playSampleBtn) dom.playSampleBtn.disabled = false;
}

function isUntimedAudioLesson() {
    return Boolean(state.lessonData?.media?.audioUrl)
        && !state.lessonData?.media?.youtubeId
        && !state.sentences.some(hasTimestamp);
}

async function hydrateWordIpa(words, requestVersion) {
    if (words.length === 0) return;

    try {
        const response = await fetch("/api/word-ipa/batch", {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify({ words: words.map(normalizeWord).filter(Boolean) })
        });
        if (!response.ok || requestVersion !== state.ipaRequestVersion) return;

        const entries = await response.json();
        const ipaByWord = new Map(entries.map(entry => [normalizeWord(entry.word), entry.ipa]));
        dom.wordsContainer?.querySelectorAll(".word-token").forEach(token => {
            const ipa = ipaByWord.get(token.dataset.word);
            if (!ipa) return;
            token.classList.add("has-ipa");
            token.querySelector(".word-ipa").textContent = ipa;
        });
    } catch (error) {
        console.warn("IPA lookup unavailable:", error);
    }
}

function scheduleIpaHydration(words, requestVersion) {
    if (state.ipaIdleHandle !== null) {
        if ("cancelIdleCallback" in window) {
            window.cancelIdleCallback(state.ipaIdleHandle);
        } else {
            window.clearTimeout(state.ipaIdleHandle);
        }
        state.ipaIdleHandle = null;
    }

    if (words.length === 0) return;

    const run = () => {
        state.ipaIdleHandle = null;
        hydrateWordIpa(words, requestVersion);
    };

    state.ipaIdleHandle = "requestIdleCallback" in window
        ? window.requestIdleCallback(run, { timeout: 2500 })
        : window.setTimeout(run, 1200);
}

function splitSentenceIpa(ipa) {
    if (!ipa) return [];

    return ipa
        .replace(/^[^:]+:\s*/, "")
        .split(/\s+/)
        .map(token => token.trim())
        .filter(Boolean)
        .map(token => token.startsWith("/") ? token : `/${token}/`);
}

function advanceSentence() {
    const current = state.currentIndex;
    if (!state.completedIndexes.has(current)) {
        showToast(`Cần đạt ${state.pronunciationTarget}% để sang câu tiếp theo.`);
        return;
    }
    if (current < state.sentences.length - 1) {
        selectSentence(current + 1);
    } else {
        showToast("Bạn đã hoàn thành tất cả câu trong bài!");
    }
}

// =====================================================================
// RECORDING
// =====================================================================
async function toggleRecording() {
    if (state.isRecording) {
        stopRecording();
    } else {
        await startRecording();
    }
}

async function startRecording() {
    try {
        stopLessonPlayback();
        resetRecordingState({ resetScore: false });
        state.attemptIdempotencyKey = createIdempotencyKey("attempt");

        const stream = await navigator.mediaDevices.getUserMedia({ audio: true });
        state.mediaRecorder = new MediaRecorder(stream);
        state.recordedChunks = [];
        state.recognizedText = "";

        state.mediaRecorder.ondataavailable = (e) => {
            if (e.data.size > 0) state.recordedChunks.push(e.data);
        };

        state.mediaRecorder.onstop = async () => {
            const browserBlob = new Blob(state.recordedChunks, {
                type: state.mediaRecorder.mimeType || "audio/webm"
            });

            try {
                state.audioBlob = await convertRecordingToWav(browserBlob);
            } catch (error) {
                console.error("WAV conversion failed:", error);
                state.audioBlob = browserBlob;
            }
            state.audioUrl = URL.createObjectURL(state.audioBlob);
            state.audioPlayback = new Audio(state.audioUrl);

            if (dom.playbackBtn) dom.playbackBtn.classList.remove("d-none");
            if (dom.clearBtn) dom.clearBtn.classList.remove("d-none");

            if (state.audioBlob.type !== "audio/wav") {
                showEvaluationError("Trình duyệt không chuyển được bản thu sang định dạng WAV. Hãy thử lại bằng Chrome hoặc Edge.");
                if (dom.recStatusText) dom.recStatusText.textContent = "Không xử lý được bản thu, hãy thu âm lại.";
                return;
            }

            await evaluateCurrentRecording();
        };

        state.mediaRecorder.start();
        state.isRecording = true;

        setRecordBtnState("recording");
        if (dom.recStatusText) {
            dom.recStatusText.innerHTML =
                '<span class="text-danger fw-bold">\u23fa Đang thu âm... Hãy đọc câu bên dưới.</span>';
        }

    } catch (err) {
        console.error("Mic access error:", err);
        showToast("Không truy cập được microphone. Kiểm tra quyền mic trong trình duyệt.");
    }
}

function stopRecording() {
    if (!state.mediaRecorder || !state.isRecording) return;

    state.isRecording = false;
    state.mediaRecorder.stop();
    state.mediaRecorder.stream.getTracks().forEach(t => t.stop());

    setRecordBtnState("idle");
    if (dom.recStatusText) {
        dom.recStatusText.innerHTML =
            '<span class="text-success fw-bold">\u2713 Đã lưu bản ghi.</span> Đang chấm điểm...';
    }
}

async function convertRecordingToWav(blob) {
    const audioContext = new (window.AudioContext || window.webkitAudioContext)();
    try {
        const audioBuffer = await audioContext.decodeAudioData(await blob.arrayBuffer());
        const mono = new Float32Array(audioBuffer.length);
        for (let channel = 0; channel < audioBuffer.numberOfChannels; channel++) {
            const channelData = audioBuffer.getChannelData(channel);
            for (let index = 0; index < channelData.length; index++) {
                mono[index] += channelData[index] / audioBuffer.numberOfChannels;
            }
        }
        return new Blob([encodeWav(mono, audioBuffer.sampleRate)], { type: "audio/wav" });
    } finally {
        await audioContext.close();
    }
}

function encodeWav(samples, sampleRate) {
    const buffer = new ArrayBuffer(44 + samples.length * 2);
    const view = new DataView(buffer);
    const writeText = (offset, value) => {
        for (let index = 0; index < value.length; index++) view.setUint8(offset + index, value.charCodeAt(index));
    };

    writeText(0, "RIFF");
    view.setUint32(4, 36 + samples.length * 2, true);
    writeText(8, "WAVE");
    writeText(12, "fmt ");
    view.setUint32(16, 16, true);
    view.setUint16(20, 1, true);
    view.setUint16(22, 1, true);
    view.setUint32(24, sampleRate, true);
    view.setUint32(28, sampleRate * 2, true);
    view.setUint16(32, 2, true);
    view.setUint16(34, 16, true);
    writeText(36, "data");
    view.setUint32(40, samples.length * 2, true);

    let offset = 44;
    for (const sample of samples) {
        const clamped = Math.max(-1, Math.min(1, sample));
        view.setInt16(offset, clamped < 0 ? clamped * 0x8000 : clamped * 0x7fff, true);
        offset += 2;
    }
    return buffer;
}

function setRecordBtnState(mode) {
    const btn = dom.recordBtn;
    const label = dom.recordBtnLabel;
    if (!btn || !label) return;

    btn.classList.remove("is-recording");

    if (mode === "recording") {
        btn.classList.add("is-recording");
        btn.disabled = false;
        label.textContent = "DỪNG THU ÂM";
        btn.setAttribute("aria-label", "Dừng thu âm");
    } else {
        btn.disabled = false;
        label.textContent = "BẮT ĐẦU THU ÂM";
        btn.setAttribute("aria-label", "Bắt đầu thu âm");
    }
}

async function evaluateCurrentRecording() {
    const sentence = state.sentences[state.currentIndex];
    if (!sentence) return;
    if (!state.aiAvailable) {
        showEvaluationError(AI_UNAVAILABLE_MESSAGE);
        return;
    }

    setScoreLoading(true);
    state.evaluationController?.abort();
    state.evaluationController = new AbortController();

    const formData = new FormData();
    formData.append("lessonId", String(state.lessonData.lessonId));
    formData.append("sentenceId", String(sentence.sentenceId));
    formData.append("sentenceIndex", String(state.currentIndex));
    if (sentence.savedSegmentId) formData.append("savedSegmentId", String(sentence.savedSegmentId));
    if (state.lessonData.aiPreviewId) formData.append("previewId", state.lessonData.aiPreviewId);
    formData.append("learningMode", window.__learningMode ?? "casual");
    state.attemptIdempotencyKey ??= createIdempotencyKey("attempt");
    formData.append("idempotencyKey", state.attemptIdempotencyKey);
    if (state.audioBlob) {
        const isWav = state.audioBlob.type === "audio/wav";
        formData.append("audio", state.audioBlob, isWav ? "recording.wav" : "recording.webm");
    }

    try {
        const response = await fetch("/api/practice/evaluate-shadowing", {
            method: "POST",
            headers: {
                "Idempotency-Key": state.attemptIdempotencyKey
            },
            body: formData,
            signal: state.evaluationController.signal
        });
        const payload = await response.json().catch(() => ({}));
        if (!response.ok) {
            if (response.status === 503) state.aiAvailable = false;
            throw new Error(payload.message || `Lỗi chấm điểm ${response.status}`);
        }

        state.aiAvailable = true;
        window.applyGamificationTransaction?.(payload.gamification);

        state.recognizedText = payload.transcript?.trim() ?? "";
        if (!state.recognizedText) {
            showNoSpeechDetected();
            return;
        }

        renderEvaluation({
            score: payload.score,
            passed: payload.passed,
            recognizedText: state.recognizedText,
            feedback: payload.feedback,
            words: payload.words ?? []
        });
    } catch (error) {
        if (error.name === "AbortError") return;
        console.error("Evaluation failed:", error);
        showEvaluationError(error.message || "Không chấm được bản thu. Vui lòng thử lại.");
    } finally {
        setScoreLoading(false);
    }
}

function renderEvaluation({ score, passed, recognizedText, feedback, words }) {
    const ring = dom.scoreRing;
    const numEl = dom.scoreNum;
    const feedbackTitle = dom.scoreFeedbackTitle;
    const feedbackSub = dom.scoreFeedbackSub;
    const nextBtn = dom.nextSentenceBtn;
    const recorderText = dom.recStatusText;
    const detailedFeedback = dom.detailedFeedback;

    const circumference = SCORE_CIRCUMFERENCE;
    const offset = circumference - (circumference * score / 100);
    const scoreColor = passed
        ? "var(--lesson-score-success)"
        : score >= 50
            ? "var(--lesson-score-warning)"
            : "var(--lesson-score-danger)";

    if (ring) {
        ring.style.strokeDasharray = circumference;
        ring.style.strokeDashoffset = offset;
        ring.style.stroke = scoreColor;
    }

    if (numEl) {
        numEl.textContent = score;
        numEl.style.color = scoreColor;
    }

    if (passed) {
        state.completedIndexes.add(state.currentIndex);
        // Math.max prevents locking a previously unlocked sentence.
        // Math.min ensures we don't advance beyond the final sentence index.
        state.unlockedIndex = Math.max(
            state.unlockedIndex,
            Math.min(state.currentIndex + 1, state.sentences.length - 1)
        );
        if (feedbackTitle) {
            feedbackTitle.textContent = "Tuyệt vời! Đạt rồi!";
            feedbackTitle.className = "score-feedback-title text-success";
        }
        if (feedbackSub) {
            feedbackSub.textContent = `Điểm ${score}% - vượt mục tiêu ${state.pronunciationTarget}%. Bạn có thể qua câu tiếp.`;
        }
        if (nextBtn) nextBtn.classList.remove("d-none");
        updateSubtitleListUI();
    } else {
        if (feedbackTitle) {
            feedbackTitle.textContent = score >= 50 ? "Gần đạt rồi!" : "Hãy thử lại!";
            feedbackTitle.className = score >= 50 ? "score-feedback-title text-warning" : "score-feedback-title text-danger";
        }
        if (feedbackSub) {
            feedbackSub.textContent = `Điểm ${score}% - cần đạt ${state.pronunciationTarget}%. Nghe lại mẫu rồi thu âm tiếp nhé.`;
        }
        if (nextBtn) nextBtn.classList.add("d-none");
    }

    if (recorderText) {
        recorderText.textContent = `"${recognizedText}"`;
        recorderText.classList.add("has-transcript");
    }
    if (detailedFeedback) detailedFeedback.value = feedback ?? "";
    applyWordFeedback(words);
}

function applyWordFeedback(words) {
    const tokens = [...(dom.wordsContainer?.querySelectorAll(".word-token") ?? [])];
    tokens.forEach(token => {
        token.classList.remove("is-correct", "is-warning", "is-incorrect");
        token.removeAttribute("title");
    });

    (words ?? []).forEach((item, index) => {
        const token = tokens[index];
        if (!token) return;
        const code = ["correct", "warning", "incorrect"].includes(item.accuracyCode)
            ? item.accuracyCode
            : "warning";
        token.classList.add(`is-${code}`);
        if (item.correction) token.title = item.correction;
    });
}

function showNoSpeechDetected() {
    const status = dom.recStatusText;
    if (status) {
        status.textContent = "Không nhận được giọng nói, hãy thu âm lại";
        status.classList.remove("has-transcript");
    }
    showEvaluationError("Không có đủ giọng nói trong bản thu để chấm điểm.");
}

function showEvaluationError(message) {
    const feedbackTitle = dom.scoreFeedbackTitle;
    const feedbackSub = dom.scoreFeedbackSub;
    const detailedFeedback = dom.detailedFeedback;
    if (feedbackTitle) {
        feedbackTitle.textContent = "Chưa thể chấm điểm";
        feedbackTitle.className = "score-feedback-title text-danger";
    }
    if (feedbackSub) feedbackSub.textContent = message;
    if (detailedFeedback) detailedFeedback.value = message;
}

function setScoreLoading(on) {
    const el = dom.scoreLoading;
    const ring = dom.scoreRingWrap;
    if (el) el.classList.toggle("d-none", !on);
    if (ring) ring.classList.toggle("d-none", on);
}

// =====================================================================
// RESET STATE
// =====================================================================
function resetRecordingState({ resetScore }) {
    state.evaluationController?.abort();
    state.evaluationController = null;

    if (state.isRecording && state.mediaRecorder) {
        state.isRecording = false;
        state.mediaRecorder.onstop = null;
        state.mediaRecorder.stream?.getTracks().forEach(t => t.stop());
        if (state.mediaRecorder.state !== "inactive") state.mediaRecorder.stop();
    }

    state.recognizedText = "";

    state.audioPlayback?.pause();
    if (state.audioUrl) URL.revokeObjectURL(state.audioUrl);
    state.recordedChunks = [];
    state.audioBlob = null;
    state.audioUrl = null;
    state.audioPlayback = null;

    setRecordBtnState("idle");
    if (dom.playbackBtn) dom.playbackBtn.classList.add("d-none");
    if (dom.clearBtn) dom.clearBtn.classList.add("d-none");
    if (dom.nextSentenceBtn) dom.nextSentenceBtn.classList.add("d-none");

    const recorderText = dom.recStatusText;
    if (recorderText) {
        recorderText.textContent = "Nhấn nút xanh để bắt đầu thu âm.";
        recorderText.classList.remove("has-transcript");
    }

    if (resetScore) resetScoreDisplay();
}

function resetScoreDisplay() {
    const ring = dom.scoreRing;
    const numEl = dom.scoreNum;
    const title = dom.scoreFeedbackTitle;
    const sub = dom.scoreFeedbackSub;
    const detailedFeedback = dom.detailedFeedback;

    if (ring) {
        ring.style.strokeDashoffset = SCORE_CIRCUMFERENCE;
        ring.style.removeProperty("stroke");
    }
    if (numEl) { numEl.textContent = "--"; numEl.style.color = ""; }
    if (title) { title.textContent = "Chưa có dữ liệu"; title.className = "score-feedback-title text-muted"; }
    if (sub) sub.textContent = "Nghe câu mẫu rồi bắt đầu thu âm.";
    if (detailedFeedback) {
        detailedFeedback.value = state.aiAvailable ? "" : AI_UNAVAILABLE_MESSAGE;
    }
    applyWordFeedback([]);

    setScoreLoading(false);
}

function playbackRecording() {
    state.audioPlayback?.play();
}

function clearRecording() {
    resetRecordingState({ resetScore: true });
}

// =====================================================================
// DICTIONARY BUBBLE
// =====================================================================
function initDictionaryBubble() {
    document.querySelector(".dict-bubble-close")?.addEventListener("click", hideDictionary);
    document.addEventListener("click", e => {
        const bubble = dom.dictBubble;
        if (!bubble || bubble.classList.contains("d-none")) return;
        if (!bubble.contains(e.target) && !e.target.closest(".word-token")) hideDictionary();
    });
    window.addEventListener("resize", hideDictionary);
    document.addEventListener("scroll", hideDictionary, true);
}

async function showDictionary(anchor, word, fallbackIpa, context) {
    const bubble = dom.dictBubble;
    if (!bubble) return;

    if (dom.dictWord) dom.dictWord.textContent = word;
    if (dom.dictIpa) dom.dictIpa.textContent = fallbackIpa;
    if (dom.dictMeaning) dom.dictMeaning.textContent = "Đang tra nghĩa...";

    bubble.classList.remove("d-none");
    positionDictionaryBubble(anchor, bubble);

    state.dictController?.abort();
    state.dictController = new AbortController();

    try {
        const response = await fetch("/api/word-meaning", {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify({ word: normalizeWord(word), context }),
            signal: state.dictController.signal
        });
        const result = await response.json().catch(() => ({}));
        if (!response.ok) throw new Error(result.message || "Không tra được từ này.");
        if (bubble.classList.contains("d-none")) return;
        if (dom.dictIpa) dom.dictIpa.textContent = result.ipa || fallbackIpa;
        if (dom.dictMeaning) dom.dictMeaning.textContent = result.meaning || "Chưa có nghĩa phù hợp.";
        positionDictionaryBubble(anchor, bubble);
    } catch (error) {
        if (error.name === "AbortError") return;
        if (dom.dictMeaning) dom.dictMeaning.textContent = error.message || "Không tra được từ này.";
        positionDictionaryBubble(anchor, bubble);
    } finally {
        state.dictController = null;
    }
}

function positionDictionaryBubble(anchor, bubble) {
    bubble.style.visibility = "hidden";

    requestAnimationFrame(() => {
        const spacing = 10, vPad = 8;
        const aRect = anchor.getBoundingClientRect();
        const bRect = bubble.getBoundingClientRect();
        const cx = aRect.left + aRect.width / 2;

        let left = cx - bRect.width / 2;
        left = Math.max(vPad, Math.min(left, window.innerWidth - bRect.width - vPad));
        let top = aRect.top - bRect.height - spacing;
        let below = false;
        if (top < vPad) { top = aRect.bottom + spacing; below = true; }

        const arrowLeft = Math.max(18, Math.min(cx - left, bRect.width - 18));
        bubble.style.left = `${left}px`;
        bubble.style.top = `${top}px`;
        bubble.style.setProperty("--arrow-left", `${arrowLeft}px`);
        bubble.classList.toggle("is-below", below);
        bubble.style.visibility = "";
    });
}

function hideDictionary() {
    state.dictController?.abort();
    state.dictController = null;
    dom.dictBubble?.classList.add("d-none");
}

// =====================================================================
// UTILS
// =====================================================================
function normalizeWord(word) {
    return word.replace(/[.,!?;:"'()\u2018\u2019\u201c\u201d]/g, "").toLowerCase().trim();
}

function createIdempotencyKey(prefix) {
    const id = window.crypto?.randomUUID?.()
        ?? `${Date.now()}-${Math.random().toString(16).slice(2)}`;
    return `${prefix}-${id}`;
}

function escapeHtml(str) {
    return String(str)
        .replace(/&/g, "&amp;")
        .replace(/</g, "&lt;")
        .replace(/>/g, "&gt;")
        .replace(/"/g, "&quot;");
}

function showToast(message) {
    const existing = document.getElementById("lesson-toast");
    if (existing) existing.remove();

    const toast = document.createElement("div");
    toast.id = "lesson-toast";
    toast.className = "lesson-toast";
    toast.textContent = message;
    document.body.appendChild(toast);

    requestAnimationFrame(() => toast.classList.add("is-visible"));
    setTimeout(() => {
        toast.classList.remove("is-visible");
        setTimeout(() => toast.remove(), 300);
    }, 3000);
}

function showPageError(message) {
    const el = dom.pageError;
    const msgEl = dom.pageErrorMsg;
    if (msgEl) msgEl.textContent = message;
    if (el) el.classList.remove("d-none");
    if (dom.lessonStudio) dom.lessonStudio.classList.add("d-none");
}

window.lessonPlayer = {
    getCurrentIndex: () => state.currentIndex,
    getCurrentSentence: () => state.sentences[state.currentIndex] ?? null,
    playSentence(index) {
        if (index < 0 || index >= state.sentences.length) return;
        const previous = state.currentIndex;
        state.currentIndex = index;
        playCurrentSentenceAudio();
        state.currentIndex = previous;
    },
    stop() {
        stopYoutubeSegment({ pauseOnly: true });
        if (dom.audio) {
            dom.audio.pause();
            clearTimeout(state.audioStopTimer);
        }
    }
};
