import i18next from 'i18next';
import { initReactI18next } from 'react-i18next';
import { comum } from './pt-BR';
import { textosDasInstituicoes } from '../instituicoes/textos';
import { textosDosConsentimentos } from '../consentimentos/textos';
import { textosDaMassa } from '../massa/textos';
import { textosDosCenarios } from '../cenarios/textos';
import { textosDaAjuda } from '../ajuda/textos';
import { textosDoInicio } from '../inicio/textos';

export const recursos = {
  ...comum,
  instituicoes: textosDasInstituicoes,
  consentimentos: textosDosConsentimentos,
  massa: textosDaMassa,
  cenarios: textosDosCenarios,
  ajuda: textosDaAjuda,
  inicio: textosDoInicio,
};

/** Só pt-BR, como pede o guia. Síncrono, para a primeira renderização já ter os textos. */
export const iniciarIdioma = () => {
  if (!i18next.isInitialized) {
    void i18next.use(initReactI18next).init({
      lng: 'pt-BR',
      fallbackLng: 'pt-BR',
      resources: { 'pt-BR': { translation: recursos } },
      interpolation: { escapeValue: false },
      initImmediate: false,
      showSupportNotice: false,
    });
  }
  return i18next;
};
