import React from 'react';
import ReactDOM from 'react-dom/client';
import { SnackbarProvider } from 'notistack';
import { ThemeProvider, createTheme } from '@mui/material/styles';
import CssBaseline from '@mui/material/CssBaseline';
import App from './App';
import TopLoadingBar from './components/Common/TopLoadingBar';
import NotistackErrorListener from './components/Common/NotistackErrorListener';
import './index.css';

const theme = createTheme({
  palette: {
    primary: { main: '#3b82f6' },
  },
});

const AppWithProviders: React.FC = () => (
  <>
    <TopLoadingBar />
    <NotistackErrorListener />
    <App />
  </>
);

ReactDOM.createRoot(document.getElementById('root')!).render(
  <React.StrictMode>
    <ThemeProvider theme={theme}>
      <CssBaseline />
      <SnackbarProvider
        maxSnack={3}
        anchorOrigin={{ vertical: 'top', horizontal: 'center' }}
      >
        <AppWithProviders />
      </SnackbarProvider>
    </ThemeProvider>
  </React.StrictMode>
);
