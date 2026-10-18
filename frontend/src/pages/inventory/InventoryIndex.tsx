import { NavLink, Outlet } from 'react-router-dom';

export default function InventoryIndex() {
  const base = '/inventario';
  return (
    <div className="space-y-4">
      <div className="flex flex-wrap gap-2">
        <NavLink end to={base} className={({ isActive }) => `btn btn-sm ${isActive ? "btn-primary" : ""}`}>
          Artículos
        </NavLink>
        <NavLink to={`${base}/lotes`} className={({ isActive }) => `btn btn-sm ${isActive ? "btn-primary" : ""}`}>
          Lotes
        </NavLink>
<NavLink to={`${base}/ajustes`} className={({ isActive }) => `btn btn-sm ${isActive ? "btn-primary" : ""}`}>
          Ajustes
        </NavLink>
        <NavLink to={`${base}/kardex`} className={({ isActive }) => `btn btn-sm ${isActive ? "btn-primary" : ""}`}>
          Kardex
        </NavLink>
      </div>
      <Outlet />
    </div>
  );
}





















