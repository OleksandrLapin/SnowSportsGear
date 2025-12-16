import { Component, EventEmitter, Input, Output } from '@angular/core';
import { NgClass, NgFor } from '@angular/common';
import { MatIconModule } from '@angular/material/icon';

@Component({
  selector: 'app-star-rating',
  standalone: true,
  imports: [NgFor, NgClass, MatIconModule],
  templateUrl: './star-rating.component.html',
  styleUrl: './star-rating.component.scss'
})
export class StarRatingComponent {
  @Input() value = 0;
  @Input() max = 5;
  @Input() readonly = true;
  @Input() clickable = false;
  @Input() showValue = false;
  @Input() size: 'sm' | 'md' = 'sm';
  @Input() ariaLabel = 'rating';
  @Output() valueChange = new EventEmitter<number>();
  @Output() clicked = new EventEmitter<void>();

  hoverValue: number | null = null;

  get stars() {
    return Array.from({ length: this.max }, (_, i) => i + 1);
  }

  iconFor(star: number) {
    const displayValue = this.hoverValue ?? this.value;
    const position = displayValue - (star - 1);

    if (position >= 0.9) return 'star';
    if (position >= 0.25) return 'star_half';
    return 'star_border';
  }

  onStarClick(star: number) {
    if (this.readonly) {
      if (this.clickable) this.clicked.emit();
      return;
    }
    this.value = star;
    this.valueChange.emit(this.value);
  }

  onStarEnter(star: number) {
    if (this.readonly) return;
    this.hoverValue = star;
  }

  onStarLeave() {
    if (this.readonly) return;
    this.hoverValue = null;
  }
}

