import { Component, computed, OnInit, signal } from '@angular/core';
import { Router } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressBarModule } from '@angular/material/progress-bar';

import { SpeechModelService } from '../speech-model.service';

type Phase = 'loading' | 'welcome' | 'recording' | 'training' | 'done';

@Component({
  selector: 'app-calibration',
  standalone: true,
  imports: [MatButtonModule, MatCardModule, MatIconModule, MatProgressBarModule],
  templateUrl: './calibration.component.html',
  styleUrl: './calibration.component.scss',
})
export class CalibrationComponent implements OnInit {
  readonly phase = signal<Phase>('loading');
  readonly currentLetterIndex = signal(0);
  readonly samplesRecorded = signal(0);
  readonly trainingProgress = signal(0);
  readonly isRecording = signal(false);
  readonly alreadyCalibrated = signal(false);

  readonly currentLetter = computed(() => this.speechModel.letters[this.currentLetterIndex()]);
  readonly totalLetters = computed(() => this.speechModel.letters.length);
  readonly samplesNeeded = computed(() => this.speechModel.samplesPerLetter);
  readonly progressPct = computed(
    () => (this.currentLetterIndex() / this.totalLetters()) * 100,
  );

  constructor(
    readonly speechModel: SpeechModelService,
    private readonly router: Router,
  ) {}

  async ngOnInit(): Promise<void> {
    const hasModel = await this.speechModel.initialize();
    this.alreadyCalibrated.set(hasModel);
    this.phase.set('welcome');
  }

  startCalibration(): void {
    this.currentLetterIndex.set(0);
    this.samplesRecorded.set(0);
    this.phase.set('recording');
  }

  async recalibrate(): Promise<void> {
    await this.speechModel.clearModel();
    this.startCalibration();
  }

  async recordSample(): Promise<void> {
    if (this.isRecording()) return;
    this.isRecording.set(true);
    try {
      await this.speechModel.recordSample(this.currentLetter());
      const next = this.samplesRecorded() + 1;
      this.samplesRecorded.set(next);

      if (next >= this.samplesNeeded()) {
        await this.advanceLetter();
      }
    } finally {
      this.isRecording.set(false);
    }
  }

  private async advanceLetter(): Promise<void> {
    const nextIndex = this.currentLetterIndex() + 1;
    if (nextIndex >= this.totalLetters()) {
      await this.startTraining();
    } else {
      this.currentLetterIndex.set(nextIndex);
      this.samplesRecorded.set(0);
    }
  }

  private async startTraining(): Promise<void> {
    this.phase.set('training');
    await this.speechModel.train((pct) => this.trainingProgress.set(pct));
    this.phase.set('done');
  }

  goToWords(): void {
    this.router.navigate(['/words']);
  }
}
