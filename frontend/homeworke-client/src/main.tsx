import React from 'react';
import ReactDOM from 'react-dom/client';
import { SnackbarProvider } from 'notistack';
import { ThemeProvider, createTheme } from '@mui/material/styles';
import CssBaseline from '@mui/material/CssBaseline';
import App from './App';
import TopLoadingBar from './components/Common/TopLoadingBar';
import NotistackErrorListener from './components/Common/NotistackErrorListener';
import { ThemeProvider as AppThemeProvider, useTheme } from './context/ThemeContext';
import './index.css';

const MuiThemeWrapper: React.FC<{ children: React.ReactNode }> = ({ children }) => {
  const { theme: mode } = useTheme();
  const muiTheme = React.useMemo(
    () =>
      createTheme({
        palette: {
          mode,
          primary: { main: '#3b82f6' },
        },
      }),
    [mode],
  );
  return (
    <ThemeProvider theme={muiTheme}>
      <CssBaseline />
      {children}
    </ThemeProvider>
  );
};

const AppWithProviders: React.FC = () => (
  <>
    <TopLoadingBar />
    <NotistackErrorListener />
    <App />
  </>
);

ReactDOM.createRoot(document.getElementById('root')!).render(
  <React.StrictMode>
    <AppThemeProvider>
      <MuiThemeWrapper>
        <SnackbarProvider
          maxSnack={3}
          anchorOrigin={{ vertical: 'top', horizontal: 'center' }}
        >
          <AppWithProviders />
        </SnackbarProvider>
      </MuiThemeWrapper>
    </AppThemeProvider>
  </React.StrictMode>
);
