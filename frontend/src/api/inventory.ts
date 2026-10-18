import { api } from './client';

export interface InventoryItem {
  id: number;
  name: string;
  category: string;
  code: string;
  unit: string;
  stockQuantity: number;
  minStock: number;
  unitPrice: number;
  supplier: string;
  lowStock: boolean;
  isFractionable: boolean;
  isActive: boolean;
}

export interface ProductLot {
  id: number;
  inventoryItemId: number;
  lotNumber: string;
  expirationDate: string | null;
  availableQuantity: number;
  isExpiringSoon: boolean;
  isActive: boolean;
}

export type StockMovementType =
  | 'EntradaCompra'
  | 'SalidaVenta'
  | 'SalidaUsoInterno'
  | 'Ajuste';

export interface KardexEntry {
  id: number;
  inventoryItemId: number;
  itemName: string;
  itemCode: string;
  type: StockMovementType;
  quantity: number;
  description: string;
  movementDate: string;
  lotNumber: string | null;
  performedByName: string | null;
}

export interface KardexFilters {
  itemId?: number;
  type?: StockMovementType;
  from?: string;
  to?: string;
}

export interface ExpiringLot {
  lotId: number;
  inventoryItemId: number;
  itemName: string;
  itemCode: string;
  lotNumber: string;
  expirationDate: string | null;
  availableQuantity: number;
  isExpired: boolean;
}

export interface InventoryFilters {
  search?: string;
  category?: string;
  lowOnly?: boolean;
  includeInactive?: boolean;
}

export interface InventoryItemInput {
  name: string;
  category: string;
  code: string;
  unit: string;
  minStock: number;
  unitPrice: number;
  supplier: string;
  isFractionable: boolean;
}

export interface AdjustStockInput {
  quantity: number;
  description: string;
}

export interface AddLotInput {
  lotNumber: string;
  expirationDate: string | null;
  quantity: number;
  description: string;
}

export const MOVEMENT_LABEL: Record<StockMovementType, string> = {
  EntradaCompra: 'Entrada por compra',
  SalidaVenta: 'Venta',
  SalidaUsoInterno: 'Uso interno',
  Ajuste: 'Ajuste',
};

function toQuery(filters: InventoryFilters): string {
  const params = new URLSearchParams();
  if (filters.search) params.set('search', filters.search);
  if (filters.category) params.set('category', filters.category);
  if (filters.lowOnly) params.set('lowOnly', 'true');
  if (filters.includeInactive) params.set('includeInactive', 'true');
  const qs = params.toString();
  return qs ? `?${qs}` : '';
}

export const listItems = (filters: InventoryFilters = {}) =>
  api<InventoryItem[]>(`/inventory${toQuery(filters)}`);

export const getItem = (id: number) =>
  api<InventoryItem>(`/inventory/${id}`);

export const createItem = (input: InventoryItemInput) =>
  api<InventoryItem>('/inventory', { method: 'POST', body: JSON.stringify(input) });

export const updateItem = (id: number, input: InventoryItemInput) =>
  api<InventoryItem>(`/inventory/${id}`, { method: 'PUT', body: JSON.stringify(input) });

export const setItemStatus = (id: number, isActive: boolean) =>
  api<InventoryItem>(`/inventory/${id}/status`, { method: 'PATCH', body: JSON.stringify({ isActive }) });

export const adjustStock = (id: number, input: AdjustStockInput) =>
  api<InventoryItem>(`/inventory/${id}/stock`, { method: 'PATCH', body: JSON.stringify(input) });

export const listLots = (id: number, includeInactive = false) =>
  api<ProductLot[]>(`/inventory/${id}/lots${includeInactive ? '?includeInactive=true' : ''}`);

export const addLot = (id: number, input: AddLotInput) =>
  api<ProductLot>(`/inventory/${id}/lots`, { method: 'POST', body: JSON.stringify(input) });

export const deactivateLot = (lotId: number) =>
  api<void>(`/inventory/lots/${lotId}`, { method: 'DELETE' });

export const reactivateLot = (lotId: number) =>
  api<void>(`/inventory/lots/${lotId}/activate`, { method: 'PATCH' });

export const listKardex = (filters: KardexFilters = {}) => {
  const params = new URLSearchParams();
  if (filters.itemId) params.set('itemId', String(filters.itemId));
  if (filters.type) params.set('type', filters.type);
  if (filters.from) params.set('from', filters.from);
  if (filters.to) params.set('to', filters.to);
  const qs = params.toString();
  return api<KardexEntry[]>(`/inventory/movements${qs ? `?${qs}` : ''}`);
};

export const listExpiringLots = (days = 30) =>
  api<ExpiringLot[]>(`/inventory/lots/expiring?days=${days}`);
