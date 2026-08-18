export interface Category {
  id: number;
  name: string;
  slug: string;
  isActive: boolean;
}

export interface Product {
  id: number;
  name: string;
  slug: string;
  categoryName: string | null;
  brandName: string | null;
  price: number;
  salePrice: number | null;
  displayPrice: number;
  imageUrls: string[];
  shortDescription: string | null;
  isActive: boolean;
}

export interface FlashSaleItem {
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

export interface FlashSale {
  id: number;
  name: string;
  startAt: string;
  endAt: string;
  items: FlashSaleItem[];
}

export interface Banner {
  id: number;
  title: string;
  imageUrl: string;
  linkUrl: string | null;
  position: string;
  sortOrder: number;
}

export interface CartItem {
  productId: number;
  name: string;
  slug: string;
  price: number;
  imageUrl: string | null;
  quantity: number;
}

export interface OtpSession {
  accessToken: string;
  phone: string;
  customerId: string;
  isNewCustomer: boolean;
}

export interface CustomerProfile {
  id: string;
  name: string;
  phone: string;
  email: string | null;
}

export interface LoyaltyAccount {
  customerId: string;
  totalPoints: number;
  availablePoints: number;
  tier: string;
  lifetimeSpent: number;
}

export interface Reward {
  id: number;
  name: string;
  description: string | null;
  pointsCost: number;
  isActive: boolean;
}

export interface Review {
  id: string;
  productId: number;
  customerName: string;
  rating: number;
  content: string;
  verifiedPurchase: boolean;
  status: string;
}

export interface ProductRating {
  productId: number;
  averageRating: number;
  totalCount: number;
  ratings: { star: number; count: number }[];
}
