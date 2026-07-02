import { Component, OnInit, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { SpeechModelService } from '../speech/speech-model.service';

@Component({
  selector: 'app-sessions',
  standalone: true,
  imports: [RouterLink, MatButtonModule, MatIconModule],
  templateUrl: './sessions.component.html',
  styleUrl: './sessions.component.scss',
})
export class SessionsComponent implements OnInit {
  readonly calibrated = signal<boolean | null>(null);

  constructor(private speechModel: SpeechModelService) {}

  async ngOnInit(): Promise<void> {
    this.calibrated.set(await this.speechModel.isCalibrated());
  }
}
