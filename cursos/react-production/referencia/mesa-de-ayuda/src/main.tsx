import { StrictMode } from 'react';
import { createRoot } from 'react-dom/client';
import { QueryClientProvider } from '@tanstack/react-query';
import { RouterProvider } from 'react-router';
import './shared/i18n';
import { crearQueryClient } from './app/query-client';
import { crearRouter } from './app/router';

const cliente = crearQueryClient();
createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <QueryClientProvider client={cliente}>
      <RouterProvider router={crearRouter()} />
    </QueryClientProvider>
  </StrictMode>,
);
