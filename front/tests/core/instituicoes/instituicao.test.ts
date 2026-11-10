import { avisoDoCampo, novaInstituicao, relogioFixado, temAnomalia } from '@/core/instituicoes/instituicao';
import { escreverPesos, lerPesos } from '@/core/instituicoes/pesos';

describe('Instituição', () => {
  it('tem aviso só com anomalia ligada (RN114)', () => {
    expect(temAnomalia({ anomaliasLigadas: [] })).toBe(false);
    expect(temAnomalia({ anomaliasLigadas: ['revogado-com-403'] })).toBe(true);
  });

  it('relógio com fator 0 está fixado', () => {
    expect(relogioFixado({ baseSimulada: 'a', baseReal: 'b', fator: 0 })).toBe(true);
    expect(relogioFixado({ baseSimulada: 'a', baseReal: 'b', fator: 60 })).toBe(false);
  });

  it('acha o aviso do campo (UI05)', () => {
    const avisos = [{ campo: 'capacidadeTps', oficial: 300, configurado: 100 }];
    expect(avisoDoCampo({ avisos }, 'capacidadeTps')).toEqual(avisos[0]);
    expect(avisoDoCampo({ avisos }, 'qca')).toBeUndefined();
  });

  it('nova instituição exige nome e perfil (UI03, RN03)', () => {
    expect(novaInstituicao('  Banco Azul ', 'lento').value).toEqual({ nome: 'Banco Azul', perfil: 'lento' });
    expect(novaInstituicao('   ', 'lento').value).toEqual({ tipo: 'validacao', erros: ['NOME_OBRIGATORIO'] });
    expect(novaInstituicao('Banco', ' ').value).toEqual({ tipo: 'validacao', erros: ['PERFIL_OBRIGATORIO'] });
  });
});

describe('Pesos por status', () => {
  it('lê e escreve', () => {
    const lido = lerPesos('500:1, 504 : 2.5,');
    expect(lido.isRight()).toBe(true);
    expect(lido.value).toEqual({ '500': 1, '504': 2.5 });
    expect(escreverPesos({ '500': 1, '504': 2.5 })).toBe('500:1, 504:2.5');
    expect(lerPesos('').value).toEqual({});
  });

  it('recusa formato errado', () => {
    expect(lerPesos('500=1').isLeft()).toBe(true);
    expect(lerPesos('50:1').value).toEqual({ tipo: 'validacao', erros: ['PESOS_INVALIDOS'] });
  });
});
