import { useEffect } from 'react';
import { useSnackbar } from 'notistack';

const NotistackErrorListener: React.FC = () => {
  const { enqueueSnackbar } = useSnackbar();

  useEffect(() => {
    const handler = (e: Event) => {
      const { message } = (e as CustomEvent).detail;
      enqueueSnackbar(message, {
        variant: 'error',
        anchorOrigin: { vertical: 'top', horizontal: 'center' },
        autoHideDuration: 5000,
        preventDuplicate: true,
      });
    };

    window.addEventListener('notistack:error', handler);
    return () => window.removeEventListener('notistack:error', handler);
  }, [enqueueSnackbar]);

  return null; // invisible — just listens
};

export default NotistackErrorListener;
