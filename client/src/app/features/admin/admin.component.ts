import { AfterViewInit, Component, inject, OnInit, ViewChild } from '@angular/core';
import {MatTableDataSource, MatTableModule} from '@angular/material/table';
import { OrderSummary } from '../../shared/models/orderSummary';
import { MatPaginator, MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { AdminService } from '../../core/services/admin.service';
import { OrderParams } from '../../shared/models/orderParams';
import { MatButton } from '@angular/material/button';
import { MatIcon } from '@angular/material/icon';
import { MatLabel, MatSelectChange, MatSelectModule } from '@angular/material/select';
import { CurrencyPipe, DatePipe } from '@angular/common';
import { MatTooltipModule } from '@angular/material/tooltip';
import {MatTabsModule} from '@angular/material/tabs';
import { RouterLink } from '@angular/router';
import { DialogService } from '../../core/services/dialog.service';
import { Product } from '../../shared/models/product';
import { AbstractControl, FormArray, FormBuilder, FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatSlideToggleModule } from '@angular/material/slide-toggle';
import { forkJoin } from 'rxjs';
import { SnackbarService } from '../../core/services/snackbar.service';
import { MatAutocompleteModule } from '@angular/material/autocomplete';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { AdminReviewsComponent } from './admin-reviews.component';
import { getDefaultSizesForType } from '../../shared/utils/product-sizes';
import { ProductSizeGuide, SizeGuideType } from '../../shared/models/size-guide';
import { getDefaultSizeGuideForType, parseSizeGuide, stringifySizeGuide } from '../../shared/utils/size-guides';
import { AdminSizeGuideDialogComponent } from './admin-size-guide-dialog.component';

@Component({
  selector: 'app-admin',
  standalone: true,
  imports: [
    MatTableModule,
    MatPaginatorModule,
    MatButton,
    MatIcon,
    MatSelectModule,
    DatePipe,
    CurrencyPipe,
    MatLabel,
    MatTooltipModule,
    MatTabsModule,
    RouterLink,
    ReactiveFormsModule,
    MatFormFieldModule,
    MatInputModule,
    MatAutocompleteModule,
    MatCheckboxModule,
    MatSlideToggleModule,
    MatDialogModule,
    AdminReviewsComponent
  ],
  templateUrl: './admin.component.html',
  styleUrl: './admin.component.scss'
})
export class AdminComponent implements OnInit {
  displayedColumns: string[] = ['id', 'buyerEmail', 'orderDate', 'total', 'status', 'action'];
  dataSource = new MatTableDataSource<OrderSummary>([]);
  private adminService = inject(AdminService);
  private dialogService = inject(DialogService);
  private fb = inject(FormBuilder);
  private snackbar = inject(SnackbarService);
  private dialog = inject(MatDialog);
  orderParams = new OrderParams();
  totalItems = 0;
  statusOptions = [
    'All',
    'Pending',
    'PaymentReceived',
    'PaymentFailed',
    'PaymentMismatch',
    'Processing',
    'Packed',
    'Shipped',
    'Delivered',
    'Cancelled',
    'Refunded'
  ];
  productColumns = ['name', 'price', 'brand', 'type', 'color', 'quantityInStock', 'actions'];
  productDataSource = new MatTableDataSource<Product>([]);
  productPageIndex = 1;
  productPageSize = 10;
  productTotal = 0;
  brands: string[] = [];
  types: string[] = [];
  editingProductId: number | null = null;
  selectedFile: File | null = null;
  imagePreview: string | null = null;
  showProductForm = false;
  productFilters = this.fb.group({
    search: [''],
    brand: [''],
    type: ['']
  });
  productForm = this.fb.group({
    id: [0],
    name: ['', Validators.required],
    description: ['', Validators.required],
    price: [0, [Validators.required, Validators.min(0.01)]],
    salePrice: [null as number | null],
    lowestPrice: [null as number | null],
    color: [''],
    onSale: [false],
    isActive: [true],
    type: ['', Validators.required],
    brand: ['', Validators.required],
    sizeGuide: this.fb.group({
      type: [''],
      title: [''],
      howToMeasure: [''],
      fitNotes: [''],
      disclaimer: [''],
      extraNotes: [''],
      rows: this.fb.array([])
    }),
    variants: this.fb.array([
      this.createVariantGroup()
    ])
  });
  sizeGuideColumns: string[] = [];
  sizeGuideTypeOptions: { label: string; value: SizeGuideType }[] = [
    { label: 'Boots', value: 'boots' },
    { label: 'Boards', value: 'boards' },
    { label: 'Hats', value: 'hats' },
    { label: 'Gloves', value: 'gloves' }
  ];
  private isSettingSizeGuide = false;

  ngOnInit(): void {
    this.loadOrders();
    this.loadProducts();
    this.loadFilters();
    this.setupFilterPredicate();
    this.productFilters.valueChanges.subscribe(() => this.applyProductFilters());
    this.sizeGuideGroup.get('type')?.valueChanges.subscribe(value => {
      if (this.isSettingSizeGuide) return;
      const guideType = value as SizeGuideType | '';
      if (!guideType) {
        this.sizeGuideColumns = [];
        this.clearSizeGuideRows();
        return;
      }
      const template = getDefaultSizeGuideForType(guideType);
      this.sizeGuideColumns = template?.columns ?? [];
      this.syncSizeGuideRowsToColumns();
      if (this.sizeGuideRows.length === 0 && this.sizeGuideColumns.length > 0) {
        this.addSizeGuideRow();
      }
    });
    this.productForm.get('type')?.valueChanges.subscribe(value => {
      if (this.isSettingSizeGuide) return;
      const currentGuideType = this.sizeGuideGroup.get('type')?.value as SizeGuideType | '';
      if (currentGuideType) return;
      const template = getDefaultSizeGuideForType(value);
      if (template) {
        this.setSizeGuide(template);
      }
    });
  }

  get variants(): FormArray {
    return this.productForm.get('variants') as FormArray;
  }

  get sizeGuideGroup(): FormGroup {
    return this.productForm.get('sizeGuide') as FormGroup;
  }

  get sizeGuideRows(): FormArray {
    return this.sizeGuideGroup.get('rows') as FormArray;
  }

  private createVariantGroup(size = '', quantity = 0) {
    return this.fb.group({
      size: [size, Validators.required],
      quantityInStock: [quantity, [Validators.required, Validators.min(0)]]
    });
  }

  private createSizeGuideRow(cells: string[] = []) {
    return this.fb.array(cells.map(cell => this.fb.control(cell)));
  }

  private clearSizeGuideRows() {
    while (this.sizeGuideRows.length > 0) {
      this.sizeGuideRows.removeAt(0);
    }
  }

  private setSizeGuide(guide: ProductSizeGuide | null) {
    this.isSettingSizeGuide = true;
    this.clearSizeGuideRows();

    if (!guide) {
      this.sizeGuideColumns = [];
      this.sizeGuideGroup.reset({
        type: '',
        title: '',
        howToMeasure: '',
        fitNotes: '',
        disclaimer: '',
        extraNotes: '',
        rows: []
      }, { emitEvent: false });
      this.isSettingSizeGuide = false;
      return;
    }

    this.sizeGuideColumns = [...guide.columns];
    this.sizeGuideGroup.patchValue({
      type: guide.type,
      title: guide.title,
      howToMeasure: guide.howToMeasure ?? '',
      fitNotes: guide.fitNotes ?? '',
      disclaimer: guide.disclaimer ?? '',
      extraNotes: (guide.extraNotes ?? []).join('\n')
    }, { emitEvent: false });

    guide.rows.forEach(row => this.sizeGuideRows.push(this.createSizeGuideRow(row)));
    this.syncSizeGuideRowsToColumns();
    this.isSettingSizeGuide = false;
  }

  private syncSizeGuideRowsToColumns() {
    const colCount = this.sizeGuideColumns.length;
    if (colCount === 0) return;
    this.sizeGuideRows.controls.forEach(row => {
      const rowArray = row as FormArray;
      while (rowArray.length < colCount) {
        rowArray.push(this.fb.control(''));
      }
      while (rowArray.length > colCount) {
        rowArray.removeAt(rowArray.length - 1);
      }
    });
  }

  applySizeGuideTemplate() {
    const guideType = this.sizeGuideGroup.get('type')?.value as SizeGuideType | '';
    const template = getDefaultSizeGuideForType(guideType);
    if (!template) {
      this.snackbar.error('Select a size guide type first');
      return;
    }
    this.setSizeGuide(template);
  }

  addSizeGuideRow() {
    if (this.sizeGuideColumns.length === 0) return;
    this.sizeGuideRows.push(this.createSizeGuideRow(new Array(this.sizeGuideColumns.length).fill('')));
  }

  removeSizeGuideRow(index: number) {
    if (this.sizeGuideRows.length > 0) {
      this.sizeGuideRows.removeAt(index);
    }
  }

  getSizeGuideCellControl(row: AbstractControl, index: number): FormControl {
    return (row as FormArray).at(index) as FormControl;
  }

  private buildDefaultVariants(type: string | null, useStockDefaults = false) {
    const sizes = getDefaultSizesForType(type);
    return sizes.map((size, index) => ({
      size,
      quantityInStock: useStockDefaults ? this.defaultQuantityForSize(size, index) : 0
    }));
  }

  private defaultQuantityForSize(size: string, index: number) {
    switch (size.toUpperCase()) {
      case 'S':
        return 5;
      case 'M':
        return 7;
      case 'L':
        return 10;
      case 'XL':
        return 12;
      default:
        return 5 + index * 2;
    }
  }

  private buildSizeGuideFromForm(): ProductSizeGuide | null {
    const guideType = this.sizeGuideGroup.get('type')?.value as SizeGuideType | '';
    if (!guideType) return null;
    const template = getDefaultSizeGuideForType(guideType);
    const columns = this.sizeGuideColumns.length > 0
      ? this.sizeGuideColumns
      : template?.columns ?? [];
    if (columns.length === 0) return null;

    const rows = this.sizeGuideRows.controls.map(row =>
      (row as FormArray).controls.map(control => (control.value ?? '').toString().trim())
    );

    const extraNotesRaw = (this.sizeGuideGroup.get('extraNotes')?.value ?? '') as string;
    const extraNotes = extraNotesRaw
      .split('\n')
      .map(note => note.trim())
      .filter(Boolean);

    return {
      type: guideType,
      title: (this.sizeGuideGroup.get('title')?.value as string) || template?.title || 'Size guide',
      columns,
      rows,
      howToMeasure: (this.sizeGuideGroup.get('howToMeasure')?.value as string) || null,
      fitNotes: (this.sizeGuideGroup.get('fitNotes')?.value as string) || null,
      disclaimer: (this.sizeGuideGroup.get('disclaimer')?.value as string) || null,
      extraNotes
    };
  }

  loadOrders() {
    this.adminService.getOrders(this.orderParams).subscribe({
      next: response => {
        if (response.data) {
          this.dataSource.data = response.data;
          this.totalItems = response.count;
        }
      }
    })
  }

  onPageChange(event: PageEvent) {
    this.orderParams.pageNumber = event.pageIndex + 1;
    this.orderParams.pageSize = event.pageSize;
    this.loadOrders();
  }

  onFilterSelect(event: MatSelectChange) {
    this.orderParams.filter = event.value;
    this.orderParams.pageNumber = 1;
    this.loadOrders();
  }

  async openConfirmDialog(id: number) {
    const confirmed = await this.dialogService.confirm(
      'Confirm refund',
      'Are you sure you want to issue this refund? This cannot be undone'
    )

    if (confirmed) this.refundOrder(id);
  }

  refundOrder(id: number) {
    this.adminService.refundOrder(id).subscribe({
      next: order => {
        this.dataSource.data = this.dataSource.data.map(o => o.id === id ? order : o)
      }
    })
  }

  loadProducts() {
    this.adminService.getProducts(this.productPageIndex, this.productPageSize).subscribe({
      next: response => {
        if (response.data) {
          this.productDataSource.data = response.data;
          this.productTotal = response.count;
          this.mergeBrandTypeFromProducts(response.data);
          this.applyProductFilters();
        }
      }
    })
  }

  onProductPageChange(event: PageEvent) {
    this.productPageIndex = event.pageIndex + 1;
    this.productPageSize = event.pageSize;
    this.loadProducts();
  }

  toggleActive(product: Product, isActive: boolean) {
    this.adminService.setProductStatus(product.id, isActive).subscribe({
      next: updated => {
        const index = this.productDataSource.data.findIndex(p => p.id === product.id);
        if (index !== -1) {
          this.productDataSource.data[index] = {...product, ...updated};
          this.productDataSource._updateChangeSubscription();
        }
        this.snackbar.success(isActive ? 'Product activated' : 'Product deactivated');
      },
      error: () => {
        this.snackbar.error('Unable to update product status');
      }
    })
  }

  loadFilters() {
    forkJoin({
      brands: this.adminService.getBrands(),
      types: this.adminService.getTypes()
    }).subscribe({
      next: result => {
        this.brands = result.brands;
        this.types = result.types;
      }
    })
  }

  editProduct(product: Product) {
    this.showProductForm = true;
    this.editingProductId = product.id;
    while (this.variants.length > 0) {
      this.variants.removeAt(0);
    }
    const variantSource = (product.variants && product.variants.length > 0)
      ? product.variants
      : this.buildDefaultVariants(product.type, true);
    variantSource.forEach(v => this.variants.push(this.createVariantGroup(v.size, v.quantityInStock)));
    const onSale = !!(product.salePrice && product.salePrice > 0 && product.salePrice < product.price);
    const sizeGuide = parseSizeGuide(product.sizeGuide) ?? getDefaultSizeGuideForType(product.type);
    this.setSizeGuide(sizeGuide);
    const { sizeGuide: _sizeGuide, ...productData } = product;
    this.productForm.patchValue({
      ...productData,
      salePrice: onSale ? product.salePrice : null,
      onSale,
      isActive: product.isActive ?? true,
      variants: this.variants.value
    });
    this.imagePreview = product.pictureUrl || null;
    this.selectedFile = null;
  }

  resetProductForm() {
    this.editingProductId = null;
    while (this.variants.length > 0) {
      this.variants.removeAt(0);
    }
    this.buildDefaultVariants(null)
      .forEach(v => this.variants.push(this.createVariantGroup(v.size, v.quantityInStock)));
    this.setSizeGuide(null);
    this.productForm.reset({
      id: 0,
      name: '',
      description: '',
      price: 0,
      salePrice: null,
      lowestPrice: null,
      color: '',
      onSale: false,
      isActive: true,
      type: '',
      brand: '',
      variants: this.variants.value,
      sizeGuide: this.sizeGuideGroup.value
    });
    this.selectedFile = null;
    this.imagePreview = null;
    this.showProductForm = false;
  }

  submitProduct() {
    if (this.productForm.invalid) return;
    const product = this.productForm.value as Product & { onSale: boolean };
    const currentLowest = (product.onSale && product.salePrice && product.salePrice > 0 && product.salePrice < product.price)
      ? product.salePrice
      : product.price;
    let lowestPrice = product.lowestPrice ?? null;
    if (lowestPrice && lowestPrice > currentLowest) {
      lowestPrice = currentLowest;
      this.productForm.patchValue({lowestPrice});
      this.snackbar.success('Lowest price adjusted to the current price');
    }
    const formData = new FormData();
    const sizeGuide = this.buildSizeGuideFromForm();
    const sizeGuideJson = stringifySizeGuide(sizeGuide);
    formData.append('name', product.name ?? '');
    formData.append('description', product.description ?? '');
    formData.append('price', product.price?.toString() ?? '0');
    if (product.onSale && product.salePrice && product.salePrice > 0) {
      formData.append('salePrice', product.salePrice.toString());
    }
    if (lowestPrice && lowestPrice > 0) {
      formData.append('lowestPrice', lowestPrice.toString());
    }
    if (product.color) {
      formData.append('color', product.color);
    }
    formData.append('isActive', (product.isActive ?? true).toString());
    formData.append('type', product.type ?? '');
    formData.append('brand', product.brand ?? '');
    if (sizeGuideJson) {
      formData.append('sizeGuide', sizeGuideJson);
    }
    (this.variants.controls as any[]).forEach((ctrl, index) => {
      const value = ctrl.value;
      formData.append(`variants[${index}].size`, value.size ?? '');
      formData.append(`variants[${index}].quantityInStock`, value.quantityInStock?.toString() ?? '0');
    });
    if (this.selectedFile) formData.append('image', this.selectedFile);

    const request$ = this.editingProductId
      ? this.adminService.updateProduct(this.editingProductId, formData)
      : this.adminService.createProduct(formData);

    request$.subscribe({
      next: () => {
        this.snackbar.success(this.editingProductId ? 'Product updated' : 'Product created');
        this.mergeBrandTypeFromProducts([{...product, id: this.editingProductId ?? 0} as Product]);
        this.resetProductForm();
        this.loadProducts();
      },
      error: (err) => {
        this.snackbar.error(err?.error || 'Operation failed');
      }
    })
  }

  async deleteProduct(id: number) {
    const confirmed = await this.dialogService.confirm(
      'Delete product',
      'Are you sure you want to delete this product?'
    );
    if (!confirmed) return;
    this.adminService.deleteProduct(id).subscribe({
      next: () => {
        this.snackbar.success('Product deleted');
        this.loadProducts();
      }
    })
  }

  onFileSelected(event: Event) {
    const input = event.target as HTMLInputElement;
    if (!input.files || input.files.length === 0) return;
    this.setFile(input.files[0]);
  }

  onDrop(event: DragEvent) {
    event.preventDefault();
    if (event.dataTransfer?.files && event.dataTransfer.files.length > 0) {
      this.setFile(event.dataTransfer.files[0]);
      event.dataTransfer.clearData();
    }
  }

  onDragOver(event: DragEvent) {
    event.preventDefault();
  }

  addVariantRow() {
    this.variants.push(this.createVariantGroup());
  }

  removeVariantRow(index: number) {
    if (this.variants.length > 1) {
      this.variants.removeAt(index);
    }
  }

  getVariantControl(group: any, controlName: string): FormControl {
    return group.get(controlName) as FormControl;
  }

  private setFile(file: File) {
    this.selectedFile = file;
    const reader = new FileReader();
    reader.onload = () => {
      this.imagePreview = reader.result as string;
    };
    reader.readAsDataURL(file);
  }

  private mergeBrandTypeFromProducts(products: Product[]) {
    const brandSet = new Set(this.brands);
    const typeSet = new Set(this.types);
    products.forEach(p => {
      if (p.brand) brandSet.add(p.brand);
      if (p.type) typeSet.add(p.type);
    });
    this.brands = Array.from(brandSet).sort();
    this.types = Array.from(typeSet).sort();
  }

  get filteredBrands() {
    const term = (this.productForm.get('brand')?.value || '').toLowerCase();
    return this.brands.filter(b => b.toLowerCase().includes(term));
  }

  get filteredTypes() {
    const term = (this.productForm.get('type')?.value || '').toLowerCase();
    return this.types.filter(t => t.toLowerCase().includes(term));
  }

  get filteredBrandFilters() {
    const term = (this.productFilters.get('brand')?.value || '').toLowerCase();
    return this.brands.filter(b => b.toLowerCase().includes(term));
  }

  get filteredTypeFilters() {
    const term = (this.productFilters.get('type')?.value || '').toLowerCase();
    return this.types.filter(t => t.toLowerCase().includes(term));
  }

  toggleCreateProduct() {
    this.showProductForm = true;
    this.resetProductForm();
    this.showProductForm = true;
  }

  openEditForm() {
    this.showProductForm = true;
  }

  private setupFilterPredicate() {
    this.productDataSource.filterPredicate = (data: Product, filter: string) => {
      const f = JSON.parse(filter) as {search: string, brand: string, type: string};
      const search = f.search?.toLowerCase() || '';
      const brand = f.brand?.toLowerCase() || '';
      const type = f.type?.toLowerCase() || '';
      const matchesSearch = !search || data.name.toLowerCase().includes(search) || data.description.toLowerCase().includes(search) || (data.color?.toLowerCase().includes(search) ?? false);
      const matchesBrand = !brand || data.brand.toLowerCase().includes(brand);
      const matchesType = !type || data.type.toLowerCase().includes(type);
      return matchesSearch && matchesBrand && matchesType;
    }
  }

  applyProductFilters() {
    const filterValue = {
      search: this.productFilters.get('search')?.value || '',
      brand: this.productFilters.get('brand')?.value || '',
      type: this.productFilters.get('type')?.value || ''
    };
    this.productDataSource.filter = JSON.stringify(filterValue);
  }

  get hasSizeGuide(): boolean {
    return !!this.buildSizeGuideFromForm();
  }

  get sizeGuideSummary(): string {
    const guide = this.buildSizeGuideFromForm();
    if (!guide) return 'No size guide assigned.';
    return `${guide.title} (${guide.columns.length} columns, ${guide.rows.length} rows)`;
  }

  openSizeGuideEditor() {
    const currentGuide = this.buildSizeGuideFromForm()
      ?? getDefaultSizeGuideForType(this.productForm.get('type')?.value)
      ?? null;

    const dialogRef = this.dialog.open(AdminSizeGuideDialogComponent, {
      data: {
        guide: currentGuide,
        productType: this.productForm.get('type')?.value
      },
      width: '900px',
      maxWidth: '95vw'
    });

    dialogRef.afterClosed().subscribe(result => {
      if (result === undefined) return;
      if (result === null) {
        this.setSizeGuide(null);
        return;
      }
      this.setSizeGuide(result);
    });
  }

  clearSizeGuide() {
    this.setSizeGuide(null);
  }
}
