import { CommonModule } from '@angular/common';
import { Component, OnInit, inject } from '@angular/core';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { AbstractControl, FormArray, FormBuilder, FormControl, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { ProductSizeGuide, SizeGuideType } from '../../shared/models/size-guide';
import { getDefaultSizeGuideForType } from '../../shared/utils/size-guides';

type SizeGuideDialogData = {
  guide: ProductSizeGuide | null;
  productType?: string | null;
};

@Component({
  selector: 'app-admin-size-guide-dialog',
  standalone: true,
  imports: [
    CommonModule,
    MatDialogModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatButtonModule,
    MatIconModule,
    ReactiveFormsModule
  ],
  templateUrl: './admin-size-guide-dialog.component.html'
})
export class AdminSizeGuideDialogComponent implements OnInit {
  private fb = inject(FormBuilder);
  dialogRef = inject(MatDialogRef<AdminSizeGuideDialogComponent, ProductSizeGuide | null>);
  data = inject<SizeGuideDialogData>(MAT_DIALOG_DATA);

  guideForm = this.fb.group({
    type: ['' as SizeGuideType | '', Validators.required],
    title: ['Size guide', Validators.required],
    howToMeasure: [''],
    fitNotes: [''],
    disclaimer: [''],
    extraNotes: [''],
    columns: this.fb.array([], Validators.minLength(1)),
    rows: this.fb.array([])
  });

  typeOptions: { label: string; value: SizeGuideType }[] = [
    { label: 'Boots', value: 'boots' },
    { label: 'Boards', value: 'boards' },
    { label: 'Hats', value: 'hats' },
    { label: 'Gloves', value: 'gloves' }
  ];

  ngOnInit(): void {
    const initialGuide = this.data.guide
      ?? getDefaultSizeGuideForType(this.data.productType)
      ?? null;

    if (initialGuide) {
      this.applyGuide(initialGuide);
    }
  }

  get columns(): FormArray {
    return this.guideForm.get('columns') as FormArray;
  }

  get rows(): FormArray {
    return this.guideForm.get('rows') as FormArray;
  }

  get canApplyTemplate(): boolean {
    const type = this.guideForm.get('type')?.value as SizeGuideType | '';
    return !!getDefaultSizeGuideForType(type);
  }

  applyTemplate() {
    const type = this.guideForm.get('type')?.value as SizeGuideType | '';
    const template = getDefaultSizeGuideForType(type);
    if (!template) return;
    this.applyGuide(template);
  }

  addColumn() {
    this.columns.push(this.fb.control('', Validators.required));
    this.rows.controls.forEach(row => (row as FormArray).push(this.fb.control('')));
  }

  removeColumn(index: number) {
    if (index < 0 || index >= this.columns.length) return;
    this.columns.removeAt(index);
    this.rows.controls.forEach(row => (row as FormArray).removeAt(index));
  }

  addRow() {
    if (this.columns.length === 0) return;
    this.rows.push(this.createRow());
  }

  removeRow(index: number) {
    if (index < 0 || index >= this.rows.length) return;
    this.rows.removeAt(index);
  }

  getRowControls(row: AbstractControl): FormArray {
    return row as FormArray;
  }

  asFormControl(control: AbstractControl): FormControl {
    return control as FormControl;
  }

  save() {
    if (this.guideForm.invalid || this.columns.length === 0) {
      this.guideForm.markAllAsTouched();
      return;
    }
    const guide = this.buildGuideFromForm();
    if (!guide) {
      this.guideForm.markAllAsTouched();
      return;
    }
    this.dialogRef.close(guide);
  }

  cancel() {
    this.dialogRef.close();
  }

  removeGuide() {
    this.dialogRef.close(null);
  }

  private applyGuide(guide: ProductSizeGuide) {
    this.clearFormArray(this.columns);
    this.clearFormArray(this.rows);

    guide.columns.forEach(column => this.columns.push(this.fb.control(column, Validators.required)));
    guide.rows.forEach(row => this.rows.push(this.createRow(row)));
    if (this.rows.length === 0 && this.columns.length > 0) {
      this.addRow();
    }

    this.guideForm.patchValue({
      type: guide.type,
      title: guide.title || 'Size guide',
      howToMeasure: guide.howToMeasure ?? '',
      fitNotes: guide.fitNotes ?? '',
      disclaimer: guide.disclaimer ?? '',
      extraNotes: (guide.extraNotes ?? []).join('\n')
    });
  }

  private createRow(values: string[] = []) {
    const controls = this.columns.controls.map((_, index) => this.fb.control(values[index] ?? ''));
    return this.fb.array(controls);
  }

  private clearFormArray(array: FormArray) {
    while (array.length > 0) {
      array.removeAt(0);
    }
  }

  private buildGuideFromForm(): ProductSizeGuide | null {
    const guideType = this.guideForm.get('type')?.value as SizeGuideType | '';
    if (!guideType) return null;
    const columns = this.columns.controls.map(control => (control.value ?? '').toString().trim());
    if (columns.length === 0) return null;

    const rows = this.rows.controls.map(row => {
      const rowArray = row as FormArray;
      const cells = rowArray.controls.map(cell => (cell.value ?? '').toString().trim());
      while (cells.length < columns.length) {
        cells.push('');
      }
      return cells.slice(0, columns.length);
    });

    const extraNotesRaw = (this.guideForm.get('extraNotes')?.value ?? '') as string;
    const extraNotes = extraNotesRaw
      .split('\n')
      .map(note => note.trim())
      .filter(Boolean);

    return {
      type: guideType,
      title: (this.guideForm.get('title')?.value as string) || 'Size guide',
      columns,
      rows,
      howToMeasure: (this.guideForm.get('howToMeasure')?.value as string) || null,
      fitNotes: (this.guideForm.get('fitNotes')?.value as string) || null,
      disclaimer: (this.guideForm.get('disclaimer')?.value as string) || null,
      extraNotes
    };
  }
}
