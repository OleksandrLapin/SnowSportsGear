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
import { FormArray, FormBuilder, FormControl, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { forkJoin } from 'rxjs';
import { SnackbarService } from '../../core/services/snackbar.service';
import { MatAutocompleteModule } from '@angular/material/autocomplete';
import { AdminReviewsComponent } from './admin-reviews.component';

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
  orderParams = new OrderParams();
  totalItems = 0;
  statusOptions = ['All', 'PaymentReceived', 'PaymentMismatch', 'Refunded', 'Pending'];
  productColumns = ['id', 'name', 'price', 'brand', 'type', 'quantityInStock', 'actions'];
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
    type: ['', Validators.required],
    brand: ['', Validators.required],
    variants: this.fb.array([
      this.createVariantGroup()
    ])
  });

  ngOnInit(): void {
    this.loadOrders();
    this.loadProducts();
    this.loadFilters();
    this.setupFilterPredicate();
    this.productFilters.valueChanges.subscribe(() => this.applyProductFilters());
  }

  get variants(): FormArray {
    return this.productForm.get('variants') as FormArray;
  }

  private createVariantGroup(size = '', quantity = 0) {
    return this.fb.group({
      size: [size, Validators.required],
      quantityInStock: [quantity, [Validators.required, Validators.min(0)]]
    });
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
      : [
        {size: 'S', quantityInStock: 5},
        {size: 'M', quantityInStock: 7},
        {size: 'L', quantityInStock: 10},
        {size: 'XL', quantityInStock: 12}
      ];
    variantSource.forEach(v => this.variants.push(this.createVariantGroup(v.size, v.quantityInStock)));
    this.productForm.patchValue({
      ...product,
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
    ['S','M','L','XL'].forEach(s => this.variants.push(this.createVariantGroup(s, 0)));
    this.productForm.reset({
      id: 0,
      name: '',
      description: '',
      price: 0,
      type: '',
      brand: '',
      variants: this.variants.value
    });
    this.selectedFile = null;
    this.imagePreview = null;
    this.showProductForm = false;
  }

  submitProduct() {
    if (this.productForm.invalid) return;
    const product = this.productForm.value as Product;
    const formData = new FormData();
    formData.append('name', product.name ?? '');
    formData.append('description', product.description ?? '');
    formData.append('price', product.price?.toString() ?? '0');
    formData.append('type', product.type ?? '');
    formData.append('brand', product.brand ?? '');
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
      const matchesSearch = !search || data.name.toLowerCase().includes(search) || data.description.toLowerCase().includes(search);
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
}
