export interface DashboardStats {
  activePosts: number;
  completedDonations: number;
  pendingRequests: number;
  totalPosts: number;
  completionRate: number;
  byStatus: { [key: string]: number };
  byCategory: { [key: string]: number };
}

export interface TrendItem {
  date?: string;
  month?: string;
  count: number;
}
