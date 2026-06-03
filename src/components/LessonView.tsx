/**
 * @license
 * SPDX-License-Identifier: Apache-2.0
 */

import React, { useState, useEffect, useRef } from 'react';
import { Lesson, Sentence, EvaluationResult, UserStats, WordGrade } from '../types';
import { 
  ArrowLeft, Mic, Square, Sparkles, Volume2, Bookmark, CheckCircle2, AlertCircle, XCircle, 
  HelpCircle, ThumbsUp, Activity, RotateCcw, Keyboard, Award, Trophy, BookmarkCheck
} from 'lucide-react';

interface LessonViewProps {
  lesson: Lesson;
  stats: UserStats;
  isPremium: boolean;
  onUpdateStats: (newStats: UserStats) => void;
  onBack: () => void;
  onSaveFavorite: (sentence: Sentence, lessonTitle: string) => void;
  onSaveFlashcard: (word: string, context: string, ipa: string, accuracyCode: string) => void;
}

export function LessonView({
  lesson,
  stats,
  isPremium,
  onUpdateStats,
  onBack,
  onSaveFavorite,
  onSaveFlashcard
}: LessonViewProps) {
  const [activeSentenceIndex, setActiveSentenceIndex] = useState(0);
  const currentSentence = lesson.sentences[activeSentenceIndex];

  // Core recording mechanisms
  const [isRecording, setIsRecording] = useState(false);
  const [transcribedText, setTranscribedText] = useState('');
  const [interimTranscript, setInterimTranscript] = useState('');
  const [recognitionError, setRecognitionError] = useState<string | null>(null);
  
  // AI Coach Evaluation states
  const [isEvaluating, setIsEvaluating] = useState(false);
  const [evaluation, setEvaluation] = useState<EvaluationResult | null>(null);
  const [selectedWord, setSelectedWord] = useState<WordGrade | null>(null);

  // Mini Games tabs
  const [activeTab, setActiveTab] = useState<'shadowing' | 'dictation' | 'ipa-game'>('shadowing');

  // Interactive Dictation States 
  const [dictationInput, setDictationInput] = useState('');
  const [dictationChecked, setDictationChecked] = useState(false);
  const [dictationPassed, setDictationPassed] = useState(false);

  // IPA Matching Game States
  const [ipaQuizOptions, setIpaQuizOptions] = useState<string[]>([]);
  const [ipaQuizCorrect, setIpaQuizCorrect] = useState('');
  const [ipaSelectedAnswer, setIpaSelectedAnswer] = useState<string | null>(null);
  const [ipaFeedbackMessage, setIpaFeedbackMessage] = useState<string | null>(null);

  const canUseIpaGame = isPremium || stats.hearts > 0;

  // Speech Recognition Ref
  const recognitionRef = useRef<any>(null);

  // Set up Speech Recognition on component load
  useEffect(() => {
    // Check compatibility
    const SpeechReg = (window as any).SpeechRecognition || (window as any).webkitSpeechRecognition;
    if (SpeechReg) {
      const rec = new SpeechReg();
      rec.continuous = true;
      rec.interimResults = true;
      rec.lang = 'en-US';

      rec.onstart = () => {
        setIsRecording(true);
        setTranscribedText('');
        setInterimTranscript('');
        setRecognitionError(null);
      };

      rec.onresult = (event: any) => {
        let interim = '';
        let final = '';

        for (let i = event.resultIndex; i < event.results.length; ++i) {
          if (event.results[i].isFinal) {
            final += event.results[i][0].transcript + ' ';
          } else {
            interim += event.results[i][0].transcript;
          }
        }

        if (final) {
          setTranscribedText((prev) => prev + final);
        }
        setInterimTranscript(interim);
      };

      rec.onerror = (event: any) => {
        console.error('Speech Recognition error', event);
        if (event.error !== 'no-speech') {
          setRecognitionError(`Hệ thống ghi âm: ${event.error}. Nhấp cho phép truy cập micro.`);
        }
      };

      rec.onend = () => {
        setIsRecording(false);
      };

      recognitionRef.current = rec;
    } else {
      setRecognitionError('Trình diệt không hỗ trợ trực tiếp Google Speech API. Hãy dùng Chrome để có kết quả tốt nhất.');
    }
  }, []);

  // Regenerate options for IPA Matching Game whenever the active sentence changes
  useEffect(() => {
    if (!currentSentence) return;
    
    // Extract correct clean target word from current sentence text
    const cleanWords = currentSentence.text
      .replace(/[.,\/#!$%\^&\*;:{}=\-_`~()?]/g, "")
      .split(/\s+/)
      .filter(Boolean);
    
    if (cleanWords.length === 0) return;
    
    const correctTargetWord = cleanWords[Math.floor(cleanWords.length / 2)].toLowerCase();
    
    // Distractor words
    // TODO: These distractor words are hardcoded heuristics for the IPA quiz.
    // A backend or AI service should generate context-aware distractors dynamically for each sentence.
    const distractors = ['about', 'around', 'activity', 'routine', 'people', 'coffee', 'business', 'environment']
      .filter(w => w !== correctTargetWord)
      .sort(() => 0.5 - Math.random())
      .slice(0, 3);
      
    const options = [correctTargetWord, ...distractors].sort(() => 0.5 - Math.random());
    
    setIpaQuizOptions(options);
    setIpaQuizCorrect(correctTargetWord);
    setIpaSelectedAnswer(null);
    setIpaFeedbackMessage(null);
    setDictationInput('');
    setDictationChecked(false);
    setDictationPassed(false);
    setEvaluation(null);
    setTranscribedText('');
    setInterimTranscript('');
    setSelectedWord(null);
  }, [activeSentenceIndex, activeTab]);

  // Handle client-side TTS pronunciation playback
  const handlePlayTTS = (text: string) => {
    if ('speechSynthesis' in window) {
      window.speechSynthesis.cancel();
      const utterance = new SpeechSynthesisUtterance(text);
      utterance.lang = 'en-US';
      utterance.rate = 0.85; // Slow down slightly for clear shadowing
      window.speechSynthesis.speak(utterance);
    } else {
      alert('Trình duyệt không hỗ trợ tổng hợp giọng nói offline.');
    }
  };

  // Toggle Standard Recording State
  const handleToggleRecording = () => {
    if (!recognitionRef.current) {
      alert('Không nhận dạng được driver Micro trên trình duyệt máy khách.');
      return;
    }

    if (isRecording) {
      recognitionRef.current.stop();
    } else {
      try {
        setEvaluation(null);
        recognitionRef.current.start();
      } catch (err) {
        console.error(err);
      }
    }
  };

  // Hand voice evaluation query to Backend (.NET/Express) API
  const handleEvaluatePronunciation = async () => {
    if (!transcribedText && !interimTranscript) {
      alert('Vui lòng thu âm giọng đọc của bạn trước khi đối chiếu chấm điểm.');
      return;
    }

    setIsEvaluating(true);
    setSelectedWord(null);

    const clientTranscript = (transcribedText + interimTranscript).trim();

    try {
      // TODO: This evaluation endpoint should be backed by a real pronunciation evaluation service.
      // The backend should accept the sentence, transcript, user context, and return structured scoring.
      // It should also persist evaluation history in the user database for analytics and progress tracking.
      const response = await fetch('/api/evaluate', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json'
        },
        body: JSON.stringify({
          targetText: currentSentence.text,
          transcript: clientTranscript,
          level: lesson.topic,
          // TODO: This goal is currently hardcoded for evaluation.
          // Use the authenticated user's real goal or send full profile context from the backend.
          userGoal: 'comprehension70'
        })
      });

      if (!response.ok) {
        throw new Error('Dịch vụ AI phản hồi chậm hoặc không kết nối.');
      }

      const result: EvaluationResult = await response.json();
      setEvaluation(result);

      // Perform dynamic EXP awards based on scores; hearts only change in IPA tab.
      const updatedStats = { ...stats };
      updatedStats.totalSentences += 1;

      if (result.score >= 80) {
        updatedStats.exp += 15;
      } else if (result.score >= 60) {
        updatedStats.exp += 10;
      } else {
        updatedStats.exp += 5;
      }

      // Automatically register incorrect words inside learners target flashcards dictionary!
      result.words.forEach((w) => {
        if (w.accuracyCode === 'incorrect' || w.accuracyCode === 'warning') {
          onSaveFlashcard(
            w.word,
            `Bối cảnh câu thoại: "${currentSentence.text}"`,
            w.ipa || `[${w.word.toLowerCase()}]`,
            w.accuracyCode
          );
        }
      });

      onUpdateStats(updatedStats);

    } catch (err) {
      console.error(err);
      alert('Không thể kết nối đến Trí tuệ nhân tập chấm điểm. Đảm bảo server đang khởi chạy.');
    } finally {
      setIsEvaluating(false);
    }
  };

  // Check manual dictation spelling matching
  const handleCheckDictation = () => {
    if (!dictationInput.trim()) return;

    // Clean up spelling discrepancies
    const cleanInput = dictationInput.trim().toLowerCase().replace(/[.,\/#!$%\^&\*;:{}=\-_`~()?]/g, "");
    const cleanTarget = currentSentence.text.trim().toLowerCase().replace(/[.,\/#!$%\^&\*;:{}=\-_`~()?]/g, "");

    const passed = cleanInput === cleanTarget;
    setDictationPassed(passed);
    setDictationChecked(true);

    if (passed) {
      const updatedStats = { ...stats };
      updatedStats.exp += 12; // Dictation complete points
      onUpdateStats(updatedStats);
    }
  };

  // Check IPA Quiz word matching
  const handleCheckIpaAnswer = (option: string) => {
    if (!canUseIpaGame) {
      setIpaFeedbackMessage('Hãy đăng ký VIP để tiếp tục trải nghiệm tính năng này.');
      return;
    }

    if (ipaSelectedAnswer) return; // Answered already
    
    setIpaSelectedAnswer(option);
    const updatedStats = { ...stats };

    if (option === ipaQuizCorrect) {
      setIpaFeedbackMessage('Chúc mừng! Đáp án hoàn toàn chuẩn xác. IPA này khớp với từ: ' + option);
      updatedStats.exp += 15;
    } else {
      setIpaFeedbackMessage(`Ơ kìa, chưa khớp rồi! IPA này thuộc về từ "${ipaQuizCorrect}". Hãy thử lại ở câu sau nhé.`);
      if (!isPremium) {
        updatedStats.hearts = Math.max(0, updatedStats.hearts - 1);
      }
    }

    onUpdateStats(updatedStats);
  };

  const getAccuracyColor = (code: string) => {
    switch (code) {
      case 'correct': return 'text-emerald-500 hover:bg-emerald-50 hover:dark:bg-emerald-500/10 cursor-help underline decoration-dotted transition-colors';
      case 'warning': return 'text-amber-500 hover:bg-amber-50 hover:dark:bg-amber-500/10 cursor-help underline decoration-dotted transition-colors';
      case 'incorrect': return 'text-rose-500 hover:bg-rose-50 hover:dark:bg-rose-500/10 cursor-help underline decoration-dotted transition-colors';
      default: return 'text-slate-800 dark:text-slate-200';
    }
  };

  return (
    <div id="lesson-view-container" className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-6">
      
      {/* Top controls and headers bar */}
      <div className="flex items-center justify-between mb-5">
        <button
          onClick={onBack}
          className="flex items-center gap-2 text-slate-500 hover:text-indigo-600 dark:text-slate-400 dark:hover:text-sky-400 font-sans font-bold text-xs cursor-pointer bg-white dark:bg-slate-900 border border-slate-100 dark:border-slate-800/80 px-3 py-1.5 rounded-xl shadow-xs"
        >
          <ArrowLeft className="w-3.5 h-3.5" />
          <span>Quay lại Thư Viện</span>
        </button>

        <span className="text-[10px] text-slate-400 font-mono bg-slate-100 dark:bg-slate-800/60 px-2.5 py-1 rounded-md">
          CHỦ ĐỀ: <span className="font-bold text-indigo-600 dark:text-sky-400">{lesson.topic.toUpperCase()}</span> • <span className="font-semibold text-slate-500">{lesson.level}</span>
        </span>
      </div>

      {/* Primary High Density 2-Column Grid */}
      <div className="grid lg:grid-cols-12 gap-6 items-start">
        
        {/* LEFT COLUMN: Training Dashboard & Content Modules (8 Cols) */}
        <div className="lg:col-span-8 space-y-6">
          
          <div className="bg-white dark:bg-slate-900 border border-slate-100 dark:border-slate-800/80 rounded-2xl p-5 md:p-6 shadow-xs relative overflow-hidden">
            
            {/* Left accent strip for High Density feel */}
            <div className="absolute left-0 top-0 bottom-0 w-1 bg-indigo-600 dark:bg-sky-500"></div>

            {/* Lesson General Title Banner */}
            <div className="flex flex-col sm:flex-row justify-between items-start sm:items-center border-b border-slate-50 dark:border-slate-800/60 pb-4 mb-5 gap-4">
              <div>
                <span className="text-[9px] bg-indigo-50 text-indigo-700 dark:bg-sky-500/10 dark:text-sky-400 px-2 py-0.5 rounded uppercase font-mono font-bold tracking-wider">
                  Khóa Học Tiếng Anh Shadowing
                </span>
                <h2 className="text-lg md:text-xl font-sans font-extrabold tracking-tight text-slate-900 dark:text-slate-100 mt-1">
                  {lesson.title}
                </h2>
              </div>

              <button
                onClick={() => onSaveFavorite(currentSentence, lesson.title)}
                className="px-2.5 py-1.5 rounded-lg border border-slate-200 dark:border-slate-800 hover:bg-slate-50 dark:hover:bg-slate-800/60 text-slate-500 hover:text-indigo-600 flex items-center gap-1.5 transition-all text-[11px] font-bold cursor-pointer shrink-0"
              >
                <Bookmark className="w-3.5 h-3.5 text-indigo-600 dark:text-sky-400" />
                <span>Bookmark câu này</span>
              </button>
            </div>

            {/* Modular Tab Headers with high density line indicators */}
            <div className="flex border-b border-slate-100 dark:border-slate-800/40 mb-5 font-sans text-xs">
              <button
                onClick={() => setActiveTab('shadowing')}
                className={`px-3 py-2 font-extrabold pb-2 border-b-2 cursor-pointer transition-colors ${
                  activeTab === 'shadowing'
                    ? 'border-indigo-600 text-indigo-600 dark:border-sky-400 dark:text-sky-400'
                    : 'border-transparent text-slate-400 hover:text-slate-600'
                }`}
              >
                Studio Shadowing
              </button>
              <button
                onClick={() => setActiveTab('dictation')}
                className={`px-3 py-2 font-extrabold pb-2 border-b-2 cursor-pointer transition-colors ${
                  activeTab === 'dictation'
                    ? 'border-indigo-600 text-indigo-600 dark:border-sky-400 dark:text-sky-400'
                    : 'border-transparent text-slate-400 hover:text-slate-600'
                }`}
              >
                Nghe & Chép Chính Tả
              </button>
              <button
                onClick={() => {
                  if (!canUseIpaGame) {
                    setIpaFeedbackMessage('Hãy đăng ký VIP để tiếp tục trải nghiệm tính năng này.');
                    return;
                  }
                  setActiveTab('ipa-game');
                }}
                className={`px-3 py-2 font-extrabold pb-2 border-b-2 cursor-pointer transition-colors ${
                  activeTab === 'ipa-game'
                    ? 'border-indigo-600 text-indigo-600 dark:border-sky-400 dark:text-sky-400'
                    : 'border-transparent text-slate-400 hover:text-slate-600'
                }`}
              >
                Thử thách cặp IPA
              </button>
            </div>

            {/* TAB CONTENT: CORE SHADOWING STUDIO */}
            {activeTab === 'shadowing' && (
              <div className="space-y-5">
                
                {/* Active Sentence practice presentation (Uses .active-sentence pattern) */}
                <div className="active-sentence p-5 md:p-6 rounded-xl border border-indigo-100/50 dark:border-sky-500/10 shadow-xxs text-center space-y-3.5">
                  
                  {/* Video embed for Video Bank lessons */}
                  {lesson.videoUrl !== undefined && (
                    <div id="lesson-video-player" className="mb-5 aspect-video w-full max-w-2xl mx-auto rounded-2xl overflow-hidden bg-black border border-slate-200 dark:border-slate-800/80 shadow-md">
                      <iframe
                        className="w-full h-full"
                        src={lesson.videoUrl || ''}
                        title={`Video ${lesson.title}`}
                        allow="accelerometer; autoplay; clipboard-write; encrypted-media; gyroscope; picture-in-picture"
                        allowFullScreen
                      ></iframe>
                    </div>
                  )}

                  {/* Target Phrase text split checking */}
                  <div id="target-phrase" className="text-lg md:text-xl font-sans font-extrabold tracking-tight text-slate-900 dark:text-slate-50 flex flex-wrap justify-center gap-x-2 gap-y-1 leading-relaxed">
                    {evaluation ? (
                      evaluation.words.map((w, i) => (
                        <button
                          key={i}
                          onClick={() => setSelectedWord(w)}
                          className={getAccuracyColor(w.accuracyCode)}
                        >
                          {w.word}
                        </button>
                      ))
                    ) : (
                      currentSentence.text.split(' ').map((w, i) => (
                        <span key={i} className="hover:text-indigo-600 dark:hover:text-sky-400 transition-colors duration-150">{w}</span>
                      ))
                    )}
                  </div>

                  {/* Target Phonetics IPA */}
                  <div className="font-mono text-xs font-semibold text-indigo-500 dark:text-sky-400 tracking-wide">
                    {currentSentence.ipa}
                  </div>

                  {/* Target Vietnamese translation */}
                  <div className="font-sans text-slate-500 dark:text-slate-400 text-xs md:text-sm font-medium">
                    "{currentSentence.translation}"
                  </div>

                  {/* Word Correction details panel */}
                  {selectedWord && (
                    <div className="mt-3 p-3.5 bg-yellow-400/5 dark:bg-yellow-400/10 border border-yellow-200/50 rounded-xl max-w-md mx-auto text-left flex gap-3 text-[11px] leading-normal animate-slide-up">
                      <div className="w-5 h-5 rounded-full bg-yellow-400/20 text-yellow-600 dark:text-yellow-400 flex items-center justify-center shrink-0 font-bold font-mono text-[10px]">!</div>
                      <div>
                        <h4 className="font-bold text-slate-900 dark:text-slate-50 flex items-center gap-1.5">
                          Phát âm từ: <span className="text-indigo-600 dark:text-sky-400 font-mono text-xs">"{selectedWord.word}"</span>
                          <span className="text-[10px] text-slate-400 font-normal">({selectedWord.accuracyCode === 'incorrect' ? 'Sai hoàn toàn' : 'Gần đúng'})</span>
                        </h4>
                        {selectedWord.ipa && <p className="text-[10px] font-mono mt-0.5 text-slate-400">IPA chuẩn: {selectedWord.ipa}</p>}
                        <p className="text-slate-500 mt-1 dark:text-slate-300 font-sans leading-relaxed">{selectedWord.correction || 'Vui lòng đọc lại chậm rãi, phát bật rõ phụ âm đuôi s/es/ed.'}</p>
                      </div>
                    </div>
                  )}

                </div>

                {/* Simulated Audio Stream Control Line with waveform */}
                <div className="flex justify-between items-center bg-slate-50 dark:bg-slate-950/20 rounded-xl p-3 border border-slate-100 dark:border-slate-800 gap-4">
                  <div className="flex items-center gap-3">
                    <button
                      onClick={() => handlePlayTTS(currentSentence.text)}
                      className="w-10 h-10 bg-indigo-600 hover:bg-indigo-700 text-white rounded-lg shadow-xs flex items-center justify-center transition-all cursor-pointer"
                      title="Nghe giọng đọc chuẩn bản xứ"
                    >
                      <Volume2 className="w-4.5 h-4.5 animate-pulse" />
                    </button>
                    <div className="text-left font-sans">
                      <p className="text-[11px] font-bold text-slate-700 dark:text-slate-200">Listening Mode (Nghe âm chuẩn)</p>
                      <p className="text-[10px] text-slate-400">Nghe máy học phát âm nhấn ngắt nhịp tự nhiên.</p>
                    </div>
                  </div>

                  {/* Audio visual waveform simulation block */}
                  <div className="h-6 flex items-center gap-1 shrink-0 px-2 bg-white dark:bg-slate-950 border border-slate-100 dark:border-slate-900 rounded-md py-1">
                    {[1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12].map((idx) => (
                      <span 
                        key={idx} 
                        className={`inline-block w-[2.5px] bg-indigo-500 dark:bg-sky-400 rounded-full ${
                          isRecording ? 'wave-bar' : 'h-1.5 opacity-30'
                        }`} 
                        style={{ 
                          height: isRecording ? undefined : `${(idx % 4 + 1) * 3}px` 
                        }}
                      ></span>
                    ))}
                  </div>
                </div>

                {/* Local Client Device Recording Engine Interface */}
                <div className="bg-slate-50/50 dark:bg-slate-950/20 border border-slate-100 dark:border-slate-800 rounded-xl p-4 flex flex-col justify-between">
                  <div>
                    <h4 className="text-[9px] font-extrabold text-slate-400 uppercase tracking-widest block mb-1">Thiết bị thu âm</h4>
                    
                    {isRecording ? (
                      <div className="text-[11px] text-rose-500 flex items-center gap-1.5 font-bold animate-pulse mt-0.5">
                        <span className="w-2 h-2 rounded-full bg-rose-500 animate-ping"></span>
                        <span>Micro bận: Nói to và bắt chước nhịp điệu của câu nói mẫu...</span>
                      </div>
                    ) : (
                      <p className="text-[10px] text-slate-400 mt-0.5">Micro sẵn sàng. Nhấp nút xanh bên dưới để ghi âm nhại tiếng.</p>
                    )}

                    {/* STT speech recognition live string display panel */}
                    <div className="mt-3 p-3 bg-white dark:bg-slate-950/90 border border-slate-200/60 dark:border-slate-800 rounded-lg min-h-[60px] max-h-[100px] overflow-y-auto text-xs font-bold text-slate-800 dark:text-slate-100 font-sans leading-relaxed relative">
                      {transcribedText || interimTranscript ? (
                        <>
                          <span className="text-slate-800 dark:text-slate-100">{transcribedText}</span>
                          <span className="text-slate-400 italic font-normal">{interimTranscript}</span>
                        </>
                      ) : (
                        <span className="text-slate-400 italic font-normal text-[11px]">Văn bản STT thực tế tương tác sẽ khớp tại đây...</span>
                      )}
                    </div>

                    {recognitionError && (
                      <p className="text-[9px] text-rose-500 mt-1.5 flex items-center gap-1 leading-normal">
                        <AlertCircle className="w-3 h-3 text-rose-500 shrink-0" />
                        <span>{recognitionError}</span>
                      </p>
                    )}
                  </div>

                  <div className="flex items-center gap-2 mt-4">
                    <button
                      onClick={handleToggleRecording}
                      className={`px-4 py-2.5 rounded-lg font-bold font-sans text-xs flex items-center gap-2 cursor-pointer transition-all ${
                        isRecording 
                          ? 'bg-rose-600 text-white animate-rec shadow-lg' 
                          : 'bg-emerald-600 hover:bg-emerald-700 text-white shadow-xs'
                      }`}
                    >
                      {isRecording ? (
                        <>
                          <Square className="w-3.5 h-3.5 fill-white text-white" />
                          <span>DỪNG THU</span>
                        </>
                      ) : (
                        <>
                          <Mic className="w-3.5 h-3.5 text-white" />
                          <span>BẮT ĐẦU THU ÂM</span>
                        </>
                      )}
                    </button>

                    {(transcribedText || interimTranscript) && !isRecording && (
                      <button
                        onClick={() => { setTranscribedText(''); setInterimTranscript(''); setEvaluation(null); }}
                        className="px-3 py-2.5 rounded-lg border border-slate-200 dark:border-slate-800 text-slate-500 hover:bg-slate-50 dark:hover:bg-slate-800 font-bold text-[11px] cursor-pointer"
                      >
                        Xóa Thu Âm
                      </button>
                    )}
                  </div>
                </div>

              </div>
            )}

            {/* TAB CONTENT: DICTATION SPELLING GAME */}
            {activeTab === 'dictation' && (
              <div className="space-y-4">
                <div className="p-4 bg-indigo-50/20 dark:bg-slate-950/40 rounded-xl border border-indigo-100/10 text-center font-sans">
                  <span className="text-[9px] uppercase font-bold tracking-widest text-indigo-600 dark:text-sky-400 block mb-1">Phương pháp Dictation</span>
                  <h3 className="font-extrabold text-xs text-slate-800 dark:text-slate-200">Gõ lại đúng những gì bạn nghe thấy</h3>
                  
                  <button
                    type="button"
                    onClick={() => handlePlayTTS(currentSentence.text)}
                    className="mt-3 mx-auto w-10 h-10 rounded-full bg-indigo-600 hover:bg-indigo-700 text-white flex items-center justify-center cursor-pointer transition-all hover:scale-105 shadow"
                    title="Nghe câu mẫu"
                  >
                    <Volume2 className="w-4.5 h-4.5" />
                  </button>
                </div>

                <div className="space-y-3 font-sans text-xs">
                  <div>
                    <label className="text-[9px] font-bold text-slate-400 uppercase tracking-wider block mb-1">NHẬP VĂN BẢN CHÍNH TẢ</label>
                    <textarea
                      value={dictationInput}
                      onChange={(e) => setDictationInput(e.target.value)}
                      disabled={dictationChecked && dictationPassed}
                      rows={2}
                      className="w-full bg-slate-50 dark:bg-slate-950 p-3 rounded-lg border border-slate-200 dark:border-slate-800 font-sans tracking-wide leading-relaxed text-xs focus:outline-none focus:border-indigo-500"
                      placeholder="Nghe, dịch rồi gõ lại chuẩn xác từng từ..."
                    />
                  </div>

                  {dictationChecked && (
                    <div className={`p-3 rounded-xl flex gap-2.5 leading-normal border ${
                      dictationPassed 
                        ? 'bg-emerald-50 text-emerald-800 border-emerald-200/50 dark:bg-emerald-500/5 dark:text-emerald-400 dark:border-emerald-500/10'
                        : 'bg-rose-50 text-rose-800 border-rose-200/50 dark:bg-rose-500/5 dark:text-rose-400 dark:border-rose-500/10'
                    }`}>
                      {dictationPassed ? (
                        <>
                          <CheckCircle2 className="w-4 h-4 text-emerald-500 shrink-0 mt-0.5" />
                          <div>
                            <h4 className="font-bold text-xs">Hoàn toàn chuẩn xác! +12 EXP</h4>
                            <p className="text-[10px] opacity-90">Từ vựng, ký tự, dấu phẩy của bạn trùng khớp 100%.</p>
                          </div>
                        </>
                      ) : (
                        <>
                          <XCircle className="w-4 h-4 text-rose-500 shrink-0 mt-0.5" />
                          <div>
                            <h4 className="font-bold text-xs">Chưa trùng khớp rồi!</h4>
                            <p className="text-[10px] opacity-90">Đáp án chuẩn: <strong className="font-mono text-indigo-600 dark:text-sky-400 select-all font-bold">"{currentSentence.text}"</strong></p>
                          </div>
                        </>
                      )}
                    </div>
                  )}

                  <div className="flex gap-2">
                    <button
                      type="button"
                      onClick={handleCheckDictation}
                      disabled={!dictationInput.trim() || (dictationChecked && dictationPassed)}
                      className={`px-4 py-2 rounded-lg font-bold flex items-center justify-center gap-1.5 transition-all text-xs cursor-pointer ${
                        (!dictationInput.trim() || (dictationChecked && dictationPassed))
                          ? 'bg-slate-100 text-slate-400 cursor-not-allowed dark:bg-slate-800 dark:text-slate-600'
                          : 'bg-indigo-600 text-white hover:bg-indigo-700'
                      }`}
                    >
                      <Keyboard className="w-3.5 h-3.5" />
                      <span>Kết quả chép</span>
                    </button>

                    {dictationChecked && (
                      <button
                        type="button"
                        onClick={() => { setDictationInput(''); setDictationChecked(false); }}
                        className="px-3 py-2 rounded-lg border border-slate-200 dark:border-slate-800 text-slate-500 hover:bg-slate-50 dark:hover:bg-slate-800 font-bold text-xs cursor-pointer"
                      >
                        Thử lại
                      </button>
                    )}
                  </div>
                </div>
              </div>
            )}

            {/* TAB CONTENT: IPA MATCHING GAME */}
            {activeTab === 'ipa-game' && (
              <div className="space-y-4">
                <div className="p-4 bg-amber-50/20 dark:bg-slate-950/40 rounded-xl border border-amber-100/15 text-center font-sans">
                  <span className="text-[9px] uppercase font-bold tracking-widest text-amber-600 dark:text-amber-400 block mb-1">IPA Challenge Game</span>
                  <p className="text-[11px] text-slate-400 max-w-sm mx-auto leading-normal">
                    Ký tự phiên âm bên dưới thuộc về từ nào trong số các lựa chọn này?
                  </p>

                  <div className="mt-3 text-lg font-mono text-indigo-600 dark:text-sky-400 font-extrabold tracking-wider p-2 bg-white dark:bg-slate-950 border border-slate-100 dark:border-slate-800/80 inline-block rounded-lg shadow-sm">
                    {currentSentence.ipa.split(' ')[Math.floor(currentSentence.ipa.split(' ').length / 2)] || currentSentence.ipa}
                  </div>
                </div>

                <div className="space-y-3 font-sans">
                  <div className="grid grid-cols-2 gap-3">
                    {ipaQuizOptions.map((option) => {
                      const isSelected = ipaSelectedAnswer === option;
                      const isCorrectAnswer = option === ipaQuizCorrect;
                      
                      let btnStyle = 'border-slate-200 hover:border-indigo-600 dark:border-slate-800 dark:hover:border-sky-500 bg-white dark:bg-slate-950';
                      if (ipaSelectedAnswer) {
                        if (isCorrectAnswer) {
                          btnStyle = 'border-emerald-500 bg-emerald-50/15 text-emerald-600';
                        } else if (isSelected) {
                          btnStyle = 'border-rose-500 bg-rose-50/15 text-rose-500';
                        } else {
                          btnStyle = 'opacity-40 border-slate-100 dark:border-slate-800 bg-transparent';
                        }
                      }

                      return (
                        <button
                          key={option}
                          type="button"
                          disabled={!!ipaSelectedAnswer}
                          onClick={() => handleCheckIpaAnswer(option)}
                          className={`p-2.5 rounded-lg border text-xs font-bold capitalize transition-all cursor-pointer ${btnStyle}`}
                        >
                          <span>{option}</span>
                        </button>
                      );
                    })}
                  </div>

                  {ipaFeedbackMessage && (
                    <div className={`p-3 rounded-lg text-xs leading-normal flex gap-2 border ${
                      ipaSelectedAnswer === ipaQuizCorrect 
                        ? 'bg-emerald-50/50 text-emerald-800 border-emerald-200 pre dark:bg-emerald-500/5 dark:text-emerald-400'
                        : 'bg-rose-50/50 text-rose-800 border-rose-200 pre dark:bg-rose-500/5 dark:text-rose-400'
                    }`}>
                      <HelpCircle className="w-4 h-4 shrink-0" />
                      <p className="font-sans font-medium text-[11px]">{ipaFeedbackMessage}</p>
                    </div>
                  )}

                  {ipaSelectedAnswer && (
                    <button
                      onClick={() => { setIpaSelectedAnswer(null); setIpaFeedbackMessage(null); setActiveSentenceIndex((prev) => (prev + 1) % lesson.sentences.length); }}
                      className="py-1.5 px-3.5 bg-indigo-600 text-white rounded-lg text-[11px] font-bold hover:bg-indigo-700 cursor-pointer"
                    >
                      Sang câu hỏi sau
                    </button>
                  )}
                </div>
              </div>
            )}

          </div>

{activeTab === 'shadowing' && (
              <div className="bg-white dark:bg-slate-900 border border-slate-100 dark:border-slate-800/80 rounded-2xl p-4 md:p-5 shadow-xs">
                <h4 className="text-[10px] font-extrabold text-slate-400 uppercase tracking-widest block mb-3">Tài liệu phụ đề bài giảng ({lesson.sentences.length} Câu)</h4>
                
                <div className="space-y-2">
                  {lesson.sentences.map((sent, index) => {
                    const isActive = activeSentenceIndex === index;
                    return (
                      <button
                        key={sent.id}
                        onClick={() => { setActiveSentenceIndex(index); }}
                        className={`w-full text-left p-3 rounded-xl border text-xs flex justify-between items-center transition-all cursor-pointer ${
                          isActive 
                            ? 'active-sentence border-l-4 border-indigo-600 bg-indigo-50/15 dark:bg-sky-500/5 font-bold text-slate-900 dark:text-white' 
                            : 'border-slate-50 dark:border-slate-800/60 hover:bg-slate-50/45 dark:hover:bg-slate-800/30 text-slate-500'
                        }`}
                      >
                        <div className="flex items-center gap-3">
                          <span className={`w-5.5 h-5.5 rounded-md font-mono font-bold text-[10px] flex items-center justify-center border ${
                            isActive 
                              ? 'border-indigo-600 bg-indigo-600 text-white dark:border-sky-500 dark:bg-sky-500 dark:text-slate-900' 
                              : 'border-slate-200 bg-slate-50 dark:border-slate-800'
                          }`}>
                            {index + 1}
                          </span>
                          <div>
                            <p className={`font-sans tracking-tight leading-normal select-none ${isActive ? 'text-slate-900 dark:text-slate-50' : 'text-slate-800 dark:text-slate-300'}`}>{sent.text}</p>
                            <p className="font-sans text-[10px] text-slate-400 select-none mt-0.5">{sent.translation}</p>
                          </div>
                        </div>

                        <div className="flex items-center gap-2 shrink-0">
                          <span className="text-[8px] font-mono font-bold text-slate-400 uppercase bg-slate-100 dark:bg-slate-800/80 px-1 rounded-sm">
                            {sent.endTime - sent.startTime}s
                          </span>
                        </div>
                      </button>
                    );
                  })}
                </div>

                {/* Pagination actions row */}
                <div className="mt-4 pt-4 border-t border-slate-50 dark:border-slate-800/80 flex justify-between items-center gap-4 text-[11px] font-sans text-slate-400">
                  <p>Mục tiêu: Đọc chính xác &gt; 80 EXP</p>
                  <button
                    onClick={() => {
                      const nextIndex = (activeSentenceIndex + 1) % lesson.sentences.length;
                      setActiveSentenceIndex(nextIndex);
                    }}
                    className="py-1.5 px-3 bg-slate-100 text-slate-700 dark:bg-slate-800 dark:text-slate-300 rounded-lg hover:bg-slate-200 transition-colors font-bold cursor-pointer shrink-0"
                  >
                    Câu Kế Tiếp ({activeSentenceIndex + 1}/{lesson.sentences.length})
                  </button>
                </div>

              </div>
            )}

        </div>

        {/* RIGHT COLUMN: EVALUATION ANALYTICS CARD & COACH PRO TIPS (4 Cols) */}
        <div className="lg:col-span-4 space-y-6">
          
          {/* Circular overall score chart HUD */}
          <div className="bg-white dark:bg-slate-900 p-5 rounded-2xl border border-slate-100 dark:border-slate-800/80 shadow-xs text-center relative overflow-hidden">
            <h3 className="text-[10px] font-extrabold text-slate-400 uppercase tracking-widest text-left mb-4">Điểm Số Đánh Giá AI</h3>

            <div className="relative w-32 h-32 mx-auto mb-4">
              <svg className="w-32 h-32 transform -rotate-90">
                {/* Background Track Circle */}
                <circle 
                  cx="64" 
                  cy="64" 
                  r="52" 
                  fill="none" 
                  stroke="var(--color-slate-100, #f1f5f9)" 
                  className="stroke-slate-100 dark:stroke-slate-800/60"
                  strokeWidth="11"
                />
                {/* Active Dash Highlight circle */}
                <circle 
                  cx="64" 
                  cy="64" 
                  r="52" 
                  fill="none" 
                  stroke={evaluation ? (evaluation.score >= 80 ? '#10b981' : evaluation.score >= 60 ? '#f59e0b' : '#ef4444') : '#e2e8f0'} 
                  strokeWidth="11" 
                  strokeDasharray="326.7" 
                  strokeDashoffset={326.7 - ((evaluation ? evaluation.score : 85) / 100) * 326.7}
                  className="score-ring"
                />
              </svg>
              <div className="absolute inset-0 flex flex-col items-center justify-center font-sans">
                <span className="text-3xl font-black text-slate-900 dark:text-slate-50 tracking-tight">
                  {evaluation ? evaluation.score : '85'}
                </span>
                <span className="text-[9px] font-bold text-slate-400 uppercase tracking-wider block mt-0.5">
                  {evaluation ? 'Current Score' : 'Target Point'}
                </span>
              </div>
            </div>

            {/* Quick summary values */}
            <div className="grid grid-cols-2 gap-2 mt-4 text-left">
              <div className="bg-slate-50 dark:bg-slate-950 p-2 rounded-xl border border-slate-100 dark:border-slate-800/60">
                <p className="text-[9px] text-slate-400 uppercase font-bold tracking-wider font-mono">Nhịp độ đọc</p>
                <p className="text-xs font-black text-slate-800 dark:text-slate-200 mt-0.5">
                  {evaluation ? (evaluation.score >= 80 ? 'Perfect / Good' : 'Moderate') : 'Chuẩn Bản Xứ'}
                </p>
              </div>
              <div className="bg-slate-50 dark:bg-slate-950 p-2 rounded-xl border border-slate-100 dark:border-slate-800/60">
                <p className="text-[9px] text-slate-400 uppercase font-bold tracking-wider font-mono">Dải tần cao độ</p>
                <p className="text-xs font-black text-slate-800 dark:text-slate-200 mt-0.5">
                  {evaluation ? 'Tự nhiên' : 'Cân Bằng'}
                </p>
              </div>
            </div>
          </div>

          {/* AI Pronunciation detailed Breakdown list */}
          <div className="bg-white dark:bg-slate-900 border border-slate-100 dark:border-slate-800/80 rounded-2xl p-4 md:p-5 shadow-xs flex flex-col min-h-[280px]">
            <h3 className="text-[10px] font-extrabold text-slate-400 uppercase tracking-widest mb-4">Chi Tiết Lỗi Phát Âm Từng Từ</h3>

            {isEvaluating ? (
              <div className="flex-1 flex flex-col justify-center items-center py-10 text-center">
                <div className="w-7 h-7 rounded-full border-2 border-indigo-600 border-t-transparent animate-spin mb-3"></div>
                <p className="text-[11px] text-slate-400 max-w-xs animate-pulse font-sans">
                  AI đang xử lý cao độ, tách âm vị học, phát hiện các quãng ngắt nhịp thoại...
                </p>
              </div>
            ) : evaluation ? (
              <div className="space-y-3.5 flex-1 overflow-y-auto max-h-[220px] pr-1.5">
                {evaluation.words.map((w, index) => {
                  let badgeBg = 'bg-emerald-500';
                  let desc = 'Phát âm tuyệt vời, chuẩn cấu trúc IPA bản ngữ.';
                  if (w.accuracyCode === 'warning') {
                    badgeBg = 'bg-amber-500';
                    desc = w.correction || 'Âm chưa được kéo dãn đúng thời lượng, nhấn giữ dài ra.';
                  } else if (w.accuracyCode === 'incorrect') {
                    badgeBg = 'bg-rose-500';
                    desc = w.correction || 'Phát âm sai hoặc bị thiếu âm cuối s/ed. Hãy luyện bật âm gió.';
                  }

                  return (
                    <div key={index} className="flex items-start gap-2.5 text-xs text-left">
                      {/* Vertical dynamic accent stripe */}
                      <span className={`w-[3px] h-9 shrink-0 rounded-full mt-0.5 ${badgeBg}`}></span>
                      <div>
                        <p className="font-bold text-slate-800 dark:text-slate-100 capitalize">{w.word}</p>
                        <p className="text-[10px] text-slate-400 font-mono mt-0.5">{desc}</p>
                      </div>
                    </div>
                  );
                })}
              </div>
            ) : (
              <div className="flex-1 flex flex-col items-center justify-center text-center py-12">
                <Sparkles className="w-7 h-7 text-slate-300 dark:text-slate-700 animate-bounce mb-3" />
                <p className="text-[10px] text-slate-400 max-w-xs font-sans leading-relaxed">
                  Chưa có dữ liệu bài nói. Ghi âm bằng nút đỏ góc trái rồi nhấp nút chấm điểm để xem phân tích.
                </p>
              </div>
            )}

            {/* AI Pro tip footer component matching Design HTML styling */}
            <div className="mt-4 p-3 bg-indigo-50/70 dark:bg-indigo-950/20 rounded-xl border border-indigo-100/40 dark:border-indigo-800/30 text-[11px] text-indigo-900 dark:text-indigo-300 leading-relaxed font-sans italic relative">
              <strong>Gemini Coach Pro Tip:</strong>
              <p className="mt-1">
                {evaluation ? evaluation.feedback : '"Luyện nói đều đặn hằng ngày giúp rèn vùng cơ môi tự nhiên. Ấn nút Phát âm tiếng bản xứ rồi nói nhại ngay sau để giữ nhịp thở chuẩn."'}
              </p>
            </div>

            {/* Submit Evaluate trigger button */}
            <div className="mt-4">
              <button
                onClick={handleEvaluatePronunciation}
                disabled={isEvaluating || isRecording || (!transcribedText && !interimTranscript)}
                className={`w-full py-2.5 rounded-xl font-bold text-xs flex items-center justify-center gap-1.5 cursor-pointer shadow-sm ${
                  (isEvaluating || isRecording || (!transcribedText && !interimTranscript))
                    ? 'bg-slate-100 text-slate-400 dark:bg-slate-800/80 dark:text-slate-600 cursor-not-allowed'
                    : 'bg-indigo-600 hover:bg-indigo-700 text-white shadow-md'
                }`}
              >
                <Sparkles className="w-3.5 h-3.5 text-amber-300 fill-amber-300" />
                <span>CHẤM CÂU THOẠI VỚI AI</span>
              </button>
            </div>

          </div>

        </div>

      </div>

    </div>
  );
}
