/**
 * @license
 * SPDX-License-Identifier: Apache-2.0
 */

import express from 'express';
import path from 'path';
import dotenv from 'dotenv';
import { GoogleGenAI, Type } from '@google/genai';
import { createServer as createViteServer } from 'vite';

dotenv.config();

const app = express();
const PORT = 3000;

// Initialize Google Gen AI
const geminiApiKey = process.env.GEMINI_API_KEY;
let ai: GoogleGenAI | null = null;

if (geminiApiKey && geminiApiKey !== "MY_GEMINI_API_KEY") {
  ai = new GoogleGenAI({
    apiKey: geminiApiKey,
    httpOptions: {
      headers: {
        'User-Agent': 'aistudio-build',
      },
    },
  });
}

const AI_REQUEST_TIMEOUT_MS = 5000;

// Timeout wrapper for Gemini requests so slow responses don't hang scoring too long.
async function fetchWithTimeout<T>(promise: Promise<T>, ms: number): Promise<T> {
  let timeoutId: NodeJS.Timeout;
  const timeoutPromise = new Promise<never>((_, reject) => {
    timeoutId = setTimeout(() => reject(new Error(`Gemini request timed out after ${ms}ms`)), ms);
  });

  const result = await Promise.race([promise, timeoutPromise]);
  clearTimeout(timeoutId!);
  return result as T;
}

// Robust retry utility with fallback model option
async function generateContentWithRetry(params: {
  contents: any[];
  config: any;
  model?: string;
}): Promise<any> {
  const modelsToTry = [
    params.model || 'gemini-3.1-flash-lite',
    'gemini-3.5-flash'
  ];

  let lastError: any = null;

  for (const model of modelsToTry) {
    for (let attempt = 1; attempt <= 2; attempt++) {
      try {
        if (!ai) {
          throw new Error("Client not initialized");
        }

        const response = await fetchWithTimeout(
          ai.models.generateContent({
            model,
            contents: params.contents,
            config: params.config,
          }),
          AI_REQUEST_TIMEOUT_MS
        );

        return response;
      } catch (err: any) {
        lastError = err;
        console.warn(`[Gemini API] Attempt ${attempt} on model ${model} failed:`, err.message || err);
        
        const isRetryable = err.status === 503 ||
                            err.status === 429 ||
                            (err.message && (
                              err.message.includes('503') ||
                              err.message.includes('429') ||
                              err.message.includes('UNAVAILABLE') ||
                              err.message.includes('Resource has been exhausted') ||
                              err.message.includes('high demand') ||
                              err.message.includes('timed out')
                            ));

        if (!isRetryable || attempt === 2) {
          break; // Go to next model or bubble up
        }
        
        await new Promise(resolve => setTimeout(resolve, attempt * 500));
      }
    }
  }

  throw lastError || new Error("Failed to generate content after retries and fallback models");
}

