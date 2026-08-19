import { Component, OnDestroy, OnInit, computed, inject, signal } from '@angular/core';
// RouterLink was used for the /calibrate link in the disabled not-calibrated case below.
// import { RouterLink } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatIconModule } from '@angular/material/icon';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { WordResponse, WordsClient } from '../api/api.generated';
// Voice recognition is disabled for now — sessions are typed instead of spoken.
// See onLetter/startListening below (kept, but not invoked from the active flow).
// import { SpeechModelService } from '../speech/speech-model.service';
import { WordAudioService } from '../words/word-audio.service';
import { classifyWords } from '../shared/word-mastery';
import { SessionService, SessionState, WordAttempt } from './session.service';

type Phase =
  | 'checking'
  // | 'not-calibrated' // unused while voice calibration is hidden
  | 'configure'
  | 'resume-prompt'
  | 'loading'
  | 'playing'
  | 'complete';

@Component({
  selector: 'app-sessions',
  standalone: true,
  imports: [
    // RouterLink,
    MatButtonModule,
    MatCardModule,
    MatCheckboxModule,
    MatIconModule,
    MatFormFieldModule,
    MatInputModule,
    MatProgressSpinnerModule,
  ],
  templateUrl: './sessions.component.html',
  styleUrl: './sessions.component.scss',
})
export class SessionsComponent implements OnInit, OnDestroy {
  // private readonly speechModel = inject(SpeechModelService);
  private readonly wordsClient = inject(WordsClient);
  private readonly sessionService = inject(SessionService);
  readonly wordAudio = inject(WordAudioService);

  readonly phase = signal<Phase>('checking');
  readonly maxWords = signal(0);
  readonly wordCount = signal(10);
  readonly seenBeforeCount = signal(0);
  readonly includeMastered = signal(false);
  readonly masteredCount = signal(0);
  readonly availableNew = signal(0);
  readonly availableInProgress = signal(0);
  readonly availableMastered = signal(0);
  readonly session = signal<SessionState | null>(null);
  readonly typedWord = signal('');
  readonly feedbackVisible = signal(false);
  readonly meaningVisible = signal(false);
  readonly originVisible = signal(false);
  readonly sentenceVisible = signal(false);
  readonly partOfSpeechVisible = signal(false);

  readonly currentWord = computed(() => {
    const s = this.session();
    return s ? s.words[s.currentWordIndex] : null;
  });

  readonly lastAttempt = computed(() => {
    const s = this.session();
    return s && s.attempts.length > 0 ? s.attempts[s.attempts.length - 1] : null;
  });

  readonly correctCount = computed(
    () => this.session()?.attempts.filter(a => a.correct).length ?? 0
  );

  readonly mistakes = computed(
    () => this.session()?.attempts.filter(a => !a.correct) ?? []
  );

  readonly newWordsCount = computed(() =>
    Math.max(0, this.wordCount() - this.seenBeforeCount() - this.masteredCount())
  );

  readonly masteredCheckboxDisabled = computed(() => this.availableMastered() === 0);
  readonly seenBeforeFieldDisabled = computed(() => this.availableInProgress() === 0);

  async ngOnInit(): Promise<void> {
    // Voice calibration gate is disabled while the calibration page is hidden.
    // const calibrated = await this.speechModel.isCalibrated();
    // if (!calibrated) {
    //   this.phase.set('not-calibrated');
    //   return;
    // }
    // if (!this.speechModel.isModelReady()) {
    //   await this.speechModel.initialize();
    // }

    const saved = await this.sessionService.load();
    if (saved?.status === 'playing') {
      this.session.set(saved);
      this.phase.set('resume-prompt');
    } else {
      await this.loadComposition();
      this.phase.set('configure');
    }
  }

  private async loadComposition(): Promise<void> {
    const [words, progress] = await Promise.all([
      firstValueFrom(this.wordsClient.getAllWords()),
      firstValueFrom(this.wordsClient.getAllProgress()),
    ]);
    this.maxWords.set(words.length);
    this.wordCount.set(Math.min(10, words.length));

    const classified = classifyWords(words, progress);
    this.availableNew.set(classified.filter(m => m.status === 'unattempted').length);
    this.availableInProgress.set(classified.filter(m => m.status === 'in-progress').length);
    this.availableMastered.set(classified.filter(m => m.status === 'mastered').length);

    // Reset sub-selections against the freshly loaded bucket sizes.
    this.seenBeforeCount.set(0);
    this.masteredCount.set(0);
    this.includeMastered.set(false);
  }

