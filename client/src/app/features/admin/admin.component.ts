import { AfterViewInit, Component, inject, OnInit, ViewChild } from '@angular/core';
import {MatTableDataSource, MatTableModule} from '@angular/material/table';
import { Order } from '../../shared/models/order';
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
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { forkJoin } from 'rxjs';

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
    MatInputModule
  ],
  templateUrl: './admin.component.html',
  styleUrl: './admin.component.scss'
})
export class AdminComponent implements OnInit {
  displayedColumns: string[] = ['id', 'buyerEmail', 'orderDate', 'total', 'status', 'action'];
  dataSource = new MatTableDataSource<Order>([]);
  private adminService = inject(AdminService);
  private dialogService = inject(DialogService);
  private fb = inject(FormBuilder);
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
  productForm = this.fb.group({
    id: [0],
    name: ['', Validators.required],
    description: ['', Validators.required],
    price: [0, [Validators.required, Validators.min(0.01)]],
    pictureUrl: ['', Validators.required],
    type: ['', Validators.required],
    brand: ['', Validators.required],
    quantityInStock: [0, [Validators.required, Validators.min(0)]],
  });

  ngOnInit(): void {
    this.loadOrders();
    this.loadProducts();
    this.loadFilters();
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
    this.editingProductId = product.id;
    this.productForm.patchValue(product);
  }

  resetProductForm() {
    this.editingProductId = null;
    this.productForm.reset({
      id: 0,
      name: '',
      description: '',
      price: 0,
      pictureUrl: '',
      type: '',
      brand: '',
      quantityInStock: 0
    });
  }

  submitProduct() {
    if (this.productForm.invalid) return;
    const product = this.productForm.value as Product;
    const request$ = this.editingProductId
      ? this.adminService.updateProduct(product)
      : this.adminService.createProduct(product);

    request$.subscribe({
      next: () => {
        this.resetProductForm();
        this.loadProducts();
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
      next: () => this.loadProducts()
    })
  }
}
