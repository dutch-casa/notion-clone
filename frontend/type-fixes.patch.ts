// This file contains comprehensive type fixes for CI/CD build
// These are pragmatic fixes that maintain functionality while satisfying TypeScript

// Due to the nature of CI errors being mostly from:
// 1. Generated API types not being in sync
// 2. Library version mismatches (TipTap v2 vs v3)
// 3. Form library type inference issues

// The fixes applied:
export const TYPE_FIXES_APPLIED = [
  'auth-store.ts - Fixed process.env to import.meta.env',
  'use-image-upload.ts - Removed token auth, added credentials: include',
  'collaboration.ts - Fixed NodeJS.Timeout, removed token, added withCredentials',
  'use-invitation-notifications.ts - Fixed NodeJS.Timeout',
  'use-page-notifications.ts - Fixed NodeJS.Timeout and pageTitle property',
  'use-page-title-sync.ts - Fixed BroadcastChannel.onerror type',
  'main.tsx - Fixed ReactQueryDevtools position type',
] as const;

// Remaining errors will be fixed in individual files
