import { createContext, useContext, useEffect, useState, type ReactNode } from 'react';
import { getPublicBranding } from '../api/settings';

interface BrandingValue {
  name: string;
  logoDataUrl: string | null;
}

const DEFAULT: BrandingValue = { name: 'VetClinic', logoDataUrl: null };

const BrandingContext = createContext<BrandingValue>(DEFAULT);

export function BrandingProvider({ children }: { children: ReactNode }) {
  const [branding, setBranding] = useState<BrandingValue>(DEFAULT);

  useEffect(() => {
    getPublicBranding()
      .then((b) => setBranding({ name: b.name?.trim() || DEFAULT.name, logoDataUrl: b.logoDataUrl }))
      .catch(() => {});
  }, []);

  return <BrandingContext.Provider value={branding}>{children}</BrandingContext.Provider>;
}

export function useBranding() {
  return useContext(BrandingContext);
}
