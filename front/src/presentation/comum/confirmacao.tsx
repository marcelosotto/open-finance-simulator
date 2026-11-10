import * as Dialog from '@radix-ui/react-dialog';
import { useTranslation } from 'react-i18next';
import { useState, type ReactNode } from 'react';
import { Botao } from './botao';

interface PropsDaConfirmacao {
  readonly gatilho: string;
  readonly titulo: string;
  readonly children: ReactNode;
  readonly aoConfirmar: () => void;
  readonly perigo?: boolean;
  readonly desabilitado?: boolean;
}

/** Ação irreversível sempre pede confirmação (Radix Dialog, acessível). */
export const Confirmacao = ({
  gatilho,
  titulo,
  children,
  aoConfirmar,
  perigo = true,
  desabilitado,
}: PropsDaConfirmacao) => {
  const { t } = useTranslation();
  const [aberto, setAberto] = useState(false);
  return (
    <Dialog.Root open={aberto} onOpenChange={setAberto}>
      <Dialog.Trigger asChild>
        <Botao variante={perigo ? 'perigo' : 'secundario'} disabled={desabilitado}>
          {gatilho}
        </Botao>
      </Dialog.Trigger>
      <Dialog.Portal>
        <Dialog.Overlay className="fixed inset-0 bg-black/30" />
        <Dialog.Content className="fixed left-1/2 top-1/3 w-[28rem] max-w-[90vw] -translate-x-1/2 rounded bg-white p-4 shadow-lg">
          <Dialog.Title className="text-lg font-semibold">{titulo}</Dialog.Title>
          <Dialog.Description asChild>
            <div className="mt-2 text-sm text-slate-700">{children}</div>
          </Dialog.Description>
          <div className="mt-4 flex justify-end gap-2">
            <Dialog.Close asChild>
              <Botao variante="secundario">{t('comum.cancelar')}</Botao>
            </Dialog.Close>
            <Botao
              variante={perigo ? 'perigo' : 'primario'}
              onClick={() => {
                setAberto(false);
                aoConfirmar();
              }}
            >
              {t('comum.confirmar')}
            </Botao>
          </div>
        </Dialog.Content>
      </Dialog.Portal>
    </Dialog.Root>
  );
};
