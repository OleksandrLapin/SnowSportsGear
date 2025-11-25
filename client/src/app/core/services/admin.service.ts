import { inject, Injectable } from '@angular/core';
import { environment } from '../../../environments/environment';
import { HttpClient, HttpParams } from '@angular/common/http';
import { OrderParams } from '../../shared/models/orderParams';
import { Pagination } from '../../shared/models/pagination';
import { Order } from '../../shared/models/order';
import { OrderSummary } from '../../shared/models/orderSummary';
import { Product } from '../../shared/models/product';

@Injectable({
  providedIn: 'root'
})
export class AdminService {
  baseUrl = environment.apiUrl;
  private http = inject(HttpClient);

  getOrders(orderParams: OrderParams) {
    let params = new HttpParams();
    if (orderParams.filter && orderParams.filter !== 'All') {
      params = params.append('status', orderParams.filter);
    }
    params = params.append('pageIndex', orderParams.pageNumber);
    params = params.append('pageSize', orderParams.pageSize);
    return this.http.get<Pagination<OrderSummary>>(this.baseUrl + 'admin/orders', {params});
  }

  getOrder(id: number) {
    return this.http.get<Order>(this.baseUrl + 'admin/orders/' + id);
  }

  refundOrder(id: number) {
    return this.http.post<OrderSummary>(this.baseUrl + 'admin/orders/refund/' + id, {});
  }

  getProducts(pageIndex: number, pageSize: number) {
    const params = new HttpParams()
      .append('pageIndex', pageIndex)
      .append('pageSize', pageSize);
    return this.http.get<Pagination<Product>>(this.baseUrl + 'products', {params});
  }

  createProduct(formData: FormData) {
    return this.http.post<Product>(this.baseUrl + 'products', formData);
  }

  updateProduct(id: number, formData: FormData) {
    return this.http.put<Product>(this.baseUrl + 'products/' + id, formData);
  }

  deleteProduct(id: number) {
    return this.http.delete(this.baseUrl + 'products/' + id);
  }

  getBrands() {
    return this.http.get<string[]>(this.baseUrl + 'products/brands');
  }

  getTypes() {
    return this.http.get<string[]>(this.baseUrl + 'products/types');
  }

}
