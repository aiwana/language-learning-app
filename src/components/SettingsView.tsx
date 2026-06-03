/**
 * @license
 * SPDX-License-Identifier: Apache-2.0
 */

import React, { useState } from 'react';
import { UserProfile, UserLevel, TargetAccent, LearningGoal } from '../types';
import { 
  User, Mail, Phone, Lock, Sparkles, CreditCard, Save, HelpCircle, ShieldAlert, BadgeCheck, CheckCircle2, Moon, Sun, Info, LogOut
} from 'lucide-react';

interface SettingsViewProps {
  profile: UserProfile;
  isDark: boolean;
  onUpdateProfile: (updatedProfile: UserProfile) => void;
  onToggleTheme: () => void;
  onLogout?: () => void;
}

export function SettingsView({
  profile,
  isDark,
  onUpdateProfile,
  onToggleTheme,
  onLogout
}: SettingsViewProps) {
  const [name, setName] = useState(profile.name);
  const [email, setEmail] = useState(profile.email);
  const [phone, setPhone] = useState(profile.phone || '');
  const [level, setLevel] = useState<UserLevel>(profile.level);
  const [targetAccent, setTargetAccent] = useState<TargetAccent>(profile.targetAccent);
  const [goal, setGoal] = useState<LearningGoal>(profile.goal);
  const [isPremium, setIsPremium] = useState(profile.isPremium);
  
  // Save confirmation states
  const [saveSuccess, setSaveSuccess] = useState(false);
  const [billingModalOpen, setBillingModalOpen] = useState(false);
  // TODO: Payment data here is purely simulated and should never be stored client-side in a real app.
  // Replace with secure billing/subscription endpoints on a backend or ASP.NET service.
  // Real billing should use server-side payment providers and store only subscription state/proof of purchase.
  const [simulatedCardNumber, setSimulatedCardNumber] = useState('4111 2222 3333 4444');

  const handleSaveProfileSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    const updated: UserProfile = {
      ...profile,
      name,
      email,
      phone,
      level,
      targetAccent,
      goal,
      isPremium
    };
    onUpdateProfile(updated);
    setSaveSuccess(true);
    setTimeout(() => setSaveSuccess(false), 2500);
  };

  const handleSimulateSubscriptionCharge = () => {
    // TODO: This is a fake subscription flow.
    // Replace with a real subscription/payments backend and persist membership status through the API.
    // Example: POST /api/subscription/activate { userId, planId }
    // Then load user premium status from backend rather than local state.
    setIsPremium(true);
    const updated: UserProfile = {
      ...profile,
      name,
      email,
      phone,
      level,
      targetAccent,
      goal,
      isPremium: true,
      paymentMethod: 'VietQR / Credit Card (Simulated)'
    };
    onUpdateProfile(updated);
    setBillingModalOpen(false);
    alert('Thanh toán giả định thành công! Tài khoản Nguyễn Văn A hiện là thành viên Pro VIP vô hạn.');
  };

  return (
    <div id="settings-view-container" className="max-w-4xl mx-auto px-4 sm:px-6 lg:px-8 py-8 font-sans">
      
      <div className="grid md:grid-cols-12 gap-8 items-start">
        
        {/* Left column: Edit Profile - 8 cols */}
        <div className="md:col-span-8 bg-white dark:bg-slate-900 border border-slate-100 dark:border-slate-800 rounded-3xl p-6 md:p-8 shadow-sm">
          <h3 className="text-lg font-extrabold tracking-tight mb-6">Cài đặt hồ sơ & Mục tiêu học</h3>
          
          <form onSubmit={handleSaveProfileSubmit} className="space-y-5 text-xs font-sans">
            
            {/* Input Name form */}
            <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
              <div>
                <label className="text-[10px] uppercase font-bold text-slate-400 block mb-1.5 font-mono">Tên thành viên</label>
                <div className="relative">
                  <span className="absolute inset-y-0 left-0 pl-3 flex items-center text-slate-400">
                    <User className="w-4 h-4" />
                  </span>
                  <input
                    type="text"
                    required
                    value={name}
                    onChange={(e) => setName(e.target.value)}
                    className="w-full pl-9 pr-3.5 py-2.5 rounded-xl border border-slate-200 dark:border-slate-800 bg-transparent text-sm focus:outline-none focus:border-indigo-500 font-sans"
                  />
                </div>
              </div>

              <div>
                <label className="text-[10px] uppercase font-bold text-slate-400 block mb-1.5 font-mono">Số điện thoại liên hệ</label>
                <div className="relative">
                  <span className="absolute inset-y-0 left-0 pl-3 flex items-center text-slate-400">
                    <Phone className="w-4 h-4" />
                  </span>
                  <input
                    type="tel"
                    value={phone}
                    onChange={(e) => setPhone(e.target.value)}
                    className="w-full pl-9 pr-3.5 py-2.5 rounded-xl border border-slate-200 dark:border-slate-800 bg-transparent text-sm focus:outline-none focus:border-indigo-500 font-sans"
                    placeholder="09xx xxx xxx"
                  />
                </div>
              </div>
            </div>

            {/* Email form */}
            <div>
              <label className="text-[10px] uppercase font-bold text-slate-400 block mb-1.5 font-mono">Địa chỉ Email</label>
              <div className="relative">
                <span className="absolute inset-y-0 left-0 pl-3 flex items-center text-slate-400">
                  <Mail className="w-4 h-4" />
                </span>
                <input
                  type="email"
                  disabled
                  value={email}
                  className="w-full pl-9 pr-3.5 py-2.5 bg-slate-50 dark:bg-slate-950/40 text-slate-400 rounded-xl border border-slate-200 dark:border-slate-800 text-sm focus:outline-none font-sans cursor-not-allowed"
                />
              </div>
              <p className="text-[10px] text-slate-400 mt-1 leading-normal">
                Để bảo mật, không được sửa đổi địa chỉ email đăng nhập. Liên hệ hỗ trợ viên nếu đổi thiết bị học.
              </p>
            </div>

            <hr className="border-slate-100 dark:border-slate-800/80 my-2" />

            {/* Custom Settings Preferences: Level selecting & accent */}
            <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
              <div>
                <label className="text-[10px] uppercase font-bold text-slate-400 block mb-1.5 font-mono">Khóa học / Chủ đề mong muốn (Miễn phí)</label>
                <select
                  value={level}
                  onChange={(e: any) => setLevel(e.target.value)}
                  className="w-full bg-slate-50 dark:bg-slate-950 border border-slate-200 dark:border-slate-800 rounded-xl py-2.5 px-3 focus:outline-none text-xs text-slate-700 dark:text-slate-300"
                >
                  <option value="Casual">Casual Speaking (Đời sống)</option>
                  <option value="Academic">Academic Prep (Học thuật)</option>
                  <option value="Professional">Professional Career (Công việc)</option>
                </select>
              </div>

              <div>
                <label className="text-[10px] uppercase font-bold text-slate-400 block mb-1.5 font-mono">Mục tiêu điểm số trôi chảy</label>
                <select
                  value={goal}
                  onChange={(e: any) => setGoal(e.target.value)}
                  className="w-full bg-slate-50 dark:bg-slate-950 border border-slate-200 dark:border-slate-800 rounded-xl py-2.5 px-3 focus:outline-none text-xs text-slate-700 dark:text-slate-300"
                >
                  <option value="fluency50">Ưu tiên trôi chảy tự nhiên (&gt; 50%)</option>
                  <option value="comprehension70">Làm việc tự tin người khác hiểu (&gt; 70%)</option>
                  <option value="accent90">Phát âm chuẩn xác bản xứ (&gt; 90%)</option>
                </select>
              </div>
            </div>

            {/* Accent selection - US vs UK */}
            <div>
              <label className="text-[10px] uppercase font-bold text-slate-400 block mb-2 font-mono">Giọng phát âm mục tiêu (Target Accent)</label>
              <div className="grid grid-cols-2 gap-3.5">
                <button
                  type="button"
                  onClick={() => setTargetAccent('US')}
                  className={`py-2.5 rounded-xl border text-xs font-bold transition-all cursor-pointer ${
                    targetAccent === 'US'
                      ? 'border-indigo-600 bg-indigo-50/15 text-indigo-600 dark:border-sky-400 dark:text-sky-400'
                      : 'border-slate-200 hover:border-slate-300 dark:border-slate-800'
                  }`}
                >
                  🇺🇸 Giọng Mỹ (General American)
                </button>
                <button
                  type="button"
                  onClick={() => setTargetAccent('UK')}
                  className={`py-2.5 rounded-xl border text-xs font-bold transition-all cursor-pointer ${
                    targetAccent === 'UK'
                      ? 'border-indigo-600 bg-indigo-50/15 text-indigo-600 dark:border-sky-400 dark:text-sky-400'
                      : 'border-slate-200 hover:border-slate-300 dark:border-slate-800'
                  }`}
                >
                  🇬🇧 Giọng Anh (RP / British)
                </button>
              </div>
            </div>

            {saveSuccess && (
              <div className="p-3 bg-emerald-50 text-emerald-800 border border-emerald-200/50 rounded-xl flex items-center gap-2 font-medium">
                <CheckCircle2 className="w-4 h-4 text-emerald-500" />
                <span>Đã lưu thành công mọi cấu hình học tập và hồ sơ!</span>
              </div>
            )}

            <button
              type="submit"
              className="px-6 py-3 bg-indigo-600 hover:bg-indigo-700 text-white rounded-xl font-bold flex items-center justify-center gap-2 cursor-pointer shadow-sm ml-auto"
            >
              <Save className="w-4 h-4" />
              <span>Lưu Cấu Hình Hồ Sơ</span>
            </button>

          </form>

        </div>

        {/* Right column: Action widgets on pricing & instructions - 4 cols */}
        <div className="md:col-span-4 space-y-6">
          
          {/* VIP Upgrade simulation panel */}
          <div className="bg-white dark:bg-slate-900 border border-slate-100 dark:border-slate-800 rounded-3xl p-6 shadow-sm">
            <div className="flex items-center gap-2 text-indigo-600 dark:text-sky-400 mb-2">
              <Sparkles className="w-5 h-5 fill-indigo-500 text-indigo-500 dark:fill-none" />
              <h4 className="font-extrabold text-slate-900 dark:text-slate-50 text-sm tracking-tight">Thành viên Pro VIP</h4>
            </div>
            
            {isPremium ? (
              <div className="space-y-3.5 pt-2">
                <div className="p-3.5 bg-emerald-50 text-emerald-800 dark:bg-emerald-500/5 dark:text-emerald-400 border border-emerald-200/50 dark:border-emerald-500/10 rounded-2xl flex gap-2 w-full text-xs items-start">
                  <BadgeCheck className="w-5 h-5 text-emerald-500 shrink-0 mt-0.5" />
                  <div>
                    <span className="font-extrabold text-xs block">Tài khoản PRO kích hoạt</span>
                    <span className="text-[10px] text-slate-400 leading-normal block mt-1">
                      Phương thức nạp: {profile.paymentMethod || 'Miễn Phí tại AI Studio'}
                    </span>
                  </div>
                </div>

                <button
                  type="button"
                  onClick={() => {
                    setIsPremium(false);
                    const updated = { ...profile, isPremium: false, paymentMethod: undefined };
                    onUpdateProfile(updated);
                    alert('Hủy Pro VIP thành công. Trải nghiệm gói thường!');
                  }}
                  className="w-full py-2 border border-slate-200 dark:border-slate-800 hover:bg-slate-50 dark:hover:bg-slate-800 font-bold text-[10px] text-slate-400 text-center rounded-xl cursor-pointer"
                >
                  Hủy kích hoạt gói VIP
                </button>
              </div>
            ) : (
              <div className="space-y-4 pt-2 font-sans text-xs">
                <p className="text-slate-500 dark:text-slate-400 text-xs leading-normal">
                  Bạn đang sử dụng gói Miễn phí. Giới hạn số tim trong ngày, không cho phép thay đổi ngẫu nhiên bài giảng bằng AI.
                </p>

                <button
                  type="button"
                  onClick={() => setBillingModalOpen(true)}
                  className="w-full py-3 bg-gradient-to-tr from-pink-500 to-indigo-600 text-white rounded-xl font-bold flex items-center justify-center gap-1.5 shadow-md hover:scale-[1.02] cursor-pointer"
                >
                  <CreditCard className="w-4 h-4 text-amber-200" />
                  <span>Nâng cấp VIP (Simulate)</span>
                </button>
              </div>
            )}
          </div>

            {/* Theme switcher panel inside settings */}
            <div className="bg-white dark:bg-slate-900 border border-slate-100 dark:border-slate-800 rounded-3xl p-6 shadow-sm text-xs">
              <h4 className="font-bold tracking-tight mb-2 flex items-center gap-1">
                <Info className="w-4 h-4 text-indigo-500" />
                <span>Giao diện ứng dụng</span>
              </h4>
              <p className="text-slate-400 block leading-normal mb-3">Ấn chuyển đổi giao diện sáng hoặc tối bảo vệ mắt người đọc buổi tối.</p>
              <button
                type="button"
                onClick={onToggleTheme}
                className="w-full py-2.5 rounded-xl border border-slate-200 dark:border-slate-800 hover:bg-slate-50 dark:hover:bg-slate-800 flex items-center justify-center gap-2 font-bold cursor-pointer"
              >
                {isDark ? (
                  <>
                    <Sun className="w-4 h-4 text-amber-500" />
                    <span>Kích hoạt giao diện Sáng</span>
                  </>
                ) : (
                  <>
                    <Moon className="w-4 h-4 text-indigo-500" />
                    <span>Kích hoạt giao diện Tối</span>
                  </>
                )}
              </button>
            </div>

            {/* Logout Section */}
            <div className="bg-white dark:bg-slate-900 border border-slate-100 dark:border-slate-800 rounded-3xl p-6 shadow-sm text-xs">
              <h4 className="font-bold tracking-tight mb-2 flex items-center gap-1 text-rose-500">
                <ShieldAlert className="w-4 h-4" />
                <span>Đăng xuất tài khoản</span>
              </h4>
              <p className="text-slate-400 block leading-normal mb-3">Đăng xuất khỏi thiết bị này để bảo mật dữ liệu học tập cá nhân.</p>
              <button
                type="button"
                onClick={onLogout}
                className="w-full py-2.5 rounded-xl border border-rose-200 hover:bg-rose-50 dark:border-rose-950 dark:hover:bg-rose-950/20 text-rose-600 dark:text-rose-400 flex items-center justify-center gap-2 font-bold cursor-pointer transition-colors"
              >
                <LogOut className="w-4 h-4" />
                <span>Đăng xuất ngay</span>
              </button>
            </div>

        </div>

      </div>

      {/* RENDER MOCK SUBSCRIPTION BILLING POPUP MODAL */}
      {billingModalOpen && (
        <div id="billing-modal" className="fixed inset-0 z-50 bg-slate-950/60 backdrop-blur-sm flex items-center justify-center p-4">
          <div className="bg-white dark:bg-slate-950 border border-slate-200 dark:border-slate-800 w-full max-w-md rounded-3xl p-6 shadow-2xl relative font-sans">
            
            <button
              onClick={() => setBillingModalOpen(false)}
              className="absolute top-4 right-4 text-slate-400 hover:text-slate-600 dark:hover:text-slate-200 text-sm font-bold p-1 cursor-pointer"
            >
              ✕
            </button>

            <div className="text-center mb-5 font-sans">
              <span className="w-12 h-12 rounded-xl bg-indigo-500/10 text-indigo-600 dark:text-sky-400 flex items-center justify-center mx-auto mb-3">
                <CreditCard className="w-6 h-6" />
              </span>
              <h3 className="font-black text-lg tracking-tight">Cổng thanh toán giả định</h3>
              <p className="text-slate-400 text-xxs mt-1">ShadowSpeak Subscription Billing Simulator - 100% Free</p>
            </div>

            <div className="space-y-4 font-sans text-xs">
              
              <div className="p-3.5 bg-slate-50 dark:bg-slate-900 border border-slate-100 rounded-2xl flex justify-between items-center text-xs">
                <div>
                  <span className="font-bold text-xs">Nâng cấp gói thành viên VIP</span>
                  <p className="text-[10px] text-slate-400 mt-0.5">Sinh ngẫu nhiên bài giảng AI không giới hạn</p>
                </div>
                <strong className="text-sm font-extrabold text-indigo-600 dark:text-sky-400">149.000 ₫</strong>
              </div>

              <div>
                <label className="text-[9px] font-bold text-slate-400 block mb-1">MÃ SỐ THE ĐĂNG KÝ (VISA SIMULATED)</label>
                <input
                  type="text"
                  value={simulatedCardNumber}
                  onChange={(e) => setSimulatedCardNumber(e.target.value)}
                  className="w-full bg-slate-50 dark:bg-slate-900 p-2.5 rounded-lg border border-slate-200 dark:border-slate-800 text-xs font-mono tracking-wider focus:outline-none focus:border-indigo-500"
                />
              </div>

              <div className="grid grid-cols-2 gap-3">
                <div>
                  <label className="text-[9px] font-bold text-slate-400 block mb-1">NGÀY HẾT HẠN</label>
                  <input
                    type="text"
                    defaultValue="12/29"
                    className="w-full bg-slate-50 dark:bg-slate-900 p-2.5 rounded-lg border border-slate-200 dark:border-slate-800 text-xs text-center"
                  />
                </div>
                <div>
                  <label className="text-[9px] font-bold text-slate-400 block mb-1">MÃ BẢO MAT (CVV)</label>
                  <input
                    type="password"
                    defaultValue="123"
                    className="w-full bg-slate-50 dark:bg-slate-900 p-2.5 rounded-lg border border-slate-200 dark:border-slate-800 text-xs text-center"
                  />
                </div>
              </div>

              <div className="bg-emerald-50 dark:bg-emerald-500/5 text-emerald-800 dark:text-emerald-400 p-3 rounded-xl border border-emerald-200/50 flex gap-2 items-start mt-4">
                <CheckCircle2 className="w-4 h-4 text-emerald-500 shrink-0 mt-0.5" />
                <p className="text-[10px] leading-normal font-sans">
                  <strong>An toàn tuyệt đối:</strong> Đây hoàn toàn là một giao dịch giả lập. Tài khoản của bạn sẽ được kích hoạt quyền thành viên Pro VIP lập tức mà không phát sinh phí thật nào cả!
                </p>
              </div>

              <button
                type="button"
                onClick={handleSimulateSubscriptionCharge}
                className="w-full py-3 bg-indigo-600 hover:bg-indigo-700 text-white font-bold text-xs rounded-xl flex items-center justify-center gap-1.5 tracking-wide shadow cursor-pointer mt-5"
              >
                <span>XÁC NHẬN NẠP TIỀN 149.000₫</span>
              </button>

            </div>

          </div>
        </div>
      )}

    </div>
  );
}
