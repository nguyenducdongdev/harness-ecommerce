// Client API — gọi qua rewrite /api của Next (tránh CORS trong dev)

export interface PagedResult<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
}

export interface ApiResponse<T> {
  success: boolean;
  data: T;
  message: string;
  errors?: Record<string, string[]>;
}

export interface ProductVariant {
  id: number;
  sku: string;
  sizeName: string;
  widthCm: number;
  depthCm: number;
  heightCm: number;
  color: string | null;
  priceOverride: number | null;
}

export interface Product {
  id: number;
  name: string;
  slug: string;
  sku: string;
  shortDescription: string | null;
  description: string | null;
  categoryId: number;
  categoryName: string | null;
  categorySlug: string | null;
  brandId: number;
  brandName: string | null;
  price: number;
  salePrice: number | null;
  warrantyMonths: number;
  isActive: boolean;
  isFeatured: boolean;
  viewCount: number;
  attributes: Record<string, string>;
  imageUrls: string[];
  variants: ProductVariant[];
  displayPrice: number;
  discountPercent: number;
  model3dUrl?: string | null;
}

export interface Category {
  id: number;
  name: string;
  slug: string;
}

const BASE = process.env.NEXT_PUBLIC_API_URL || "";

async function fetchApi<T>(path: string, init?: RequestInit): Promise<T> {
  const res = await fetch(`${BASE}${path}`, {
    headers: { "Content-Type": "application/json", ...init?.headers },
    ...init,
  });
  const body = (await res.json()) as ApiResponse<T>;
  if (!res.ok || !body.success) {
    throw new Error(body.message || `API lỗi ${res.status}`);
  }
  return body.data;
}

// Server-side fetch (gọi thẳng backend, dùng trong Server Components)
export async function fetchFromServer<T>(path: string): Promise<T | null> {
  try {
    const apiUrl = process.env.API_URL ?? "http://localhost:5080";
    const res = await fetch(`${apiUrl}${path}`, { next: { revalidate: 60 } });
    if (!res.ok) return null;
    const body = (await res.json()) as ApiResponse<T>;
    return body.success ? body.data : null;
  } catch {
    return null; // Backend chưa chạy → render trang rỗng thay vì crash
  }
}

export const api = {
  searchProducts: (params: Record<string, string | number | undefined>) => {
    const qs = new URLSearchParams();
    Object.entries(params).forEach(([k, v]) => {
      if (v !== undefined && v !== "") qs.set(k, String(v));
    });
    return fetchApi<PagedResult<Product>>(`/api/v1/products?${qs}`);
  },
  getProduct: (slug: string) => fetchApi<Product>(`/api/v1/products/${slug}`),
  getCategories: () => fetchApi<Category[]>("/api/v1/categories"),
  getQuizRecommendation: (data: QuizRequest) => fetchApi<QuizRecommendationResult>("/api/v1/products/quiz/recommend", {
    method: "POST",
    body: JSON.stringify(data),
  }),
};

export interface QuizRequest {
  roomType: string;
  roomAreaM2?: number;
  style?: string;
  minBudget?: number;
  maxBudget?: number;
}

export interface QuizRecommendationResult {
  roomType: string;
  style: string | null;
  roomAreaM2: number | null;
  summary: string;
  totalEstimatedPrice: number;
  recommendedProducts: Product[];
  recommendedCombos: Array<{
    id: number;
    name: string;
    slug: string;
    roomTypeLabel: string;
    description: string | null;
    saleTotal: number;
    savings: number;
  }>;
}

// ===== Promotion: Flash Sale =====
export interface FlashSaleItemDto {
  id: number;
  productId: number;
  productName: string;
  productSlug: string | null;
  productPrice: number | null;
  imageUrl: string | null;
  salePrice: number;
  quantityLimit: number;
  quantitySold: number;
  isSoldOut: boolean;
}

export interface FlashSaleDto {
  id: number;
  name: string;
  startAt: string;
  endAt: string;
  items: FlashSaleItemDto[];
}

// ===== Promotion: Voucher =====
export interface VoucherResult {
  isValid: boolean;
  discountAmount: number;
  message: string;
}

