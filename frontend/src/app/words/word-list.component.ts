import { Component, inject } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { DatePipe } from '@angular/common';
import { WordsClient } from '../api/api.generated';

@Component({
  selector: 'app-word-list',
  standalone: true,
  imports: [DatePipe],
  templateUrl: './word-list.component.html',
})
export class WordListComponent {
  private readonly client = inject(WordsClient);
  readonly words = toSignal(this.client.getAllWords(), { initialValue: [] });

  audioUrl(key: string): string {
    let subdir: string;
    if (key.startsWith('bix')) subdir = 'bix';
    else if (key.startsWith('gg')) subdir = 'gg';
    else if (/^[^a-zA-Z]/.test(key[0])) subdir = 'number';
    else subdir = key[0].toLowerCase();
    return `https://media.merriam-webster.com/audio/prons/en/us/mp3/${subdir}/${key}.mp3`;
  }
}
