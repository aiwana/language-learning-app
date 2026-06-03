/**
 * @license
 * SPDX-License-Identifier: Apache-2.0
 */

export type UserLevel = 'Academic' | 'Casual' | 'Professional';
export type TargetAccent = 'UK' | 'US';
export type LearningGoal = 'fluency50' | 'comprehension70' | 'accent90';

export interface UserStats {
  streak: number;
  lastPracticed: string | null;
  totalSentences: number;
  totalTimeSeconds: number;
  exp: number;
  hearts: number;
}

export interface UserProfile {
  id: string;
  name: string;
  email: string;
  phone?: string;
  level: UserLevel;
  targetAccent: TargetAccent;
  goal: LearningGoal;
  isPremium: boolean;
  paymentMethod?: string;
}

export interface WordGrade {
  word: string;
  accuracyCode: 'correct' | 'incorrect' | 'warning';
  ipa?: string;
  correction?: string;
}

export interface EvaluationResult {
  score: number;
  accuracy: number;
  fluency: number;
  intonation: number;
  words: WordGrade[];
  feedback: string;
}

export interface Sentence {
  id: string;
  text: string;
  translation: string;
  ipa: string;
  startTime: number; // in seconds
  endTime: number; // in seconds
}

export interface Lesson {
  id: string;
  title: string;
  level: 'Cơ bản' | 'Trung cấp' | 'Nâng cao';
  topic: string;
  duration: string;
  youtubeId?: string; // Optional embedded youtube video
  videoUrl?: string; // Optional direct video link for Video Bank lessons
  sentences: Sentence[];
  isGenerated?: boolean;
}

export interface PracticeHistory {
  id: string;
  lessonId: string;
  lessonTitle: string;
  sentenceId: string;
  targetText: string;
  transcript: string;
  score: number;
  accuracy: number;
  fluency: number;
  intonation: number;
  feedback: string;
  words: WordGrade[];
  timestamp: string;
}

export interface FavoriteSentence {
  id: string;
  lessonId: string;
  lessonTitle: string;
  sentence: Sentence;
}

export interface Flashcard {
  id: string;
  word: string;
  meaning: string;
  ipa: string;
  sentenceContext: string;
  lessonTitle: string;
  score: number;
  nextReviewDate: string;
  box: number; // For Leitner System
}
