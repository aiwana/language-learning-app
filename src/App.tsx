/**
 * @license
 * SPDX-License-Identifier: Apache-2.0
 */

import React, { useState, useEffect } from 'react';
import { UserProfile, UserStats, Lesson, Sentence, FavoriteSentence, Flashcard } from './types';
import { Header } from './components/Header';
import { AuthFlow } from './components/AuthFlow';
import { LessonLibrary } from './components/LessonLibrary';
import { LessonView } from './components/LessonView';
import { StatsDashboard } from './components/StatsDashboard';
import { SettingsView } from './components/SettingsView';

export default function App() {
  // Global States
  const [profile, setProfile] = useState<UserProfile | null>(null);
  const [stats, setStats] = useState<UserStats>({
    streak: 3, // Start with a realistic mock streak to feel live immediately
    lastPracticed: null,
    totalSentences: 0,
    totalTimeSeconds: 0,
    exp: 420,  // Start with a small realistic exp base
    hearts: 5
  });
  // TODO: These initial stats are mock defaults for the local demo.
  // In a real application, these values should be loaded from the backend/database
  // once the user logs in. The client should not invent or hardcode progress values.
  // Example backend flow: GET /api/user/profile + /api/user/stats
  const [favorites, setFavorites] = useState<FavoriteSentence[]>([]);
  const [flashcards, setFlashcards] = useState<Flashcard[]>([]);
  const [isDark, setIsDark] = useState(false);
  
  // Navigation Routing States
  const [activeTab, setActiveTab] = useState<'home' | 'stats' | 'settings'>('home');
  const [selectedLesson, setSelectedLesson] = useState<Lesson | null>(null);

  // Load configuration and data from localStorage on Mount
  useEffect(() => {
    try {
      // TODO: localStorage is used for client-side persistence only.
      // In a backend architecture, use API calls instead of localStorage here.
      // Example: fetch the logged-in user's profile and learning progress from the database.
      //   GET /api/user/profile
      //   GET /api/user/stats
      //   GET /api/user/favorites
      //   GET /api/user/flashcards
      const storedProfile = localStorage.getItem('shadow_speak_profile');
      if (storedProfile) {
        setProfile(JSON.parse(storedProfile));
      }

      const storedStats = localStorage.getItem('shadow_speak_stats');
      if (storedStats) {
        setStats(JSON.parse(storedStats));
      }

      const storedFavorites = localStorage.getItem('shadow_speak_favorites');
      if (storedFavorites) {
        setFavorites(JSON.parse(storedFavorites));
      }

      const storedFlashcards = localStorage.getItem('shadow_speak_flashcards');
      if (storedFlashcards) {
        setFlashcards(JSON.parse(storedFlashcards));
      }

      const storedTheme = localStorage.getItem('shadow_speak_dark_mode');
      if (storedTheme === 'true') {
        setIsDark(true);
        document.documentElement.classList.add('dark');
      }
    } catch (e) {
      console.error('Error loading data from localState:', e);
    }
  }, []);

  // Update light/dark mode css classes
  const handleToggleTheme = () => {
    const nextTheme = !isDark;
    setIsDark(nextTheme);
    localStorage.setItem('shadow_speak_dark_mode', String(nextTheme));
    if (nextTheme) {
      document.documentElement.classList.add('dark');
    } else {
      document.documentElement.classList.remove('dark');
    }
  };

  // Complete Onboarding Flow
  const handleCompleteOnboarding = (newProfile: UserProfile) => {
    setProfile(newProfile);
    localStorage.setItem('shadow_speak_profile', JSON.stringify(newProfile));
    
    // Create baseline stats
    const baseStats: UserStats = {
      streak: 4,
      lastPracticed: new Date().toISOString(),
      totalSentences: 0,
      totalTimeSeconds: 0,
      exp: 150,
      hearts: 5
    };
    setStats(baseStats);
    localStorage.setItem('shadow_speak_stats', JSON.stringify(baseStats));

    // Save dummy initial flashcards and favorites for visual fullness
    // TODO: These sample items are hardcoded onboarding examples for the demo.
    // In production, the backend should create or return real favorites and flashcards
    // for the authenticated user from the database.
    // Example: POST /api/user/onboard or GET /api/user/favorites and /api/user/flashcards.
    const sampleFavorite: FavoriteSentence = {
      id: 'fav-sample',
      lessonTitle: 'Giao tiếp hằng ngày: Chuyện buổi sáng',
      lessonId: 'lesson-1',
      sentence: {
        id: 's1-2',
        text: 'Today, I made myself a perfect cup of hot coffee.',
        translation: 'Hôm nay, tôi tự pha cho mình một tách cà phê nóng thật tuyệt vời.',
        ipa: '[təˈdeɪ, aɪ meɪd maɪˈsɛlf ə ˈpɜːrfɪkt kʌp ʌv hɑːt ˈkɔːfi]',
        startTime: 5,
        endTime: 9
      }
    };
    setFavorites([sampleFavorite]);
    localStorage.setItem('shadow_speak_favorites', JSON.stringify([sampleFavorite]));

    const sampleFlashcard: Flashcard = {
      id: 'fc-sample',
      word: 'environment',
      meaning: 'Danh từ: Môi trường tự nhiên xung quanh. Chú ý đọc nhấn ba: en-VI-ron-ment, âm N câm giữa từ.',
      ipa: '[ɪnˈvaɪrənmənt]',
      sentenceContext: 'Bối cảnh câu thoại: "Transitioning to clean renewable energy is our ultimate solution."',
      lessonTitle: 'Ô nhiễm môi trường & Hành động',
      score: 42,
      nextReviewDate: new Date().toISOString(),
      box: 1
    };
    setFlashcards([sampleFlashcard]);
    localStorage.setItem('shadow_speak_flashcards', JSON.stringify([sampleFlashcard]));
  };

  const handleUpdateProfile = (updatedProfile: UserProfile) => {
    setProfile(updatedProfile);
    localStorage.setItem('shadow_speak_profile', JSON.stringify(updatedProfile));
  };

  // Stats incremental save
  const handleUpdateStats = (newStats: UserStats) => {
    setStats(newStats);
    localStorage.setItem('shadow_speak_stats', JSON.stringify(newStats));
  };

  // Log Out / Clear User Space
  const handleLogout = () => {
    if (confirm('Bạn có chắc chắn muốn đăng xuất khỏi ứng dụng không?')) {
      setProfile(null);
      setSelectedLesson(null);
      setActiveTab('home');
      localStorage.removeItem('shadow_speak_profile');
      localStorage.removeItem('shadow_speak_stats');
      localStorage.removeItem('shadow_speak_favorites');
      localStorage.removeItem('shadow_speak_flashcards');
    }
  };

  // Bookmark active sentence to favorites list
  // TODO: This currently writes favorites only to localStorage.
  // Replace with a backend API call so favorites persist across devices and sessions.
  // Example: POST /api/user/favorites { sentenceId, lessonId, lessonTitle }
  const handleSaveFavorite = (sentence: Sentence, lessonTitle: string) => {
    const exits = favorites.find((f) => f.sentence.id === sentence.id);
    if (exits) {
      alert('Câu thoại này đã có mặt trong danh sách yêu thích của bạn.');
      return;
    }

    const newFavorite: FavoriteSentence = {
      id: `fav-${Date.now()}`,
      lessonId: selectedLesson?.id || 'lesson-custom',
      lessonTitle,
      sentence
    };

    const nextFavorites = [newFavorite, ...favorites];
    setFavorites(nextFavorites);
    localStorage.setItem('shadow_speak_favorites', JSON.stringify(nextFavorites));
    alert('Đã bookmark thành công vào Sổ tay câu thoại yêu thích!');
  };

  // Remove sentence from favorites list
  const handleRemoveFavorite = (favoriteId: string) => {
    const nextFavorites = favorites.filter((f) => f.id !== favoriteId);
    setFavorites(nextFavorites);
    localStorage.setItem('shadow_speak_favorites', JSON.stringify(nextFavorites));
  };

  // Save mispronounced words to spaced-repetition dictionary
  // TODO: This flashcard storage is local only. In a real system, persist these flashcards
  // with the backend in a database so the user's memory bank is available on every device.
  // Example: POST /api/user/flashcards { word, context, ipa, accuracyCode }
  const handleSaveFlashcard = (word: string, context: string, ipa: string, accuracyCode: string) => {
    const cleanWord = word.trim().toLowerCase().replace(/[.,\/#!$%\^&\*;:{}=\-_`~()?]/g, "");
    
    // Check if word already registered inside learning box
    const exists = flashcards.find((fc) => fc.word.toLowerCase() === cleanWord);
    if (exists) return; // Ignore duplication

    const newFlashcard: Flashcard = {
      id: `fc-${Date.now()}-${Math.floor(Math.random() * 1000)}`,
      word: cleanWord,
      meaning: accuracyCode === 'incorrect' 
        ? 'Bạn phát âm sai hoàn toàn hoặc bị thiếu âm gió cuối từ này. Hãy nhấp Mic nghe âm mẫu để kéo dài khẩu hình.'
        : 'Bạn đọc từ này gần đúng nhưng chưa nhấn đúng âm sắc và âm tiết. Chú ý khẩu hình môi lưỡi.',
      ipa,
      sentenceContext: context,
      lessonTitle: selectedLesson?.title || 'Bài tập tự do',
      score: 50,
      nextReviewDate: new Date().toISOString(),
      box: 1 // Starts at leitner box 1
    };

    const nextFlashcards = [newFlashcard, ...flashcards];
    setFlashcards(nextFlashcards);
    localStorage.setItem('shadow_speak_flashcards', JSON.stringify(nextFlashcards));
  };

  // Leitner Spaced Repetition Box update logic
  const handleReviewFlashcard = (flashcardId: string, answer: 'remembered' | 'forgot') => {
    const nextFlashcards = flashcards.map((fc) => {
      if (fc.id !== flashcardId) return fc;

      let nextBox = fc.box;
      if (answer === 'remembered') {
        nextBox = Math.min(5, fc.box + 1); // Move to higher Leitner boxes
      } else {
        nextBox = 1; // Reset to box 1 if misspelled/forgotten
      }

      // Schedule review interval days: box 1 = 1 day, box 2 = 2 days, box 3 = 4 days, box 4 = 8 days, box 5 = 14 days
      const daysToAdd = nextBox === 1 ? 1 : nextBox === 2 ? 2 : nextBox === 3 ? 4 : nextBox === 4 ? 8 : 14;
      const reviewDate = new Date();
      reviewDate.setDate(reviewDate.getDate() + daysToAdd);

      return {
        ...fc,
        box: nextBox,
        nextReviewDate: reviewDate.toISOString()
      };
    });

    setFlashcards(nextFlashcards);
    localStorage.setItem('shadow_speak_flashcards', JSON.stringify(nextFlashcards));

    // Award bonus experience path
    if (answer === 'remembered') {
      const nextStats = { ...stats, exp: stats.exp + 5 };
      setStats(nextStats);
      localStorage.setItem('shadow_speak_stats', JSON.stringify(nextStats));
    }
  };

  const handleConvertExpToHeart = () => {
    if (!profile || profile.isPremium) return;
    if (stats.exp < 100) {
      alert('Bạn cần ít nhất 100 EXP để đổi lấy 1 Tim.');
      return;
    }
    if (stats.hearts >= 5) {
      alert('Bạn đã có đủ 5 Tim. Hãy dùng trước khi đổi thêm.');
      return;
    }

    const nextStats = {
      ...stats,
      exp: stats.exp - 100,
      hearts: Math.min(5, stats.hearts + 1)
    };
    setStats(nextStats);
    localStorage.setItem('shadow_speak_stats', JSON.stringify(nextStats));
    alert('Đã đổi 100 EXP thành 1 Tim thành công!');
  };

  const handleNavigate = (tab: 'home' | 'stats' | 'settings') => {
    setActiveTab(tab);
    setSelectedLesson(null); // Clear selected lesson when switching tabs
  };

  // RENDER INTERACTION CONTROLLERS
  if (!profile) {
    return <AuthFlow onComplete={handleCompleteOnboarding} />;
  }

  return (
    <div id="app-root-container" className="min-h-screen bg-slate-50 text-slate-900 transition-colors duration-300 dark:bg-slate-950 dark:text-slate-50 selection:bg-indigo-500 selection:text-white pb-14 md:pb-0">
      
      {/* Dynamic Header Navbar */}
      <Header
        profile={profile}
        stats={stats}
        isDark={isDark}
        onToggleTheme={handleToggleTheme}
        onLogout={handleLogout}
        onNavigate={handleNavigate}
        activeTab={activeTab}
      />

      {/* Primary Routing Canvas Panel */}
      <main className="transition-all duration-300">
        
        {/* TAB 1: BROWSE LESSONS & REPEAT STUDIO */}
        {activeTab === 'home' && (
          selectedLesson ? (
            <LessonView
              lesson={selectedLesson}
              stats={stats}
              isPremium={profile.isPremium}
              onUpdateStats={handleUpdateStats}
              onBack={() => setSelectedLesson(null)}
              onSaveFavorite={handleSaveFavorite}
              onSaveFlashcard={handleSaveFlashcard}
            />
          ) : (
            <LessonLibrary
              profile={profile}
              onSelectLesson={(lesson) => setSelectedLesson(lesson)}
            />
          )
        )}

        {/* TAB 2: SPEECH METRICS & REPETITION ANKI FLASHCARDS */}
        {activeTab === 'stats' && (
          <StatsDashboard
            stats={stats}
            profile={profile}
            favorites={favorites}
            flashcards={flashcards}
            onReviewFlashcard={handleReviewFlashcard}
            onRemoveFavorite={handleRemoveFavorite}
            onConvertExpToHeart={handleConvertExpToHeart}
          />
        )}

        {/* TAB 3: ACCOUNT & GOAL CONTROL PANEL */}
        {activeTab === 'settings' && (
          <SettingsView
            profile={profile}
            isDark={isDark}
            onUpdateProfile={handleUpdateProfile}
            onToggleTheme={handleToggleTheme}
            onLogout={handleLogout}
          />
        )}

      </main>

      {/* Smart Mobile Tab bar bar */}
      <div className="md:hidden fixed bottom-0 inset-x-0 bg-white/95 dark:bg-slate-950/95 backdrop-blur-md border-t border-slate-100 dark:border-slate-800 flex justify-around p-2.5 z-40 text-xxs tracking-tighter">
        <button
          onClick={() => handleNavigate('home')}
          className={`flex flex-col items-center gap-1 font-bold ${
            activeTab === 'home' ? 'text-indigo-600 dark:text-sky-400' : 'text-slate-400'
          }`}
        >
          <span className="w-1.5 h-1.5 rounded-full bg-current"></span>
          <span>Khóa Học</span>
        </button>
        <button
          onClick={() => handleNavigate('stats')}
          className={`flex flex-col items-center gap-1 font-bold ${
            activeTab === 'stats' ? 'text-indigo-600 dark:text-sky-400' : 'text-slate-400'
          }`}
        >
          <span className="w-1.5 h-1.5 rounded-full bg-current"></span>
          <span>Thẻ Nhớ</span>
        </button>
        <button
          onClick={() => handleNavigate('settings')}
          className={`flex flex-col items-center gap-1 font-bold ${
            activeTab === 'settings' ? 'text-indigo-600 dark:text-sky-400' : 'text-slate-400'
          }`}
        >
          <span className="w-1.5 h-1.5 rounded-full bg-current"></span>
          <span>Tài Khoản</span>
        </button>
      </div>

    </div>
  );
}
