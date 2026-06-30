import { Routes } from '@angular/router';
import { WordListComponent } from './words/word-list.component';
import { HomeComponent } from './home/home.component';

export const routes: Routes = [
  { path: '', component: HomeComponent },
  { path: 'words', component: WordListComponent },
  {
    path: 'calibrate',
    loadComponent: () =>
      import('./speech/calibration/calibration.component').then(
        (m) => m.CalibrationComponent,
      ),
  },
];
