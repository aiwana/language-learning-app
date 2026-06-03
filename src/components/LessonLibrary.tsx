/**
 * @license
 * SPDX-License-Identifier: Apache-2.0
 */

import React, { useState } from 'react';
import { Lesson, UserLevel, UserProfile } from '../types';
import { STATIC_LESSONS } from '../data/courses';
import { 
  BookOpen, Compass, GraduationCap, Briefcase, Sparkles, Wand2, 
  RefreshCw, Layers, ArrowRight, Play, Video, Eye, ThumbsUp, 
  HelpCircle, Check, PlayCircle, VideoOff, Info, Search 
} from 'lucide-react';

interface LessonLibraryProps {
  profile: UserProfile;
  onSelectLesson: (lesson: Lesson) => void;
}

interface VideoLessonMock {
  id: string;
  title: string;
  level: 'Cơ bản' | 'Trung cấp' | 'Nâng cao';
  topic: 'Casual' | 'Academic' | 'Professional';
  duration: string;
  speaker: string;
  views: string;
  likes: string;
  imageUrl: string;
  youtubeId: string;
  subtitles: string[];
}

// TODO: This video library is currently hardcoded mock content.
// In production, remove this static array and fetch video metadata from the backend.
// The backend should query a database of lesson/video records and return them via API.
// Example: GET /api/lesson-videos or GET /api/lessons?topic=...
const MOCK_VIDEO_LESSONS: VideoLessonMock[] = [
  // Casual (Đời sống)
  {
    id: 'vid-casual-1',
    title: 'Daily Conversational British English in a Local London Coffee Shop',
    level: 'Cơ bản',
    topic: 'Casual',
    duration: '03:15',
    speaker: 'Easy British Club',
    views: '12.4K views',
    likes: '96% helpful',
    youtubeId: 'e8Z7rXg69g0',
    imageUrl: 'https://images.unsplash.com/photo-1507133750040-4a8f57021571?q=80&w=400&auto=format&fit=crop',
    subtitles: [
      "Hello there! Welcome to The Coffee House, what can I get you?",
      "Hi! I would like to order a large oatmeal latte to go, please.",
      "Of course! Would you like any pastry or croissant with that today?",
      "No thank you, just the latte. Can I pay with my contactless card?"
    ]
  },
  {
    id: 'vid-casual-2',
    title: 'Ordering Burgers & Fast Food Meals Like a Native American Speaker',
    level: 'Trung cấp',
    topic: 'Casual',
    duration: '04:45',
    speaker: 'American Accent Lab',
    views: '8.1K views',
    likes: '92% helpful',
    youtubeId: '9Kq89k6S_gI',
    imageUrl: 'https://images.unsplash.com/photo-1550547660-d9450f859349?q=80&w=400&auto=format&fit=crop',
    subtitles: [
      "Hi there, welcome to Super Burgers! What are we having today?",
      "Can I get a double cheeseburger with extra pickles and curly fries?",
      "Sure thing, would you like to make that a combo meal with a drink?",
      "Yeah, let's do a diet soda, no ice. Also, can I get some ranch on the side?"
    ]
  },
  {
    id: 'vid-casual-3',
    title: 'Exploring Streets of New York: Real-Life Interviews & Greetings',
    level: 'Nâng cao',
    topic: 'Casual',
    duration: '05:30',
    speaker: 'NYC Street English',
    views: '15.9K views',
    likes: '98% helpful',
    youtubeId: 'O32S7M-N9j8',
    imageUrl: 'https://images.unsplash.com/photo-1513635269975-59663e0ac1ad?q=80&w=400&auto=format&fit=crop',
    subtitles: [
      "Excuse me! Do you have a minute to answer a quick question about life in NYC?",
      "Oh, hey! Sure, I'm just heading to the subway, but I've got a moment.",
      "What is the single best advice you would give to someone visiting New York for the first time?",
      "Definitely wear comfortable shoes, walk fast, and keep your head up to enjoy the skyscrapers!"
    ]
  },

  // Academic (Học thuật)
  {
    id: 'vid-acad-1',
    title: 'Renewable and Sustainable Green Energy Solutions of 2026',
    level: 'Trung cấp',
    topic: 'Academic',
    duration: '06:20',
    speaker: 'Science Today Network',
    views: '9.4K views',
    likes: '95% helpful',
    youtubeId: 'e8Z7rXg69g0',
    imageUrl: 'https://images.unsplash.com/photo-1466611653911-95081537e5b7?q=80&w=400&auto=format&fit=crop',
    subtitles: [
      "Welcome to our lecture on sustainable technological integration.",
      "We will examine solar, wind, and geothermal innovations driving carbon reduction.",
      "The primary obstacle for renewable systems remains the grid storage consistency.",
      "Transitioning successfully requires national policies aligning with local utilities."
    ]
  },
  {
    id: 'vid-acad-2',
    title: 'Vocabulary & Phrases for IELTS Academic Speaking Exam Part 3',
    level: 'Nâng cao',
    topic: 'Academic',
    duration: '08:12',
    speaker: 'Cambridge Prep Centre',
    views: '24.1K views',
    likes: '97% helpful',
    youtubeId: '9Kq89k6S_gI',
    imageUrl: 'https://images.unsplash.com/photo-1434030216411-0b793f4b4173?q=80&w=400&auto=format&fit=crop',
    subtitles: [
      "In Part 3 of the IELTS speaking exam, you must expand your answers theoretically.",
      "Use academic discourse markers such as 'consequently' or 'on the other hand'.",
      "Explain the sociological impact of remote learning on young children's behavior.",
      "Providing multifaceted perspectives highlights your advanced lexical resource scores."
    ]
  },
  {
    id: 'vid-acad-3',
    title: 'Introduction to Cognitive Neuroscience: Mind and Brain Connection',
    level: 'Nâng cao',
    topic: 'Academic',
    duration: '10:05',
    speaker: 'Open Education Foundation',
    views: '5.2K views',
    likes: '90% helpful',
    youtubeId: 'O32S7M-N9j8',
    imageUrl: 'https://images.unsplash.com/photo-1456513080510-7bf3a84b82f8?q=80&w=400&auto=format&fit=crop',
    subtitles: [
      "In this introductory session, we investigate cognitive mechanisms.",
      "How does cellular neurological firing translate directly to abstract mental imagery?",
      "Scientists map localized cortical areas corresponding to emotional regulation.",
      "Synthesizing cognitive behavioral models with FMRI imaging is highly vital."
    ]
  },

  // Professional (Công sở)
  {
    id: 'vid-prof-1',
    title: 'Mastering Silicon Valley Tech Job Interviews & Culture Fit',
    level: 'Nâng cao',
    topic: 'Professional',
    duration: '07:40',
    speaker: 'Tech Career Coach',
    views: '18.7K views',
    likes: '97% helpful',
    youtubeId: '9Kq89k6S_gI',
    imageUrl: 'https://images.unsplash.com/photo-1573496359142-b8d87734a5a2?q=80&w=400&auto=format&fit=crop',
    subtitles: [
      "Tell me about a time you handled a severe conflict inside a project deadline.",
      "Well, in my previous role, we had a major architectural disagreement before release.",
      "Interesting. How did you align the product roadmap with engineering output?",
      "I hosted a consensus-building workshop and prioritized tasks by impact scores."
    ]
  },
  {
    id: 'vid-prof-2',
    title: 'Daily Agile Standup Meetings & Scrum Practices in Global Teams',
    level: 'Trung cấp',
    topic: 'Professional',
    duration: '05:15',
    speaker: 'Agile Alliance Global',
    views: '11.8K views',
    likes: '94% helpful',
    youtubeId: 'e8Z7rXg69g0',
    imageUrl: 'https://images.unsplash.com/photo-1531538606174-0f90ff5dce83?q=80&w=400&auto=format&fit=crop',
    subtitles: [
      "Morning team! Let's get started with our quick daily sync.",
      "Yesterday, I finished the checkout page API gateway refactoring task.",
      "Today, I am going to write testing scripts for payment webhooks.",
      "Are there any major blockers or release concerns we should flag today?"
    ]
  },
  {
    id: 'vid-prof-3',
    title: 'How to Pitch Your Tech Startup & Core Business Plans to VCs',
    level: 'Nâng cao',
    topic: 'Professional',
    duration: '09:50',
    speaker: 'Forbes Business Growth',
    views: '7.6K views',
    likes: '93% helpful',
    youtubeId: 'O32S7M-N9j8',
    imageUrl: 'https://images.unsplash.com/photo-1542744173-8e18301e5788?q=80&w=400&auto=format&fit=crop',
    subtitles: [
      "We are building the first decentralized data layer for small businesses.",
      "Our initial customer feedback indicates a severe lack of simple billing APIs.",
      "What is your customer acquisition plan and estimated margin next quarter?",
      "We address this by scaling developer integrations and charging per usage tiers."
    ]
  }
];

