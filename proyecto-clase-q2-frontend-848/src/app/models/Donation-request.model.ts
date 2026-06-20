export interface DonationRequest {
  id: string;
  postId: string;
  receiverId: string;
  receiverName: string;
  status: 'pendiente' | 'aceptada' | 'rechazada' | 'cancelada';
  requestTimestamp: string;
}
