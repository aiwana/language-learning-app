/**
 * @license
 * SPDX-License-Identifier: Apache-2.0
 */

import React from 'react';
import { UserProfile, UserStats } from '../types';
import { Sparkles, Star, Flame, Heart, Sun, Moon, LogOut, Code, User } from 'lucide-react';

interface HeaderProps {
  profile: UserProfile;
  stats: UserStats;
  isDark: boolean;
  onToggleTheme: () => void;
  onLogout: () => void;
  onNavigate: (tab: 'home' | 'stats' | 'settings') => void;
  activeTab: 'home' | 'stats' | 'settings';
}

export function Header({
  profile,
  stats,
  isDark,
  onToggleTheme,
  onLogout,
  onNavigate,
  activeTab
}: HeaderProps) {
  return (
    <header id="app-top-header" className="sticky top-0 z-50 bg-white/85 dark:bg-slate-900/85 backdrop-blur-md border-b border-slate-100 dark:border-slate-800 transition-colors duration-300">
      <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
        <div className="flex justify-between items-center h-16">
          
          {/* Leftside Branding logo */}
          <div className="flex items-center gap-7">
            <button 
              onClick={() => onNavigate('home')} 
              className="flex items-center gap-2 px-1 focus:outline-none cursor-pointer"
            >
              <div className="w-8 h-8 rounded-lg bg-indigo-600 flex items-center justify-center text-white shadow-sm shadow-indigo-600/30">
                <Code className="w-4 h-4" />
              </div>
              <span className="font-sans font-extrabold text-base tracking-tight text-indigo-700 dark:text-sky-400">
                ShadowSpeak <span className="text-slate-500 font-normal">AI</span>
              </span>
            </button>

            {/* Main Tabs Navigation */}
            <nav className="hidden md:flex space-x-1 font-sans font-medium text-sm">
              <button
                onClick={() => onNavigate('home')}
                className={`px-4.5 py-1.5 rounded-lg transition-all cursor-pointer ${
                  activeTab === 'home'
                    ? 'bg-slate-100 text-indigo-600 dark:bg-slate-800 dark:text-sky-400 font-semibold'
                    : 'text-slate-600 hover:text-slate-950 dark:text-slate-300 dark:hover:text-slate-100'
                }`}
              >
                Khóa Học
              </button>
              <button
                onClick={() => onNavigate('stats')}
                className={`px-4.5 py-1.5 rounded-lg transition-all cursor-pointer ${
                  activeTab === 'stats'
                    ? 'bg-slate-100 text-indigo-600 dark:bg-slate-800 dark:text-sky-400 font-semibold'
                    : 'text-slate-600 hover:text-slate-950 dark:text-slate-300 dark:hover:text-slate-100'
                }`}
              >
                Tiến Trình & Thẻ Nhớ
              </button>
              <button
                onClick={() => onNavigate('settings')}
                className={`px-4.5 py-1.5 rounded-lg transition-all cursor-pointer ${
                  activeTab === 'settings'
                    ? 'bg-slate-100 text-indigo-600 dark:bg-slate-800 dark:text-sky-400 font-semibold'
                    : 'text-slate-600 hover:text-slate-950 dark:text-slate-300 dark:hover:text-slate-100'
                }`}
              >
                Tài Khoản
              </button>
            </nav>
          </div>

          {/* Rightside Statuses and Settings */}
          <div className="flex items-center gap-2 sm:gap-4 font-mono text-xs">
            
            {/* Streak Counter */}
            <div className="flex items-center gap-1.5 px-2.5 py-1.5 bg-orange-50 text-orange-600 dark:bg-orange-500/10 dark:text-orange-400 rounded-lg" title="Chuỗi ngày học liên tiếp">
              <Flame className="w-4 h-4 fill-orange-500 animate-pulse" />
              <span className="font-bold">{stats.streak} Ngày</span>
            </div>

            {/* Heart point system */}
            <div className="flex items-center gap-1.5 px-2.5 py-1.5 bg-rose-50 text-rose-600 dark:bg-rose-500/10 dark:text-rose-400 rounded-lg" title="Số tim còn lại trong ngày">
              <Heart className="w-4 h-4 fill-rose-500" />
              <span className="font-bold">{profile.isPremium ? '∞ Tim' : `${stats.hearts} Tim`}</span>
            </div>

            {/* EXP Point Counter */}
            <div className="flex items-center gap-1.5 px-2.5 py-1.5 bg-amber-50 text-amber-600 dark:bg-amber-500/10 dark:text-amber-400 rounded-lg" title="Điểm EXP tích lũy">
              <Star className="w-4 h-4 fill-amber-500" />
              <span className="font-bold">{stats.exp} EXP</span>
            </div>

            {/* Subscription status */}
            {profile.isPremium && (
              <div className="hidden lg:flex items-center gap-1.5 px-2.5 py-1.5 bg-emerald-50 text-emerald-600 dark:bg-emerald-500/10 dark:text-emerald-400 rounded-lg">
                <Sparkles className="w-3.5 h-3.5 fill-emerald-500 text-emerald-500" />
                <span className="font-bold uppercase tracking-wider text-[10px]">Pro VIP</span>
              </div>
            )}

            {/* Dark & Light Theme Toggling */}
            <div className="h-5 w-[1px] bg-slate-100 dark:bg-slate-800 mx-1"></div>

            <button
              onClick={onToggleTheme}
              className="p-2 rounded-lg text-slate-500 hover:text-slate-800 hover:bg-slate-100 dark:text-slate-400 dark:hover:text-slate-100 dark:hover:bg-slate-800 transition-colors cursor-pointer"
              title="Đổi giao diện Sáng / Tối"
            >
              {isDark ? <Sun className="w-4 h-4" /> : <Moon className="w-4 h-4" />}
            </button>

            {/* User Info & Out */}
            <button
              onClick={onLogout}
              className="p-2 rounded-lg text-rose-500 hover:bg-rose-50 dark:hover:bg-rose-500/10 transition-colors cursor-pointer"
              title="Đăng xuất khỏi hệ thống"
            >
              <LogOut className="w-4 h-4" />
            </button>

          </div>

        </div>
      </div>
    </header>
  );
}
