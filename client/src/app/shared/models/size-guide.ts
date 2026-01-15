export type SizeGuideType = 'boots' | 'boards' | 'hats' | 'gloves';

export type ProductSizeGuide = {
  type: SizeGuideType;
  title: string;
  columns: string[];
  rows: string[][];
  howToMeasure?: string | null;
  fitNotes?: string | null;
  disclaimer?: string | null;
  extraNotes?: string[];
};
