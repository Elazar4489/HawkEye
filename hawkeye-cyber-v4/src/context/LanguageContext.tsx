import React, { createContext, useContext, useState, useEffect } from 'react';
import { translations } from '../i18n/translations';

type Language = 'he' | 'en';

interface LanguageContextType {
  lang: Language;
  dir: 'rtl' | 'ltr';
  setLang: (lang: Language) => void;
  toggleLanguage: () => void;
  t: (key: keyof typeof translations['he']) => string;
}

const LanguageContext = createContext<LanguageContextType | undefined>(undefined);

export const LanguageProvider: React.FC<{ children: React.ReactNode }> = ({ children }) => {
  const [lang, setLangState] = useState<Language>('he');

  const setLang = (newLang: Language) => {
    setLangState(newLang);
    document.documentElement.lang = newLang;
    document.documentElement.dir = newLang === 'he' ? 'rtl' : 'ltr';
  };

  const toggleLanguage = () => {
    setLang(lang === 'he' ? 'en' : 'he');
  };

  useEffect(() => {
    document.documentElement.lang = lang;
    document.documentElement.dir = 'rtl';
  }, []);

  const t = (key: keyof typeof translations['he']): string => {
    return translations[lang][key] || key;
  };

  return (
    <LanguageContext.Provider value={{ lang, dir: lang === 'he' ? 'rtl' : 'ltr', setLang, toggleLanguage, t }}>
      {children}
    </LanguageContext.Provider>
  );
};

export const useLanguage = () => {
  const context = useContext(LanguageContext);
  if (!context) throw new Error('useLanguage must be used within LanguageProvider');
  return context;
};
