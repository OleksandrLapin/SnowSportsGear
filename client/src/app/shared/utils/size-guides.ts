import { ProductSizeGuide, SizeGuideType } from '../models/size-guide';

const BOOT_GUIDE: ProductSizeGuide = {
  type: 'boots',
  title: 'Snowboard Boot Size Guide',
  columns: [
    'EU',
    'US',
    'UK',
    'Foot length (cm)',
    'Mondopoint (cm)',
    'Width',
    'Insole length (cm)'
  ],
  rows: [
    ['36', '4', '3', '22.5', '22.5', 'Regular', '23.0'],
    ['37', '5', '4', '23.0', '23.0', 'Regular', '23.5'],
    ['38', '6', '5', '24.0', '24.0', 'Regular', '24.5'],
    ['39', '7', '6', '25.0', '25.0', 'Regular', '25.5'],
    ['40', '7.5', '6.5', '25.5', '25.5', 'Regular', '26.0'],
    ['41', '8', '7', '26.0', '26.0', 'Regular', '26.5'],
    ['42', '9', '8', '27.0', '27.0', 'Regular', '27.5'],
    ['43', '10', '9', '28.0', '28.0', 'Regular', '28.5'],
    ['44', '11', '10', '29.0', '29.0', 'Wide', '29.5'],
    ['45', '12', '11', '30.0', '30.0', 'Wide', '30.5'],
    ['46', '13', '12', '31.0', '31.0', 'Wide', '31.5']
  ],
  howToMeasure: 'Stand with your heel against a wall and measure to the tip of your longest toe.',
  fitNotes: 'Most snowboard boots run snug. If you are between sizes, consider going 0.5 up for a regular fit.',
  disclaimer: 'Sizing can vary by brand and model. Use this chart as a general guide.',
  extraNotes: ['Mondopoint usually matches foot length.', 'Wide fit is recommended for broader feet.']
};

const BOARD_GUIDE: ProductSizeGuide = {
  type: 'boards',
  title: 'Snowboard Size Guide',
  columns: [
    'Board length (cm)',
    'Rider weight (kg)',
    'Boot size (EU)',
    'Waist width (mm)',
    'Effective edge (cm)',
    'Width',
    'Riding style',
    'Level'
  ],
  rows: [
    ['150', '45-60', '38-42', '246', '112', 'Regular', 'All-mountain', 'Beginner-Intermediate'],
    ['154', '55-70', '39-43', '248', '116', 'Regular', 'All-mountain', 'Intermediate'],
    ['156', '60-75', '40-44', '250', '118', 'Regular', 'All-mountain', 'Intermediate'],
    ['158', '65-80', '41-44', '252', '120', 'Regular', 'Freeride', 'Intermediate-Advanced'],
    ['162', '75-95', '42-45', '254', '124', 'Regular', 'Freeride', 'Advanced'],
    ['154W', '55-75', '43-46', '258', '116', 'Wide', 'All-mountain', 'Intermediate'],
    ['158W', '65-85', '44-47', '260', '120', 'Wide', 'Freeride', 'Intermediate-Advanced'],
    ['162W', '75-95', '44-47', '262', '124', 'Wide', 'Freeride', 'Advanced'],
    ['166W', '85-105', '45-48', '265', '128', 'Wide', 'Freeride', 'Advanced'],
    ['170W', '95-115', '46-49', '268', '132', 'Wide', 'Freeride', 'Advanced']
  ],
  fitNotes: 'Choose length based on rider weight first, then adjust for style and preference.',
  disclaimer: 'Board specs vary by model. Use waist width and boot size as guidance.',
  extraNotes: [
    'If your boot size is EU 44-45 or larger, a wide board is often recommended.',
    'Freestyle riders may size down for easier spins; freeride riders may size up for stability.'
  ]
};