export function LessonLibrary({ profile, onSelectLesson }: LessonLibraryProps) {
  const [selectedTopic, setSelectedTopic] = useState<UserLevel | 'All'>('All');
  const [selectedDifficulty, setSelectedDifficulty] = useState<'All' | 'Cơ bản' | 'Trung cấp' | 'Nâng cao'>('All');
  
  // AI generation states
  const [promptText, setPromptText] = useState('Đoạn hội thoại phỏng vấn tại công ty công nghệ lớn');
  const [isGenerating, setIsGenerating] = useState(false);
  const [generationError, setGenerationError] = useState<string | null>(null);
  const [generatedLessons, setGeneratedLessons] = useState<Lesson[]>([]);

  // Filter lessons
  // TODO: STATIC_LESSONS is client-side hardcoded content; it should be replaced by a backend lesson catalog API.
  // Example: fetch lessons from GET /api/lessons and remove static lesson imports entirely.
  const allAvailableLessons = [...STATIC_LESSONS, ...generatedLessons];
  const filteredLessons = allAvailableLessons.filter((lesson) => {
    const matchesTopic = selectedTopic === 'All' || lesson.topic === selectedTopic;
    const matchesDifficulty = selectedDifficulty === 'All' || lesson.level === selectedDifficulty;
    return matchesTopic && matchesDifficulty;
  });

  // Filter videos for the video bank section
  const filteredVideos = MOCK_VIDEO_LESSONS.filter((video) => {
    const matchesTopic = selectedTopic === 'All' || video.topic === selectedTopic;
    const matchesDifficulty = selectedDifficulty === 'All' || video.level === selectedDifficulty;
    return matchesTopic && matchesDifficulty;
  });

  const handleGenerateAILesson = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!promptText.trim()) return;

    setIsGenerating(true);
    setGenerationError(null);

    try {
      // TODO: This client call currently relies on the local Node/Vite server endpoint.
      // In a backend architecture, this should call a real API endpoint that either:
      //   1) fetches generated lesson content from a lesson database, or
      //   2) forwards the request to an AI generation service on the server.
      // Example: POST /api/lessons/generate { level, theme }
      const response = await fetch('/api/generate-lesson', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          level: profile.level,
          theme: promptText.trim()
        })
      });

      if (!response.ok) {
        throw new Error('Không thể khởi tạo bài giảng với Gemini AI. Vui lòng kiểm tra lại thiết lập hoặc API Key.');
      }

      const data = await response.json();
      
      const newLessonId = `gen-${Date.now()}`;
      const newLesson: Lesson = {
        id: newLessonId,
        title: data.title || `Bài Học AI: ${promptText.trim()}`,
        level: data.level || 'Trung cấp',
        topic: data.topic || profile.level,
        duration: '0:30',
        isGenerated: true,
        sentences: data.sentences.map((sent: any, index: number) => ({
          id: `${newLessonId}-s-${index}`,
          text: sent.text,
          translation: sent.translation,
          ipa: sent.ipa,
          startTime: sent.startTime ?? (index * 6),
          endTime: sent.endTime ?? ((index + 1) * 6)
        }))
      };

      setGeneratedLessons((prev) => [newLesson, ...prev]);
      setPromptText('');
      // Auto trigger simulation
      onSelectLesson(newLesson);
    } catch (err: any) {
      console.error(err);
      setGenerationError(err.message || 'Hệ thống AI bận, vui lòng thử lại sau giây lát.');
    } finally {
      setIsGenerating(false);
    }
  };

  const getTopicIcon = (topic: string) => {
    switch (topic) {
      case 'Casual':
        return <Compass className="w-4 h-4 text-orange-500" />;
      case 'Academic':
        return <GraduationCap className="w-4 h-4 text-emerald-500" />;
      case 'Professional':
        return <Briefcase className="w-4 h-4 text-indigo-500" />;
      default:
        return <BookOpen className="w-4 h-4 text-slate-500" />;
    }
  };

  const getDifficultyBadgeColor = (level: string) => {
    switch (level) {
      case 'Cơ bản':
        return 'bg-emerald-50 text-emerald-700 dark:bg-emerald-500/10 dark:text-emerald-400 border-emerald-200/50';
      case 'Trung cấp':
        return 'bg-amber-50 text-amber-700 dark:bg-amber-500/10 dark:text-amber-400 border-amber-200/50';
      case 'Nâng cao':
        return 'bg-rose-50 text-rose-700 dark:bg-rose-500/10 dark:text-rose-400 border-rose-200/50';
      default:
        return 'bg-slate-50 text-slate-700 dark:bg-slate-500/10 dark:text-slate-400 border-slate-200/50';
    }
  };

  const getTopicLabel = (topic: string) => {
    switch (topic) {
      case 'Casual': return 'Đời Sống (Casual)';
      case 'Academic': return 'Học Thuật (Academic)';
      case 'Professional': return 'Công Sở (Professional)';
      default: return topic;
    }
  };

  return (
    <div id="lesson-library-container" className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8">
      
      {/* Top Welcome Title Banner */}
      <div id="library-banner" className="bg-white dark:bg-slate-900 border border-slate-100 dark:border-slate-800 rounded-3xl p-6 md:p-8 flex flex-col md:flex-row justify-between items-start md:items-center gap-6 shadow-sm mb-10 transition-all">
        <div>
          <h1 className="text-2xl md:text-3xl font-sans font-black tracking-tight flex items-center gap-2">
            <span>Thư viện bài học Shadowing</span>
          </h1>
          <p className="text-slate-500 dark:text-slate-400 mt-1.5 text-sm max-w-xl">
            Lựa chọn chủ đề bài học có sẵn do chuyên gia cấu trúc, hoặc phát kiến tức thì một kịch bản giao tiếp tương tác bằng Trí Tuệ Nhân Tạo đằng sau.
          </p>
        </div>

        <div className="flex flex-wrap items-center gap-3">
          <div className="text-left font-mono">
            <span className="text-[10px] text-slate-400 block uppercase font-bold tracking-wider">CẤP ĐỘ KHUYÊN CHỌN</span>
            <span className="text-sm font-bold text-indigo-600 dark:text-sky-400 flex items-center gap-1.5 mt-0.5">
              {getTopicIcon(profile.level)}
              {getTopicLabel(profile.level)}
            </span>
          </div>
        </div>
      </div>

      <div className="grid lg:grid-cols-12 gap-8 items-start">
        
        {/* Left Side: Dynamic Double Section split layout - 8 cols */}
        <div className="lg:col-span-8 space-y-12">
          
          {/* Global Filtering Tabs for BOTH Sections */}
          <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4 border-b border-indigo-100/30 dark:border-slate-800 pb-5">
            
            {/* Filter by Course Topic */}
            <div className="flex flex-wrap items-center gap-1.5 font-medium text-xs">
              <span className="text-slate-400 mr-2 font-mono uppercase font-bold tracking-wider">KHOÁ HỌC:</span>
              <button
                type="button"
                onClick={() => setSelectedTopic('All')}
                className={`px-3 py-1.5 rounded-lg transition-colors cursor-pointer ${
                  selectedTopic === 'All'
                    ? 'bg-indigo-600 text-white font-semibold'
                    : 'bg-slate-100 text-slate-600 hover:bg-slate-200 dark:bg-slate-800 dark:text-slate-300 dark:hover:bg-slate-700'
                }`}
              >
                TẤT CẢ
              </button>
              <button
                type="button"
                onClick={() => setSelectedTopic('Casual')}
                className={`px-3 py-1.5 rounded-lg transition-colors cursor-pointer flex items-center gap-1 ${
                  selectedTopic === 'Casual'
                    ? 'bg-indigo-600 text-white font-semibold'
                    : 'bg-slate-100 text-slate-600 hover:bg-slate-200 dark:bg-slate-800 dark:text-slate-300'
                }`}
              >
                <Compass className="w-3.5 h-3.5" />
                <span>ĐỜI SỐNG</span>
              </button>
              <button
                type="button"
                onClick={() => setSelectedTopic('Professional')}
                className={`px-3 py-1.5 rounded-lg transition-colors cursor-pointer flex items-center gap-1 ${
                  selectedTopic === 'Professional'
                    ? 'bg-indigo-600 text-white font-semibold'
                    : 'bg-slate-100 text-slate-600 hover:bg-slate-200 dark:bg-slate-800 dark:text-slate-300'
                }`}
              >
                <Briefcase className="w-3.5 h-3.5" />
                <span>CÔNG SỞ</span>
              </button>
              <button
                type="button"
                onClick={() => setSelectedTopic('Academic')}
                className={`px-3 py-1.5 rounded-lg transition-colors cursor-pointer flex items-center gap-1 ${
                  selectedTopic === 'Academic'
                    ? 'bg-indigo-600 text-white font-semibold'
                    : 'bg-slate-100 text-slate-600 hover:bg-slate-200 dark:bg-slate-800 dark:text-slate-300'
                }`}
              >
                <GraduationCap className="w-3.5 h-3.5" />
                <span>HỌC THUẬT</span>
              </button>
            </div>

            {/* Filter by Level difficulty */}
            <div className="flex items-center gap-1.5 font-medium text-xs">
              <span className="text-slate-400 mr-2 font-mono uppercase font-bold tracking-wider">ĐỘ KHÓ:</span>
              <select
                value={selectedDifficulty}
                onChange={(e: any) => setSelectedDifficulty(e.target.value)}
                className="bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 rounded-lg py-1.5 px-3 focus:outline-none text-xs text-slate-700 dark:text-slate-300"
              >
                <option value="All">Tất Cả</option>
                <option value="Cơ bản">Cơ Bản</option>
                <option value="Trung cấp">Trung Cấp</option>
                <option value="Nâng cao">Nâng Cao</option>
              </select>
            </div>

          </div>

          {/* ==================== UPPER SECTION: AI LESSONS ==================== */}
          <div id="section-ai-lectures" className="space-y-4">
            <div className="flex items-center justify-between">
              <div className="space-y-0.5">
                <span className="text-[10px] font-bold text-indigo-600 dark:text-sky-400 font-mono tracking-widest block uppercase">PHẦN 1 — CORE LESSONS</span>
                <h2 className="text-lg font-black tracking-tight text-slate-800 dark:text-slate-150 flex items-center gap-2">
                  <Sparkles className="w-5 h-5 text-indigo-500 fill-indigo-100 dark:fill-none animate-pulse" />
                  <span>Bài Giảng Độc Thoại & Trí Tuệ Nhân Tạo (AI)</span>
                </h2>
                <p className="text-slate-455 text-[11px] text-slate-400">
                  Phù hợp để rèn luyện ghi nhớ, luyện phát âm chuẩn xác IPA và kịch bản tương tác đa cấu trúc.
                </p>
              </div>
            </div>

            {/* AI Lessons Grid */}
            <div className="grid sm:grid-cols-2 gap-5">
              {filteredLessons.length > 0 ? (
                filteredLessons.map((lesson) => (
                  <div
                    key={lesson.id}
                    className="bg-white dark:bg-slate-900 border border-slate-100 dark:border-slate-800 rounded-2xl p-5 hover:border-indigo-600 dark:hover:border-indigo-500 transition-all hover:shadow-lg hover:shadow-indigo-600/5 group flex flex-col justify-between"
                  >
                    <div>
                      <div className="flex items-center justify-between gap-3 mb-3.5">
                        <span className={`text-[10px] uppercase tracking-widest font-bold px-2 py-0.5 rounded-md border ${getDifficultyBadgeColor(lesson.level)}`}>
                          {lesson.level}
                        </span>
                        {lesson.isGenerated ? (
                          <span className="bg-pink-50 text-pink-600 dark:bg-pink-500/10 dark:text-pink-400 text-[9px] uppercase tracking-wider font-extrabold px-2 py-0.5 rounded-full flex items-center gap-1">
                            <Sparkles className="w-2.5 h-2.5 fill-pink-500 text-pink-500" />
                            AI Created
                          </span>
                        ) : (
                          <span className="text-[10px] text-slate-400 font-mono">
                            {lesson.duration}
                          </span>
                        )}
                      </div>

                      <h3 className="font-sans font-bold text-base text-slate-800 dark:text-slate-100 group-hover:text-indigo-600 dark:group-hover:text-indigo-400 transition-colors leading-snug">
                        {lesson.title}
                      </h3>
                    </div>

                    <div className="mt-5 pt-4 border-t border-slate-50 dark:border-slate-800 flex justify-between items-center text-xs">
                      <span className="text-slate-400 font-semibold flex items-center gap-1.5">
                        {getTopicIcon(lesson.topic)}
                        {getTopicLabel(lesson.topic)}
                      </span>

                      <button
                        type="button"
                        onClick={() => onSelectLesson(lesson)}
                        className="text-indigo-600 dark:text-sky-400 font-bold flex items-center gap-1 hover:underline cursor-pointer focus:outline-none"
                      >
                        <span>Luyện tập</span>
                        <Play className="w-3.5 h-3.5 fill-current" />
                      </button>
                    </div>
                  </div>
                ))
              ) : (
                <div className="col-span-2 text-center py-12 bg-white dark:bg-slate-900 border border-dashed border-slate-200 dark:border-slate-800 rounded-2xl">
                  <p className="text-slate-400 text-xs">Không tìm thấy bài học phù hợp với bộ lọc hiện tại.</p>
                  <button
                    type="button"
                    onClick={() => { setSelectedTopic('All'); setSelectedDifficulty('All'); }}
                    className="mt-3 text-xs text-indigo-600 hover:underline font-bold"
                  >
                    Xóa bộ lọc tìm kiếm
                  </button>
                </div>
              )}
            </div>
          </div>

          <hr className="border-indigo-100/20 dark:border-slate-850 my-2" />

          {/* ==================== LOWER SECTION: VIDEO CHANNEL BANK ==================== */}
          <div id="section-video-lectures" className="space-y-4">
            <div className="flex items-center justify-between">
              <div className="space-y-0.5">
                <span className="text-[10px] font-bold text-rose-500 dark:text-rose-400 font-mono tracking-widest block uppercase">PHẦN 2 — VIDEO BANK</span>
                <h2 className="text-lg font-black tracking-tight text-slate-800 dark:text-slate-150 flex items-center gap-2">
                  <Video className="w-5 h-5 text-rose-500 fill-rose-100 dark:fill-none" />
                  <span>Học Qua Ngân Hàng Video Shadows Thực Tế</span>
                </h2>
                <p className="text-slate-455 text-[11px] text-slate-400 leading-normal">
                  Chế độ shadowing theo nhân vật trong phim kịch, sự kiện nổi bật và tản mạn đường phố thực tế của người bản ngữ.
                </p>
              </div>
            </div>

            {/* Video List Grid */}
            <div className="grid sm:grid-cols-2 gap-5">
              {filteredVideos.length > 0 ? (
                filteredVideos.map((video) => (
                  <div
                    key={video.id}
                    className="bg-white dark:bg-slate-900 border border-slate-100 dark:border-slate-800 rounded-2xl overflow-hidden hover:border-rose-500 transition-all hover:shadow-xl hover:shadow-rose-600/5 group flex flex-col justify-between"
                  >
                    {/* Image Thumbnail Aspect */}
                    <div 
                      onClick={() => {
                        const mappedLesson: Lesson = {
                          id: video.id,
                          title: video.title,
                          level: video.level,
                          topic: video.topic,
                          duration: video.duration,
                          youtubeId: video.youtubeId,
                          videoUrl: '',
                          isGenerated: false,
                          sentences: video.subtitles.map((sub, i) => ({
                            id: `${video.id}-s-${i}`,
                            text: sub,
                            translation: `Phụ đề cuộc trò chuyện #${i + 1}`,
                            ipa: `[${sub.toLowerCase().replace(/[.,\/#!$%\^&\*;:{}=\-_`~()?]/g, '')}]`,
                            startTime: i * 5,
                            endTime: (i + 1) * 5
                          }))
                        };
                        onSelectLesson(mappedLesson);
                      }}
                      className="relative aspect-video w-full overflow-hidden bg-slate-100 dark:bg-slate-950 cursor-pointer"
                    >
                      <img 
                        src={video.imageUrl} 
                        alt={video.title} 
                        className="object-cover w-full h-full group-hover:scale-105 transition-transform duration-300"
                        referrerPolicy="no-referrer"
                      />
                      <div className="absolute inset-0 bg-gradient-to-t from-slate-900/60 to-transparent flex items-end p-3">
                        <span className="text-[10px] font-mono text-white bg-slate-950/80 px-2 py-0.5 rounded-md font-bold">
                          {video.duration}
                        </span>
                      </div>
                      <div className="absolute top-3 right-3">
                        <span className="text-[9px] uppercase tracking-wider font-extrabold px-2 py-0.5 rounded-full bg-rose-500 text-white shadow">
                          Video Shadow
                        </span>
                      </div>
                      
                      {/* Play Hover Overlay Banner */}
                      <div className="absolute inset-0 bg-slate-950/20 opacity-0 group-hover:opacity-100 transition-opacity flex items-center justify-center">
                        <span className="w-11 h-11 rounded-full bg-rose-500 text-white flex items-center justify-center shadow-lg transform scale-90 group-hover:scale-100 transition-all duration-300">
                          <Play className="w-5 h-5 fill-current ml-0.5" />
                        </span>
                      </div>
                    </div>

                    <div className="p-5 flex-1 flex flex-col justify-between">
                      <div>
                        <div className="flex items-center justify-between text-[11px] text-slate-400 mb-2">
                          <span className="font-semibold text-slate-600 dark:text-slate-350">{video.speaker}</span>
                          <span className="font-mono text-slate-400">{video.views}</span>
                        </div>
                        <h3 className="font-sans font-bold text-sm text-slate-800 dark:text-slate-100 group-hover:text-rose-500 dark:group-hover:text-rose-455 transition-colors leading-snug">
                          {video.title}
                        </h3>
                      </div>

                      <div className="mt-5 pt-3 border-t border-slate-50 dark:border-slate-800/60 flex items-center justify-between text-[11px]">
                        <span className={`text-[9px] uppercase tracking-wider font-bold px-1.5 py-0.5 rounded ${getDifficultyBadgeColor(video.level)}`}>
                          {video.level}
                        </span>

                        <button
                          type="button"
                          onClick={() => {
                            const mappedLesson: Lesson = {
                              id: video.id,
                              title: video.title,
                              level: video.level,
                              topic: video.topic,
                              duration: video.duration,
                              youtubeId: video.youtubeId,
                              videoUrl: '',
                             
                              isGenerated: false,
                              sentences: video.subtitles.map((sub, i) => ({
                                id: `${video.id}-s-${i}`,
                                text: sub,
                                translation: `Phụ đề cuộc trò chuyện #${i + 1}`,
                                ipa: `[${sub.toLowerCase().replace(/[.,\/#!$%\^&\*;:{}=\-_`~()?]/g, '')}]`,
                                startTime: i * 5,
                                endTime: (i + 1) * 5
                              }))
                            };
                            onSelectLesson(mappedLesson);
                          }}
                          className="bg-rose-50 hover:bg-rose-100 dark:bg-rose-500/10 dark:hover:bg-rose-500/25 text-rose-600 dark:text-rose-455 font-bold px-3.5 py-1.5 rounded-lg flex items-center gap-1 cursor-pointer transition-colors"
                        >
                          <span>Xem Video</span>
                          <ArrowRight className="w-3.5 h-3.5" />
                        </button>
                      </div>
                    </div>
                  </div>
                ))
              ) : (
                <div className="col-span-2 text-center py-12 bg-white dark:bg-slate-900 border border-dashed border-slate-200 dark:border-slate-800 rounded-2xl">
                  <p className="text-slate-400 text-xs text-center">Không tìm thấy video phù hợp trong ngân hàng.</p>
                </div>
              )}
            </div>

            {/* Note banner on database integration */}
            <div className="p-4 bg-slate-50 dark:bg-slate-900/60 border border-slate-100 dark:border-slate-800 rounded-2xl flex items-start gap-3">
              <Info className="w-4 h-4 text-emerald-500 shrink-0 mt-0.5" />
              <div className="text-[11px] leading-relaxed text-slate-400">
                <strong>Kết nối ngân hàng Video:</strong> Đây là phân khu layout Ngân hàng Video theo cấu hình yêu cầu. Trình kết nối mạng xã hội, bóc tách dòng sub thoại tự động và bộ điều phối tốc độ video (0.5x, 0.75x, 1x) sẽ nhanh chóng được đưa vào đấu nối khi cơ sở dữ liệu video khả dụng nâng cấp.
              </div>
            </div>
          </div>

        </div>

        {/* Right Side: AI Generative Lesson Terminal - 4 cols */}
        <div className="lg:col-span-4 bg-white dark:bg-slate-900 border border-slate-100 dark:border-slate-800 rounded-3xl p-6 shadow-sm transition-all sticky top-20">
          <div className="flex items-center gap-2 text-pink-600 dark:text-pink-400 mb-2">
            <Sparkles className="w-5 h-5 fill-pink-500" />
            <h3 className="font-sans font-extrabold text-base tracking-tight text-slate-900 dark:text-slate-50">Tạo đề tài AI Generator</h3>
          </div>
          <p className="text-slate-500 dark:text-slate-400 text-xs mt-1 leading-normal">
            Bảo đạt kỹ năng hội thoại nhanh hơn mọi khi. Nhập bất kỳ chủ đề, cấp độ, hoặc hoàn cảnh công sở mong muốn để sinh đoạn thoại nhại học tức thì with Gemini.
          </p>

          <form onSubmit={handleGenerateAILesson} className="mt-6 space-y-4 font-sans">
            <div>
              <label className="text-[10px] font-bold text-slate-400 block uppercase tracking-wider mb-1.5">Prompt nội dung / Cấp độ học</label>
              <textarea
                value={promptText}
                onChange={(e) => setPromptText(e.target.value)}
                disabled={isGenerating}
                rows={3}
                className="w-full bg-slate-50 dark:bg-slate-950 p-3.5 rounded-xl border border-slate-200 dark:border-slate-800 text-xs focus:outline-none focus:border-indigo-500 focus:ring-1 focus:ring-indigo-500 focus:bg-transparent transition-all leading-relaxed"
                placeholder="Ví dụ: Hội thoại đi gọi trà sữa trân châu, viết đoạn hội thoại cấp độ B2 chủ đề bảo vệ đa dạng sinh học..."
              />
            </div>

            <div className="p-3 bg-slate-50 dark:bg-slate-950/40 rounded-xl space-y-2.5 text-[11px]">
              <div className="flex justify-between items-center text-slate-500">
                <span>Cấu hình độ tuổi/khóa:</span>
                <span className="font-semibold text-slate-800 dark:text-slate-300">{getTopicLabel(profile.level)}</span>
              </div>
              <div className="flex justify-between items-center text-slate-500">
                <span>Giọng bản xứ ưu tiên:</span>
                <span className="font-semibold text-slate-800 dark:text-slate-300">{profile.targetAccent === 'US' ? 'Anh - Mỹ (US)' : 'Anh - Anh (UK)'}</span>
              </div>
            </div>

            {generationError && (
              <div className="p-3 bg-rose-50 text-rose-600 border border-rose-200/50 rounded-xl text-xs leading-normal">
                {generationError}
              </div>
            )}

            <button
              id="ai-generate-btn"
              type="submit"
              disabled={isGenerating}
              className={`w-full py-3.5 rounded-xl text-white font-bold text-xs flex items-center justify-center gap-2 cursor-pointer shadow-md ${
                isGenerating 
                  ? 'bg-slate-400 cursor-not-allowed animate-pulse' 
                  : 'bg-gradient-to-r from-pink-500 via-purple-600 to-indigo-600 hover:scale-[1.02] active:scale-[0.98] transition-all'
              }`}
            >
              {isGenerating ? (
                <>
                  <RefreshCw className="w-4 h-4 animate-spin shrink-0" />
                  <span className="font-mono">AI đang biên soạn hội thoại...</span>
                </>
              ) : (
                <>
                  <Wand2 className="w-4 h-4 shrink-0 text-amber-200" />
                  <span>XUẤT BẢN BÀI GIẢNG AI</span>
                </>
              )}
            </button>
          </form>

          {isGenerating && (
            <div className="mt-4 p-3 bg-sky-50 dark:bg-sky-500/5 rounded-xl text-[10px] text-sky-800 dark:text-sky-300 leading-normal border border-sky-100 dark:border-sky-500/10 animate-pulse">
              <strong>Mẹo học tập:</strong> Gemini đang phân tích cú pháp và dịch kịch bản, thêm phần phiên âm IPA cho bài giảng riêng cho bạn. Tiến trình này thường tốn từ 3-5 giây!
            </div>
          )}

        </div>

      </div>

    </div>
  );
}
