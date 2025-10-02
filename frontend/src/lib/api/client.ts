import createClient from 'openapi-fetch';
import type { paths } from './schema';
import { API_BASE_URL } from '@/lib/config';

export const apiClient = createClient<paths>({
  baseUrl: API_BASE_URL,
  headers: {
    'Content-Type': 'application/json',
  },
  // Send cookies with every request for HttpOnly cookie authentication
  credentials: 'include',
});
