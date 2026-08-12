import { Injectable } from '@angular/core';
import { environment } from '../../environments/environment';

@Injectable({ providedIn: 'root' })
export class WordAudioService {
  audioUrl(audioFilePath: string): string {
    return `${environment.apiBaseUrl}/audio/${audioFilePath}`;
  }

  play(audioFilePath: string): void {
    new Audio(this.audioUrl(audioFilePath)).play();
  }
}
