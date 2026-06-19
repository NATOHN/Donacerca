export interface DonationPost {
  id: string;
  donorId: string;
  donorName: string;
  categoryId: string;
  itemName: string;
  description: string;
  itemCondition: string;
  zone: string;
  photoUrls: string[];
  status: 'disponible' | 'reservado' | 'entregado' | 'vencido';
  selectedReceiverId?: string;
  isActive: boolean;
  createdAt: string;
}

export interface CreatePostRequest {
  categoryId: string;
  itemName: string;
  description: string;
  itemCondition: string;
  zone: string;
  photoUrls: string[];
}
