import { Injectable, signal } from '@angular/core';
import * as tf from '@tensorflow/tfjs';
import * as speechCommands from '@tensorflow-models/speech-commands';

const LETTERS = 'ABCDEFGHIJKLMNOPQRSTUVWXYZ'.split('');
const SAMPLES_PER_LETTER = 5;
const SAVED_MODEL_KEY = 'indexeddb://spelling-bee-model';
const TRANSFER_NAME = 'spelling-bee';
const PROBABILITY_THRESHOLD = 0.85;

@Injectable({ providedIn: 'root' })
export class SpeechModelService {
  readonly isModelReady = signal(false);
  readonly isBaseModelLoaded = signal(false);

  private baseRecognizer: speechCommands.SpeechCommandRecognizer | null = null;
  private transferRecognizer: speechCommands.TransferSpeechCommandRecognizer | null = null;

  async initialize(): Promise<boolean> {
    this.baseRecognizer = speechCommands.create('BROWSER_FFT');
    await this.baseRecognizer.ensureModelLoaded();
    this.transferRecognizer = this.baseRecognizer.createTransfer(TRANSFER_NAME);
    this.isBaseModelLoaded.set(true);

    try {
      await this.transferRecognizer.load(SAVED_MODEL_KEY);
      this.isModelReady.set(true);
      return true;
    } catch {
      return false;
    }
  }

  get letters(): string[] {
    return LETTERS;
  }

  get samplesPerLetter(): number {
    return SAMPLES_PER_LETTER;
  }

  async recordSample(letter: string): Promise<void> {
    if (!this.transferRecognizer) throw new Error('Model not initialized');
    await this.transferRecognizer.collectExample(letter);
  }

  async train(onProgress: (pct: number) => void): Promise<void> {
    if (!this.transferRecognizer) throw new Error('Model not initialized');
    await this.transferRecognizer.train({
      epochs: 30,
      callback: {
        onEpochEnd: async (epoch: number) => {
          onProgress(Math.round(((epoch + 1) / 30) * 100));
        },
      },
    });
    await this.transferRecognizer.save(SAVED_MODEL_KEY);
    this.isModelReady.set(true);
  }

  async listen(onLetter: (letter: string) => void): Promise<void> {
    if (!this.transferRecognizer || !this.isModelReady()) {
      throw new Error('Model not ready');
    }
    await this.transferRecognizer.listen(
      async (result: speechCommands.SpeechCommandRecognizerResult) => {
        const scores = result.scores as Float32Array;
        const labels = this.transferRecognizer!.wordLabels();
        let maxScore = 0;
        let topLabel = '';
        for (let i = 0; i < scores.length; i++) {
          if (scores[i] > maxScore) {
            maxScore = scores[i];
            topLabel = labels[i];
          }
        }
        if (maxScore >= PROBABILITY_THRESHOLD && topLabel !== '_background_noise_') {
          onLetter(topLabel);
        }
      },
      { probabilityThreshold: PROBABILITY_THRESHOLD },
    );
  }

  async stopListening(): Promise<void> {
    if (this.transferRecognizer?.isListening()) {
      await this.transferRecognizer.stopListening();
    }
  }

  async clearModel(): Promise<void> {
    if (!this.transferRecognizer) return;
    this.transferRecognizer.clearExamples();
    this.isModelReady.set(false);
    try {
      await tf.io.removeModel(SAVED_MODEL_KEY);
    } catch {
      // ignore if not found
    }
  }
}
