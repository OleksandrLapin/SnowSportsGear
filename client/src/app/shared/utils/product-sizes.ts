const BOARD_SIZES = [
  '150',
  '154',
  '156',
  '158',
  '162',
  '154W',
  '158W',
  '162W',
  '166W',
  '170W'
];

const BOOT_SIZES = [
  '36',
  '37',
  '38',
  '39',
  '40',
  '41',
  '42',
  '43',
  '44',
  '45',
  '46'
];

const APPAREL_SIZES = ['S', 'M', 'L', 'XL'];

export const DEFAULT_SIZE_GROUPS = {
  board: BOARD_SIZES,
  boot: BOOT_SIZES,
  apparel: APPAREL_SIZES
};

export function getDefaultSizesForType(type?: string | null): string[] {
  if (!type) return [...APPAREL_SIZES];
  const normalized = type.trim().toLowerCase();

  if (normalized.includes('board')) {
    return [...BOARD_SIZES];
  }

  if (normalized.includes('boot')) {
    return [...BOOT_SIZES];
  }

  if (normalized.includes('hat') || normalized.includes('glove')) {
    return [...APPAREL_SIZES];
  }

  return [...APPAREL_SIZES];
}

export function isDisallowedSize(size?: string | null): boolean {
  return (size ?? '').trim().toUpperCase() === 'UNI';
}
