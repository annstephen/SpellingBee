import { Component, OnDestroy, OnInit, computed, inject, signal } from '@angular/core';
// RouterLink was used for the /calibrate link in the disabled not-calibrated case below.
// import { RouterLink } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { WordsClient } from '../api/api.generated';
// Voice recognition is disabled for now — sessions are typed instead of spoken.
// See onLetter/startListening below (kept, but not invoked from the active flow).
// import { SpeechModelService } from '../speech/speech-model.service';
import { WordAudioService } from '../words/word-audio.service';
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
      await this.loadWordCount();
      this.phase.set('configure');
    }
  }

  private async loadWordCount(): Promise<void> {
    const words = await firstValueFrom(this.wordsClient.getAllWords());
    this.maxWords.set(words.length);
    this.wordCount.set(Math.min(10, words.length));
  }

  setWordCount(value: string): void {
    const n = parseInt(value, 10);
    if (!isNaN(n)) this.wordCount.set(Math.max(1, Math.min(n, this.maxWords())));
  }

  async startSession(): Promise<void> {
    this.phase.set('loading');
    const allWords = await firstValueFrom(this.wordsClient.getAllWords());
    const shuffled = this.shuffle([...allWords]);
    const selected = shuffled.slice(0, this.wordCount());

    const state: SessionState = {
      id: 'current',
      wordCount: this.wordCount(),
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
    await this.loadWordCount();
    this.session.set(null);
    this.typedWord.set('');
    this.phase.set('configure');
  }

  playCurrentWord(): void {
    const word = this.currentWord();
    if (word?.audioKey) this.wordAudio.play(word.audioKey);
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
    await this.loadWordCount();
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
  }

  private shuffle<T>(arr: T[]): T[] {
    for (let i = arr.length - 1; i > 0; i--) {
      const j = Math.floor(Math.random() * (i + 1));
      [arr[i], arr[j]] = [arr[j], arr[i]];
    }
    return arr;
  }
}