  setWordCount(value: string): void {
    const n = parseInt(value, 10);
    if (isNaN(n)) return;
    this.wordCount.set(Math.max(1, Math.min(n, this.maxWords())));
    this.clampSubCounts();
  }

  setSeenBeforeCount(value: string): void {
    const n = parseInt(value, 10);
    if (isNaN(n)) return;
    const maxAllowed = Math.min(this.availableInProgress(), this.wordCount() - this.masteredCount());
    this.seenBeforeCount.set(Math.max(0, Math.min(n, Math.max(0, maxAllowed))));
  }

  setMasteredCount(value: string): void {
    const n = parseInt(value, 10);
    if (isNaN(n)) return;
    const maxAllowed = Math.min(this.availableMastered(), this.wordCount() - this.seenBeforeCount());
    this.masteredCount.set(Math.max(0, Math.min(n, Math.max(0, maxAllowed))));
  }

  toggleIncludeMastered(): void {
    this.includeMastered.update(v => !v);
    if (!this.includeMastered()) {
      this.masteredCount.set(0);
    } else if (this.masteredCount() === 0 && this.availableMastered() > 0) {
      const remaining = this.wordCount() - this.seenBeforeCount();
      this.masteredCount.set(Math.max(0, Math.min(1, this.availableMastered(), remaining)));
    }
  }

  private clampSubCounts(): void {
    const total = this.wordCount();
    this.masteredCount.set(Math.max(0, Math.min(this.masteredCount(), this.availableMastered(), total)));
    const remaining = total - this.masteredCount();
    this.seenBeforeCount.set(Math.max(0, Math.min(this.seenBeforeCount(), this.availableInProgress(), remaining)));
  }

  async startSession(): Promise<void> {
    this.phase.set('loading');
    const [allWords, progress] = await Promise.all([
      firstValueFrom(this.wordsClient.getAllWords()),
      firstValueFrom(this.wordsClient.getAllProgress()),
    ]);
    const classified = classifyWords(allWords, progress);

    const newBucket = this.shuffle(classified.filter(m => m.status === 'unattempted').map(m => m.word));
    const inProgressBucket = this.shuffle(classified.filter(m => m.status === 'in-progress').map(m => m.word));
    const masteredBucket = this.shuffle(classified.filter(m => m.status === 'mastered').map(m => m.word));

    const total = this.wordCount();
    const requestedMastered = this.includeMastered() ? this.masteredCount() : 0;
    const requestedSeenBefore = this.seenBeforeCount();

    // Take up to the requested amount from each exclusive bucket.
    const takenMastered = masteredBucket.slice(0, requestedMastered);
    const takenSeenBefore = inProgressBucket.slice(0, requestedSeenBefore);

    // Remaining slots after mastered+seen-before are filled from new words.
    const remainingSlots = total - takenMastered.length - takenSeenBefore.length;
    const takenNew = newBucket.slice(0, remainingSlots);

    // If the new bucket itself falls short, backfill from unused
    // in-progress words, then unused mastered words, in that order.
    let stillNeeded = remainingSlots - takenNew.length;
    if (stillNeeded > 0) {
      const extraInProgress = inProgressBucket.slice(
        takenSeenBefore.length,
        takenSeenBefore.length + stillNeeded
      );
      takenSeenBefore.push(...extraInProgress);
      stillNeeded -= extraInProgress.length;
    }
    if (stillNeeded > 0) {
      const extraMastered = masteredBucket.slice(takenMastered.length, takenMastered.length + stillNeeded);
      takenMastered.push(...extraMastered);
      stillNeeded -= extraMastered.length;
    }
    // If stillNeeded is still > 0, the whole library is short of `total`
    // words — the session will simply have fewer than requested.

    const selected = this.shuffle<WordResponse>([...takenNew, ...takenSeenBefore, ...takenMastered]);

    const state: SessionState = {
      id: 'current',
      wordCount: selected.length,
      words: selected,
      currentWordIndex: 0,
      attempts: [],
      startedAt: new Date().toISOString(),
      status: 'playing',
    };
    this.session.set(state);
    this.typedWord.set('');
    this.feedbackVisible.set(false);
    this.meaningVisible.set(false);
    this.originVisible.set(false);
    this.sentenceVisible.set(false);
    this.partOfSpeechVisible.set(false);
    await this.sessionService.save(state);

    this.playCurrentWord();
    // this.startListening();
    this.phase.set('playing');
  }

