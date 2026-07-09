import { Injectable, signal } from '@angular/core';
import * as tf from '@tensorflow/tfjs';
import * as speechCommands from '@tensorflow-models/speech-commands';

const LETTERS = 'ABCDEFGHIJKLMNOPQRSTUVWXYZ'.split('');
const SAMPLES_PER_LETTER = 12;
const TRANSFER_NAME = 'spelling-bee';
const PROBABILITY_THRESHOLD = 0.65;
// speech-commands canonical path (SAVE_PATH_PREFIX + TRANSFER_NAME) — used for isCalibrated / clearModel.
// Do NOT pass this as an argument to save()/load(): the library only persists word labels to
// localStorage when called without an explicit key.
const CANONICAL_MODEL_KEY = `indexeddb://tfjs-speech-commands-model/${TRANSFER_NAME}`;

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
      await this.transferRecognizer.load();
      this.isModelReady.set(true);
      return true;
    } catch (err) {
      console.error('[SpeechModelService] Failed to load transfer model:', err);
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
    await this.transferRecognizer.save();
    this.isModelReady.set(true);
  }

  async listen(onLetter: (letter: string) => void): Promise<void> {
    if (!this.transferRecognizer || !this.isModelReady()) {
      throw new Error('Model not ready');
    }
    await this.transferRecognizer.listen(
      async (result: speechCommands.SpeechCommandRecognizerResult) => {
        try {
          // result.scores may be Float32Array or Float32Array[] (when overlapFactor > 0)
          const raw = result.scores;
          if (!raw) return;
          const scores: Float32Array = Array.isArray(raw)
            ? (raw as Float32Array[])[raw.length - 1]
            : (raw as Float32Array);
          if (!scores) return;

          const labels = this.transferRecognizer!.wordLabels() ?? LETTERS;
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
        } catch (err) {
          // Keep the loop alive, but surface the error instead of swallowing it silently
          console.error('[SpeechModelService] Error processing audio frame:', err);
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

  async isCalibrated(): Promise<boolean> {
    if (this.isModelReady()) return true;
    try {
      const models = await tf.io.listModels();
      return CANONICAL_MODEL_KEY in models;
    } catch {
      return false;
    }
  }

  async clearModel(): Promise<void> {
    if (!this.transferRecognizer) return;
    try {
      this.transferRecognizer.clearExamples();
    } catch {
      // no in-memory examples to clear (model was loaded from storage)
    }
    // When a model is loaded from IndexedDB, the library sets `model` but never sets
    // `secondLastBaseDenseLayer`. Nulling `model` forces train() to call
    // createTransferModelFromBaseModel() which sets both correctly.
    (this.transferRecognizer as any).model = null;
    this.isModelReady.set(false);
    try {
      await tf.io.removeModel(CANONICAL_MODEL_KEY);
    } catch {
      // ignore if not found
    }
  }
}
