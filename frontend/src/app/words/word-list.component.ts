import { Component, computed, inject, signal } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { DatePipe } from '@angular/common';
import { BehaviorSubject, switchMap } from 'rxjs';
import { finalize } from 'rxjs/operators';
import { ApiException, WordsClient } from '../api/api.generated';
import { MatTableModule } from '@angular/material/table';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatCardModule } from '@angular/material/card';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';

@Component({
  selector: 'app-word-list',
  standalone: true,
  imports: [
    DatePipe,
    MatTableModule,
    MatCheckboxModule,
    MatButtonModule,
    MatIconModule,
    MatFormFieldModule,
    MatInputModule,
    MatCardModule,
    MatTooltipModule,
    MatProgressSpinnerModule,
  ],
  templateUrl: './word-list.component.html',
  styleUrl: './word-list.component.scss',
})
export class WordListComponent {
  private readonly client = inject(WordsClient);

  private readonly refresh$ = new BehaviorSubject<void>(undefined);
  readonly words = toSignal(
    this.refresh$.pipe(switchMap(() => this.client.getAllWords())),
    { initialValue: [] }
  );

  private readonly selectedIds = signal(new Set<number>());
  readonly selectedCount = computed(() => this.selectedIds().size);
  readonly allSelected = computed(() => {
    const words = this.words();
    return words.length > 0 && words.every(w => this.selectedIds().has(w.id));
  });

  readonly newWordText = signal('');
  readonly addWordError = signal<string | null>(null);
  readonly importResult = signal<string | null>(null);
  readonly importing = signal(false);

  readonly displayedColumns: string[] = [
    'select', 'text', 'partOfSpeech', 'definition', 'importedAt', 'audio', 'actions',
  ];

  addWord(): void {
    const text = this.newWordText().trim();
    if (!text) return;
    this.addWordError.set(null);
    this.client.addWord({ text }).subscribe({
      next: () => {
        this.newWordText.set('');
        this.refresh$.next();
      },
      error: (err: ApiException) => {
        this.addWordError.set(err.result?.detail ?? 'Could not add word. Please try again.');
      },
    });
  }

  importCsv(event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    if (!file) return;
    this.importing.set(true);
    this.client.importWords({ data: file, fileName: file.name })
      .pipe(finalize(() => this.importing.set(false)))
      .subscribe(result => {
        const failedDetail = result.failedWords?.length
          ? ` (${result.failedWords.join(', ')})`
          : '';
        this.importResult.set(`Imported: ${result.imported}, Skipped: ${result.skipped}, Failed: ${result.failed}${failedDetail}`);
        input.value = '';
        this.refresh$.next();
      });
  }

  isSelected(id: number): boolean {
    return this.selectedIds().has(id);
  }

  toggleSelection(id: number): void {
    this.selectedIds.update(set => {
      const next = new Set(set);
      next.has(id) ? next.delete(id) : next.add(id);
      return next;
    });
  }

  toggleSelectAll(): void {
    if (this.allSelected()) {
      this.selectedIds.set(new Set());
    } else {
      this.selectedIds.set(new Set(this.words().map(w => w.id)));
    }
  }

  deleteOne(id: number): void {
    if (!confirm('Delete this word?')) return;
    this.client.deleteWord(id).subscribe(() => {
      this.selectedIds.update(s => { const n = new Set(s); n.delete(id); return n; });
      this.refresh$.next();
    });
  }

  deleteSelected(): void {
    const ids = [...this.selectedIds()];
    if (!confirm(`Delete ${ids.length} word(s)?`)) return;
    this.client.deleteWords({ ids }).subscribe(() => {
      this.selectedIds.set(new Set());
      this.refresh$.next();
    });
  }

  clearAll(): void {
    if (!confirm('Delete ALL words? This cannot be undone.')) return;
    this.client.clearAllWords().subscribe(() => {
      this.selectedIds.set(new Set());
      this.refresh$.next();
    });
  }

  audioUrl(key: string): string {
    let subdir: string;
    if (key.startsWith('bix')) subdir = 'bix';
    else if (key.startsWith('gg')) subdir = 'gg';
    else if (/^[^a-zA-Z]/.test(key[0])) subdir = 'number';
    else subdir = key[0].toLowerCase();
    return `https://media.merriam-webster.com/audio/prons/en/us/mp3/${subdir}/${key}.mp3`;
  }

  playAudio(key: string): void {
    new Audio(this.audioUrl(key)).play();
  }
}
