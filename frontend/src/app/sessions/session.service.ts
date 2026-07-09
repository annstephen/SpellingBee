import { Injectable } from '@angular/core';
import { WordResponse } from '../api/api.generated';

export interface WordAttempt {
  wordId: number;
  word: string;
  userInput: string;
  correct: boolean;
}

export interface SessionState {
  id: 'current';
  wordCount: number;
  words: WordResponse[];
  currentWordIndex: number;
  // letterBuffer was used for the letter-by-letter voice/keyboard input flow,
  // now replaced by typed whole-word input (see SessionsComponent.typedWord).
  attempts: WordAttempt[];
  startedAt: string;
  status: 'playing' | 'complete';
}

const DB_NAME = 'spelling-bee-sessions';
const STORE_NAME = 'sessions';
const DB_VERSION = 1;

@Injectable({ providedIn: 'root' })
export class SessionService {
  private db: IDBDatabase | null = null;

  private openDb(): Promise<IDBDatabase> {
    if (this.db) return Promise.resolve(this.db);
    return new Promise((resolve, reject) => {
      const req = indexedDB.open(DB_NAME, DB_VERSION);
      req.onupgradeneeded = () => {
        req.result.createObjectStore(STORE_NAME, { keyPath: 'id' });
      };
      req.onsuccess = () => {
        this.db = req.result;
        resolve(this.db);
      };
      req.onerror = () => reject(req.error);
    });
  }

  async save(state: SessionState): Promise<void> {
    const db = await this.openDb();
    return new Promise((resolve, reject) => {
      const tx = db.transaction(STORE_NAME, 'readwrite');
      tx.objectStore(STORE_NAME).put(state);
      tx.oncomplete = () => resolve();
      tx.onerror = () => reject(tx.error);
    });
  }

  async load(): Promise<SessionState | null> {
    const db = await this.openDb();
    return new Promise((resolve, reject) => {
      const tx = db.transaction(STORE_NAME, 'readonly');
      const req = tx.objectStore(STORE_NAME).get('current');
      req.onsuccess = () => resolve((req.result as SessionState) ?? null);
      req.onerror = () => reject(req.error);
    });
  }

  async clear(): Promise<void> {
    const db = await this.openDb();
    return new Promise((resolve, reject) => {
      const tx = db.transaction(STORE_NAME, 'readwrite');
      tx.objectStore(STORE_NAME).delete('current');
      tx.oncomplete = () => resolve();
      tx.onerror = () => reject(tx.error);
    });
  }
}
