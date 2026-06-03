/**
 * @license
 * SPDX-License-Identifier: Apache-2.0
 */

import React, { useState } from 'react';
import { UserStats, UserProfile, FavoriteSentence, Flashcard } from '../types';
import { 
  Flame, Award, Heart, Star, Bookmark, RefreshCw, Calendar, Sparkles, CheckCircle, Brain, BookOpen, Volume2
} from 'lucide-react';

interface StatsDashboardProps {
  stats: UserStats;
  profile: UserProfile;
  favorites: FavoriteSentence[];
  flashcards: Flashcard[];
  onReviewFlashcard: (flashcardId: string, answer: 'remembered' | 'forgot') => void;
  onRemoveFavorite: (sentenceId: string) => void;
  onConvertExpToHeart: () => void;
}

export function StatsDashboard({
  stats,
  profile,
  favorites,
  flashcards,
  onReviewFlashcard,
  onRemoveFavorite,
  onConvertExpToHeart
}: StatsDashboardProps) {
  // Flip state for the active flashcard index
  const [activeCardIdx, setActiveCardIdx] = useState(0);
  const [isFlipped, setIsFlipped] = useState(false);

  const currentFlashcard = flashcards[activeCardIdx];

  const handleFlashcardAnswer = (answer: 'remembered' | 'forgot') => {
    if (!currentFlashcard) return;
    onReviewFlashcard(currentFlashcard.id, answer);
    setIsFlipped(false);
    
    // Jump to next card if available, or stay at 0
    if (flashcards.length > 1) {
      setTimeout(() => {
        setActiveCardIdx((prev) => (prev + 1) % flashcards.length);
      }, 300);
    }
  };

  const handlePlayTTS = (text: string) => {
    if ('speechSynthesis' in window) {
      window.speechSynthesis.cancel();
      const utterance = new SpeechSynthesisUtterance(text);
      utterance.lang = 'en-US';
      utterance.rate = 0.85;
      window.speechSynthesis.speak(utterance);
    }
  };

  return (
    <div id="stats-dashboard-container" className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8">
      
      {/* Grid: Stats Summary Numbers widgets */}
      <div className="grid grid-cols-2 lg:grid-cols-4 gap-5 mb-8 text-center font-sans">
        
        <div className="bg-white dark:bg-slate-900 border border-slate-100 dark:border-slate-800 rounded-2xl p-5 shadow-sm transition-all">
          <div className="mx-auto w-10 h-10 rounded-xl bg-orange-50 text-orange-500 dark:bg-orange-500/10 dark:text-orange-400 flex items-center justify-center mb-3">
            <Flame className="w-5 h-5 fill-orange-500" />
          </div>
          <span className="text-xs text-slate-400 block font-bold tracking-wider uppercase font-mono">Streak liên tiếp</span>
          <span className="text-2xl font-black text-slate-800 dark:text-white mt-1 block font-sans">{stats.streak} Ngày</span>
          <p className="text-[10px] text-slate-400 mt-1">Duy trì đều đặn để giữ nhiệt!</p>
        </div>

        <div className="bg-white dark:bg-slate-900 border border-slate-100 dark:border-slate-800 rounded-2xl p-5 shadow-sm transition-all">
          <div className="mx-auto w-10 h-10 rounded-xl bg-indigo-50 text-indigo-500 dark:bg-indigo-500/10 dark:text-indigo-400 flex items-center justify-center mb-3">
            <Award className="w-5 h-5" />
          </div>
          <span className="text-xs text-slate-400 block font-bold tracking-wider uppercase font-mono">Số câu hoàn thành</span>
          <span className="text-2xl font-black text-slate-800 dark:text-white mt-1 block font-sans">{stats.totalSentences} Câu</span>
          <p className="text-[10px] text-slate-400 mt-1">Nỗ lực qua từng bối cảnh</p>
        </div>

        <div className="bg-white dark:bg-slate-900 border border-slate-100 dark:border-slate-800 rounded-2xl p-5 shadow-sm transition-all">
          <div className="mx-auto w-10 h-10 rounded-xl bg-rose-50 text-rose-500 dark:bg-rose-500/10 dark:text-rose-400 flex items-center justify-center mb-3">
            <Heart className="w-5 h-5 fill-rose-500 text-rose-500" />
          </div>
          <span className="text-xs text-slate-400 block font-bold tracking-wider uppercase font-mono">Sức Khỏe (Hearts)</span>
          <span className="text-2xl font-black text-slate-800 dark:text-white mt-1 block font-sans">
            {profile.isPremium ? '∞ Tim' : `${stats.hearts} / 5 Tim`}
          </span>
          <p className="text-[10px] text-slate-400 mt-1">
            {profile.isPremium ? 'VIP không giới hạn tim khi luyện IPA.' : 'Đổi 100 EXP để nhận 1 tim tối đa 5.'}
          </p>
          {!profile.isPremium && (
            <button
              type="button"
              onClick={onConvertExpToHeart}
              className="mt-3 w-full rounded-xl bg-indigo-600 hover:bg-indigo-700 text-white text-[10px] font-bold py-2 transition-colors"
            >
              Đổi 100 EXP → 1 Tim
            </button>
          )}
        </div>

        <div className="bg-white dark:bg-slate-900 border border-slate-100 dark:border-slate-800 rounded-2xl p-5 shadow-sm transition-all">
          <div className="mx-auto w-10 h-10 rounded-xl bg-amber-50 text-amber-500 dark:bg-amber-500/10 dark:text-amber-400 flex items-center justify-center mb-3">
            <Star className="w-5 h-5 fill-amber-500 text-amber-500" />
          </div>
          <span className="text-xs text-slate-400 block font-bold tracking-wider uppercase font-mono">Kinh nghiệm EXP</span>
          <span className="text-2xl font-black text-slate-800 dark:text-white mt-1 block font-sans">{stats.exp} EXP</span>
          <p className="text-[10px] text-slate-400 mt-1">Thi đua bảng xếp hạng trường</p>
        </div>

      </div>

      <div className="grid lg:grid-cols-12 gap-8 items-start">
        
        {/* Left Side: Dynamic Anki Flashcards Spaced Repetition Box - 5 cols */}
        <div className="lg:col-span-5 bg-white dark:bg-slate-900 border border-slate-100 dark:border-slate-800 rounded-3xl p-6 shadow-sm">
          
          <div className="flex items-center gap-2 mb-4">
            <Brain className="w-5 h-5 text-indigo-500" />
            <h3 className="font-sans font-extrabold text-base tracking-tight text-slate-900 dark:text-slate-50">Sổ tay từ vựng thông minh (Anki)</h3>
          </div>
          <p className="text-slate-500 dark:text-slate-400 text-xs leading-normal">
            Hệ thống lặp lại ngắt quãng (Spaced Repetition) tự động lọc và gom nhóm các từ vựng bạn phát âm sai hoặc còn yếu trong lúc shadowing để ôn tập lại.
          </p>

          <div id="anki-canvas" className="mt-6 pt-2">
            {flashcards.length > 0 && currentFlashcard ? (
              <div className="space-y-6">
                
                {/* Physical Flip Card Layout */}
                <div 
                  onClick={() => setIsFlipped(!isFlipped)}
                  className={`w-full min-h-[180px] p-6 rounded-2xl border flex flex-col justify-between items-center text-center cursor-pointer transition-all preserve-3d duration-500 shadow-sm relative overflow-hidden ${
                    isFlipped 
                      ? 'border-indigo-500 bg-indigo-50/15 dark:bg-indigo-950/10 ring-1 ring-indigo-500' 
                      : 'border-slate-100 hover:border-slate-200 dark:border-slate-800 dark:hover:border-slate-700'
                  }`}
                >
                  <span className="text-[8px] font-mono font-bold tracking-wider text-slate-400 uppercase bg-slate-100 dark:bg-slate-800 px-1.5 py-0.5 rounded absolute top-3 right-3 select-none">
                    Nhấp để Lật Thẻ
                  </span>

                  <div></div> {/* Spacer */}

                  {!isFlipped ? (
                    // SIDE A: WORD & AUDIBLE BUTTON
                    <div className="space-y-2">
                      <h4 className="text-2xl font-black text-indigo-700 dark:text-sky-400 capitalize tracking-tight font-sans">
                        {currentFlashcard.word}
                      </h4>
                      <p className="text-[10px] text-slate-400 font-sans font-medium uppercase tracking-wider">
                        Phát âm sai trong bài học • Box {currentFlashcard.box}
                      </p>
                    </div>
                  ) : (
                    // SIDE B: PHONETIC IPA & DETAIL ASSIST
                    <div className="space-y-2.5">
                      <h4 className="text-sm font-mono font-bold text-slate-400 dark:text-slate-500">
                        {currentFlashcard.ipa}
                      </h4>
                      <div className="text-xs text-slate-700 dark:text-slate-300 leading-normal max-w-xs font-sans">
                        {currentFlashcard.meaning || 'Hãy luyện đọc mở miệng to, hạ hàm dưới, nhấn đúng trọng âm.'}
                      </div>
                      <p className="text-[10px] text-indigo-500 italic leading-snug">
                        {currentFlashcard.sentenceContext}
                      </p>
                    </div>
                  )}

                  {/* Audible TTS helper */}
                  <button
                    type="button"
                    onClick={(e) => { e.stopPropagation(); handlePlayTTS(currentFlashcard.word); }}
                    className="w-8 h-8 rounded-full bg-slate-100 hover:bg-slate-200 dark:bg-slate-800 dark:hover:bg-slate-700 text-slate-600 dark:text-slate-300 flex items-center justify-center cursor-pointer transition-colors"
                    title="Nghe phát âm chuẩn từ này"
                  >
                    <Volume2 className="w-4 h-4" />
                  </button>

                </div>

                {/* Leitner Box Actions buttons */}
                <div className="grid grid-cols-2 gap-3">
                  <button
                    type="button"
                    onClick={() => handleFlashcardAnswer('forgot')}
                    className="py-2 px-4 rounded-xl border border-rose-200 text-rose-600 hover:bg-rose-50 dark:border-rose-500/10 dark:hover:bg-rose-500/5 transition-all text-xs font-bold cursor-pointer"
                  >
                    Học Lại (Quên)
                  </button>
                  <button
                    type="button"
                    onClick={() => handleFlashcardAnswer('remembered')}
                    className="py-2 px-4 bg-emerald-600 hover:bg-emerald-700 text-white rounded-xl text-xs font-bold shadow-sm cursor-pointer transition-colors"
                  >
                    Đã Nhớ (+5 EXP)
                  </button>
                </div>

                <div className="text-center">
                  <span className="text-[10px] font-mono text-slate-400 font-semibold">
                    ĐANG XEM THẺ {activeCardIdx + 1} / {flashcards.length}
                  </span>
                </div>

              </div>
            ) : (
              <div className="text-center py-12 border border-dashed border-slate-100 dark:border-slate-800 rounded-2xl mt-5">
                <BookOpen className="w-8 h-8 text-slate-300 mx-auto" />
                <h4 className="font-bold text-xs text-slate-500 dark:text-slate-400 mt-2">Thật Tuyệt Vời!</h4>
                <p className="text-[10px] text-slate-400 mt-1 max-w-[220px] mx-auto leading-normal">
                  Bạn không có từ vựng nào bị sai cần ôn tập lúc này. Hãy tiếp tục luyện bài mới!
                </p>
              </div>
            )}
          </div>

        </div>

        {/* Right Side: Favorite Subtitle & History checklist - 7 cols */}
        <div className="lg:col-span-7 bg-white dark:bg-slate-900 border border-slate-100 dark:border-slate-800 rounded-3xl p-6 shadow-sm">
          
          <div className="flex items-center gap-2 mb-4">
            <Bookmark className="w-5 h-5 text-indigo-500" />
            <h3 className="font-sans font-extrabold text-base tracking-tight text-slate-900 dark:text-slate-50">Sổ tay câu thoại yêu thích ({favorites.length})</h3>
          </div>
          <p className="text-slate-500 dark:text-slate-400 text-xs leading-normal">
            Danh sách những câu thoại tiếng Anh ý nghĩa được bạn đánh dấu lưu trữ trong các chủ đề để phục vụ ôn luyện Shadowing nhanh bất cứ lúc nào.
          </p>

          <div id="favorites-list" className="mt-6 space-y-3">
            {favorites.length > 0 ? (
              favorites.map((fav) => (
                <div
                  key={fav.id}
                  className="p-4 rounded-xl border border-slate-100 dark:border-slate-800/80 bg-slate-50/40 dark:bg-slate-950/20 flex flex-col md:flex-row justify-between items-start md:items-center gap-4 text-xs font-sans"
                >
                  <div className="space-y-1 md:max-w-md">
                    <span className="text-[8px] bg-slate-100 dark:bg-slate-800 text-slate-400 px-1.5 py-0.5 rounded font-mono font-bold uppercase tracking-wider block w-max select-none">
                      {fav.lessonTitle}
                    </span>
                    <p className="font-bold text-slate-950 dark:text-slate-50 text-sm leading-snug">{fav.sentence.text}</p>
                    <p className="text-[10px] font-mono text-slate-400">{fav.sentence.ipa}</p>
                    <p className="text-slate-500 text-[11px] italic">"{fav.sentence.translation}"</p>
                  </div>

                  <div className="flex items-center gap-2 shrink-0">
                    <button
                      onClick={() => handlePlayTTS(fav.sentence.text)}
                      className="w-8 h-8 rounded-lg bg-indigo-50 hover:bg-indigo-100 text-indigo-600 dark:bg-indigo-500/10 dark:text-sky-400 dark:hover:bg-indigo-500/20 flex items-center justify-center transition-all cursor-pointer"
                      title="Phát âm câu thoại này"
                    >
                      <Volume2 className="w-4 h-4" />
                    </button>
                    <button
                      onClick={() => onRemoveFavorite(fav.id)}
                      className="px-2 py-1.5 rounded-lg text-slate-400 hover:text-rose-500 text-[10px] font-bold cursor-pointer"
                    >
                      Bỏ lưu
                    </button>
                  </div>
                </div>
              ))
            ) : (
              <div className="text-center py-16 border border-dashed border-slate-100 dark:border-slate-800 rounded-2xl">
                <Bookmark className="w-8 h-8 text-slate-300 mx-auto" />
                <p className="text-slate-400 text-xs mt-2 select-none">Bạn chưa lưu bất cứ câu thoại yêu thích nào.</p>
              </div>
            )}
          </div>

        </div>

      </div>

    </div>
  );
}
