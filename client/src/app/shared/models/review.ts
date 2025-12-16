export interface Review {
  id: number;
  productId: number;
  rating: number;
  title?: string | null;
  content?: string | null;
  createdAt: string;
  updatedAt: string;
  author: string;
  isOwner: boolean;
  adminResponse?: string | null;
  adminResponder?: string | null;
  adminRespondedAt?: string | null;
}

export interface ReviewAdmin extends Review {
  isHidden: boolean;
  authorEmail: string;
  productName: string;
  brand: string;
  type: string;
  adminResponderEmail?: string;
  orderId?: number | null;
}

export interface ReviewAudit {
  id: number;
  reviewId: number;
  action: number;
  actorEmail: string;
  createdAt: string;
  oldRating?: number | null;
  newRating?: number | null;
  oldTitle?: string | null;
  newTitle?: string | null;
  oldContent?: string | null;
  newContent?: string | null;
  oldHidden?: boolean | null;
  newHidden?: boolean | null;
  oldResponse?: string | null;
  newResponse?: string | null;
}

export interface ReviewEligibility {
  canReview: boolean;
  alreadyReviewed: boolean;
}

export class ReviewParams {
  pageNumber = 1;
  pageSize = 5;
  sort: 'newest' | 'oldest' | 'ratingDesc' | 'ratingAsc' = 'newest';
}

export class AdminReviewParams extends ReviewParams {
  productId?: number;
  brand?: string;
  type?: string;
  minRating?: number;
  maxRating?: number;
  status = 'all';
  search = '';
  userEmail?: string;
  from?: string;
  to?: string;
  override pageSize = 10;
}

export interface ReviewPayload {
  rating: number;
  title?: string | null;
  content?: string | null;
  orderId?: number | null;
}

export interface AdminReviewUpdatePayload {
  rating: number;
  title?: string | null;
  content?: string | null;
  isHidden: boolean;
}

export interface ReviewVisibilityPayload {
  isHidden: boolean;
}

export interface ReviewReplyPayload {
  response?: string | null;
}
