import { WordProgressResponse, WordResponse } from '../api/api.generated';

export type MasteryStatus = 'unattempted' | 'in-progress' | 'mastered';

export interface WordMastery {
  word: WordResponse;
  status: MasteryStatus;
  score: number;
  progress: WordProgressResponse | undefined;
}

export function classifyWord(
  word: WordResponse,
  progress: WordProgressResponse | undefined
): WordMastery {
  if (!progress || progress.attemptCount === 0) {
    return { word, status: 'unattempted', score: 0, progress };
  }
  const accuracy = progress.correctCount / progress.attemptCount;
  const mastered = progress.attemptCount > 10 && accuracy > 0.9;
  // Floor of 0.15 so any attempted word is visibly distinct from an
  // unattempted (pure white) one, even before accuracy/volume build up.
  const score = mastered
    ? 1
    : Math.min(0.15 + 0.75 * accuracy * Math.min(progress.attemptCount / 5, 1), 0.95);
  return {
    word,
    status: mastered ? 'mastered' : 'in-progress',
    score,
    progress,
  };
}

export function classifyWords(
  words: WordResponse[],
  progress: WordProgressResponse[]
): WordMastery[] {
  const progressByWordId = new Map(progress.map(p => [p.wordId, p]));
  return words.map(word => classifyWord(word, progressByWordId.get(word.id)));
}
