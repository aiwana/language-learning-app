/**
 * @license
 * SPDX-License-Identifier: Apache-2.0
 */

import { Lesson } from '../types';

// TODO: These static lesson objects are hardcoded sample content.
// In a real integrated system, remove this file and fetch lessons from a backend database or ASP.NET service.
// The client should receive lesson metadata and sentence details via API calls, not import them as module constants.
export const STATIC_LESSONS: Lesson[] = [
  {
    id: 'lesson-1',
    title: 'Giao tiếp hằng ngày: Chuyện buổi sáng',
    level: 'Cơ bản',
    topic: 'Casual',
    duration: '0:35',
    youtubeId: 'e8Z7rXg69g0', // Simple morning routine snippet or mock
    sentences: [
      {
        id: 's1-1',
        text: 'I usually wake up around six in the morning.',
        translation: 'Tôi thường thức dậy vào khoảng sáu giờ sáng.',
        ipa: '[aɪ ˈjuːʒuəli weɪk ʌp əˈraʊnd sɪks ɪn ðə ˈmɔːrnɪŋ]',
        startTime: 0,
        endTime: 4
      },
      {
        id: 's1-2',
        text: 'Today, I made myself a perfect cup of hot coffee.',
        translation: 'Hôm nay, tôi tự pha cho mình một tách cà phê nóng thật tuyệt vời.',
        ipa: '[təˈdeɪ, aɪ meɪd maɪˈsɛlf ə ˈpɜːrfɪkt kʌp ʌv hɑːt ˈkɔːfi]',
        startTime: 5,
        endTime: 9
      },
      {
        id: 's1-3',
        text: 'It helps me stay active and focused throughout the day.',
        translation: 'Nó giúp tôi tỉnh táo và tập trung trong suốt cả ngày.',
        ipa: '[ɪt hɛlps miː steɪ ˈæktɪv ænd ˈfoʊkəst θruːˈaʊt ðə deɪ]',
        startTime: 10,
        endTime: 15
      },
      {
        id: 's1-4',
        text: 'A clean routine makes my life simple and relaxed.',
        translation: 'Một thói quen ngăn nắp giúp cuộc sống của tôi đơn giản và thư thái.',
        ipa: '[ə kliːn ruːˈtiːn meɪks maɪ laɪf ˈsɪmpl ænd rɪˈlækst]',
        startTime: 16,
        endTime: 21
      }
    ]
  },
  {
    id: 'lesson-2',
    title: 'Phát biển hội thảo: Thách thức công nghệ',
    level: 'Nâng cao',
    topic: 'Professional',
    duration: '0:42',
    youtubeId: '9Kq89k6S_gI',
    sentences: [
      {
        id: 's2-1',
        text: 'Artificial intelligence is rapidly transforming global business operations.',
        translation: 'Trí tuệ nhân tạo đang nhanh chóng làm thay đổi các hoạt động kinh doanh toàn cầu.',
        ipa: '[ˌɑːrtɪˈfɪʃl ɪnˈtɛlɪdʒəns ɪz ˈræpɪdli trænsˈfɔːrmɪŋ ˈɡloʊbl ˈbɪznəs ˌɑːpəˈreɪʃnz]',
        startTime: 0,
        endTime: 6
      },
      {
        id: 's2-2',
        text: 'Companies must adapt quickly to secure a competitive advantage.',
        translation: 'Các công ty phải thích ứng thật nhanh để bảo đảm lợi thế cạnh tranh.',
        ipa: '[ˈkʌmpəniz mʌst əˈdæpt ˈkwɪkli tuː sɪˈkjʊr ə kəmˈpɛtətɪv ædˈvæntɪdʒ]',
        startTime: 7,
        endTime: 12
      },
      {
        id: 's2-3',
        text: 'Innovation requires not only capital but also cultural agility.',
        translation: 'Sự đổi mới sáng tạo đòi hỏi không chỉ vốn nguồn lực mà còn cả sự linh hoạt về văn hóa.',
        ipa: '[ˌɪnəˈveɪʃn rɪˈkwaɪərz nɑːt ˈoʊnli ˈkæpɪtl bʌt ˈɔːlsoʊ ˈkʌltʃərəl əˈdʒɪləti]',
        startTime: 13,
        endTime: 20
      },
      {
        id: 's2-4',
        text: 'Therefore, our main focus should remain on talent development.',
        translation: 'Vì vậy, trọng tâm chính của chúng ta nên luôn luôn là phát triển tài năng.',
        ipa: '[ˈðɛrfɔːr, ˈaʊər meɪn ˈfoʊkəs ʃʊd rɪˈmeɪn ɑːn ˈtælənt dɪˈvɛlɒpmənt]',
        startTime: 21,
        endTime: 27
      }
    ]
  },
  {
    id: 'lesson-3',
    title: 'Học thuật: Ô nhiễm môi trường & Hành động',
    level: 'Trung cấp',
    topic: 'Academic',
    duration: '0:38',
    youtubeId: 'O32S7M-N9j8',
    sentences: [
      {
        id: 's3-1',
        text: 'Global carbon emissions continue to rise at an alarming speed.',
        translation: 'Lượng khí thải carbon toàn cầu tiếp tục tăng với tốc độ đáng báo động.',
        ipa: '[ˈɡloʊbl ˈkɑːrbən ɪˈmɪʃnz kənˈtɪnjuː tuː raɪz æt ən əˈlɑːrmɪŋ spiːd]',
        startTime: 0,
        endTime: 5
      },
      {
        id: 's3-2',
        text: 'This trend significantly accelerates the cycle of climate change.',
        translation: 'Xu hướng này làm tăng tốc đáng kể chu kỳ biến đổi khí hậu.',
        ipa: '[ðɪs trɛnd sɪɡˈnɪfɪkəntli ækˈsɛləreɪts ðə ˈsaɪkl ʌv ˈklaɪmət tʃeɪndʒ]',
        startTime: 6,
        endTime: 11
      },
      {
        id: 's3-3',
        text: 'Transitioning to clean renewable energy is our ultimate solution.',
        translation: 'Chuyển dịch sang năng lượng tái tạo sạch là giải pháp tối ưu của chúng ta.',
        ipa: '[trænˈzɪʃənɪŋ tuː kliːn rɪˈnjuːəbl ˈɛnərdʒi ɪz ˈaʊər ˈʌltəmət səˈluːʃn]',
        startTime: 12,
        endTime: 18
      },
      {
        id: 's3-4',
        text: 'Every small action contributes directly to preserving our biodiversity.',
        translation: 'Mỗi hành động nhỏ đều đóng góp trực tiếp vào việc bảo tồn đa dạng sinh học.',
        ipa: '[ˈɛvri smɔːl ˈækʃn kənˈtrɪbjuːts dɪˈrɛktli tuː prɪˈzɜːrvɪŋ ˈaʊər ˌbaɪoʊdaɪˈvɜːrsəti]',
        startTime: 19,
        endTime: 25
      }
    ]
  },
  {
    id: 'lesson-4',
    title: 'Du lịch & Ẩm thực: Đặt đồ ăn tại London',
    level: 'Cơ bản',
    topic: 'Casual',
    duration: '0:28',
    sentences: [
      {
        id: 's4-1',
        text: 'Pardon me, could I take a look at the dinner menu please?',
        translation: 'Xin lỗi, tôi có thể xem qua thực đơn bữa tối được không?',
        ipa: '[ˈpɑːrdn miː, kʊd aɪ teɪk ə lʊk æt ðə ˈdɪnər ˈmɛnjuː pliːz]',
        startTime: 0,
        endTime: 4
      },
      {
        id: 's4-2',
        text: 'I would like to try your local specialties tonight.',
        translation: 'Tôi muốn thưởng thức những món đặc sản địa phương của các bạn tối nay.',
        ipa: '[aɪ wʊd laɪk tuː traɪ jɔːr ˈloʊkl ˈspɛʃəltiz təˈnaɪt]',
        startTime: 5,
        endTime: 9
      },
      {
        id: 's4-3',
        text: 'Also, does this traditional dish contain any dairy or seafood?',
        translation: 'Ngoài ra, món ăn truyền thống này có chứa bơ sữa hay hải sản không?',
        ipa: '[ˈɔːlsoʊ, dʌz ðɪs trəˈdɪʃənl dɪʃ kənˈteɪn ˈɛni ˈdɛri ɔːr ˈsiːfuːd]',
        startTime: 10,
        endTime: 14
      }
    ]
  }
];