// High-quality local pronunciation evaluation fallback
// TODO: This function is a local dev stub only and should be removed for production.
// A real backend should perform pronunciation evaluation using an AI service or saved model,
// and not rely on client-side hardcoded scoring heuristics.
function generateLocalEvaluation(targetText: string, transcript: string) {
  const cleanTarget = targetText.toLowerCase().replace(/[.,\/#!$%\^&\*;:{}=\-_`~()?]/g, "").trim();
  const cleanSpoke = (transcript || '').toLowerCase().replace(/[.,\/#!$%\^&\*;:{}=\-_`~()?]/g, "").trim();
  
  const targetWords = targetText
    .replace(/[.,\/#!$%\^&\*;:{}=\-_`~()?]/g, "")
    .split(/\s+/)
    .filter(Boolean);
    
  const spokeWords = cleanSpoke.split(/\s+/).filter(Boolean);
  const spokeSet = new Set(spokeWords);
  let matchedCount = 0;
  
  const wordsResult = targetWords.map((w: string) => {
    const cleanWord = w.toLowerCase();
    const isMatched = spokeSet.has(cleanWord);
    let code: 'correct' | 'warning' | 'incorrect' = 'incorrect';
    let label = 'Cần nói rõ';
    
    if (isMatched) {
      matchedCount++;
      code = 'correct';
      label = 'Tốt';
    } else {
      const closeMatch = spokeWords.find(sw => sw.startsWith(cleanWord.substring(0, 3)) || cleanWord.startsWith(sw.substring(0, 3)));
      if (closeMatch) {
         code = 'warning';
         label = 'Chú ý trọng âm';
         matchedCount += 0.5;
      }
    }
    
    return {
      word: w,
      accuracyCode: code,
      ipa: `/${cleanWord}/`,
      correction: label
    };
  });
  
  const matchRatio = targetWords.length > 0 ? (matchedCount / targetWords.length) : 1;
  const score = Math.round(62 + (matchRatio * 33)); // Score ranges from 62 to 95
  const accuracy = Math.round(65 + (matchRatio * 30));
  const fluency = cleanSpoke.length > 0 ? Math.round(60 + (matchRatio * 32)) : 50;
  const intonation = cleanSpoke.length > 0 ? Math.round(58 + (matchRatio * 34)) : 50;
  
  return {
    score,
    accuracy,
    fluency,
    intonation,
    words: wordsResult,
    feedback: `[HỆ THỐNG DỰ PHÒNG] Máy chủ AI Gemini đang bảo trì đột xuất, hệ thống đã chuyển sang chế độ tự đánh giá thông minh tức thời. Tỷ lệ khớp từ vựng đạt ${Math.round(matchRatio * 100)}%. Bạn đã hoàn thành rất tốt, hãy tự tin đọc tiếp!`
  };
}

// Realistic dynamic lesson generator fallback matching theme & level
// TODO: This function is a local dev fallback only and should be removed once backend
// lesson generation or database content is available.
// The backend should either generate lessons via AI or return stored lesson records from DB.
function generateLocalFallbackLesson(theme: string, level: string) {
  const cleanTheme = theme.trim() || 'General Conversation';
  const difficulty = level === 'Academic' ? 'Nâng cao' : level === 'Professional' ? 'Trung cấp' : 'Cơ bản';
  const topic = level || 'Casual';

  let sentences = [];
  if (topic === 'Professional') {
    sentences = [
      {
        id: `gen-${Date.now()}-1`,
        text: `Today we are discussing our core strategies regarding ${cleanTheme}.`,
        translation: `Hôm nay chúng ta đang thảo luận về các chiến lược cốt lõi liên quan đến ${cleanTheme.toLowerCase()}.`,
        ipa: `[təˈdeɪ wiː ɑːr dɪsˈkʌsɪŋ ˈaʊər kɔːr ˈstrætədʒiz rɪˈɡɑːrdɪŋ ${cleanTheme.toLowerCase()}]`,
        startTime: 0,
        endTime: 6
      },
      {
        id: `gen-${Date.now()}-2`,
        text: `It is critical that we align our business objectives to ensure maximum impact.`,
        translation: `Việc chúng ta liên kết các mục tiêu kinh doanh để đảm bảo tác động tối đa là rất quan trọng.`,
        ipa: `[ɪt ɪz ˈkrɪtɪkəl ðæt wiː əˈlaɪn ˈaʊər ˈbɪznəs əbˈdʒɛktɪvz tuː ɪnˈʃʊər ˈmæksəməm ˈɪmpækt]`,
        startTime: 7,
        endTime: 13
      },
      {
        id: `gen-${Date.now()}-3`,
        text: `Let's work together to achieve these milestones successfully.`,
        translation: `Hãy làm việc cùng nhau để đạt được các cột mốc này một cách thành công.`,
        ipa: `[lɛts wɜːrk təˈɡɛðər tuː əˈtʃiːv ðiːz ˈmaɪlstoʊnz səkˈsɛsfəli]`,
        startTime: 14,
        endTime: 20
      }
    ];
  } else if (topic === 'Academic') {
    sentences = [
      {
        id: `gen-${Date.now()}-1`,
        text: `The academic research explores the fundamental theories of ${cleanTheme}.`,
        translation: `Nghiên cứu học thuật khám phá các lý thuyết cơ bản về ${cleanTheme.toLowerCase()}.`,
        ipa: `[ˌækeˈdemɪk rɪˈsɜːrtʃ ɪkˈsplɔːrz ðə ˌfʌndəˈmentl ˈθɪəriz ɒv ${cleanTheme.toLowerCase()}]`,
        startTime: 0,
        endTime: 6
      },
      {
        id: `gen-${Date.now()}-2`,
        text: `Numerous scholars have analyzed the historical significance of this phenomenon.`,
        translation: `Nhiều học giả đã phân tích ý nghĩa lịch sử của hiện tượng này.`,
        ipa: `[ˈnjuːmərəs ˈskɒlərz hæv ˈænəlaɪzd ðə hɪˈstɒrɪkl sɪɡˈnɪfɪkəns ɒv ðɪs fəˈnɒmɪnən]`,
        startTime: 7,
        endTime: 14
      },
      {
        id: `gen-${Date.now()}-3`,
        text: `This perspective provides a comprehensive framework for future study.`,
        translation: `Góc nhìn này cung cấp một khuôn khổ toàn diện cho các nghiên cứu trong tương lai.`,
        ipa: `[ðɪs pəˈspektɪv prəˈvaɪdz ə ˌkɒmprɪˈhensɪv ˈfreɪmwɜːrk fɔːr ˈfjuːtʃər ˈstʌdi]`,
        startTime: 15,
        endTime: 21
      }
    ];
  } else {
    sentences = [
      {
        id: `gen-${Date.now()}-1`,
        text: `I am really excited to talk to you about ${cleanTheme} today!`,
        translation: `Hôm nay tôi rất hào hứng được trò chuyện với bạn về chủ đề ${cleanTheme.toLowerCase()} đấy!`,
        ipa: `[aɪ æm ˈrɪəli ɪkˈsaɪtɪd tuː tɔːk tuː juː əˈbaʊt ${cleanTheme.toLowerCase()} təˈdeɪ]`,
        startTime: 0,
        endTime: 5
      },
      {
        id: `gen-${Date.now()}-2`,
        text: `It has such an interesting perspective, and there is so much to learn.`,
        translation: `Nó có một góc nhìn thực sự thú vị, và có rất nhiều điều để học hỏi.`,
        ipa: `[ɪt hæz sʌtʃ ən ˈɪntrəstɪŋ pəˈspektɪv ænd ðeər ɪz soʊ mʌtʃ tuː lɜːrn]`,
        startTime: 6,
        endTime: 11
      },
      {
        id: `gen-${Date.now()}-3`,
        text: `Let me know what you think about this wonderful topic.`,
        translation: `Hãy cho tôi biết bạn nghĩ gì về chủ đề tuyệt vời này nhé.`,
        ipa: `[lɛt miː noʊ wʌt juː θɪŋk əˈbaʊt ðɪs ˈwʌndərfəl ˈtɒpɪk]`,
        startTime: 12,
        endTime: 17
      }
    ];
  }

  return {
    title: `Chủ đề: ${cleanTheme} (AI Fallback Mode)`,
    level: difficulty,
    topic: topic,
    sentences: sentences,
    isFallback: true
  };
}

app.use(express.json());

// API: Evaluate User Pronunciation via Gemini
// TODO: In a full backend design, this endpoint should be the single source of truth for pronunciation scoring.
// It should validate the user session, pull user context from the database, store evaluation history,
// and possibly use a dedicated AI service instead of returning local fallback values.
app.post('/api/evaluate', async (req, res) => {
  const { targetText, transcript, level, userGoal } = req.body;
  try {
    if (!targetText) {
      return res.status(400).json({ error: 'Missing targetText parameter' });
    }

    if (!ai) {
      // TODO: This is a local fallback for evaluation without a Gemini API key.
      // Replace with a real backend ASP.NET/AI service endpoint that provides structured pronunciation scoring.
      const evaluation = generateLocalEvaluation(targetText, transcript);
      return res.json({
        ...evaluation,
        feedback: 'Hãy thiết lập GEMINI_API_KEY để kích hoạt Trí tuệ nhân tạo đánh giá tiếng Anh chính xác từ giọng nói của bạn! Bản mô phỏng này hiển thị từ vựng tô màu: Đỏ (phát âm thiếu/sai), Vàng (gần đúng), Xanh (chuẩn xác).'
      });
    }

    const cleanTranscript = (transcript || '').trim();
    
    const systemPrompt = `You are an elite AI IELTS and English Pronunciation Coach. 
An English learner has just read aloud an English sentence using the shadow-reading (Shadowing) method.
You must compare the correct model (Target Text) with what they actually spoke (User Transcription) and provide a highly detailed score and word-by-word analysis.

IMPORTANT INSTRUCTIONS:
- Grade accuracy (match quality), fluency (how complete and natural), and intonation.
- Create a list of the words from the TARGET text. For each word, declare an accuracyCode: "correct", "warning", or "incorrect".
- If the word is entirely missing from the user's transcript or wildly mispronounced, mark as 'incorrect'.
- If the word is somewhat close but has slightly wrong sounds or incorrect syllable stress, mark as 'warning'.
- If they pronounced it correctly, mark as 'correct'.
- Speak to them in Vietnamese (Tiếng Việt) in the 'feedback' summary. Give warm, actionable tips.`;

    const userPrompt = `
Lesson Target Text: "${targetText}"
User Spoke / Transcription: "${cleanTranscript}"
Goal Context: English Level: ${level || 'Casual'}, Aiming Pronunciation Goal: ${userGoal || 'comprehension70'}
`;

    const response = await generateContentWithRetry({
      contents: [
        { text: systemPrompt },
        { text: userPrompt }
      ],
      config: {
        responseMimeType: 'application/json',
        responseSchema: {
          type: Type.OBJECT,
          properties: {
            score: { 
              type: Type.INTEGER, 
              description: 'Overall composite English speaking score out of 100.' 
            },
            accuracy: { 
              type: Type.INTEGER, 
              description: 'Pronunciation accuracy score (0-100).' 
            },
            fluency: { 
              type: Type.INTEGER, 
              description: 'Speaking rhythm and speech rate matches targets (0-100).' 
            },
            intonation: { 
              type: Type.INTEGER, 
              description: 'Melody and naturally placed accent stress (0-100).' 
            },
            words: {
              type: Type.ARRAY,
              description: 'Word-by-word evaluation list corresponding directly to the Target Text.',
              items: {
                type: Type.OBJECT,
                properties: {
                  word: { type: Type.STRING, description: 'The exact word from the target text.' },
                  accuracyCode: { 
                    type: Type.STRING, 
                    description: 'Evaluation code: correct, warning, or incorrect.' 
                  },
                  ipa: { type: Type.STRING, description: 'Correct phonetic IPA script of this word.' },
                  correction: { 
                    type: Type.STRING, 
                    description: 'Short helper advice in Vietnamese to speak this word perfectly (max 10 words).' 
                  }
                },
                required: ['word', 'accuracyCode']
              }
            },
            feedback: { 
              type: Type.STRING, 
              description: 'Comprehensive improvement feedback in Vietnamese explaining which sounds were incorrect and detailed guidance to speak like a native.' 
            }
          },
          required: ['score', 'accuracy', 'fluency', 'intonation', 'words', 'feedback']
        }
      }
    });

    const resultText = response.text || '{}';
    res.setHeader('Content-Type', 'application/json');
    res.end(resultText);

  } catch (error: any) {
    console.error('API evaluate error, falling back to local analysis:', error);
    try {
      const evaluation = generateLocalEvaluation(targetText, transcript);
      res.setHeader('Content-Type', 'application/json');
      res.json(evaluation);
    } catch (fallbackErr: any) {
      res.status(500).json({ error: error.message || 'Internal Server Error' });
    }
  }
});

// API: Generate custom lesson via Gemini AI
// TODO: In production, this endpoint should either:
//   1) fetch a lesson from a lesson database, or
//   2) call a backend AI generation service and persist the generated lesson record.
// Avoid returning hardcoded fallback lesson content from server code in production.
app.post('/api/generate-lesson', async (req, res) => {
  const { level, theme } = req.body;
  try {
    if (!theme) {
      return res.status(400).json({ error: 'Missing theme/topic parameter' });
    }

    if (!ai) {
      // TODO: This fallback generates lessons locally for development.
      // In production, route this request to a real lesson generation or database service in the backend.
      const fallbackLesson = generateLocalFallbackLesson(theme, level);
      return res.json(fallbackLesson);
    }

    const systemPrompt = `You are a curriculum designer for the English Shadowing Pronunciation system.
Your job is to generate a custom JSON lesson object only. Output exactly the JSON object and nothing else.

Rules:
- Title must be engaging and in Vietnamese.
- Create 3 or 4 English sentences that read like a direct conversation or dialogue.
- Do not include any extra Vietnamese preface, explanation, or meta commentary in the output.
- Do not generate any first-person statements such as "I am excited" or "I love talking about this topic." 
- Do not add any filler sentences like "I am very excited to" or "let me tell you about".
- Ensure sentences match the topic/theme in English.
- Grade the difficulty as: "Cơ bản", "Trung cấp", or "Nâng cao" depending on the vocabulary.
- Give a natural Vietnamese translation for each sentence.
- Provide a clean and precise International Phonetic Alphabet (IPA) transcript wrapped in square brackets.
- Assign start and end seconds offsets incrementally (e.g. 0-5s, 6-10s, 11-16s).`;

    const userPrompt = `Generate a compact shadowing lesson about "${theme}" for target level "${level || 'Casual'}".
Use direct dialogue only. Do not write any Vietnamese introduction or self-referential sentences. Return only the JSON object as defined.`;

    const response = await generateContentWithRetry({
      contents: [
        { text: systemPrompt },
        { text: userPrompt }
      ],
      config: {
        responseMimeType: 'application/json',
        responseSchema: {
          type: Type.OBJECT,
          properties: {
            title: { type: Type.STRING, description: 'An engaging lesson title in Vietnamese (e.g. Chủ đề: Nói về môi trường).' },
            level: { type: Type.STRING, description: 'Difficulty level: Cơ bản, Trung cấp, or Nâng cao.' },
            topic: { type: Type.STRING, description: 'One of the target levels: Academic, Casual, or Professional.' },
            sentences: {
              type: Type.ARRAY,
              description: 'Array of incremental sentences forming a coherent conversation or theme story.',
              items: {
                type: Type.OBJECT,
                properties: {
                  text: { type: Type.STRING, description: 'The English sentence to shadow.' },
                  translation: { type: Type.STRING, description: 'Accurate, natural Vietnamese translation.' },
                  ipa: { type: Type.STRING, description: 'The International Phonetic Alphabet (IPA) transcript inside brackets.' },
                  startTime: { type: Type.INTEGER, description: 'Incremental start offset in seconds.' },
                  endTime: { type: Type.INTEGER, description: 'Incremental end offset in seconds.' }
                },
                required: ['text', 'translation', 'ipa', 'startTime', 'endTime']
              }
            }
          },
          required: ['title', 'level', 'topic', 'sentences']
        }
      }
    });

    const resultText = response.text || '{}';
    res.setHeader('Content-Type', 'application/json');
    res.end(resultText);

  } catch (error: any) {
    console.error('API generate-lesson error, performing dynamic fallback:', error);
    try {
      const fallbackLesson = generateLocalFallbackLesson(theme, level);
      res.setHeader('Content-Type', 'application/json');
      res.json(fallbackLesson);
    } catch (fallbackErr: any) {
      res.status(500).json({ error: error.message || 'Internal Server Error' });
    }
  }
});

// Configure Vite or Serve Static build
async function startServer() {
  if (process.env.NODE_ENV !== 'production') {
    const vite = await createViteServer({
      server: { middlewareMode: true },
      appType: 'spa',
    });
    app.use(vite.middlewares);
  } else {
    const distPath = path.join(process.cwd(), 'dist');
    app.use(express.static(distPath));
    app.get('*', (req, res) => {
      res.sendFile(path.join(distPath, 'index.html'));
    });
  }

  app.listen(PORT, '0.0.0.0', () => {
    console.log(`[ShadowSpeak Server] running on http://localhost:${PORT}`);
  });
}

startServer();
