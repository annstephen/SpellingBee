import { Component, computed, inject } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { forkJoin, switchMap, timer } from 'rxjs';
import { RouterLink, RouterOutlet } from '@angular/router';
import { MatToolbarModule } from '@angular/material/toolbar';
import { MatButtonModule } from '@angular/material/button';
import { WordsClient, WordResponse, WordProgressResponse } from './api/api.generated';
import { classifyWords, WordMastery } from './shared/word-mastery';

@Component({
  selector: 'app-root',
  imports: [RouterLink, RouterOutlet, MatToolbarModule, MatButtonModule],
  templateUrl: './app.html',
  styleUrl: './app.scss',
})
export class App {
  private readonly wordsClient = inject(WordsClient);

  private readonly wordsAndProgress = toSignal(
    timer(0, 5000).pipe(
      switchMap(() =>
        forkJoin({
          words: this.wordsClient.getAllWords(),
          progress: this.wordsClient.getAllProgress(),
        })
      )
    ),
    { initialValue: { words: [] as WordResponse[], progress: [] as WordProgressResponse[] } }
  );

  readonly totalWords = computed(() => this.wordsAndProgress().words.length);

  readonly wordMastery = computed<WordMastery[]>(() => {
    const { words, progress } = this.wordsAndProgress();
    const items = classifyWords(words, progress);

    // Attempted words (in-progress or mastered) first, unattempted last.
    return items.sort((a, b) => {
      const aAttempted = a.status !== 'unattempted';
      const bAttempted = b.status !== 'unattempted';
      return aAttempted === bAttempted ? 0 : aAttempted ? -1 : 1;
    });
  });

  readonly masteredCount = computed(
    () => this.wordMastery().filter(m => m.status === 'mastered').length
  );

  masteryColor(score: number): string {
    const r = Math.round(255 + (76 - 255) * score);
    const g = Math.round(255 + (175 - 255) * score);
    const b = Math.round(255 + (80 - 255) * score);
    return `rgb(${r}, ${g}, ${b})`;
  }

  masteryTooltip(m: WordMastery): string {
    const attempts = m.progress?.attemptCount ?? 0;
    const correct = m.progress?.correctCount ?? 0;
    return `${m.word.text} — ${correct}/${attempts} correct`;
  }
}
