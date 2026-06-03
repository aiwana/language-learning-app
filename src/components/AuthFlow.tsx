/**
 * @license
 * SPDX-License-Identifier: Apache-2.0
 */

import React, { useState } from 'react';
import { motion } from 'motion/react';
import { UserProfile, UserLevel, TargetAccent, LearningGoal } from '../types';
import { User, Mail, Shield, Sparkles, AudioLines, GraduationCap, Compass, Briefcase, ChevronRight, Check } from 'lucide-react';

interface AuthFlowProps {
  onComplete: (profile: UserProfile) => void;
}

export function AuthFlow({ onComplete }: AuthFlowProps) {
  const [step, setStep] = useState<'auth' | 'level' | 'goal' | 'premium'>('auth');
  const [isLogin, setIsLogin] = useState(true);
  
  // Registration States
  // TODO: These initial values are hardcoded UI defaults only.
  // In a backend-enabled app, user registration and login should happen through secure API endpoints.
  // Example: POST /api/auth/register or POST /api/auth/login, then load profile data from the server.
  // Passwords should never be maintained in client state long-term or stored in localStorage.
  const [name, setName] = useState('Phạm Duy Anh');
  const [email, setEmail] = useState('phamdu1356@gmail.com');
  const [phone, setPhone] = useState('0987654321');
  const [password, setPassword] = useState('password123');

  // Speaking Preferences
  const [level, setLevel] = useState<UserLevel>('Casual');
  const [targetAccent, setTargetAccent] = useState<TargetAccent>('US');
  const [goal, setGoal] = useState<LearningGoal>('comprehension70');
  const [isPremium, setIsPremium] = useState(false);

  const handleAuthSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    setStep('level');
  };

  const handleLevelNext = () => {
    setStep('goal');
  };

  const handleGoalNext = () => {
    setStep('premium');
  };

  const handleCompleteSetup = (premiumStatus: boolean) => {
    // TODO: In a real implementation, this should not create the profile client-side.
    // Instead, submit the registration data to backend auth and receive the saved profile.
    // The server should generate the user ID, persist it in the database, and return a secure session.
    setIsPremium(premiumStatus);
    const profile: UserProfile = {
      id: `usr-${Date.now()}`,
      name,
      email,
      phone,
      level,
      targetAccent,
      goal,
      isPremium: premiumStatus,
      paymentMethod: premiumStatus ? 'Google Pay (Simulated)' : undefined
    };
    onComplete(profile);
  };

  return (
    <div id="auth-flow-container" className="min-h-screen grid lg:grid-cols-12 bg-slate-50 text-slate-900 transition-colors duration-300 dark:bg-slate-950 dark:text-slate-50">
      
      {/* Editorial Branding Section - left side */}
      <div id="auth-branding-panel" className="lg:col-span-5 bg-gradient-to-tr from-sky-600 via-indigo-700 to-indigo-900 text-white p-8 md:p-12 flex flex-col justify-between relative overflow-hidden">
        <div className="absolute inset-0 opacity-10 bg-[radial-gradient(#fff_1px,transparent_1px)] [background-size:16px_16px]"></div>
        
        {/* Top Header Logo */}
        <div className="flex items-center gap-3 relative z-10">
          <div className="w-10 h-10 rounded-xl bg-white/20 backdrop-blur-md flex items-center justify-center border border-white/30">
            <AudioLines className="w-6 h-6 text-white" />
          </div>
          <span className="font-sans font-bold text-xl tracking-tight">ShadowSpeak AI</span>
        </div>

        {/* Branding Slogan Middle */}
        <div className="my-auto py-12 relative z-10 max-w-md">
          <span className="text-sky-300 font-mono text-xs uppercase tracking-widest font-semibold px-2.5 py-1 bg-white/10 rounded-full">Method Shadowing</span>
          <h1 className="text-3xl md:text-4xl font-sans font-extrabold tracking-tight mt-6 leading-tight text-white">
            Luyện phát âm chuẩn Anh - Mỹ cùng Trí Tuệ Nhân Tạo
          </h1>
          <p className="text-slate-200 mt-4 text-sm leading-relaxed">
            Áp dụng phương pháp Shadowing (Nhại giọng) của chuyên gia kết hợp trí tuệ nhân tạo đánh giá cao độ, ngữ diệu và chỉ ra lỗi sai trên từng ký tự âm tiết.
          </p>
          
          <div className="mt-8 space-y-3.5">
            {[
              "Lắng nghe audio chuẩn phát âm bản xứ",
              "Thu âm nhại giọng phản hồi giọng nói với Web Audio",
              "Gemini AI chấm điểm, sửa lỗi phát âm và chú giải IPA",
              "Flashcard thuật toán giãn cách hỗ trợ lặp lại từ sai"
            ].map((feature, i) => (
              <div key={i} className="flex items-center gap-3 text-slate-100 text-sm">
                <div className="w-5 h-5 rounded-full bg-emerald-400/20 flex items-center justify-center text-emerald-400">
                  <Check className="w-3.5 h-3.5" />
                </div>
                <span>{feature}</span>
              </div>
            ))}
          </div>
        </div>

        {/* Footer Credit Line */}
        <div className="text-xs text-white/60 relative z-10 flex justify-between items-center">
          <span>© 2026 ShadowSpeak</span>
        </div>
      </div>

      {/* Interactive Form Flow Panel - right side */}
      <div id="auth-form-panel" className="lg:col-span-7 flex items-center justify-center p-6 md:p-12">
        <div className="w-full max-w-lg">
          
          {/* STEP 1: LOGIN & STANDARD REGISTER */}
          {step === 'auth' && (
            <motion.div
              initial={{ opacity: 0, y: 15 }}
              animate={{ opacity: 1, y: 0 }}
              className="bg-white dark:bg-slate-900 rounded-3xl p-8 shadow-xl shadow-slate-100 dark:shadow-none border border-slate-100 dark:border-slate-800"
            >
              <div className="text-center">
                <h2 className="text-2xl font-bold tracking-tight">
                  {isLogin ? "Xin chào quay trở lại!" : "Tạo tài khoản học tập"}
                </h2>
                <p className="text-slate-500 dark:text-slate-400 text-sm mt-1.5">
                  {isLogin 
                    ? "Nhập tài khoản để tiếp tục theo dõi chuỗi ngày Streak" 
                    : "Đăng ký thành viên để bắt đầu hành trình nói tiếng Anh trôi chảy"}
                </p>
              </div>

              <form onSubmit={handleAuthSubmit} className="mt-8 space-y-4">
                {!isLogin && (
                  <div>
                    <label className="block text-xs font-medium text-slate-500 dark:text-slate-400 mb-1">HỌ VÀ TEN</label>
                    <div className="relative">
                      <span className="absolute inset-y-0 left-0 pl-3.5 flex items-center text-slate-400">
                        <User className="w-4 h-4" />
                      </span>
                      <input
                        type="text"
                        required
                        value={name}
                        onChange={(e) => setName(e.target.value)}
                        className="w-full pl-10 pr-4 py-2.5 rounded-xl border border-slate-200 dark:border-slate-800 bg-transparent text-sm focus:outline-none focus:border-indigo-500 focus:ring-1 focus:ring-indigo-500 transition-all font-sans"
                        placeholder="Nguyễn Văn A"
                      />
                    </div>
                  </div>
                )}

                <div>
                  <label className="block text-xs font-medium text-slate-500 dark:text-slate-400 mb-1">EMAIL CHUYÊN DÙNG</label>
                  <div className="relative">
                    <span className="absolute inset-y-0 left-0 pl-3.5 flex items-center text-slate-400">
                      <Mail className="w-4 h-4" />
                    </span>
                    <input
                      type="email"
                      required
                      value={email}
                      onChange={(e) => setEmail(e.target.value)}
                      className="w-full pl-10 pr-4 py-2.5 rounded-xl border border-slate-200 dark:border-slate-800 bg-transparent text-sm focus:outline-none focus:border-indigo-500 focus:ring-1 focus:ring-indigo-500 transition-all font-sans"
                      placeholder="email@username.com"
                    />
                  </div>
                </div>

                {!isLogin && (
                  <div>
                    <label className="block text-xs font-medium text-slate-500 dark:text-slate-400 mb-1">SỐ ĐIỆN THOẠI</label>
                    <input
                      type="tel"
                      value={phone}
                      onChange={(e) => setPhone(e.target.value)}
                      className="w-full px-4 py-2.5 rounded-xl border border-slate-200 dark:border-slate-800 bg-transparent text-sm focus:outline-none focus:border-indigo-500 focus:ring-1 focus:ring-indigo-500 transition-all font-sans"
                      placeholder="09xx xxx xxx"
                    />
                  </div>
                )}

                <div>
                  <div className="flex justify-between mb-1">
                    <label className="text-xs font-medium text-slate-500 dark:text-slate-400">MẬT KHẨU KHÓA</label>
                    {isLogin && <a href="#" className="text-xs text-indigo-600 hover:underline">Quên mật khẩu?</a>}
                  </div>
                  <input
                    type="password"
                    required
                    value={password}
                    onChange={(e) => setPassword(e.target.value)}
                    className="w-full px-4 py-2.5 rounded-xl border border-slate-200 dark:border-slate-800 bg-transparent text-sm focus:outline-none focus:border-indigo-500 focus:ring-1 focus:ring-indigo-500 transition-all font-sans"
                    placeholder="••••••••"
                  />
                </div>

                <button
                  id="auth-submit-btn"
                  type="submit"
                  className="w-full py-3 px-4 bg-indigo-600 hover:bg-indigo-700 text-white rounded-xl font-medium text-sm transition-all flex items-center justify-center gap-2 cursor-pointer shadow-lg shadow-indigo-600/10 mt-6"
                >
                  <span>{isLogin ? "Đăng Nhập Thử Nghiệm" : "Tạo Tài Khoản"}</span>
                  <ChevronRight className="w-4 h-4" />
                </button>
              </form>

              {/* OAuth Alternative Buttons Mock */}
              {/* TODO: These buttons are locally mocked and do not perform real OAuth.
                  Implement actual OAuth redirect/callback logic with a backend authentication service. */}
              <div className="mt-6">
                <div className="relative flex items-center justify-center">
                  <div className="absolute inset-0 flex items-center">
                    <div className="w-full border-t border-slate-100 dark:border-slate-800"></div>
                  </div>
                  <span className="relative px-3 text-xs text-slate-400 bg-white dark:bg-slate-900 font-mono">HOẶC ĐĂNG NHẬP NHANH</span>
                </div>

                <div className="grid grid-cols-2 gap-3.5 mt-5">
                  <button
                    type="button"
                    onClick={() => setStep('level')}
                    className="flex justify-center items-center py-2 px-4 rounded-xl border border-slate-200 dark:border-slate-800 hover:bg-slate-50 dark:hover:bg-slate-800 font-sans text-sm gap-2 cursor-pointer transition-colors"
                  >
                    <svg className="w-4 h-4" viewBox="0 0 24 24" width="24" height="24">
                      <path fill="#EA4335" d="M12.24 10.285V14.4h6.887c-.648 2.41-2.519 4.114-5.137 4.114-3.41 0-6.173-2.763-6.173-6.173s2.763-6.173 6.173-6.173c1.5 0 2.87.545 3.93 1.446l3.07-3.07C19.16 2.06 15.93 1 12.24 1 5.48 1 0 6.48 0 13.24s5.48 12.24 12.24 12.24c6.76 0 11.74-4.76 11.74-11.74 0-.64-.06-1.28-.18-1.95H12.24z"/>
                    </svg>
                    <span>Google OAuth</span>
                  </button>
                  <button
                    type="button"
                    onClick={() => setStep('level')}
                    className="flex justify-center items-center py-2 px-4 rounded-xl border border-slate-200 dark:border-slate-800 hover:bg-slate-50 dark:hover:bg-slate-800 font-sans text-sm gap-2 cursor-pointer transition-colors"
                  >
                    <svg className="w-4 h-4 text-[#1877F2]" fill="currentColor" viewBox="0 0 24 24">
                      <path d="M24 12.073c0-6.627-5.373-12-12-12s-12 5.373-12 12c0 5.99 4.388 10.954 10.125 11.854v-8.385H7.078v-3.47h3.047V9.43c0-3.007 1.792-4.669 4.533-4.669 1.312 0 2.686.235 2.686.235v2.953H15.83c-1.491 0-1.956.925-1.956 1.874v2.25h3.328l-.532 3.47h-2.796v8.385C19.612 23.027 24 18.062 24 12.073z"/>
                    </svg>
                    <span>Facebook</span>
                  </button>
                </div>
              </div>

              <div className="mt-6 text-center text-xs">
                <button
                  type="button"
                  onClick={() => setIsLogin(!isLogin)}
                  className="text-slate-500 dark:text-slate-400 hover:text-indigo-600 font-sans font-medium"
                >
                  {isLogin ? "Chưa có tài khoản? Đăng ký ngay" : "Đã có tài khoản? Đăng nhập"}
                </button>
              </div>
            </motion.div>
          )}

          {/* STEP 2: ENGLISH LEVEL SELECTION */}
          {step === 'level' && (
            <motion.div
              initial={{ opacity: 0, scale: 0.95 }}
              animate={{ opacity: 1, scale: 1 }}
              className="bg-white dark:bg-slate-900 rounded-3xl p-8 shadow-xl border border-slate-100 dark:border-slate-800"
            >
              <div className="text-center">
                <span className="font-mono text-xs text-indigo-600 dark:text-sky-400 font-semibold uppercase tracking-wider">Cấu hình cấp độ</span>
                <h2 className="text-2xl font-bold tracking-tight mt-1">Cấp độ tiếng Anh hiện tại?</h2>
                <p className="text-slate-500 dark:text-slate-400 text-sm mt-1.5">
                  ShadowSpeak thiết kế lộ trình video, audio tương ứng với nhu cầu của riêng bạn
                </p>
              </div>

              <div className="mt-8 space-y-4">
                {[
                  {
                    id: 'Casual' as UserLevel,
                    title: 'Đời sống giao tiếp (Casual)',
                    desc: 'Luyện nói đời thường, đi du lịch, đàm thoại bạn bè, mua sắm ẩm thực.',
                    icon: Compass,
                    color: 'from-orange-500 to-amber-500'
                  },
                  {
                    id: 'Academic' as UserLevel,
                    title: 'Học thuật & Trình độ cao (Academic)',
                    desc: 'Cải thiện phát âm viết luận, nghiên cứu, từ vựng thi IELTS/TOEFL hùng biện.',
                    icon: GraduationCap,
                    color: 'from-emerald-500 to-teal-500'
                  },
                  {
                    id: 'Professional' as UserLevel,
                    title: 'Công sở & Doanh nghiệp (Professional)',
                    desc: 'Đàm phán thương mại, thuyết trình dự án, trả lời phỏng vấn chuyên nghiệp.',
                    icon: Briefcase,
                    color: 'from-indigo-500 to-blue-500'
                  }
                ].map((item) => {
                  const Icon = item.icon;
                  const isSelected = level === item.id;
                  return (
                    <button
                      key={item.id}
                      type="button"
                      onClick={() => setLevel(item.id)}
                      className={`w-full text-left p-4 rounded-2xl border transition-all cursor-pointer flex gap-4 ${
                        isSelected 
                          ? 'border-indigo-600 bg-indigo-50/20 dark:border-sky-500 dark:bg-sky-500/10' 
                          : 'border-slate-100 hover:border-slate-200 dark:border-slate-800 dark:hover:border-slate-700'
                      }`}
                    >
                      <div className={`w-12 h-12 rounded-xl bg-gradient-to-br ${item.color} text-white flex items-center justify-center shrink-0 shadow-md`}>
                        <Icon className="w-6 h-6" />
                      </div>
                      <div>
                        <div className="font-bold text-sm tracking-tight flex items-center gap-1.5">
                          <span>{item.title}</span>
                          {isSelected && <span className="w-2 h-2 rounded-full bg-indigo-600 dark:bg-sky-400"></span>}
                        </div>
                        <p className="text-slate-500 dark:text-slate-400 text-xs mt-1 leading-normal">{item.desc}</p>
                      </div>
                    </button>
                  );
                })}
              </div>

              {/* Accent setting */}
              <div className="mt-8 pt-6 border-t border-slate-100 dark:border-slate-800">
                <label className="text-xs font-semibold text-slate-500 uppercase tracking-widest block mb-3">
                  Giọng nói mục tiêu hướng tới (Target Accent)
                </label>
                <div className="grid grid-cols-2 gap-3">
                  <button
                    type="button"
                    onClick={() => setTargetAccent('US')}
                    className={`py-2.5 rounded-xl border font-sans font-semibold text-xs cursor-pointer text-center transition-all ${
                      targetAccent === 'US'
                        ? 'border-indigo-600 bg-indigo-50/10 text-indigo-600 dark:border-sky-400 dark:text-sky-400'
                        : 'border-slate-200 hover:border-slate-300 dark:border-slate-800 dark:hover:border-slate-700'
                    }`}
                  >
                    🇺🇸 Giọng Mỹ (General American)
                  </button>
                  <button
                    type="button"
                    onClick={() => setTargetAccent('UK')}
                    className={`py-2.5 rounded-xl border font-sans font-semibold text-xs cursor-pointer text-center transition-all ${
                      targetAccent === 'UK'
                        ? 'border-indigo-600 bg-indigo-50/10 text-indigo-600 dark:border-sky-400 dark:text-sky-400'
                        : 'border-slate-200 hover:border-slate-300 dark:border-slate-800 dark:hover:border-slate-700'
                    }`}
                  >
                    🇬🇧 Giọng Anh (RP / British)
                  </button>
                </div>
              </div>

              <button
                type="button"
                onClick={handleLevelNext}
                className="w-full mt-6 py-3 px-4 bg-indigo-600 hover:bg-indigo-700 text-white rounded-xl font-medium text-sm transition-all flex items-center justify-center gap-2 cursor-pointer"
              >
                <span>Tiếp tục chọn Mục Tiêu</span>
                <ChevronRight className="w-4 h-4" />
              </button>
            </motion.div>
          )}

          {/* STEP 3: LEARNING GOAL */}
          {step === 'goal' && (
            <motion.div
              initial={{ opacity: 0, scale: 0.95 }}
              animate={{ opacity: 1, scale: 1 }}
              className="bg-white dark:bg-slate-900 rounded-3xl p-8 shadow-xl border border-slate-100 dark:border-slate-800"
            >
              <div className="text-center">
                <span className="font-mono text-xs text-indigo-600 dark:text-sky-400 font-semibold uppercase tracking-wider">Chọn đích đến</span>
                <h2 className="text-2xl font-bold tracking-tight mt-1">Mức độ phát âm kỳ vọng?</h2>
                <p className="text-slate-500 dark:text-slate-400 text-sm mt-1.5">
                  Đặt chỉ tiêu điểm số tối thiểu để hoàn thành mọi câu luyện đọc Shadowing
                </p>
              </div>

              <div className="mt-8 space-y-4">
                {[
                  {
                    id: 'fluency50' as LearningGoal,
                    title: 'Luyện giao tiếp phản xạ trôi chảy (Mục tiêu 50%)',
                    desc: 'Chấp nhận sai lệch phát âm nhỏ, ưu tiên tốc độ nói nhanh và thói quen ngắt nghỉ tự nhiên.',
                    percentage: '50%+',
                    badge: 'Casual Speaking'
                  },
                  {
                    id: 'comprehension70' as LearningGoal,
                    title: 'Phát âm rõ ràng người nghe hiểu (Mục tiêu 70%)',
                    desc: 'Đọc đúng đa số từ, đủ âm cuối quan trọng, giao tiếp làm việc tự tin, trôi trảy.',
                    percentage: '70%+',
                    badge: 'Standard Clarity'
                  },
                  {
                    id: 'accent90' as LearningGoal,
                    title: 'Phát âm chuẩn xác bản xứ (Mục tiêu 90%)',
                    desc: 'Tiêu chuẩn khắt khe, kiểm tra từng nốt cao độ, trọng âm, nguyên âm đôi, nuốt âm đặc trưng.',
                    percentage: '90%+',
                    badge: 'Near-Native Mastery'
                  }
                ].map((item) => {
                  const isSelected = goal === item.id;
                  return (
                    <button
                      key={item.id}
                      type="button"
                      onClick={() => setGoal(item.id)}
                      className={`w-full text-left p-4 rounded-2xl border transition-all cursor-pointer flex items-center justify-between gap-4 ${
                        isSelected 
                          ? 'border-indigo-600 bg-indigo-50/20 dark:border-sky-500 dark:bg-sky-500/10' 
                          : 'border-slate-100 hover:border-slate-200 dark:border-slate-800 dark:hover:border-slate-700'
                      }`}
                    >
                      <div className="flex-1">
                        <span className="inline-block text-[10px] font-bold uppercase tracking-wider text-indigo-600 dark:text-sky-400 bg-slate-100 dark:bg-slate-800 px-2 py-0.5 rounded">
                          {item.badge}
                        </span>
                        <div className="font-bold text-sm tracking-tight mt-1">{item.title}</div>
                        <p className="text-slate-500 dark:text-slate-400 text-xs mt-1 leading-normal">{item.desc}</p>
                      </div>
                      <div className={`w-12 h-12 rounded-xl flex items-center justify-center font-mono font-bold text-sm shrink-0 border ${
                        isSelected 
                          ? 'border-indigo-600 bg-indigo-600 text-white dark:border-sky-400 dark:bg-sky-400 dark:text-slate-950' 
                          : 'border-slate-200 bg-slate-50 text-slate-500 dark:border-slate-800 dark:bg-slate-800'
                      }`}>
                        {item.percentage}
                      </div>
                    </button>
                  );
                })}
              </div>

              <div className="bg-amber-50 dark:bg-amber-500/5 text-amber-800 dark:text-amber-300 rounded-xl p-3 text-xs flex gap-2.5 mt-5 border border-amber-200/50 dark:border-amber-500/10 font-sans">
                <Sparkles className="w-5 h-5 shrink-0" />
                <span>
                  <strong>Lưu ý:</strong> Cấu hình mức độ phát âm cho phép thay đổi hoàn toàn miễn phí bất cứ lúc nào trong bảng <strong>Cài đặt tài khoản</strong>.
                </span>
              </div>

              <button
                type="button"
                onClick={handleGoalNext}
                className="w-full mt-6 py-3 px-4 bg-indigo-600 hover:bg-indigo-700 text-white rounded-xl font-medium text-sm transition-all flex items-center justify-center gap-2 cursor-pointer"
              >
                <span>Xem Quyền Lợi Premium</span>
                <ChevronRight className="w-4 h-4" />
              </button>
            </motion.div>
          )}

          {/* STEP 4: PREMIUM SUBSCRIPTION OFFER */}
          {step === 'premium' && (
            <motion.div
              initial={{ opacity: 0, scale: 0.95 }}
              animate={{ opacity: 1, scale: 1 }}
              className="bg-white dark:bg-slate-900 rounded-3xl p-8 shadow-xl border border-slate-100 dark:border-slate-800 text-center relative overflow-hidden"
            >
              {/* Premium Glow banner background */}
              <div className="absolute top-0 inset-x-0 h-1.5 bg-gradient-to-r from-pink-500 via-purple-500 to-indigo-500"></div>

              <div className="mx-auto w-16 h-16 rounded-2xl bg-indigo-500/10 text-indigo-600 dark:text-indigo-400 flex items-center justify-center mt-3">
                <Shield className="w-8 h-8" />
              </div>

              <h2 className="text-2xl font-bold tracking-tight mt-4">Kích hoạt Gói Pro Thành Viên?</h2>
              <p className="text-slate-500 dark:text-slate-400 text-xs mt-1.5 max-w-sm mx-auto">
                Mở khóa không giới hạn số tim luyện tập, công nghệ chấm điểm Gemini AI và tính năng tự tạo bài giảng theo ý muốn.
              </p>

              <div className="mt-6 border border-slate-100 dark:border-slate-800 bg-slate-50/50 dark:bg-slate-950/20 rounded-2xl p-4 text-left space-y-3.5">
                {[
                  "Không quảng cáo cắt ngang luồng ghi âm",
                  "Mở rộng phân tích từ vựng IPA chuyên sâu từ Gemini AI",
                  "Vô hạn sinh ngẫu nhiên chủ đề học bằng AI",
                  "Đặc quyền thay đổi cấp độ học tập linh hoạt (Subscription-based)*",
                  "Tải xuống file thu âm, thống kê lịch sử phát âm nâng cao"
                ].map((perk, idx) => (
                  <div key={idx} className="flex gap-2.5 items-start text-xs text-slate-600 dark:text-slate-300">
                    <span className="w-4 h-4 rounded-full bg-emerald-500/20 text-emerald-500 flex items-center justify-center mt-0.5 shrink-0 font-bold">✓</span>
                    <span>{perk}</span>
                  </div>
                ))}
              </div>

              <div className="mt-6 flex flex-col md:flex-row items-center justify-between p-4 bg-indigo-50/30 dark:bg-indigo-950/20 rounded-2xl gap-3">
                <div className="text-left">
                  <span className="text-[10px] text-indigo-600 dark:text-sky-400 font-mono uppercase font-bold tracking-wider">HỌC PHÍ THỬ NGHIỆM</span>
                  <div className="text-xl font-extrabold flex items-baseline gap-1 mt-0.5">
                    <span>149K ₫</span>
                    <span className="text-xs font-normal text-slate-400">/ tháng</span>
                  </div>
                </div>
                <span className="text-xs text-emerald-600 dark:text-emerald-400 px-3 py-1 bg-emerald-100/30 rounded-lg font-mono font-semibold">Tặng miễn phí 3 ngày test</span>
              </div>

              <p className="text-[10px] text-slate-400 mt-4 leading-normal">
                *Theo quy chế, bạn được dùng đầy đủ chức năng miễn phí tại AI Studio. Bấm "Nâng Cấp VIP" để trải nghiệm giao diện nạp tiền giả định, hoặc bấm "Dùng Miễn Phí" để trải nghiệm luôn bản thường gốc.
              </p>

              <div className="grid grid-cols-2 gap-3 mt-6">
                <button
                  type="button"
                  onClick={() => handleCompleteSetup(false)}
                  className="py-3 px-4 rounded-xl border border-slate-200 dark:border-slate-800 text-slate-600 hover:text-slate-800 dark:text-slate-400 dark:hover:text-slate-200 font-medium text-xs cursor-pointer transition-colors"
                >
                  Dùng Gói Miễn Phí
                </button>
                <button
                  type="button"
                  onClick={() => handleCompleteSetup(true)}
                  className="py-3 px-4 bg-gradient-to-r from-pink-500 to-indigo-600 hover:from-pink-600 hover:to-indigo-700 text-white font-semibold text-xs rounded-xl flex items-center justify-center gap-2 cursor-pointer shadow-lg shadow-indigo-500/10"
                >
                  <Sparkles className="w-4 h-4 shrink-0 text-amber-300" />
                  <span>Kích Hoạt VIP MIỄN PHÍ</span>
                </button>
              </div>
            </motion.div>
          )}

        </div>
      </div>

    </div>
  );
}