  async resumeSession(): Promise<void> {
    this.feedbackVisible.set(false);
    this.typedWord.set('');
    this.meaningVisible.set(false);
    this.originVisible.set(false);
    this.sentenceVisible.set(false);
    this.partOfSpeechVisible.set(false);
    this.playCurrentWord();
    // this.startListening();
    this.phase.set('playing');
  }

  async discardAndStartNew(): Promise<void> {
    await this.sessionService.clear();
    await this.loadComposition();
    this.session.set(null);
    this.typedWord.set('');
    this.phase.set('configure');
  }

  playCurrentWord(): void {
    const word = this.currentWord();
    if (word?.audioFilePath) this.wordAudio.play(word.audioFilePath);
  }

  setTypedWord(value: string): void {
    this.typedWord.set(value);
  }

  toggleMeaning(): void {
    this.meaningVisible.update(v => !v);
  }

  toggleOrigin(): void {
    this.originVisible.update(v => !v);
  }

  toggleSentence(): void {
    this.sentenceVisible.update(v => !v);
  }

  togglePartOfSpeech(): void {
    this.partOfSpeechVisible.update(v => !v);
  }

  async submitWord(): Promise<void> {
    if (this.feedbackVisible() || !this.typedWord().trim()) return;
    await this.evaluateWord(this.typedWord().trim());
  }

  async nextWord(): Promise<void> {
    const s = this.session()!;
    const nextIndex = s.currentWordIndex + 1;

    if (nextIndex >= s.wordCount) {
      const updated: SessionState = {
        ...s,
        currentWordIndex: nextIndex,
        status: 'complete',
      };
      this.session.set(updated);
      await this.sessionService.save(updated);
      // await this.speechModel.stopListening();
      this.phase.set('complete');
      return;
    }

    const updated: SessionState = { ...s, currentWordIndex: nextIndex };
    this.session.set(updated);
    this.typedWord.set('');
    this.feedbackVisible.set(false);
    this.meaningVisible.set(false);
    this.originVisible.set(false);
    this.sentenceVisible.set(false);
    this.partOfSpeechVisible.set(false);
    await this.sessionService.save(updated);
    this.playCurrentWord();
  }

  async newSession(): Promise<void> {
    await this.sessionService.clear();
    this.session.set(null);
    this.typedWord.set('');
    this.feedbackVisible.set(false);
    this.meaningVisible.set(false);
    this.originVisible.set(false);
    this.sentenceVisible.set(false);
    this.partOfSpeechVisible.set(false);
    await this.loadComposition();
    this.phase.set('configure');
  }

  async ngOnDestroy(): Promise<void> {
    // await this.speechModel.stopListening();
  }

  // Voice recognition is disabled for now. Kept for when the calibration
  // flow is re-enabled — wire this back up alongside typed input if desired.
  // private startListening(): void {
  //   this.zone.runOutsideAngular(() => {
  //     this.speechModel
  //       .listen(letter => this.zone.run(() => this.onLetter(letter)))
  //       .catch(err => console.error('[SessionsComponent] Failed to start listening:', err));
  //   });
  // }
  //
  // private onLetter(letter: string): void {
  //   ...
  // }

  private async evaluateWord(input: string): Promise<void> {
    const word = this.currentWord()!;
    const s = this.session()!;
    const userInput = input.toLowerCase();
    const correct = userInput === word.text.toLowerCase();

    const attempt: WordAttempt = { wordId: word.id, word: word.text, userInput, correct };
    const updated: SessionState = {
      ...s,
      attempts: [...s.attempts, attempt],
    };
    this.session.set(updated);
    await this.sessionService.save(updated);
    this.feedbackVisible.set(true);

    try {
      await firstValueFrom(this.wordsClient.recordAttempt({ wordId: word.id, correct }));
    } catch (err) {
      console.error('[SessionsComponent] Failed to record word progress:', err);
    }
  }

  private shuffle<T>(arr: T[]): T[] {
    for (let i = arr.length - 1; i > 0; i--) {
      const j = Math.floor(Math.random() * (i + 1));
      [arr[i], arr[j]] = [arr[j], arr[i]];
    }
    return arr;
  }
}
