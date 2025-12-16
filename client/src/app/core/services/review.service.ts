import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { environment } from '../../../environments/environment';
import { Pagination } from '../../shared/models/pagination';
import {
  AdminReviewParams,
  AdminReviewUpdatePayload,
  Review,
  ReviewAdmin,
  ReviewAudit,
  ReviewEligibility,
  ReviewParams,
  ReviewPayload,
  ReviewReplyPayload,
  ReviewVisibilityPayload
} from '../../shared/models/review';

@Injectable({
  providedIn: 'root'
})
export class ReviewService {
  baseUrl = environment.apiUrl;
  private http = inject(HttpClient);

  getProductReviews(productId: number, params: ReviewParams) {
    let httpParams = new HttpParams()
      .append('pageIndex', params.pageNumber)
      .append('pageSize', params.pageSize)
      .append('sort', params.sort);

    return this.http.get<Pagination<Review>>(
      `${this.baseUrl}products/${productId}/reviews`,
      { params: httpParams }
    );
  }

  getMyReview(productId: number) {
    return this.http.get<Review>(`${this.baseUrl}products/${productId}/reviews/me`);
  }

  getEligibility(productId: number) {
    return this.http.get<ReviewEligibility>(`${this.baseUrl}products/${productId}/reviews/eligibility`);
  }

  createReview(productId: number, payload: ReviewPayload) {
    return this.http.post<Review>(`${this.baseUrl}products/${productId}/reviews`, payload);
  }

  updateReview(productId: number, reviewId: number, payload: ReviewPayload) {
    return this.http.put<Review>(`${this.baseUrl}products/${productId}/reviews/${reviewId}`, payload);
  }

  deleteReview(productId: number, reviewId: number) {
    return this.http.delete(`${this.baseUrl}products/${productId}/reviews/${reviewId}`);
  }

  // admin
  getAdminReviews(params: AdminReviewParams) {
    let httpParams = new HttpParams()
      .append('pageIndex', params.pageNumber)
      .append('pageSize', params.pageSize)
      .append('sort', params.sort);

    if (params.productId) httpParams = httpParams.append('productId', params.productId);
    if (params.brand) httpParams = httpParams.append('brand', params.brand);
    if (params.type) httpParams = httpParams.append('type', params.type);
    if (params.minRating) httpParams = httpParams.append('minRating', params.minRating);
    if (params.maxRating) httpParams = httpParams.append('maxRating', params.maxRating);
    if (params.status && params.status !== 'all') httpParams = httpParams.append('status', params.status);
    if (params.search) httpParams = httpParams.append('search', params.search);
    if (params.userEmail) httpParams = httpParams.append('userEmail', params.userEmail);
    if (params.from) httpParams = httpParams.append('from', params.from);
    if (params.to) httpParams = httpParams.append('to', params.to);

    return this.http.get<Pagination<ReviewAdmin>>(
      `${this.baseUrl}admin/reviews`,
      { params: httpParams }
    );
  }

  updateAdminReview(id: number, payload: AdminReviewUpdatePayload) {
    return this.http.put<ReviewAdmin>(`${this.baseUrl}admin/reviews/${id}`, payload);
  }

  updateVisibility(id: number, payload: ReviewVisibilityPayload) {
    return this.http.patch<ReviewAdmin>(`${this.baseUrl}admin/reviews/${id}/visibility`, payload);
  }

  deleteAdminReview(id: number) {
    return this.http.delete(`${this.baseUrl}admin/reviews/${id}`);
  }

  getAuditTrail(id: number) {
    return this.http.get<ReviewAudit[]>(`${this.baseUrl}admin/reviews/${id}/audits`);
  }

  replyToReview(id: number, payload: ReviewReplyPayload) {
    return this.http.post<ReviewAdmin>(`${this.baseUrl}admin/reviews/${id}/reply`, payload);
  }
}