const HAT_GUIDE: ProductSizeGuide = {
  type: 'hats',
  title: 'Beanie Size Guide',
  columns: [
    'Head circumference (cm)',
    'Size',
    'Depth (cm)',
    'Fit',
    'Stretch',
    'Material',
    'Helmet compatible'
  ],
  rows: [
    ['52-55', 'S', '21', 'Snug', 'Stretch', 'Acrylic/Wool blend', 'Yes'],
    ['56-58', 'M', '22', 'Regular', 'Stretch', 'Acrylic/Wool blend', 'Yes'],
    ['59-61', 'L', '23', 'Regular', 'Stretch', 'Acrylic/Wool blend', 'Yes'],
    ['62-64', 'XL', '24', 'Relaxed', 'Stretch', 'Acrylic/Wool blend', 'Yes']
  ],
  howToMeasure: 'Measure around the widest part of your head, just above the ears.',
  fitNotes: 'If you prefer a looser fit or wear a helmet, size up.',
  disclaimer: 'Stretch and thickness can vary by knit style and brand.'
};

const GLOVE_GUIDE: ProductSizeGuide = {
  type: 'gloves',
  title: 'Glove Size Guide',
  columns: [
    'Palm circumference (cm)',
    'Hand length (cm)',
    'Size',
    'Fit',
    'Gender',
    'Type',
    'Insulation'
  ],
  rows: [
    ['16-17', '16-17', 'XS', 'Snug', 'Unisex', 'Gloves', 'Light (0 to -5C)'],
    ['18-19', '17-18', 'S', 'Snug', 'Unisex', 'Gloves', 'Medium (-5 to -10C)'],
    ['20-21', '18-19', 'M', 'Regular', 'Unisex', 'Gloves', 'Medium (-5 to -10C)'],
    ['22-23', '19-20', 'L', 'Regular', 'Unisex', 'Gloves', 'Warm (-10 to -20C)'],
    ['24-25', '20-21', 'XL', 'Relaxed', 'Unisex', 'Gloves', 'Warm (-10 to -20C)']
  ],
  howToMeasure: 'Measure around the knuckles without the thumb. Then measure from wrist to tip of middle finger.',
  fitNotes: 'For a snug fit, stay true to size. For mittens, consider sizing up.',
  disclaimer: 'Men\'s, women\'s, and youth sizing can differ by brand.',
  extraNotes: [
    'Mittens and lobster styles are usually warmer but less dexterous.',
    'Temperature ranges are approximate and depend on activity level.'
  ]
};

const TEMPLATE_MAP: Record<SizeGuideType, ProductSizeGuide> = {
  boots: BOOT_GUIDE,
  boards: BOARD_GUIDE,
  hats: HAT_GUIDE,
  gloves: GLOVE_GUIDE
};

export function normalizeGuideType(type?: string | null): SizeGuideType | null {
  if (!type) return null;
  const normalized = type.trim().toLowerCase();
  if (normalized.includes('boot')) return 'boots';
  if (normalized.includes('board')) return 'boards';
  if (normalized.includes('hat')) return 'hats';
  if (normalized.includes('glove')) return 'gloves';
  return null;
}

export function getDefaultSizeGuideForType(type?: string | null): ProductSizeGuide | null {
  const key = normalizeGuideType(type);
  if (!key) return null;
  return cloneGuide(TEMPLATE_MAP[key]);
}

export function parseSizeGuide(value?: ProductSizeGuide | string | null): ProductSizeGuide | null {
  if (!value) return null;
  if (typeof value !== 'string') return cloneGuide(value);
  try {
    const parsed = JSON.parse(value) as ProductSizeGuide;
    return cloneGuide(parsed);
  } catch {
    return null;
  }
}

export function stringifySizeGuide(guide?: ProductSizeGuide | null): string | null {
  if (!guide) return null;
  return JSON.stringify(guide);
}

function cloneGuide(guide: ProductSizeGuide): ProductSizeGuide {
  return {
    type: guide.type,
    title: guide.title,
    columns: [...guide.columns],
    rows: guide.rows.map(row => [...row]),
    howToMeasure: guide.howToMeasure ?? null,
    fitNotes: guide.fitNotes ?? null,
    disclaimer: guide.disclaimer ?? null,
    extraNotes: guide.extraNotes ? [...guide.extraNotes] : []
  };
}
