import React, { createContext, useContext, useState } from 'react';

type MapTheme = 'dark' | 'light';

interface ThemeContextType {
  mapTheme: MapTheme;
  toggleMapTheme: () => void;
}

const ThemeContext = createContext<ThemeContextType | undefined>(undefined);

export const ThemeProvider: React.FC<{ children: React.ReactNode }> = ({ children }) => {
  const [mapTheme, setMapTheme] = useState<MapTheme>('dark');

  const toggleMapTheme = () => {
    setMapTheme(prev => (prev === 'dark' ? 'light' : 'dark'));
  };

  return (
    <ThemeContext.Provider value={{ mapTheme, toggleMapTheme }}>
      {children}
    </ThemeContext.Provider>
  );
};

export const useTheme = () => {
  const context = useContext(ThemeContext);
  if (!context) throw new Error('useTheme must be used within ThemeProvider');
  return context;
};
