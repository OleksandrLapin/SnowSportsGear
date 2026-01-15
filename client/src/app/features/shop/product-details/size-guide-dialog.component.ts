import { Component, inject } from '@angular/core';
import { MAT_DIALOG_DATA, MatDialogModule } from '@angular/material/dialog';
import { MatButton } from '@angular/material/button';
import { ProductSizeGuide } from '../../../shared/models/size-guide';

@Component({
  selector: 'app-size-guide-dialog',
  standalone: true,
  imports: [MatDialogModule, MatButton],
  template: `
    <h2 mat-dialog-title>{{data.title}}</h2>
    <div mat-dialog-content class="space-y-4">
      <div class="overflow-x-auto">
        <table class="w-full text-sm border border-gray-200">
          <thead class="bg-gray-50">
            <tr>
              @for (column of data.columns; track column) {
                <th class="text-left px-3 py-2 font-semibold border-b border-gray-200">{{column}}</th>
              }
            </tr>
          </thead>
          <tbody>
            @for (row of data.rows; track $index) {
              <tr class="border-b border-gray-100">
                @for (cell of row; track $index) {
                  <td class="px-3 py-2 text-gray-700">{{cell}}</td>
                }
              </tr>
            }
          </tbody>
        </table>
      </div>

      @if (data.howToMeasure) {
        <div>
          <h4 class="font-semibold text-gray-900">How to measure</h4>
          <p class="text-sm text-gray-700">{{data.howToMeasure}}</p>
        </div>
      }

      @if (data.fitNotes) {
        <div>
          <h4 class="font-semibold text-gray-900">Fit notes</h4>
          <p class="text-sm text-gray-700">{{data.fitNotes}}</p>
        </div>
      }

      @if (data.disclaimer) {
        <div>
          <h4 class="font-semibold text-gray-900">Disclaimer</h4>
          <p class="text-sm text-gray-700">{{data.disclaimer}}</p>
        </div>
      }

      @if (data.extraNotes?.length) {
        <div>
          <h4 class="font-semibold text-gray-900">Extra notes</h4>
          <ul class="list-disc ml-5 text-sm text-gray-700">
            @for (note of data.extraNotes; track note) {
              <li>{{note}}</li>
            }
          </ul>
        </div>
      }
    </div>
    <div mat-dialog-actions align="end">
      <button mat-stroked-button mat-dialog-close>Close</button>
    </div>
  `
})
export class SizeGuideDialogComponent {
  data = inject<ProductSizeGuide>(MAT_DIALOG_DATA);
}