// ===== Shipping: carrier quote =====
export interface ShippingFeeResult {
  carrier: string;
  fee: number;
  estimatedDays: string;
  success: boolean;
  message: string;
  rawResponse?: string | null;
}

export const promotionApi = {
  activeFlashSales: () => fetchApi<FlashSaleDto[]>("/api/v1/flash-sales/active"),
  validateVoucher: (code: string, orderAmount: number) =>
    fetchApi<VoucherResult>(
      `/api/v1/vouchers/validate?code=${encodeURIComponent(code)}&orderAmount=${orderAmount}`,
    ),
  carrierQuote: (carrier: "Ghn" | "Ghtk", params: Record<string, string | number>) => {
    const qs = new URLSearchParams();
    Object.entries(params).forEach(([k, v]) => qs.set(k, String(v)));
    return fetchApi<ShippingFeeResult>(`/api/v1/shipping/quotes/carriers/${carrier}/quote?${qs}`);
  },
};

// ===== Loyalty: tích điểm / đổi quà =====
export interface CustomerDto {
  id: string;
  fullName: string;
  phone: string;
  email: string | null;
}

export interface LoyaltyDto {
  customerId: string;
  points: number;
  tier: string;
  lifetimeSpend: number;
}

export interface RewardDto {
  id: number;
  name: string;
  description: string | null;
  pointsCost: number;
  value: number;
}

export interface LoyaltyTransactionDto {
  id: string;
  pointsDelta: number;
  type: string;
  reference: string;
  note: string | null;
  createdAt: string;
}

async function fetchApiWithAuth<T>(path: string, token: string, init?: RequestInit): Promise<T> {
  const res = await fetch(`${BASE}${path}`, {
    headers: { "Content-Type": "application/json", Authorization: `Bearer ${token}`, ...init?.headers },
    ...init,
  });
  const body = (await res.json()) as ApiResponse<T>;
  if (!res.ok || !body.success) {
    throw new Error(body.message || `API lỗi ${res.status}`);
  }
  return body.data;
}

export const loyaltyApi = {
  me: (token: string) => fetchApiWithAuth<CustomerDto>("/api/v1/customers/me", token),
  get: (customerId: string) => fetchApi<LoyaltyDto>(`/api/v1/loyalty/${customerId}`),
  rewards: () => fetchApi<RewardDto[]>("/api/v1/loyalty/rewards"),
  transactions: (customerId: string) =>
    fetchApi<LoyaltyTransactionDto[]>(`/api/v1/loyalty/${customerId}/transactions`),
  redeem: (customerId: string, rewardId: number) =>
    fetchApi<LoyaltyDto>("/api/v1/loyalty/redeem-reward", {
      method: "POST",
      body: JSON.stringify({ customerId, rewardId }),
    }),
};

// ===== Cms: banner / nội dung =====
export interface BannerDto {
  id: number;
  title: string;
  imageUrl: string;
  linkUrl: string | null;
  position: string;
  sortOrder: number;
}

export const cmsApi = {
  activeBanners: (position: string = "home-hero") =>
    fetchApi<BannerDto[]>(`/api/v1/banners?position=${position}`),
};

// ===== Review: đánh giá sản phẩm =====
export interface ReviewDto {
  id: string;
  productId: number;
  customerName: string;
  rating: number;
  content: string;
  verifiedPurchase: boolean;
  status: string;
}

export interface ProductRatingDto {
  productId: number;
  averageRating: number;
  totalCount: number;
  ratings: { star: number; count: number }[];
}

export interface PagedReviewResult {
  items: ReviewDto[];
}

export const reviewApi = {
  getByProduct: (productId: number, page = 1) =>
    fetchApi<PagedReviewResult>(`/api/v1/reviews/product/${productId}?page=${page}`),
  rating: (productId: number) =>
    fetchApi<ProductRatingDto>(`/api/v1/reviews/product/${productId}/rating`),
  submit: (input: {
    productId: number;
    customerName: string;
    customerPhone: string;
    rating: number;
    content: string;
  }) =>
    fetchApi<ReviewDto>("/api/v1/reviews", {
      method: "POST",
      body: JSON.stringify(input),
    }),
};
