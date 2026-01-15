import { DEFAULT_SIZE_GROUPS, getDefaultSizesForType, isDisallowedSize } from './product-sizes';

describe('product sizes', () => {
  it('returns board sizes for boards', () => {
    expect(getDefaultSizesForType('Boards')).toEqual(DEFAULT_SIZE_GROUPS.board);
  });

  it('returns boot sizes for boots', () => {
    expect(getDefaultSizesForType('Boots')).toEqual(DEFAULT_SIZE_GROUPS.boot);
  });

  it('returns apparel sizes for hats and gloves', () => {
    expect(getDefaultSizesForType('Hats')).toEqual(DEFAULT_SIZE_GROUPS.apparel);
    expect(getDefaultSizesForType('Gloves')).toEqual(DEFAULT_SIZE_GROUPS.apparel);
  });

  it('falls back to apparel sizes for unknown types', () => {
    expect(getDefaultSizesForType('Goggles')).toEqual(DEFAULT_SIZE_GROUPS.apparel);
  });

  it('identifies UNI as disallowed', () => {
    expect(isDisallowedSize('UNI')).toBeTrue();
    expect(isDisallowedSize('uni')).toBeTrue();
    expect(isDisallowedSize('L')).toBeFalse();
  });
});
