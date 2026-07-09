import { Injectable } from '@angular/core';

@Injectable({ providedIn: 'root' })
export class WordAudioService {
  audioUrl(audioKey: string): string {
    let subdir: string;
    if (audioKey.startsWith('bix')) subdir = 'bix';
    else if (audioKey.startsWith('gg')) subdir = 'gg';
    else if (/^[^a-zA-Z]/.test(audioKey[0])) subdir = 'number';
    else subdir = audioKey[0].toLowerCase();
    return `https://media.merriam-webster.com/audio/prons/en/us/mp3/${subdir}/${audioKey}.mp3`;
  }

  play(audioKey: string): void {
    new Audio(this.audioUrl(audioKey)).play();
  }
}
